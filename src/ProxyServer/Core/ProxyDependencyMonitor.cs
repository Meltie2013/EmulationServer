
using System.Collections.Concurrent;

using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Network.Networking.Sessions;
using EmulationServer.ProxyServer.Configuration;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.ProxyServer.Core;

public sealed class ProxyDependencyMonitor : IAsyncDisposable
{
    private static readonly TimeSpan MonitorTickInterval = TimeSpan.FromSeconds(1);

    private readonly ProxyDependencySettings _settings;
    private readonly ConcurrentDictionary<string, ServerState> _servers;

    private CancellationTokenSource? _stopCancellation;
    private Task? _monitorTask;
    private int _started;
    private int _stopping;

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

    public InternalNetworkCallbacks CreateCallbacks()
    {
        return new InternalNetworkCallbacks
        {
            ServerAuthenticatedAsync = OnServerAuthenticatedAsync,
            PacketReceivedAsync = OnPacketReceivedAsync,
            ServerDisconnectedAsync = OnServerDisconnectedAsync,
        };
    }

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

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
    }

    private Task OnServerAuthenticatedAsync(
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
            state.HasEverConnected = true;
        }

        string role = state.IsCritical ? "critical" : "non-critical";
        Logger.Write(LogType.NETWORK, $"Proxy registered {role} internal server '{remoteServerName}'.", nameof(ProxyDependencyMonitor));

        return Task.CompletedTask;
    }

    private Task OnPacketReceivedAsync(
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
        }

        return Task.CompletedTask;
    }

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
                if (!snapshot.HasEverConnected)
                {
                    continue;
                }

                ReportNonCriticalServerDownIfNeeded(state, now);
            }
        }
    }

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

    private ServerState GetOrCreateServerState(string serverName)
    {
        bool isCritical = _settings.CriticalServers.Contains(serverName);

        return _servers.GetOrAdd(
            serverName,
            name => new ServerState(name, isCritical));
    }

    private sealed class ServerState
    {
        public ServerState(string name, bool isCritical)
        {
            Name = name;
            IsCritical = isCritical;
            LastPacketReceivedUtc = DateTimeOffset.UtcNow;
        }

        public object SyncRoot { get; } = new();

        public string Name { get; }

        public bool IsCritical { get; }

        public InternalServerSession? Session { get; set; }

        public DateTimeOffset LastPacketReceivedUtc { get; set; }

        public DateTimeOffset? LastDownReportUtc { get; set; }

        public bool IsConnected { get; set; }

        public bool ShutdownTriggered { get; set; }

        public bool HasEverConnected { get; set; }

        public ServerSnapshot GetSnapshot()
        {
            lock (SyncRoot)
            {
                return new ServerSnapshot(
                    Name,
                    IsCritical,
                    Session,
                    LastPacketReceivedUtc,
                    IsConnected,
                    ShutdownTriggered,
                    HasEverConnected);
            }
        }
    }

    private sealed record ServerSnapshot(
        string Name,
        bool IsCritical,
        InternalServerSession? Session,
        DateTimeOffset LastPacketReceivedUtc,
        bool IsConnected,
        bool ShutdownTriggered,
        bool HasEverConnected);
}
