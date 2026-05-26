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

    private enum HealthLevel
    {
        Unknown = 0,
        Healthy = 1,
        Degraded = 2,
        Unhealthy = 3,
    }

    /**
      * Holds the private settings state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly ProxyDependencySettings _settings;
    private readonly ConcurrentDictionary<string, ServerState> _servers;
    private readonly ConcurrentDictionary<string, InternalMapServiceStatusPacket> _mapServiceStatuses = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _mapServiceStatusReceivedUtc = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, HealthReportState> _mapServiceHealthReports = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, HealthReportState> _serverHealthReports = new(StringComparer.OrdinalIgnoreCase);

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
            LatencyMeasured = OnLatencyMeasured,
            PingTimedOut = OnPingTimedOut,
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
            state.LastLatencyMeasuredUtc = null;
            state.LastPongReceivedUtc = null;
            state.LastLatencyMilliseconds = null;
            state.AverageLatencyMilliseconds = null;
            state.LastPingTimeoutUtc = null;
            state.ConsecutivePingTimeouts = 0;
            state.TotalPingTimeouts = 0;
        }

        string role = state.IsCritical ? "critical" : "non-critical";
        Logger.Write(LogType.NETWORK, $"Proxy registered {role} internal server '{remoteServerName}'.", "ProxyDependencyMonitor");

        await AnnounceWorldCapacityToServerAsync(state, cancellationToken);
        await AnnounceCachedMapServicesToServerAsync(state, cancellationToken);
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

        if (packet.StartsWith(InternalProtocol.WorldHealthStatus, StringComparison.OrdinalIgnoreCase))
        {
            HandleWorldHealthStatusPacket(remoteServerName, packet);
            return;
        }

        if (packet.StartsWith(InternalProtocol.MapServiceStatus, StringComparison.OrdinalIgnoreCase))
        {
            await HandleMapServiceStatusPacketAsync(remoteServerName, packet, cancellationToken);
        }
    }

    /**
      * Handles the on server disconnected event for the proxy startup, service discovery, and client-routing support workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: session, remoteServerName, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task OnServerDisconnectedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        ServerState state = GetOrCreateServerState(remoteServerName);

        bool acceptedDisconnect;

        lock (state.SyncRoot)
        {
            acceptedDisconnect = ReferenceEquals(state.Session, session);

            if (acceptedDisconnect)
            {
                state.Session = null;
                state.IsConnected = false;
                state.DisconnectedUtc = DateTimeOffset.UtcNow;
                state.ReconnectTimedOut = false;
            }
        }

        if (!acceptedDisconnect)
        {
            Logger.Write(LogType.TRACE, $"Ignored stale disconnect notification for internal server '{remoteServerName}' because a newer session is already registered.", "ProxyDependencyMonitor");
            return;
        }

        if (state.IsCritical)
        {
            Logger.Write(LogType.WARNING, $"Critical internal server '{remoteServerName}' disconnected. Proxy will request dependent server shutdown if no packet is received within {_settings.CriticalServerPacketTimeout.TotalSeconds:0.##} second(s).", "ProxyDependencyMonitor");
        }
        else
        {
            Logger.Write(LogType.WARNING, $"Non-critical internal server '{remoteServerName}' disconnected. Proxy will monitor for reconnect every {_settings.NonCriticalReconnectReportInterval.TotalSeconds:0.##} second(s).", "ProxyDependencyMonitor");
        }

        if (IsMapControlServer(remoteServerName))
        {
            await MarkCachedMapServicesUnavailableAsync(remoteServerName, "internal server disconnected", cancellationToken);
        }
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
      * Records a successful ping/pong round trip for a connected internal server.
      * ProxyServer owns the health state; the shared latency monitor only reports the measurement.
      */
    private void OnLatencyMeasured(string remoteServerName, TimeSpan latency)
    {
        ServerState state = GetOrCreateServerState(remoteServerName);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        lock (state.SyncRoot)
        {
            state.LastLatencyMeasuredUtc = now;
            state.LastPongReceivedUtc = now;
            state.LastLatencyMilliseconds = latency.TotalMilliseconds;
            state.AverageLatencyMilliseconds = state.AverageLatencyMilliseconds is null
                ? latency.TotalMilliseconds
                : (state.AverageLatencyMilliseconds.Value * 0.8d) + (latency.TotalMilliseconds * 0.2d);
            state.ConsecutivePingTimeouts = 0;
        }
    }

    /**
      * Records a missed ping/pong response for a connected internal server.
      * Ping health is measured by consecutive missed pongs instead of the successful latency threshold.
      */
    private void OnPingTimedOut(string remoteServerName, TimeSpan elapsed)
    {
        ServerState state = GetOrCreateServerState(remoteServerName);

        lock (state.SyncRoot)
        {
            state.LastPingTimeoutUtc = DateTimeOffset.UtcNow;
            state.ConsecutivePingTimeouts++;
            state.TotalPingTimeouts++;
        }
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
    private async Task HandleMapServiceStatusPacketAsync(string remoteServerName, string packet, CancellationToken cancellationToken)
    {
        if (!InternalMapServiceStatusPacket.TryParse(packet, out InternalMapServiceStatusPacket status))
        {
            Logger.Write(LogType.WARNING, $"Proxy received invalid MAP_SERVICE_STATUS packet from '{remoteServerName}': {packet}", "ProxyDependencyMonitor");
            return;
        }

        string key = GetStatusKey(status);
        _mapServiceStatuses.TryGetValue(key, out InternalMapServiceStatusPacket? previous);
        _mapServiceStatuses[key] = status;
        _mapServiceStatusReceivedUtc[key] = DateTimeOffset.UtcNow;

        bool isOnline = IsMapServiceOnline(status.State);
        bool previousIsOnline = previous is not null && IsMapServiceOnline(previous.State);
        bool firstSnapshot = previous is null;
        bool stateChanged = previous is not null && !string.Equals(previous.State, status.State, StringComparison.OrdinalIgnoreCase);
        bool playerCountChanged = previous is not null && previous.ActivePlayers != status.ActivePlayers;
        bool becameUnavailable = previousIsOnline && !isOnline;
        bool loadWarning = isOnline && status.LoadPercent >= 85d;
        bool loadWarningStarted = loadWarning && (previous is null || previous.LoadPercent < 85d);

        // Map services can briefly report Offline while the MapServer has connected but its services
        // are still moving through startup. Cache that snapshot, but do not warn or forward it as a
        // failure unless this is a real transition from a routable Online service.
        if (becameUnavailable)
        {
            Logger.Write(LogType.WARNING, $"Proxy cached offline map service state for {status.OwnerServerName}: kind={status.Kind}, map={status.MapId}, instance={status.InstanceId}, players={status.ActivePlayers}.", "ProxyDependencyMonitor");
        }
        else if (loadWarningStarted)
        {
            Logger.Write(LogType.WARNING, $"Proxy cached high-load map service state for {status.OwnerServerName}: kind={status.Kind}, map={status.MapId}, instance={status.InstanceId}, load={status.LoadPercent:0.##}%, avgTick={status.AverageTickMilliseconds:0.###} ms.", "ProxyDependencyMonitor");
        }

        bool shouldForwardSnapshot =
            (isOnline && (firstSnapshot || stateChanged || playerCountChanged || loadWarningStarted)) ||
            becameUnavailable;

        if (shouldForwardSnapshot)
        {
            await BroadcastMapServiceStatusToCriticalServersAsync(status, cancellationToken);
        }
    }

    /**
      * Marks cached map services as offline when a map or instance server socket disappears.
      */
    private async Task MarkCachedMapServicesUnavailableAsync(string ownerServerName, string reason, CancellationToken cancellationToken)
    {
        InternalMapServiceStatusPacket[] affectedStatuses = _mapServiceStatuses.Values
            .Where(status => string.Equals(status.OwnerServerName, ownerServerName, StringComparison.OrdinalIgnoreCase))
            .Where(status => !string.Equals(status.State, "Offline", StringComparison.OrdinalIgnoreCase))
            .Select(status => status with { State = "Offline" })
            .ToArray();

        if (affectedStatuses.Length == 0)
        {
            return;
        }

        foreach (InternalMapServiceStatusPacket status in affectedStatuses)
        {
            string key = GetStatusKey(status);
            _mapServiceStatuses[key] = status;
            _mapServiceStatusReceivedUtc[key] = DateTimeOffset.UtcNow;
        }

        Logger.Write(LogType.WARNING, $"Proxy marked {affectedStatuses.Length} cached map service status snapshot(s) for '{ownerServerName}' as Offline because {reason}.", "ProxyDependencyMonitor");

        foreach (InternalMapServiceStatusPacket status in affectedStatuses)
        {
            await BroadcastMapServiceStatusToCriticalServersAsync(status, cancellationToken);
        }
    }

    /**
      * Forwards map service state snapshots to critical servers so WorldServer can keep routing decisions current.
      */
    private async Task BroadcastMapServiceStatusToCriticalServersAsync(InternalMapServiceStatusPacket status, CancellationToken cancellationToken)
    {
        List<ServerSnapshot> connectedCriticalServers = _servers.Values
            .Select(server => server.GetSnapshot())
            .Where(server => server.IsCritical && server.IsConnected && server.Session is not null)
            .ToList();

        if (connectedCriticalServers.Count == 0)
        {
            return;
        }

        string packet = status.ToPacketLine();

        foreach (ServerSnapshot server in connectedCriticalServers)
        {
            try
            {
                await server.Session!.SendPacketAsync(packet, cancellationToken);

            }
            catch (Exception exception) when (exception is IOException or ObjectDisposedException or InvalidOperationException)
            {
                Logger.Write(LogType.WARNING, $"Proxy could not forward map service status to '{server.Name}': {exception.Message}", "ProxyDependencyMonitor");
            }
        }
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
      * Handles a WorldServer health snapshot and stores it only in ProxyServer memory.
      */
    private void HandleWorldHealthStatusPacket(string remoteServerName, string packet)
    {
        if (!string.Equals(remoteServerName, WorldServerName, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.WARNING, $"Proxy ignored WORLD_HEALTH_STATUS packet from unexpected server '{remoteServerName}'.", "ProxyDependencyMonitor");
            return;
        }

        if (!InternalWorldHealthStatusPacket.TryParse(packet, out InternalWorldHealthStatusPacket status))
        {
            Logger.Write(LogType.WARNING, $"Proxy received invalid WORLD_HEALTH_STATUS packet from '{remoteServerName}': {packet}", "ProxyDependencyMonitor");
            return;
        }

        if (!string.Equals(status.OwnerServerName, WorldServerName, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.WARNING, $"Proxy ignored WORLD_HEALTH_STATUS packet with unexpected owner '{status.OwnerServerName}' from '{remoteServerName}'.", "ProxyDependencyMonitor");
            return;
        }

        ServerState state = GetOrCreateServerState(WorldServerName);
        lock (state.SyncRoot)
        {
            state.WorldActivePlayers = status.ActivePlayers;
            state.WorldMaxConnections = status.MaxConnections;
            state.LastWorldHealthStatusUtc = DateTimeOffset.UtcNow;
        }

        int previousCapacity = Volatile.Read(ref _worldCapacityLimit);
        if (previousCapacity != status.MaxConnections)
        {
            Volatile.Write(ref _worldCapacityLimit, status.MaxConnections);
        }

        Logger.Write(LogType.TRACE, $"Proxy cached WorldServer health status: players={status.ActivePlayers}/{status.MaxConnections}.", "ProxyDependencyMonitor");
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
      * Sends cached map service state to a newly connected critical server so routing starts with the current proxy view.
      */
    private async Task AnnounceCachedMapServicesToServerAsync(ServerState state, CancellationToken cancellationToken)
    {
        ServerSnapshot snapshot = state.GetSnapshot();
        if (!snapshot.IsCritical || snapshot.Session is null || !snapshot.IsConnected)
        {
            return;
        }

        InternalMapServiceStatusPacket[] statuses = _mapServiceStatuses.Values
            .OrderBy(status => status.OwnerServerName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(status => status.MapId)
            .ThenBy(status => status.InstanceId)
            .ToArray();

        foreach (InternalMapServiceStatusPacket status in statuses)
        {
            try
            {
                await snapshot.Session.SendPacketAsync(status.ToPacketLine(), cancellationToken);
            }
            catch (Exception exception) when (exception is IOException or ObjectDisposedException or InvalidOperationException)
            {
                Logger.Write(LogType.WARNING, $"Proxy could not announce cached map service status to '{snapshot.Name}': {exception.Message}", "ProxyDependencyMonitor");
                return;
            }
        }

        if (statuses.Length > 0)
        {
            Logger.Write(LogType.NETWORK, $"Proxy announced {statuses.Length} cached map service status snapshot(s) to '{snapshot.Name}'.", "ProxyDependencyMonitor");
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
            DateTimeOffset lastPacketReceivedUtc = GetLatestPacketReceivedUtc(snapshot);
            TimeSpan timeSinceLastPacket = now - lastPacketReceivedUtc;

            if (snapshot.IsCritical)
            {
                TimeSpan criticalPacketTimeout = GetEffectiveCriticalPacketTimeout();

                if (snapshot.HasEverConnected && timeSinceLastPacket > criticalPacketTimeout && !snapshot.ShutdownTriggered)
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

        EvaluateAndReportHealth(now);
    }

    /**
      * Evaluates and reports health for the connected internal servers and cached map services.
      * Health state is owned by ProxyServer and only recomputed from the latest runtime inputs it has received.
      */
    private void EvaluateAndReportHealth(DateTimeOffset now)
    {
        if (!_settings.HealthLoggingEnabled)
        {
            return;
        }

        foreach (ServerState state in _servers.Values)
        {
            ServerSnapshot snapshot = state.GetSnapshot();

            if (!snapshot.HasEverConnected && !snapshot.IsConnected)
            {
                continue;
            }

            if (string.Equals(snapshot.Name, WorldServerName, StringComparison.OrdinalIgnoreCase))
            {
                ReportHealthIfNeeded(
                    _serverHealthReports,
                    $"server:{snapshot.Name}",
                    EvaluateWorldServerHealth(snapshot, now),
                    now,
                    snapshot.IsCritical);
                continue;
            }

            if (IsMapControlServer(snapshot.Name))
            {
                ReportHealthIfNeeded(
                    _serverHealthReports,
                    $"server:{snapshot.Name}",
                    EvaluateMapOwnerHealth(snapshot, now),
                    now,
                    snapshot.IsCritical);
                continue;
            }

            ReportHealthIfNeeded(
                _serverHealthReports,
                $"server:{snapshot.Name}",
                EvaluateBaseServerHealth(snapshot, now, $"Proxy health {snapshot.Name}"),
                now,
                snapshot.IsCritical);
        }

        foreach (InternalMapServiceStatusPacket status in _mapServiceStatuses.Values)
        {
            ReportHealthIfNeeded(
                _mapServiceHealthReports,
                $"map:{GetStatusKey(status)}",
                EvaluateMapServiceHealth(status, now),
                now,
                critical: false);
        }
    }

    /**
      * Evaluates WorldServer health from ping health, latency health, and player-load pressure.
      */
    private HealthEvaluation EvaluateWorldServerHealth(ServerSnapshot snapshot, DateTimeOffset now)
    {
        HealthComponent ping = EvaluatePingHealth(snapshot, now);
        HealthComponent latency = EvaluateLatencyHealth(snapshot, now);
        HealthComponent load = EvaluateWorldLoadHealth(snapshot, now);
        HealthLevel level = Worst(ping.Level, latency.Level, load.Level);

        return new HealthEvaluation(
            level,
            $"Proxy health WorldServer: {level} (ping={ping.Summary}, latency={latency.Summary}, load={load.Summary}).",
            string.Join("; ", new[] { ping.Reason, latency.Reason, load.Reason }.Where(reason => !string.IsNullOrWhiteSpace(reason))));
    }

    /**
      * Evaluates MapServer or InstanceServer overall health from server ping/latency and every owned map service snapshot.
      */
    private HealthEvaluation EvaluateMapOwnerHealth(ServerSnapshot snapshot, DateTimeOffset now)
    {
        HealthComponent ping = EvaluatePingHealth(snapshot, now);
        HealthComponent latency = EvaluateLatencyHealth(snapshot, now);

        InternalMapServiceStatusPacket[] ownedServices = _mapServiceStatuses.Values
            .Where(status => string.Equals(status.OwnerServerName, snapshot.Name, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        HealthLevel serviceLevel = HealthLevel.Healthy;
        int healthyServices = 0;
        int degradedServices = 0;
        int unhealthyServices = 0;
        double worstLoadPercent = 0d;
        double worstAverageTickMilliseconds = 0d;

        if (ownedServices.Length == 0)
        {
            serviceLevel = snapshot.IsConnected ? HealthLevel.Degraded : HealthLevel.Unhealthy;
        }

        foreach (InternalMapServiceStatusPacket serviceStatus in ownedServices)
        {
            HealthEvaluation serviceHealth = EvaluateMapServiceHealth(serviceStatus, now);
            serviceLevel = Worst(serviceLevel, serviceHealth.Level);
            worstLoadPercent = Math.Max(worstLoadPercent, serviceStatus.LoadPercent);
            worstAverageTickMilliseconds = Math.Max(worstAverageTickMilliseconds, serviceStatus.AverageTickMilliseconds);

            switch (serviceHealth.Level)
            {
                case HealthLevel.Healthy:
                    healthyServices++;
                    break;

                case HealthLevel.Degraded:
                    degradedServices++;
                    break;

                case HealthLevel.Unhealthy:
                    unhealthyServices++;
                    break;
            }
        }

        HealthLevel level = Worst(ping.Level, latency.Level, serviceLevel);
        string serviceSummary = ownedServices.Length == 0
            ? "services=none reporting"
            : $"services={healthyServices} healthy/{degradedServices} degraded/{unhealthyServices} unhealthy, worstLoad={worstLoadPercent:0.##}%, worstAvgTick={worstAverageTickMilliseconds:0.###} ms";

        return new HealthEvaluation(
            level,
            $"Proxy health {snapshot.Name} overall: {level} (ping={ping.Summary}, latency={latency.Summary}, {serviceSummary}).",
            string.Join("; ", new[] { ping.Reason, latency.Reason, ownedServices.Length == 0 ? "no map service status snapshots have been received" : string.Empty }.Where(reason => !string.IsNullOrWhiteSpace(reason))));
    }

    /**
      * Evaluates basic server health from ping and latency only.
      */
    private HealthEvaluation EvaluateBaseServerHealth(ServerSnapshot snapshot, DateTimeOffset now, string prefix)
    {
        HealthComponent ping = EvaluatePingHealth(snapshot, now);
        HealthComponent latency = EvaluateLatencyHealth(snapshot, now);
        HealthLevel level = Worst(ping.Level, latency.Level);

        return new HealthEvaluation(
            level,
            $"{prefix}: {level} (ping={ping.Summary}, latency={latency.Summary}).",
            string.Join("; ", new[] { ping.Reason, latency.Reason }.Where(reason => !string.IsNullOrWhiteSpace(reason))));
    }

    /**
      * Evaluates ping health using missed pong counts rather than successful latency values.
      */
    private HealthComponent EvaluatePingHealth(ServerSnapshot snapshot, DateTimeOffset now)
    {
        if (!snapshot.IsConnected)
        {
            string duration = snapshot.DisconnectedUtc is null
                ? "unknown duration"
                : FormatDuration(now - snapshot.DisconnectedUtc.Value);

            return new HealthComponent(HealthLevel.Unhealthy, $"Unhealthy disconnected for {duration}", "server is disconnected");
        }

        if (snapshot.ConsecutivePingTimeouts >= _settings.UnhealthyPingMissCount)
        {
            return new HealthComponent(
                HealthLevel.Unhealthy,
                $"Unhealthy missed={snapshot.ConsecutivePingTimeouts}",
                $"missed {snapshot.ConsecutivePingTimeouts} consecutive pong response(s)");
        }

        if (snapshot.ConsecutivePingTimeouts >= _settings.DegradedPingMissCount)
        {
            return new HealthComponent(
                HealthLevel.Degraded,
                $"Degraded missed={snapshot.ConsecutivePingTimeouts}",
                $"missed {snapshot.ConsecutivePingTimeouts} consecutive pong response(s)");
        }

        if (snapshot.LastPongReceivedUtc is null)
        {
            return new HealthComponent(HealthLevel.Degraded, "Degraded waiting for first pong", "no successful pong has been recorded yet");
        }

        TimeSpan lastPongAge = now - snapshot.LastPongReceivedUtc.Value;
        if (lastPongAge > _settings.HealthStatusStaleTimeout)
        {
            return new HealthComponent(
                HealthLevel.Degraded,
                $"Degraded lastPong={FormatDuration(lastPongAge)} ago",
                "last successful pong is stale");
        }

        return new HealthComponent(HealthLevel.Healthy, $"Healthy lastPong={FormatDuration(lastPongAge)} ago", string.Empty);
    }

    /**
      * Evaluates latency health using the smoothed latency value from successful ping/pong round trips.
      */
    private HealthComponent EvaluateLatencyHealth(ServerSnapshot snapshot, DateTimeOffset now)
    {
        if (!snapshot.IsConnected)
        {
            return new HealthComponent(HealthLevel.Unhealthy, "Unhealthy disconnected", "latency cannot be measured while disconnected");
        }

        if (snapshot.AverageLatencyMilliseconds is null || snapshot.LastLatencyMeasuredUtc is null)
        {
            return new HealthComponent(HealthLevel.Degraded, "Degraded waiting for measurement", "no successful latency measurement has been recorded yet");
        }

        TimeSpan measurementAge = now - snapshot.LastLatencyMeasuredUtc.Value;
        if (measurementAge > _settings.HealthStatusStaleTimeout)
        {
            return new HealthComponent(
                HealthLevel.Degraded,
                $"Degraded stale={FormatDuration(measurementAge)} avg={snapshot.AverageLatencyMilliseconds.Value:0.##} ms",
                "latency measurement is stale");
        }

        double averageLatency = snapshot.AverageLatencyMilliseconds.Value;
        if (averageLatency >= _settings.UnhealthyLatencyThreshold.TotalMilliseconds)
        {
            return new HealthComponent(
                HealthLevel.Unhealthy,
                $"Unhealthy avg={averageLatency:0.##} ms",
                "latency exceeds unhealthy threshold");
        }

        if (averageLatency >= _settings.DegradedLatencyThreshold.TotalMilliseconds)
        {
            return new HealthComponent(
                HealthLevel.Degraded,
                $"Degraded avg={averageLatency:0.##} ms",
                "latency exceeds degraded threshold");
        }

        return new HealthComponent(HealthLevel.Healthy, $"Healthy avg={averageLatency:0.##} ms", string.Empty);
    }

    /**
      * Evaluates WorldServer load pressure from active in-world players and max connection capacity.
      */
    private HealthComponent EvaluateWorldLoadHealth(ServerSnapshot snapshot, DateTimeOffset now)
    {
        if (!snapshot.IsConnected)
        {
            return new HealthComponent(HealthLevel.Unhealthy, "Unhealthy disconnected", "WorldServer load cannot be measured while disconnected");
        }

        if (snapshot.LastWorldHealthStatusUtc is null || snapshot.WorldMaxConnections <= 0)
        {
            return new HealthComponent(HealthLevel.Degraded, "Degraded waiting for status", "no WorldServer health status snapshot has been received yet");
        }

        TimeSpan statusAge = now - snapshot.LastWorldHealthStatusUtc.Value;
        if (statusAge > _settings.HealthStatusStaleTimeout)
        {
            return new HealthComponent(
                HealthLevel.Degraded,
                $"Degraded stale={FormatDuration(statusAge)} players={snapshot.WorldActivePlayers}/{snapshot.WorldMaxConnections}",
                "WorldServer health status snapshot is stale");
        }

        double loadPercent = CalculatePercent(snapshot.WorldActivePlayers, snapshot.WorldMaxConnections);
        if (loadPercent >= _settings.UnhealthyLoadPercent)
        {
            return new HealthComponent(
                HealthLevel.Unhealthy,
                $"Unhealthy players={snapshot.WorldActivePlayers}/{snapshot.WorldMaxConnections} ({loadPercent:0.##}%)",
                "WorldServer player load exceeds unhealthy threshold");
        }

        if (loadPercent >= _settings.DegradedLoadPercent)
        {
            return new HealthComponent(
                HealthLevel.Degraded,
                $"Degraded players={snapshot.WorldActivePlayers}/{snapshot.WorldMaxConnections} ({loadPercent:0.##}%)",
                "WorldServer player load exceeds degraded threshold");
        }

        return new HealthComponent(
            HealthLevel.Healthy,
            $"Healthy players={snapshot.WorldActivePlayers}/{snapshot.WorldMaxConnections} ({loadPercent:0.##}%)",
            string.Empty);
    }

    /**
      * Evaluates one map or instance service health from service state, stale status, tick pressure, and reported load pressure.
      */
    private HealthEvaluation EvaluateMapServiceHealth(InternalMapServiceStatusPacket status, DateTimeOffset now)
    {
        List<string> reasons = [];
        HealthLevel level = HealthLevel.Healthy;

        if (!IsMapServiceOnline(status.State))
        {
            level = Worst(level, HealthLevel.Unhealthy);
            reasons.Add($"state={status.State}");
        }

        string statusKey = GetStatusKey(status);
        if (!_mapServiceStatusReceivedUtc.TryGetValue(statusKey, out DateTimeOffset statusReceivedUtc))
        {
            level = Worst(level, HealthLevel.Degraded);
            reasons.Add("status receive timestamp is missing");
        }
        else
        {
            TimeSpan statusAge = now - statusReceivedUtc;
            if (statusAge > _settings.HealthStatusStaleTimeout)
            {
                level = Worst(level, HealthLevel.Degraded);
                reasons.Add($"status stale for {FormatDuration(statusAge)}");
            }
        }

        if (status.LoadPercent >= _settings.UnhealthyLoadPercent)
        {
            level = Worst(level, HealthLevel.Unhealthy);
            reasons.Add($"load={status.LoadPercent:0.##}% exceeds unhealthy threshold");
        }
        else if (status.LoadPercent >= _settings.DegradedLoadPercent)
        {
            level = Worst(level, HealthLevel.Degraded);
            reasons.Add($"load={status.LoadPercent:0.##}% exceeds degraded threshold");
        }

        if (status.AverageTickMilliseconds >= _settings.UnhealthyAverageTickThreshold.TotalMilliseconds)
        {
            level = Worst(level, HealthLevel.Unhealthy);
            reasons.Add($"avgTick={status.AverageTickMilliseconds:0.###} ms exceeds unhealthy threshold");
        }
        else if (status.AverageTickMilliseconds >= _settings.DegradedAverageTickThreshold.TotalMilliseconds)
        {
            level = Worst(level, HealthLevel.Degraded);
            reasons.Add($"avgTick={status.AverageTickMilliseconds:0.###} ms exceeds degraded threshold");
        }

        string reasonText = reasons.Count == 0 ? string.Empty : string.Join("; ", reasons);
        string reasonSuffix = reasons.Count == 0 ? string.Empty : $" reason={reasonText}";

        return new HealthEvaluation(
            level,
            $"Proxy health {status.OwnerServerName} map service: {level} kind={status.Kind}, map={status.MapId}, instance={status.InstanceId}, state={status.State}, players={status.ActivePlayers}, load={status.LoadPercent:0.##}%, avgTick={status.AverageTickMilliseconds:0.###} ms.{reasonSuffix}",
            reasonText);
    }

    /**
      * Reports a health evaluation when the state changes or the periodic health report interval expires.
      */
    private void ReportHealthIfNeeded(
        ConcurrentDictionary<string, HealthReportState> reports,
        string key,
        HealthEvaluation evaluation,
        DateTimeOffset now,
        bool critical)
    {
        if (evaluation.Level == HealthLevel.Unknown)
        {
            return;
        }

        HealthReportState reportState = reports.GetOrAdd(key, _ => new HealthReportState());
        bool shouldReport;

        lock (reportState.SyncRoot)
        {
            shouldReport = reportState.LastLevel != evaluation.Level ||
                reportState.LastReportUtc is null ||
                now - reportState.LastReportUtc.Value >= _settings.HealthReportInterval;

            if (shouldReport)
            {
                reportState.LastLevel = evaluation.Level;
                reportState.LastSummary = evaluation.Summary;
                reportState.LastReportUtc = now;
            }
        }

        if (!shouldReport)
        {
            return;
        }

        LogType logType = evaluation.Level switch
        {
            HealthLevel.Healthy => LogType.SYSTEM,
            HealthLevel.Degraded => LogType.WARNING,
            HealthLevel.Unhealthy => LogType.WARNING,
            _ => LogType.DEBUG,
        };

        Logger.Write(logType, evaluation.Summary, "ProxyHealth");
    }

    /**
      * Returns the most severe health level from the supplied values.
      */
    private static HealthLevel Worst(params HealthLevel[] levels)
    {
        HealthLevel worst = HealthLevel.Unknown;

        foreach (HealthLevel level in levels)
        {
            if ((int)level > (int)worst)
            {
                worst = level;
            }
        }

        return worst;
    }

    /**
      * Formats a short, stable duration for health output.
      */
    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 1d)
        {
            return "<1s";
        }

        if (duration.TotalMinutes < 1d)
        {
            return $"{duration.TotalSeconds:0.#}s";
        }

        if (duration.TotalHours < 1d)
        {
            return $"{duration.TotalMinutes:0.#}m";
        }

        return $"{duration.TotalHours:0.#}h";
    }

    /**
      * Calculates a safe percentage while avoiding divide-by-zero pressure during startup.
      */
    private static double CalculatePercent(int value, int maximum)
    {
        if (maximum <= 0)
        {
            return 100d;
        }

        return Math.Clamp(value / (double)maximum * 100d, 0d, 100d);
    }

    /**
      * Returns the newest packet timestamp known for a server.
      * ServerState is updated by normal routed packets, while InternalServerSession is updated by every line, including ping/pong health packets.
      * Using both prevents ProxyServer from treating a quiet but healthy WorldServer as stale during normal startup or idle runtime.
      */
    private static DateTimeOffset GetLatestPacketReceivedUtc(ServerSnapshot snapshot)
    {
        DateTimeOffset latest = snapshot.LastPacketReceivedUtc;

        if (snapshot.Session is not null && snapshot.Session.LastPacketReceivedUtc > latest)
        {
            latest = snapshot.Session.LastPacketReceivedUtc;
        }

        return latest;
    }

    /**
      * Keeps the critical-server watchdog from racing the internal ping interval.
      * The configured timeout still controls the policy, but a small minimum avoids false shutdowns when health packets arrive near the monitor tick boundary.
      */
    private TimeSpan GetEffectiveCriticalPacketTimeout()
    {
        TimeSpan minimumSafeTimeout = TimeSpan.FromSeconds(45);
        return _settings.CriticalServerPacketTimeout >= minimumSafeTimeout
            ? _settings.CriticalServerPacketTimeout
            : minimumSafeTimeout;
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
      * Returns whether the named dependency owns map or instance execution.
      */
    private static bool IsMapControlServer(string remoteServerName)
    {
        return string.Equals(remoteServerName, "MapServer", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(remoteServerName, "InstanceServer", StringComparison.OrdinalIgnoreCase);
    }

    /**
      * Returns whether a map service status should be treated as routable.
      */
    private static bool IsMapServiceOnline(string state)
    {
        return string.Equals(state, "Online", StringComparison.OrdinalIgnoreCase);
    }

    /**
      * Builds the cache key for a map service status snapshot.
      */
    private static string GetStatusKey(InternalMapServiceStatusPacket status)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{status.OwnerServerName}|{status.Kind}|{status.MapId}|{status.InstanceId}");
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
          * Gets or stores the last successful latency measurement timestamp.
          */
        public DateTimeOffset? LastLatencyMeasuredUtc { get; set; }

        /**
          * Gets or stores the last successful pong timestamp.
          */
        public DateTimeOffset? LastPongReceivedUtc { get; set; }

        /**
          * Gets or stores the latest latency measurement in milliseconds.
          */
        public double? LastLatencyMilliseconds { get; set; }

        /**
          * Gets or stores the smoothed latency measurement in milliseconds.
          */
        public double? AverageLatencyMilliseconds { get; set; }

        /**
          * Gets or stores the last ping timeout timestamp.
          */
        public DateTimeOffset? LastPingTimeoutUtc { get; set; }

        /**
          * Gets or stores consecutive ping timeouts since the last successful pong.
          */
        public int ConsecutivePingTimeouts { get; set; }

        /**
          * Gets or stores total ping timeouts observed for this server session.
          */
        public int TotalPingTimeouts { get; set; }

        /**
          * Gets or stores the latest active player count reported by WorldServer.
          */
        public int WorldActivePlayers { get; set; }

        /**
          * Gets or stores the latest maximum connection count reported by WorldServer.
          */
        public int WorldMaxConnections { get; set; }

        /**
          * Gets or stores when ProxyServer last received a WorldServer health status snapshot.
          */
        public DateTimeOffset? LastWorldHealthStatusUtc { get; set; }

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
                    ReconnectTimedOut,
                    LastLatencyMeasuredUtc,
                    LastPongReceivedUtc,
                    LastLatencyMilliseconds,
                    AverageLatencyMilliseconds,
                    LastPingTimeoutUtc,
                    ConsecutivePingTimeouts,
                    TotalPingTimeouts,
                    WorldActivePlayers,
                    WorldMaxConnections,
                    LastWorldHealthStatusUtc);
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
        bool ReconnectTimedOut,
        DateTimeOffset? LastLatencyMeasuredUtc,
        DateTimeOffset? LastPongReceivedUtc,
        double? LastLatencyMilliseconds,
        double? AverageLatencyMilliseconds,
        DateTimeOffset? LastPingTimeoutUtc,
        int ConsecutivePingTimeouts,
        int TotalPingTimeouts,
        int WorldActivePlayers,
        int WorldMaxConnections,
        DateTimeOffset? LastWorldHealthStatusUtc);

    /**
      * Represents one component of a larger health evaluation.
      */
    private sealed record HealthComponent(HealthLevel Level, string Summary, string Reason);

    /**
      * Represents an evaluated health target and the output text to log.
      */
    private sealed record HealthEvaluation(HealthLevel Level, string Summary, string Reason);

    /**
      * Stores the last logged health state for a server or map service so unchanged state does not spam normal runtime.
      */
    private sealed class HealthReportState
    {
        public object SyncRoot { get; } = new();

        public HealthLevel LastLevel { get; set; } = HealthLevel.Unknown;

        public string LastSummary { get; set; } = string.Empty;

        public DateTimeOffset? LastReportUtc { get; set; }
    }
}
