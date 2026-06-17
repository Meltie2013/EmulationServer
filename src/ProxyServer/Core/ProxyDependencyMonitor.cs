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
// File: src/ProxyServer/Core/ProxyDependencyMonitor.cs
// Purpose: Contains proxy dependency monitor code for the proxy server gateway, internal routing, and public connection coordination.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Collections.Concurrent;
using System.Globalization;

using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Network.Networking.Sessions;
using EmulationServer.ProxyServer.Configuration;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.ProxyServer.Core;

// Type: ProxyDependencyMonitor
// Purpose: Provides proxy dependency monitor behavior for the proxy server gateway, internal routing, and public connection coordination.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class ProxyDependencyMonitor : IAsyncDisposable
{

    // Method: FromSeconds
    // Purpose: Executes the from seconds operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters: none.
    // Returns: Returns the time span monitor tick interval = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    private static readonly TimeSpan MonitorTickInterval = TimeSpan.FromSeconds(1);

    // Constant: Defines the world server name constant used by the proxy server gateway, internal routing, and public connection coordination.
    // Value: fixed world server name value used anywhere this rule or protocol value is needed.
    private const string WorldServerName = "WorldServer";

    // Type: HealthLevel
    // Purpose: Defines the allowed health level values used by the proxy server gateway, internal routing, and public connection coordination.
    // Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
    private enum HealthLevel
    {
        // Enum Value: Defines the unknown enum value.
        // Value: explicit expression 0.
        Unknown = 0,
        // Enum Value: Defines the healthy enum value.
        // Value: explicit expression 1.
        Healthy = 1,
        // Enum Value: Defines the degraded enum value.
        // Value: explicit expression 2.
        Degraded = 2,
        // Enum Value: Defines the unhealthy enum value.
        // Value: explicit expression 3.
        Unhealthy = 3,
    }

    // Field: Stores the settings state used by the proxy server gateway, internal routing, and public connection coordination.
    // Value: current settings backing value maintained by the owning type.
    private readonly ProxyDependencySettings _settings;
    // Field: Stores the string state used by the proxy server gateway, internal routing, and public connection coordination.
    // Value: current string backing value maintained by the owning type.
    private readonly ConcurrentDictionary<string, ServerState> _servers;
    private readonly ConcurrentDictionary<string, InternalMapServiceStatusPacket> _mapServiceStatuses = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _mapServiceStatusReceivedUtc = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, HealthReportState> _mapServiceHealthReports = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, HealthReportState> _serverHealthReports = new(StringComparer.OrdinalIgnoreCase);

    // Field: Stores the stop cancellation state used by the proxy server gateway, internal routing, and public connection coordination.
    // Value: current stop cancellation backing value maintained by the owning type.
    private CancellationTokenSource? _stopCancellation;

    // Field: Stores the monitor task state used by the proxy server gateway, internal routing, and public connection coordination.
    // Value: current monitor task backing value maintained by the owning type.
    private Task? _monitorTask;

    // Field: Stores the world capacity limit state used by the proxy server gateway, internal routing, and public connection coordination.
    // Value: current world capacity limit backing value maintained by the owning type.
    private int _worldCapacityLimit;

    // Field: Stores the started state used by the proxy server gateway, internal routing, and public connection coordination.
    // Value: current started backing value maintained by the owning type.
    private int _started;

    // Field: Stores the stopping state used by the proxy server gateway, internal routing, and public connection coordination.
    // Value: current stopping backing value maintained by the owning type.
    private int _stopping;

    // Constructor: ProxyDependencyMonitor
    // Purpose: Initializes a new ProxyDependencyMonitor instance with dependencies and values required by the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - settings: Settings values that control how this operation should run.
    // Returns: none.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: CreateCallbacks
    // Purpose: Applies create callbacks changes for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters: none.
    // Returns: Returns the internal network callbacks value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: StartAsync
    // Purpose: Controls the start lifecycle step for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: StopAsync
    // Purpose: Controls the stop lifecycle step for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: DisposeAsync
    // Purpose: Controls the dispose lifecycle step for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters: none.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
    }

    // Method: OnServerAuthenticatedAsync
    // Purpose: Executes the on server authenticated operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - session: Session value supplied by the caller for this operation.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: OnPacketReceivedAsync
    // Purpose: Executes the on packet received operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - session: Session value supplied by the caller for this operation.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: OnServerDisconnectedAsync
    // Purpose: Executes the on server disconnected operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - session: Session value supplied by the caller for this operation.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: OnPeerReconnectTimedOutAsync
    // Purpose: Executes the on peer reconnect timed out operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - reconnectTimeout: Reconnect timeout value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private Task OnPeerReconnectTimedOutAsync(
        string remoteServerName,
        TimeSpan reconnectTimeout,
        CancellationToken cancellationToken)
    {
        ServerState state = GetOrCreateServerState(remoteServerName);
        MarkNonCriticalReconnectTimedOut(state, reconnectTimeout);

        return Task.CompletedTask;
    }

    // Method: OnLatencyMeasured
    // Purpose: Executes the on latency measured operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - latency: Latency value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: OnPingTimedOut
    // Purpose: Executes the on ping timed out operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - elapsed: Elapsed value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: RunAsync
    // Purpose: Controls the run lifecycle step for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

        }
        catch (Exception exception)
        {
            Logger.Write(LogType.CRITICAL, exception.ToString(), "ProxyDependencyMonitor");
        }
    }

    // Method: HandleMapServiceStatusPacketAsync
    // Purpose: Handles handle map service status packet work for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: MarkCachedMapServicesUnavailableAsync
    // Purpose: Executes the mark cached map services unavailable operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // - reason: Reason value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: BroadcastMapServiceStatusToCriticalServersAsync
    // Purpose: Executes the broadcast map service status to critical servers operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - status: Status value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: HandleWorldCapacityPacketAsync
    // Purpose: Handles handle world capacity packet work for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: HandleWorldHealthStatusPacket
    // Purpose: Handles handle world health status packet work for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - packet: Packet bytes or structured payload consumed by this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: BroadcastWorldCapacityAsync
    // Purpose: Executes the broadcast world capacity operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - sourceServerName: Source server name value supplied by the caller for this operation.
    // - capacityLimit: Capacity limit value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: AnnounceCachedMapServicesToServerAsync
    // Purpose: Executes the announce cached map services to server operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - state: State value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: AnnounceWorldCapacityToServerAsync
    // Purpose: Executes the announce world capacity to server operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - state: State value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: CheckServerHealthAsync
    // Purpose: Validates or evaluates check server health rules for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: EvaluateAndReportHealth
    // Purpose: Executes the evaluate and report health operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - now: Now value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: EvaluateWorldServerHealth
    // Purpose: Executes the evaluate world server health operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - snapshot: Snapshot value supplied by the caller for this operation.
    // - now: Now value supplied by the caller for this operation.
    // Returns: Returns the health evaluation value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: EvaluateMapOwnerHealth
    // Purpose: Executes the evaluate map owner health operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - snapshot: Snapshot value supplied by the caller for this operation.
    // - now: Now value supplied by the caller for this operation.
    // Returns: Returns the health evaluation value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: EvaluateBaseServerHealth
    // Purpose: Executes the evaluate base server health operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - snapshot: Snapshot value supplied by the caller for this operation.
    // - now: Now value supplied by the caller for this operation.
    // - prefix: Prefix value supplied by the caller for this operation.
    // Returns: Returns the health evaluation value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: EvaluatePingHealth
    // Purpose: Executes the evaluate ping health operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - snapshot: Snapshot value supplied by the caller for this operation.
    // - now: Now value supplied by the caller for this operation.
    // Returns: Returns the health component value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: EvaluateLatencyHealth
    // Purpose: Executes the evaluate latency health operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - snapshot: Snapshot value supplied by the caller for this operation.
    // - now: Now value supplied by the caller for this operation.
    // Returns: Returns the health component value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: EvaluateWorldLoadHealth
    // Purpose: Executes the evaluate world load health operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - snapshot: Snapshot value supplied by the caller for this operation.
    // - now: Now value supplied by the caller for this operation.
    // Returns: Returns the health component value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: EvaluateMapServiceHealth
    // Purpose: Executes the evaluate map service health operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - status: Status value supplied by the caller for this operation.
    // - now: Now value supplied by the caller for this operation.
    // Returns: Returns the health evaluation value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReportHealthIfNeeded
    // Purpose: Executes the report health if needed operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - reports: Reports value supplied by the caller for this operation.
    // - key: Key value supplied by the caller for this operation.
    // - evaluation: Evaluation value supplied by the caller for this operation.
    // - now: Now value supplied by the caller for this operation.
    // - critical: Critical value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: Worst
    // Purpose: Executes the worst operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - HealthLevellevels: Health levellevels value supplied by the caller for this operation.
    // Returns: Returns the health level value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: FormatDuration
    // Purpose: Executes the format duration operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - duration: Duration value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: CalculatePercent
    // Purpose: Calculates calculate percent values for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // - maximum: Maximum value supplied by the caller for this operation.
    // Returns: Returns the double value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    private static double CalculatePercent(int value, int maximum)
    {
        if (maximum <= 0)
        {
            return 100d;
        }

        return Math.Clamp(value / (double)maximum * 100d, 0d, 100d);
    }

    // Method: GetLatestPacketReceivedUtc
    // Purpose: Retrieves get latest packet received utc data for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - snapshot: Snapshot value supplied by the caller for this operation.
    // Returns: Returns the date time offset value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    private static DateTimeOffset GetLatestPacketReceivedUtc(ServerSnapshot snapshot)
    {
        DateTimeOffset latest = snapshot.LastPacketReceivedUtc;

        if (snapshot.Session is not null && snapshot.Session.LastPacketReceivedUtc > latest)
        {
            latest = snapshot.Session.LastPacketReceivedUtc;
        }

        return latest;
    }

    // Method: GetEffectiveCriticalPacketTimeout
    // Purpose: Retrieves get effective critical packet timeout data for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters: none.
    // Returns: Returns the time span value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    private TimeSpan GetEffectiveCriticalPacketTimeout()
    {
        TimeSpan minimumSafeTimeout = TimeSpan.FromSeconds(45);
        return _settings.CriticalServerPacketTimeout >= minimumSafeTimeout
            ? _settings.CriticalServerPacketTimeout
            : minimumSafeTimeout;
    }

    // Method: HandleCriticalServerDownAsync
    // Purpose: Handles handle critical server down work for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - criticalState: Critical state value supplied by the caller for this operation.
    // - timeSinceLastPacket: Time since last packet value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: BroadcastShutdownRequestAsync
    // Purpose: Executes the broadcast shutdown request operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - failedServerName: Failed server name value supplied by the caller for this operation.
    // - reason: Reason value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: MarkNonCriticalReconnectTimedOut
    // Purpose: Executes the mark non critical reconnect timed out operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - state: State value supplied by the caller for this operation.
    // - reconnectTimeout: Reconnect timeout value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReportNonCriticalServerDownIfNeeded
    // Purpose: Executes the report non critical server down if needed operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - state: State value supplied by the caller for this operation.
    // - now: Now value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: IsMapControlServer
    // Purpose: Validates or evaluates is map control server rules for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // Returns: Returns true when is map control server succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    private static bool IsMapControlServer(string remoteServerName)
    {
        return string.Equals(remoteServerName, "MapServer", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(remoteServerName, "InstanceServer", StringComparison.OrdinalIgnoreCase);
    }

    // Method: IsMapServiceOnline
    // Purpose: Validates or evaluates is map service online rules for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - state: State value supplied by the caller for this operation.
    // Returns: Returns true when is map service online succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    private static bool IsMapServiceOnline(string state)
    {
        return string.Equals(state, "Online", StringComparison.OrdinalIgnoreCase);
    }

    // Method: GetStatusKey
    // Purpose: Retrieves get status key data for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - status: Status value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    private static string GetStatusKey(InternalMapServiceStatusPacket status)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{status.OwnerServerName}|{status.Kind}|{status.MapId}|{status.InstanceId}");
    }

    // Method: GetOrCreateServerState
    // Purpose: Retrieves get or create server state data for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - serverName: Server name value supplied by the caller for this operation.
    // Returns: Returns the server state value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    private ServerState GetOrCreateServerState(string serverName)
    {
        bool isCritical = _settings.CriticalServers.Contains(serverName);

        return _servers.GetOrAdd(
            serverName,
            name => new ServerState(name, isCritical));
    }

    // Type: ServerState
    // Purpose: Provides server state behavior for the proxy server gateway, internal routing, and public connection coordination.
    // Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
    private sealed class ServerState
    {

        // Constructor: ServerState
        // Purpose: Initializes a new ServerState instance with dependencies and values required by the proxy server gateway, internal routing, and public connection coordination.
        // Parameters:
        // - name: Name value supplied by the caller for this operation.
        // - isCritical: Is critical value supplied by the caller for this operation.
        // Returns: none.
        // Notes: This keeps the operation scoped to ServerState so callers do not duplicate validation, protocol, or persistence rules.
        public ServerState(string name, bool isCritical)
        {
            Name = name;
            IsCritical = isCritical;
            LastPacketReceivedUtc = DateTimeOffset.UtcNow;
        }

        public object SyncRoot { get; } = new();

        // Property: Gets or sets the name value used by the proxy server gateway, internal routing, and public connection coordination.
        // Value: name value exposed by the owning type.
        public string Name { get; }

        // Property: Gets or sets the is critical value used by the proxy server gateway, internal routing, and public connection coordination.
        // Value: is critical value exposed by the owning type.
        public bool IsCritical { get; }

        // Property: Gets or sets the session value used by the proxy server gateway, internal routing, and public connection coordination.
        // Value: session value exposed by the owning type.
        public InternalServerSession? Session { get; set; }

        // Property: Gets or sets the last packet received utc value used by the proxy server gateway, internal routing, and public connection coordination.
        // Value: last packet received utc value exposed by the owning type.
        public DateTimeOffset LastPacketReceivedUtc { get; set; }

        // Property: Gets or sets the last down report utc value used by the proxy server gateway, internal routing, and public connection coordination.
        // Value: last down report utc value exposed by the owning type.
        public DateTimeOffset? LastDownReportUtc { get; set; }

        // Property: Gets or sets the disconnected utc value used by the proxy server gateway, internal routing, and public connection coordination.
        // Value: disconnected utc value exposed by the owning type.
        public DateTimeOffset? DisconnectedUtc { get; set; }

        // Property: Gets or sets the reconnect timed out value used by the proxy server gateway, internal routing, and public connection coordination.
        // Value: reconnect timed out value exposed by the owning type.
        public bool ReconnectTimedOut { get; set; }

        // Property: Gets or sets the is connected value used by the proxy server gateway, internal routing, and public connection coordination.
        // Value: is connected value exposed by the owning type.
        public bool IsConnected { get; set; }

        // Property: Gets or sets the shutdown triggered value used by the proxy server gateway, internal routing, and public connection coordination.
        // Value: shutdown triggered value exposed by the owning type.
        public bool ShutdownTriggered { get; set; }

        // Property: Gets or sets the has ever connected value used by the proxy server gateway, internal routing, and public connection coordination.
        // Value: has ever connected value exposed by the owning type.
        public bool HasEverConnected { get; set; }

        // Property: Gets or sets the last latency measured utc value used by the proxy server gateway, internal routing, and public connection coordination.
        // Value: last latency measured utc value exposed by the owning type.
        public DateTimeOffset? LastLatencyMeasuredUtc { get; set; }

        // Property: Gets or sets the last pong received utc value used by the proxy server gateway, internal routing, and public connection coordination.
        // Value: last pong received utc value exposed by the owning type.
        public DateTimeOffset? LastPongReceivedUtc { get; set; }

        // Property: Gets or sets the last latency milliseconds value used by the proxy server gateway, internal routing, and public connection coordination.
        // Value: last latency milliseconds value exposed by the owning type.
        public double? LastLatencyMilliseconds { get; set; }

        // Property: Gets or sets the average latency milliseconds value used by the proxy server gateway, internal routing, and public connection coordination.
        // Value: average latency milliseconds value exposed by the owning type.
        public double? AverageLatencyMilliseconds { get; set; }

        // Property: Gets or sets the last ping timeout utc value used by the proxy server gateway, internal routing, and public connection coordination.
        // Value: last ping timeout utc value exposed by the owning type.
        public DateTimeOffset? LastPingTimeoutUtc { get; set; }

        // Property: Gets or sets the consecutive ping timeouts value used by the proxy server gateway, internal routing, and public connection coordination.
        // Value: consecutive ping timeouts value exposed by the owning type.
        public int ConsecutivePingTimeouts { get; set; }

        // Property: Gets or sets the total ping timeouts value used by the proxy server gateway, internal routing, and public connection coordination.
        // Value: total ping timeouts value exposed by the owning type.
        public int TotalPingTimeouts { get; set; }

        // Property: Gets or sets the world active players value used by the proxy server gateway, internal routing, and public connection coordination.
        // Value: world active players value exposed by the owning type.
        public int WorldActivePlayers { get; set; }

        // Property: Gets or sets the world max connections value used by the proxy server gateway, internal routing, and public connection coordination.
        // Value: world max connections value exposed by the owning type.
        public int WorldMaxConnections { get; set; }

        // Property: Gets or sets the last world health status utc value used by the proxy server gateway, internal routing, and public connection coordination.
        // Value: last world health status utc value exposed by the owning type.
        public DateTimeOffset? LastWorldHealthStatusUtc { get; set; }

        // Method: GetSnapshot
        // Purpose: Retrieves get snapshot data for the proxy server gateway, internal routing, and public connection coordination.
        // Parameters: none.
        // Returns: Returns the server snapshot value produced by this operation.
        // Notes: This keeps the operation scoped to ServerState so callers do not duplicate validation, protocol, or persistence rules.
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

    // Type: ServerSnapshot
    // Purpose: Represents server snapshot data passed through the proxy server gateway, internal routing, and public connection coordination.
    // Constructor values:
    // - Name: Name value supplied by the caller for this operation.
    // - IsCritical: Is critical value supplied by the caller for this operation.
    // - Session: Session value supplied by the caller for this operation.
    // - LastPacketReceivedUtc: Last packet received utc value supplied by the caller for this operation.
    // - DisconnectedUtc: Disconnected utc value supplied by the caller for this operation.
    // - IsConnected: Is connected value supplied by the caller for this operation.
    // - ShutdownTriggered: Shutdown triggered value supplied by the caller for this operation.
    // - HasEverConnected: Has ever connected value supplied by the caller for this operation.
    // - ReconnectTimedOut: Reconnect timed out value supplied by the caller for this operation.
    // - LastLatencyMeasuredUtc: Last latency measured utc value supplied by the caller for this operation.
    // - LastPongReceivedUtc: Last pong received utc value supplied by the caller for this operation.
    // - LastLatencyMilliseconds: Last latency milliseconds value supplied by the caller for this operation.
    // - AverageLatencyMilliseconds: Average latency milliseconds value supplied by the caller for this operation.
    // - LastPingTimeoutUtc: Last ping timeout utc value supplied by the caller for this operation.
    // - ConsecutivePingTimeouts: Consecutive ping timeouts value supplied by the caller for this operation.
    // - TotalPingTimeouts: Total ping timeouts value supplied by the caller for this operation.
    // - WorldActivePlayers: World active players value supplied by the caller for this operation.
    // - WorldMaxConnections: World max connections value supplied by the caller for this operation.
    // - LastWorldHealthStatusUtc: Last world health status utc value supplied by the caller for this operation.
    // Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
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

    // Type: HealthComponent
    // Purpose: Represents health component data passed through the proxy server gateway, internal routing, and public connection coordination.
    // Constructor values:
    // - Level: Level value supplied by the caller for this operation.
    // - Summary: Summary value supplied by the caller for this operation.
    // - Reason: Reason value supplied by the caller for this operation.
    // Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
    private sealed record HealthComponent(HealthLevel Level, string Summary, string Reason);

    // Type: HealthEvaluation
    // Purpose: Represents health evaluation data passed through the proxy server gateway, internal routing, and public connection coordination.
    // Constructor values:
    // - Level: Level value supplied by the caller for this operation.
    // - Summary: Summary value supplied by the caller for this operation.
    // - Reason: Reason value supplied by the caller for this operation.
    // Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
    private sealed record HealthEvaluation(HealthLevel Level, string Summary, string Reason);

    // Type: HealthReportState
    // Purpose: Provides health report state behavior for the proxy server gateway, internal routing, and public connection coordination.
    // Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
    private sealed class HealthReportState
    {
        public object SyncRoot { get; } = new();

        // Property: Gets or sets the last level value used by the proxy server gateway, internal routing, and public connection coordination.
        // Value: last level value exposed by the owning type.
        public HealthLevel LastLevel { get; set; } = HealthLevel.Unknown;

        // Property: Gets or sets the last summary value used by the proxy server gateway, internal routing, and public connection coordination.
        // Value: last summary value exposed by the owning type.
        public string LastSummary { get; set; } = string.Empty;

        // Property: Gets or sets the last report utc value used by the proxy server gateway, internal routing, and public connection coordination.
        // Value: last report utc value exposed by the owning type.
        public DateTimeOffset? LastReportUtc { get; set; }
    }
}
