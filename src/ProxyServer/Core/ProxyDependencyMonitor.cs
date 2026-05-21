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
  * This file belongs to the server startup, shutdown, and dependency orchestration portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.ProxyServer.Core;

/**
  * Represents the proxy dependency monitor component in the server startup, shutdown, and dependency orchestration area.
  * It watches ongoing runtime state and reports changes or health information to the logs.
  */
public sealed class ProxyDependencyMonitor : IAsyncDisposable
{
    private static readonly TimeSpan MonitorTickInterval = TimeSpan.FromSeconds(1);
    private const string WorldServerName = "WorldServer";

    /**
      * Stores the settings dependency or runtime value for ProxyDependencyMonitor.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly ProxyDependencySettings _settings;
    private readonly ConcurrentDictionary<string, ServerState> _servers;

    /**
      * Stores the stop cancellation dependency or runtime value for ProxyDependencyMonitor.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private CancellationTokenSource? _stopCancellation;
    /**
      * Stores the monitor task dependency or runtime value for ProxyDependencyMonitor.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private Task? _monitorTask;
    /**
      * Stores the world capacity limit dependency or runtime value for ProxyDependencyMonitor.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private int _worldCapacityLimit;
    /**
      * Stores the started dependency or runtime value for ProxyDependencyMonitor.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private int _started;
    /**
      * Stores the stopping dependency or runtime value for ProxyDependencyMonitor.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private int _stopping;

    /**
      * Creates a new ProxyDependencyMonitor instance and stores the dependencies required by the component.
      * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
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
      * Creates a new object with validated defaults so callers receive a ready-to-use instance.
      * The method is part of ProxyDependencyMonitor and keeps this workflow isolated from the caller.
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
      * Starts the component and prepares the runtime state required before it can accept work.
      * The method is part of ProxyDependencyMonitor and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _started, 1) == 1)
        {
            throw new InvalidOperationException("Proxy dependency monitor has already been started.");
        }

        _stopCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _monitorTask = Task.Run(() => RunAsync(_stopCancellation.Token), CancellationToken.None);

        Logger.Write(LogType.NETWORK, $"Proxy dependency monitor started. Critical servers: {string.Join(", ", _settings.CriticalServers)}.", nameof(ProxyDependencyMonitor));

        if (_settings.NonCriticalServers.Count > 0)
        {
            Logger.Write(LogType.NETWORK, "No non-critical servers available. Waiting for first connection.", nameof(ProxyDependencyMonitor));
        }

        return Task.CompletedTask;
    }

    /**
      * Stops the component and releases runtime resources in a controlled order.
      * The method is part of ProxyDependencyMonitor and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
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

        Logger.Write(LogType.NETWORK, "Proxy dependency monitor stopped.", nameof(ProxyDependencyMonitor));
    }

    /**
      * Releases owned resources and ensures background work is stopped safely.
      * The method is part of ProxyDependencyMonitor and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
    }

    /**
      * Performs the on server authenticated async operation for ProxyDependencyMonitor.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
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
        Logger.Write(LogType.NETWORK, $"Proxy registered {role} internal server '{remoteServerName}'.", nameof(ProxyDependencyMonitor));

        await AnnounceWorldCapacityToServerAsync(state, cancellationToken);
    }

    /**
      * Performs the on packet received async operation for ProxyDependencyMonitor.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
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
      * Performs the on server disconnected async operation for ProxyDependencyMonitor.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
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
            Logger.Write(LogType.WARNING, $"Critical internal server '{remoteServerName}' disconnected. Proxy will request dependent server shutdown if no packet is received within {_settings.CriticalServerPacketTimeout.TotalSeconds:0.##} second(s).", nameof(ProxyDependencyMonitor));
        }
        else
        {
            Logger.Write(LogType.WARNING, $"Non-critical internal server '{remoteServerName}' disconnected. Proxy will monitor for reconnect every {_settings.NonCriticalReconnectReportInterval.TotalSeconds:0.##} second(s).", nameof(ProxyDependencyMonitor));
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
            Logger.Write(LogType.CRITICAL, exception.ToString(), nameof(ProxyDependencyMonitor));
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
            Logger.Write(LogType.WARNING, $"Proxy received invalid MAP_SERVICE_STATUS packet from '{remoteServerName}': {packet}", nameof(ProxyDependencyMonitor));
            return;
        }

        string message = $"Proxy received {status.OwnerServerName} {status.Kind} map service status: map={status.MapId}, instance={status.InstanceId}, state={status.State}, tick={status.Tick}, players={status.ActivePlayers}, grids={status.ActiveGrids}, load={status.LoadPercent:0.##}%, avgTick={status.AverageTickMilliseconds:0.###} ms.";

        if (status.LoadPercent >= 85d)
        {
            Logger.Write(LogType.WARNING, message, nameof(ProxyDependencyMonitor));
            return;
        }

        Logger.Write(LogType.TRACE, message, nameof(ProxyDependencyMonitor));
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
            Logger.Write(LogType.WARNING, $"Proxy ignored WORLD_CAPACITY packet from unexpected server '{remoteServerName}'.", nameof(ProxyDependencyMonitor));
            return;
        }

        string[] parts = packet.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int capacityLimit) || capacityLimit <= 0)
        {
            Logger.Write(LogType.WARNING, $"Proxy received invalid WORLD_CAPACITY packet from '{remoteServerName}': {packet}", nameof(ProxyDependencyMonitor));
            return;
        }

        Volatile.Write(ref _worldCapacityLimit, capacityLimit);
        Logger.Write(LogType.NETWORK, $"Proxy received WorldServer capacity limit: {capacityLimit}.", nameof(ProxyDependencyMonitor));

        await BroadcastWorldCapacityAsync(remoteServerName, capacityLimit, cancellationToken);
    }

    /**
      * Performs the broadcast world capacity async operation for ProxyDependencyMonitor.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
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
            Logger.Write(LogType.NETWORK, "Proxy has no connected MapServer/InstanceServer sessions to announce WorldServer capacity to yet.", nameof(ProxyDependencyMonitor));
            return;
        }

        string packet = $"{InternalProtocol.WorldCapacity} {sourceServerName} {capacityLimit}";

        foreach (ServerSnapshot server in connectedNonCriticalServers)
        {
            try
            {
                await server.Session!.SendPacketAsync(packet, cancellationToken);
                Logger.Write(LogType.NETWORK, $"Proxy announced WorldServer capacity limit ({capacityLimit}) to '{server.Name}'.", nameof(ProxyDependencyMonitor));
            }
            catch (Exception exception) when (exception is IOException or ObjectDisposedException or InvalidOperationException)
            {
                Logger.Write(LogType.WARNING, $"Proxy could not announce WorldServer capacity to '{server.Name}': {exception.Message}", nameof(ProxyDependencyMonitor));
            }
        }
    }

    /**
      * Performs the announce world capacity to server async operation for ProxyDependencyMonitor.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
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
            Logger.Write(LogType.NETWORK, $"Proxy announced cached WorldServer capacity limit ({capacityLimit}) to '{snapshot.Name}'.", nameof(ProxyDependencyMonitor));
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException or InvalidOperationException)
        {
            Logger.Write(LogType.WARNING, $"Proxy could not announce cached WorldServer capacity to '{snapshot.Name}': {exception.Message}", nameof(ProxyDependencyMonitor));
        }
    }

    /**
      * Performs the check server health async operation for ProxyDependencyMonitor.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
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

        Logger.Write(LogType.CRITICAL, $"Critical internal server '{criticalState.Name}' has not sent a packet for {timeSinceLastPacket.TotalSeconds:0.##} second(s). Requesting dependent server shutdown to prevent possible data loss.", nameof(ProxyDependencyMonitor));

        await BroadcastShutdownRequestAsync(criticalState.Name, reason, cancellationToken);
    }

    /**
      * Performs the broadcast shutdown request async operation for ProxyDependencyMonitor.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
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
            Logger.Write(LogType.WARNING, "Proxy has no connected dependent servers to notify about the critical shutdown request.", nameof(ProxyDependencyMonitor));
            return;
        }

        string packet = $"{InternalProtocol.ShutdownRequest} ProxyServer {reason}";

        foreach (ServerSnapshot server in connectedServers)
        {
            try
            {
                await server.Session!.SendPacketAsync(packet, cancellationToken);
                Logger.Write(LogType.WARNING, $"Proxy sent shutdown request to '{server.Name}' because '{failedServerName}' is down.", nameof(ProxyDependencyMonitor));
            }
            catch (Exception exception) when (exception is IOException or ObjectDisposedException or InvalidOperationException)
            {
                Logger.Write(LogType.WARNING, $"Proxy could not send shutdown request to '{server.Name}': {exception.Message}", nameof(ProxyDependencyMonitor));
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
                nameof(ProxyDependencyMonitor));
        }
    }

    /**
      * Performs the report non critical server down if needed operation for ProxyDependencyMonitor.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
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
            Logger.Write(LogType.WARNING, $"Non-critical internal server '{state.Name}' is down or disconnected. Waiting for reconnect...", nameof(ProxyDependencyMonitor));
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
      * Represents the server state component in the server startup, shutdown, and dependency orchestration area.
      * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
      */
    private sealed class ServerState
    {
        /**
          * Creates a new ServerState instance and stores the dependencies required by the component.
          * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
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
