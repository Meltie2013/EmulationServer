//
// Copyright (C) 2026 Emulation Server Project
//
// This program is free software. You can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation. either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY. Without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//

using System.Collections.Concurrent;
using System.Globalization;

using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Network.Networking.Sessions;
using EmulationServer.ProxyServer.Configuration;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

/**
  * File overview: src/ProxyServer/Core/ProxyDependencyMonitor.cs
  * Documents the ProxyDependencyMonitor source file in the proxy startup, service discovery, and client-routing support area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.ProxyServer.Core;

/**
  * Owns the proxy dependency monitor behavior for the proxy startup, service discovery, and client-routing support layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class ProxyDependencyMonitor : IAsyncDisposable
{
    /**
      * Stores the default monitor tick interval value used when the caller does not supply an override.
      * Centralizing the default keeps configuration and packet behavior consistent across the server process.
      */
    private static readonly TimeSpan MonitorTickInterval = TimeSpan.FromSeconds(1);
    /**
      * Defines the constant value for world server name.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const string WorldServerName = "WorldServer";

    /**
      * Holds the private settings state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly ProxyDependencySettings _settings;
    private readonly ConcurrentDictionary<string, ServerState> _servers;

    /**
      * Holds the private stop cancellation state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private CancellationTokenSource? _stopCancellation;
    /**
      * Holds the private monitor task state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private Task? _monitorTask;
    /**
      * Holds the private world capacity limit state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private int _worldCapacityLimit;
    /**
      * Holds the private started state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private int _started;
    /**
      * Holds the private stopping state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private int _stopping;

    /**
      * Initializes a new ProxyDependencyMonitor instance with the dependencies required by the proxy startup, service discovery, and client-routing support workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: settings.
      */
    public ProxyDependencyMonitor(ProxyDependencySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _settings = settings;
        _servers = new ConcurrentDictionary<string, ServerState>(StringComparer.OrdinalIgnoreCase);

        foreach (string serverName in settings.CriticalServers)
        {
            _servers.TryAdd(serverName, new ServerState(serverName, isCritical: true));
        }

        foreach (string serverName in settings.NonCriticalServers)
        {
            _servers.TryAdd(serverName, new ServerState(serverName, isCritical: false));
        }
    }

    /**
      * Creates the callbacks result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      */
    public InternalNetworkCallbacks CreateCallbacks()
    {
        return new InternalNetworkCallbacks
        {
            ServerAuthenticatedAsync = OnServerAuthenticatedAsync,
            PacketReceivedAsync = OnPacketReceivedAsync,
            ServerDisconnectedAsync = OnServerDisconnectedAsync,
            PeerReconnectTimedOutAsync = OnPeerReconnectTimedOutAsync,
        };
    }

    /**
      * Starts the start workflow and prepares the component to accept runtime work.
      * Startup is ordered so validation and dependency setup finish before services are announced as available.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _started, 1) == 1)
        {
            throw new InvalidOperationException("Proxy dependency monitor has already been started.");
        }

        _stopCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _monitorTask = Task.Run(() => RunAsync(_stopCancellation.Token), CancellationToken.None);

        Logger.Write(LogType.NETWORK, $"Proxy dependency monitor started. Critical servers: {string.Join(", ", _settings.CriticalServers)}.", "ProxyDependencyMonitor");

        if (_settings.NonCriticalServers.Count > 0)
        {
            Logger.Write(LogType.NETWORK, "No non-critical servers available. Waiting for first connection.", "ProxyDependencyMonitor");
        }

        return Task.CompletedTask;
    }

    /**
      * Stops the stop workflow and releases owned runtime resources in a controlled order.
      * Shutdown logic is centralized to avoid dangling connections, incomplete saves, or partially registered services.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _stopping, 1) == 1)
        {
            return;
        }

        CancellationTokenSource? stopCancellation = _stopCancellation;
        if (stopCancellation is not null)
        {
            await stopCancellation.CancelAsync();
        }

        if (_monitorTask is not null)
        {
            Task completedTask = await Task.WhenAny(_monitorTask, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken));
            if (completedTask == _monitorTask)
            {
                await _monitorTask;
            }
        }

        stopCancellation?.Dispose();
        _stopCancellation = null;

        Logger.Write(LogType.NETWORK, "Proxy dependency monitor stopped.", "ProxyDependencyMonitor");
    }

    /**
      * Stops the dispose workflow and releases owned runtime resources in a controlled order.
      * Shutdown logic is centralized to avoid dangling connections, incomplete saves, or partially registered services.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
    }

    /**
      * Handles the on server authenticated event for the proxy startup, service discovery, and client-routing support workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: session, remoteServerName, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task OnServerAuthenticatedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        ServerState state = GetOrCreateServerState(remoteServerName);

        lock (state.SyncRoot)
        {
            state.Session = session;
            state.LastPacketReceivedUtc = DateTimeOffset.UtcNow;
            state.IsConnected = true;
            state.ShutdownTriggered = false;
            state.LastDownReportUtc = null;
            state.DisconnectedUtc = null;
            state.ReconnectTimedOut = false;
            state.HasEverConnected = true;
        }

        string role = state.IsCritical ? "critical" : "non-critical";
        Logger.Write(LogType.NETWORK, $"Proxy registered {role} internal server '{remoteServerName}'.", "ProxyDependencyMonitor");

        await AnnounceWorldCapacityToServerAsync(state, cancellationToken);
    }

    /**
      * Handles the on packet received event for the proxy startup, service discovery, and client-routing support workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: session, remoteServerName, packet, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task OnPacketReceivedAsync(
        InternalServerSession session,
        string remoteServerName,
        string packet,
        CancellationToken cancellationToken)
    {
        ServerState state = GetOrCreateServerState(remoteServerName);

        lock (state.SyncRoot)
        {
            state.LastPacketReceivedUtc = DateTimeOffset.UtcNow;
            state.Session = session;
            state.IsConnected = true;
            state.DisconnectedUtc = null;
            state.ReconnectTimedOut = false;
        }

        if (packet.StartsWith(InternalProtocol.WorldCapacity, StringComparison.OrdinalIgnoreCase))
        {
            await HandleWorldCapacityPacketAsync(remoteServerName, packet, cancellationToken);
            return;
        }

        if (packet.StartsWith(InternalProtocol.MapServiceStatus, StringComparison.OrdinalIgnoreCase))
        {
            HandleMapServiceStatusPacket(remoteServerName, packet);
        }
    }

    /**
      * Handles the on server disconnected event for the proxy startup, service discovery, and client-routing support workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: session, remoteServerName, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private Task OnServerDisconnectedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        ServerState state = GetOrCreateServerState(remoteServerName);

        lock (state.SyncRoot)
        {
            if (ReferenceEquals(state.Session, session))
            {
                state.Session = null;
            }

            state.IsConnected = false;
            state.DisconnectedUtc = DateTimeOffset.UtcNow;
            state.ReconnectTimedOut = false;
        }

        if (state.IsCritical)
        {
            Logger.Write(LogType.WARNING, $"Critical internal server '{remoteServerName}' disconnected. Proxy will request dependent server shutdown if no packet is received within {_settings.CriticalServerPacketTimeout.TotalSeconds:0.##} second(s).", "ProxyDependencyMonitor");
        }
        else
        {
            Logger.Write(LogType.WARNING, $"Non-critical internal server '{remoteServerName}' disconnected. Proxy will monitor for reconnect every {_settings.NonCriticalReconnectReportInterval.TotalSeconds:0.##} second(s).", "ProxyDependencyMonitor");
        }

        return Task.CompletedTask;
    }

    /**
      * Handles outbound peer reconnect timeout notifications raised by the shared internal peer connector.
      * This keeps the dependency monitor aligned with the connector so it stops reporting reconnect attempts after the configured window expires.
      */
    private Task OnPeerReconnectTimedOutAsync(
        string remoteServerName,
        TimeSpan reconnectTimeout,
        CancellationToken cancellationToken)
    {
        ServerState state = GetOrCreateServerState(remoteServerName);
        MarkNonCriticalReconnectTimedOut(state, reconnectTimeout);

        return Task.CompletedTask;
    }

    /**
      * Runs the main loop for this component until cancellation or shutdown is requested.
      * The method is part of ProxyDependencyMonitor and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(MonitorTickInterval, cancellationToken);
                await CheckServerHealthAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown.
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.CRITICAL, exception.ToString(), "ProxyDependencyMonitor");
        }
    }

    /**
      * Handles a single operation or packet and keeps the calling code focused on flow control.
      * The method is part of ProxyDependencyMonitor and keeps this workflow isolated from the caller.
      */
    private void HandleMapServiceStatusPacket(string remoteServerName, string packet)
    {
        if (!InternalMapServiceStatusPacket.TryParse(packet, out InternalMapServiceStatusPacket status))
        {
            Logger.Write(LogType.WARNING, $"Proxy received invalid MAP_SERVICE_STATUS packet from '{remoteServerName}': {packet}", "ProxyDependencyMonitor");
            return;
        }

        string message = $"Proxy received {status.OwnerServerName} {status.Kind} map service status: map={status.MapId}, instance={status.InstanceId}, state={status.State}, tick={status.Tick}, players={status.ActivePlayers}, grids={status.ActiveGrids}, load={status.LoadPercent:0.##}%, avgTick={status.AverageTickMilliseconds:0.###} ms.";

        if (status.LoadPercent >= 85d)
        {
            Logger.Write(LogType.WARNING, message, "ProxyDependencyMonitor");
            return;
        }

        Logger.Write(LogType.TRACE, message, "ProxyDependencyMonitor");
    }

    /**
      * Handles a single operation or packet and keeps the calling code focused on flow control.
      * The method is part of ProxyDependencyMonitor and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    private async Task HandleWorldCapacityPacketAsync(
        string remoteServerName,
        string packet,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(remoteServerName, WorldServerName, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.WARNING, $"Proxy ignored WORLD_CAPACITY packet from unexpected server '{remoteServerName}'.", "ProxyDependencyMonitor");
            return;
        }

        string[] parts = packet.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int capacityLimit) || capacityLimit <= 0)
        {
            Logger.Write(LogType.WARNING, $"Proxy received invalid WORLD_CAPACITY packet from '{remoteServerName}': {packet}", "ProxyDependencyMonitor");
            return;
        }

        Volatile.Write(ref _worldCapacityLimit, capacityLimit);
        Logger.Write(LogType.NETWORK, $"Proxy received WorldServer capacity limit: {capacityLimit}.", "ProxyDependencyMonitor");

        await BroadcastWorldCapacityAsync(remoteServerName, capacityLimit, cancellationToken);
    }

    /**
      * Performs the broadcast world capacity operation for the proxy startup, service discovery, and client-routing support workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: sourceServerName, capacityLimit, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task BroadcastWorldCapacityAsync(
        string sourceServerName,
        int capacityLimit,
        CancellationToken cancellationToken)
    {
        List<ServerSnapshot> connectedNonCriticalServers = _servers.Values
            .Select(server => server.GetSnapshot())
            .Where(server => !server.IsCritical && server.IsConnected && server.Session is not null)
            .ToList();

        if (connectedNonCriticalServers.Count == 0)
        {
            Logger.Write(LogType.NETWORK, "Proxy has no connected MapServer/InstanceServer sessions to announce WorldServer capacity to yet.", "ProxyDependencyMonitor");
            return;
        }

        string packet = $"{InternalProtocol.WorldCapacity} {sourceServerName} {capacityLimit}";

        foreach (ServerSnapshot server in connectedNonCriticalServers)
        {
            try
            {
                await server.Session!.SendPacketAsync(packet, cancellationToken);
                Logger.Write(LogType.NETWORK, $"Proxy announced WorldServer capacity limit ({capacityLimit}) to '{server.Name}'.", "ProxyDependencyMonitor");
            }
            catch (Exception exception) when (exception is IOException or ObjectDisposedException or InvalidOperationException)
            {
                Logger.Write(LogType.WARNING, $"Proxy could not announce WorldServer capacity to '{server.Name}': {exception.Message}", "ProxyDependencyMonitor");
            }
        }
    }

    /**
      * Performs the announce world capacity to server operation for the proxy startup, service discovery, and client-routing support workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: state, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task AnnounceWorldCapacityToServerAsync(ServerState state, CancellationToken cancellationToken)
    {
        ServerSnapshot snapshot = state.GetSnapshot();
        int capacityLimit = Volatile.Read(ref _worldCapacityLimit);

        if (snapshot.IsCritical || capacityLimit <= 0 || snapshot.Session is null || !snapshot.IsConnected)
        {
            return;
        }

        string packet = $"{InternalProtocol.WorldCapacity} {WorldServerName} {capacityLimit}";

        try
        {
            await snapshot.Session.SendPacketAsync(packet, cancellationToken);
            Logger.Write(LogType.NETWORK, $"Proxy announced cached WorldServer capacity limit ({capacityLimit}) to '{snapshot.Name}'.", "ProxyDependencyMonitor");
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException or InvalidOperationException)
        {
            Logger.Write(LogType.WARNING, $"Proxy could not announce cached WorldServer capacity to '{snapshot.Name}': {exception.Message}", "ProxyDependencyMonitor");
        }
    }

    /**
      * Performs the check server health operation for the proxy startup, service discovery, and client-routing support workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task CheckServerHealthAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        foreach (ServerState state in _servers.Values)
        {
            ServerSnapshot snapshot = state.GetSnapshot();
            TimeSpan timeSinceLastPacket = now - snapshot.LastPacketReceivedUtc;

            if (snapshot.IsCritical)
            {
                if (snapshot.HasEverConnected && timeSinceLastPacket > _settings.CriticalServerPacketTimeout && !snapshot.ShutdownTriggered)
                {
                    await HandleCriticalServerDownAsync(state, timeSinceLastPacket, cancellationToken);
                }

                continue;
            }

            if (!snapshot.IsConnected)
            {
                if (!snapshot.HasEverConnected || snapshot.ReconnectTimedOut)
                {
                    continue;
                }

                DateTimeOffset downStartedUtc = snapshot.DisconnectedUtc ?? snapshot.LastPacketReceivedUtc;
                if (now - downStartedUtc >= _settings.NonCriticalReconnectTimeout)
                {
                    MarkNonCriticalReconnectTimedOut(state, _settings.NonCriticalReconnectTimeout);
                    continue;
                }

                ReportNonCriticalServerDownIfNeeded(state, now);
            }
        }
    }

    /**
      * Handles a single operation or packet and keeps the calling code focused on flow control.
      * The method is part of ProxyDependencyMonitor and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    private async Task HandleCriticalServerDownAsync(
        ServerState criticalState,
        TimeSpan timeSinceLastPacket,
        CancellationToken cancellationToken)
    {
        bool shouldShutdown;

        lock (criticalState.SyncRoot)
        {
            shouldShutdown = !criticalState.ShutdownTriggered;
            criticalState.ShutdownTriggered = true;
        }

        if (!shouldShutdown)
        {
            return;
        }

        string reason = $"CriticalServerDown:{criticalState.Name}";

        Logger.Write(LogType.CRITICAL, $"Critical internal server '{criticalState.Name}' has not sent a packet for {timeSinceLastPacket.TotalSeconds:0.##} second(s). Requesting dependent server shutdown to prevent possible data loss.", "ProxyDependencyMonitor");

        await BroadcastShutdownRequestAsync(criticalState.Name, reason, cancellationToken);
    }

    /**
      * Performs the broadcast shutdown request operation for the proxy startup, service discovery, and client-routing support workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: failedServerName, reason, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task BroadcastShutdownRequestAsync(
        string failedServerName,
        string reason,
        CancellationToken cancellationToken)
    {
        List<ServerSnapshot> connectedServers = _servers.Values
            .Select(server => server.GetSnapshot())
            .Where(server => server.IsConnected && server.Session is not null)
            .Where(server => !string.Equals(server.Name, failedServerName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (connectedServers.Count == 0)
        {
            Logger.Write(LogType.WARNING, "Proxy has no connected dependent servers to notify about the critical shutdown request.", "ProxyDependencyMonitor");
            return;
        }

        string packet = $"{InternalProtocol.ShutdownRequest} ProxyServer {reason}";

        foreach (ServerSnapshot server in connectedServers)
        {
            try
            {
                await server.Session!.SendPacketAsync(packet, cancellationToken);
                Logger.Write(LogType.WARNING, $"Proxy sent shutdown request to '{server.Name}' because '{failedServerName}' is down.", "ProxyDependencyMonitor");
            }
            catch (Exception exception) when (exception is IOException or ObjectDisposedException or InvalidOperationException)
            {
                Logger.Write(LogType.WARNING, $"Proxy could not send shutdown request to '{server.Name}': {exception.Message}", "ProxyDependencyMonitor");
            }
        }
    }

    /**
      * Marks a non-critical dependency as timed out so the monitor stops repeated reconnect warnings.
      * The dependency remains known, but it returns to passive wait mode until the service registers again.
      */
    private void MarkNonCriticalReconnectTimedOut(ServerState state, TimeSpan reconnectTimeout)
    {
        if (state.IsCritical)
        {
            return;
        }

        bool shouldReport;

        lock (state.SyncRoot)
        {
            shouldReport = !state.ReconnectTimedOut;
            state.IsConnected = false;
            state.Session = null;
            state.ReconnectTimedOut = true;
            state.LastDownReportUtc = null;
        }

        if (shouldReport)
        {
            Logger.Write(
                LogType.WARNING,
                $"Non-critical internal server '{state.Name}' has been unavailable for {reconnectTimeout.TotalSeconds:0.##} second(s). Stopping reconnect monitoring and waiting for the service to register again.",
                "ProxyDependencyMonitor");
        }
    }

    /**
      * Performs the report non critical server down if needed operation for the proxy startup, service discovery, and client-routing support workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: state, now.
      */
    private void ReportNonCriticalServerDownIfNeeded(ServerState state, DateTimeOffset now)
    {
        bool shouldReport;

        lock (state.SyncRoot)
        {
            shouldReport = state.LastDownReportUtc is null ||
                now - state.LastDownReportUtc.Value >= _settings.NonCriticalReconnectReportInterval;

            if (shouldReport)
            {
                state.LastDownReportUtc = now;
            }
        }

        if (shouldReport)
        {
            Logger.Write(LogType.WARNING, $"Non-critical internal server '{state.Name}' is down or disconnected. Waiting for reconnect...", "ProxyDependencyMonitor");
        }
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of ProxyDependencyMonitor and keeps this workflow isolated from the caller.
      */
    private ServerState GetOrCreateServerState(string serverName)
    {
        bool isCritical = _settings.CriticalServers.Contains(serverName);

        return _servers.GetOrAdd(
            serverName,
            name => new ServerState(name, isCritical));
    }

    /**
      * Owns the server state behavior for the proxy startup, service discovery, and client-routing support layer.
      * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
      */
    private sealed class ServerState
    {
        /**
          * Performs the server state operation for the proxy startup, service discovery, and client-routing support workflow.
          * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
          * Inputs used by this operation: name, isCritical.
          */
        public ServerState(string name, bool isCritical)
        {
            Name = name;
            IsCritical = isCritical;
            LastPacketReceivedUtc = DateTimeOffset.UtcNow;
        }

        /**
          * Gets or stores the sync root value used by ServerState.
          * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
          */
        public object SyncRoot { get; } = new();

        /**
          * Gets or stores the name value used by ServerState.
          * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
          */
        public string Name { get; }

        /**
          * Gets or stores the is critical value used by ServerState.
          * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
          */
        public bool IsCritical { get; }

        /**
          * Gets or stores the session value used by ServerState.
          * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
          */
        public InternalServerSession? Session { get; set; }

        /**
          * Gets or stores the last packet received utc value used by ServerState.
          * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
          */
        public DateTimeOffset LastPacketReceivedUtc { get; set; }

        /**
          * Gets or stores the last down report utc value used by ServerState.
          * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
          */
        public DateTimeOffset? LastDownReportUtc { get; set; }

        /**
          * Gets or stores the disconnected timestamp for a server that has gone offline.
          * The monitor uses this value to decide when to stop active reconnect reporting and return to passive wait mode.
          */
        public DateTimeOffset? DisconnectedUtc { get; set; }

        /**
          * Gets or stores whether reconnect monitoring has timed out for this dependency.
          * Once set, the dependency is silent until it registers again through the normal internal listener.
          */
        public bool ReconnectTimedOut { get; set; }

        /**
          * Gets or stores the is connected value used by ServerState.
          * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
          */
        public bool IsConnected { get; set; }

        /**
          * Gets or stores the shutdown triggered value used by ServerState.
          * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
          */
        public bool ShutdownTriggered { get; set; }

        /**
          * Gets or stores the has ever connected value used by ServerState.
          * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
          */
        public bool HasEverConnected { get; set; }

        /**
          * Returns the current value or snapshot without exposing mutable internal state.
          * The method is part of ServerState and keeps this workflow isolated from the caller.
          */
        public ServerSnapshot GetSnapshot()
        {
            lock (SyncRoot)
            {
                return new ServerSnapshot(
                    Name,
                    IsCritical,
                    Session,
                    LastPacketReceivedUtc,
                    DisconnectedUtc,
                    IsConnected,
                    ShutdownTriggered,
                    HasEverConnected,
                    ReconnectTimedOut);
            }
        }
    }

    /**
      * Represents immutable server snapshot data passed between parts of the server.
      * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
      * Positional fields carried by this record: Name, IsCritical, Session, LastPacketReceivedUtc, DisconnectedUtc, IsConnected, ShutdownTriggered, HasEverConnected, ReconnectTimedOut.
      */
    private sealed record ServerSnapshot(
        string Name,
        bool IsCritical,
        InternalServerSession? Session,
        DateTimeOffset LastPacketReceivedUtc,
        DateTimeOffset? DisconnectedUtc,
        bool IsConnected,
        bool ShutdownTriggered,
        bool HasEverConnected,
        bool ReconnectTimedOut);
}
