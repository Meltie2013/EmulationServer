using System.Net.Sockets;

using EmulationServer.Network.Configuration;
using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Health;
using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Network.Networking.Peers;

public sealed class InternalPeerConnector : IAsyncDisposable
{
    private readonly string _serverName;
    private readonly IReadOnlyList<InternalPeerSettings> _peers;
    private readonly string _registrationKey;
    private readonly TimeSpan _latencyReportInterval;
    private readonly TimeSpan _pingTimeout;
    private readonly InternalNetworkCallbacks _callbacks;
    private readonly List<Task> _connectionTasks = [];
    private readonly object _syncRoot = new();

    private CancellationTokenSource? _stopCancellation;
    private int _started;
    private int _stopping;

    public InternalPeerConnector(
        string serverName,
        IReadOnlyList<InternalPeerSettings> peers,
        string registrationKey,
        TimeSpan latencyReportInterval,
        TimeSpan pingTimeout,
        InternalNetworkCallbacks? callbacks = null)
    {
        if (string.IsNullOrWhiteSpace(serverName))
        {
            throw new ArgumentException("Server name is required.", nameof(serverName));
        }

        if (string.IsNullOrWhiteSpace(registrationKey))
        {
            throw new ArgumentException("Registration key is required.", nameof(registrationKey));
        }

        if (latencyReportInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(latencyReportInterval), "Latency report interval must be greater than zero.");
        }

        if (pingTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(pingTimeout), "Ping timeout must be greater than zero.");
        }

        _serverName = serverName;
        _peers = peers ?? throw new ArgumentNullException(nameof(peers));
        _registrationKey = registrationKey;
        _latencyReportInterval = latencyReportInterval;
        _pingTimeout = pingTimeout;
        _callbacks = callbacks ?? InternalNetworkCallbacks.Empty;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _started, 1) == 1)
        {
            throw new InvalidOperationException($"{_serverName} internal peer connector has already been started.");
        }

        List<InternalPeerSettings> enabledPeers = _peers
            .Where(peer => peer.Enabled)
            .ToList();

        if (enabledPeers.Count == 0)
        {
            Logger.Write(LogType.TRACE, $"{_serverName} has no configured outgoing internal peers.", nameof(InternalPeerConnector));
            return Task.CompletedTask;
        }

        _stopCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        CancellationToken peerCancellationToken = _stopCancellation.Token;

        lock (_syncRoot)
        {
            foreach (InternalPeerSettings peer in enabledPeers)
            {
                _connectionTasks.Add(Task.Run(() => RunPeerLoopAsync(peer, peerCancellationToken), CancellationToken.None));
            }
        }

        Logger.Write(LogType.NETWORK, $"{_serverName} internal peer connector started with {enabledPeers.Count} peer(s).", nameof(InternalPeerConnector));
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

        Task[] connectionTasks;
        lock (_syncRoot)
        {
            connectionTasks = _connectionTasks.ToArray();
        }

        if (connectionTasks.Length > 0)
        {
            Task allConnectionsStopped = Task.WhenAll(connectionTasks);
            Task completedTask = await Task.WhenAny(allConnectionsStopped, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken));

            if (completedTask == allConnectionsStopped)
            {
                await allConnectionsStopped;
            }
            else
            {
                Logger.Write(LogType.WARNING, $"Stopped waiting for {_serverName} peer connector because shutdown wait timed out.", nameof(InternalPeerConnector));
            }
        }

        stopCancellation?.Dispose();
        _stopCancellation = null;

        Logger.Write(LogType.NETWORK, $"{_serverName} internal peer connector stopped.", nameof(InternalPeerConnector));
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
    }

    private async Task RunPeerLoopAsync(InternalPeerSettings peer, CancellationToken cancellationToken)
    {
        bool everAuthenticated = false;
        bool loggedInitialWait = false;

        while (!cancellationToken.IsCancellationRequested)
        {
            InternalPeerConnection? connection = null;

            try
            {
                using TcpClient client = new();
                client.NoDelay = true;
                client.ReceiveBufferSize = 8192;
                client.SendBufferSize = 8192;

                if (everAuthenticated)
                {
                    Logger.Write(LogType.NETWORK, $"{_serverName} reconnecting to internal peer {peer.Name} at {peer.Host}:{peer.Port}...", nameof(InternalPeerConnector));
                }
                else if (!loggedInitialWait)
                {
                    Logger.Write(LogType.NETWORK, $"{_serverName} waiting for internal peer {peer.Name} at {peer.Host}:{peer.Port} to become available...", nameof(InternalPeerConnector));
                    loggedInitialWait = true;
                }

                await client.ConnectAsync(peer.Host, peer.Port, cancellationToken);

                await using NetworkStream stream = client.GetStream();
                using SemaphoreSlim sendLock = new(1, 1);

                await AuthenticateWithPeerAsync(peer, stream, sendLock, cancellationToken);

                connection = new InternalPeerConnection(_serverName, peer, stream, sendLock);
                everAuthenticated = true;

                Logger.Write(LogType.NETWORK, $"{_serverName} authenticated with internal peer {peer.Name}.", nameof(InternalPeerConnector));
                await _callbacks.NotifyPeerAuthenticatedAsync(connection, peer.Name, cancellationToken);

                await ProcessAuthenticatedPeerAsync(connection, stream, sendLock, cancellationToken);

                Logger.Write(LogType.NETWORK, $"{_serverName} disconnected from internal peer {peer.Name}.", nameof(InternalPeerConnector));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                if (everAuthenticated)
                {
                    Logger.Write(LogType.WARNING, $"{_serverName} lost or could not reconnect to internal peer {peer.Name} at {peer.Host}:{peer.Port}: {exception.Message}", nameof(InternalPeerConnector));
                }
                else
                {
                    // Keep startup clean: before the first successful authentication, the peer may simply not be online yet.
                }
            }
            finally
            {
                if (connection is not null)
                {
                    try
                    {
                        await _callbacks.NotifyPeerDisconnectedAsync(connection, peer.Name, CancellationToken.None);
                    }
                    catch (Exception exception)
                    {
                        Logger.Write(LogType.CRITICAL, exception.ToString(), nameof(InternalPeerConnector));
                    }
                }
            }

            try
            {
                await Task.Delay(peer.ReconnectDelay, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task AuthenticateWithPeerAsync(
        InternalPeerSettings peer,
        NetworkStream stream,
        SemaphoreSlim sendLock,
        CancellationToken cancellationToken)
    {
        string? challenge = await InternalProtocol.ReadLineAsync(
            stream,
            InternalProtocol.MaximumAuthenticationLineLength,
            cancellationToken);

        if (challenge is null)
        {
            throw new InvalidOperationException($"Internal peer {peer.Name} disconnected before requesting authentication.");
        }

        string[] challengeParts = challenge.Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (challengeParts.Length != 2 || !string.Equals(challengeParts[0], InternalProtocol.AuthenticationChallenge, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Internal peer {peer.Name} sent an invalid authentication challenge.");
        }

        await InternalProtocol.WriteLineAsync(
            stream,
            sendLock,
            $"{InternalProtocol.AuthenticationResponse} {_serverName} {_registrationKey}",
            cancellationToken);

        string? response = await InternalProtocol.ReadLineAsync(
            stream,
            InternalProtocol.MaximumAuthenticationLineLength,
            cancellationToken);

        if (response is null)
        {
            throw new InvalidOperationException($"Internal peer {peer.Name} disconnected before accepting authentication.");
        }

        if (!response.StartsWith($"{InternalProtocol.AuthenticationAccepted} ", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Internal peer {peer.Name} rejected authentication.");
        }
    }

    private async Task ProcessAuthenticatedPeerAsync(
        InternalPeerConnection connection,
        NetworkStream stream,
        SemaphoreSlim sendLock,
        CancellationToken cancellationToken)
    {
        await using InternalLatencyMonitor latencyMonitor = new(
            _serverName,
            connection.RemoteServerName,
            stream,
            sendLock,
            _latencyReportInterval,
            _pingTimeout);

        latencyMonitor.Start(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            string? line = await InternalProtocol.ReadLineAsync(
                stream,
                InternalProtocol.MaximumPacketLineLength,
                cancellationToken);

            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            await ProcessPeerPacketAsync(connection, line, latencyMonitor, cancellationToken);
        }
    }

    private async Task ProcessPeerPacketAsync(
        InternalPeerConnection connection,
        string line,
        InternalLatencyMonitor latencyMonitor,
        CancellationToken cancellationToken)
    {
        string[] parts = line.Split(' ', 3, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return;
        }

        if (parts.Length >= 2 && string.Equals(parts[0], InternalProtocol.Ping, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.TRACE, $"{_serverName} received PING packet from {connection.RemoteServerName}.", nameof(InternalPeerConnector));
            await latencyMonitor.RespondToPingAsync(parts[1], cancellationToken);
            return;
        }

        if (parts.Length >= 2 && string.Equals(parts[0], InternalProtocol.Pong, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.TRACE, $"{_serverName} received PONG packet from {connection.RemoteServerName}.", nameof(InternalPeerConnector));
            latencyMonitor.RecordPong(parts[1]);
            return;
        }

        if (parts.Length >= 2 && string.Equals(parts[0], InternalProtocol.ShutdownRequest, StringComparison.OrdinalIgnoreCase))
        {
            string reason = parts.Length == 3 ? parts[2] : "No reason provided.";
            await _callbacks.NotifyShutdownRequestedAsync(parts[1], reason, cancellationToken);
            return;
        }

        if (string.Equals(parts[0], InternalProtocol.WorldCapacity, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.NETWORK, $"{_serverName} received world capacity packet from {connection.RemoteServerName}: {line}", nameof(InternalPeerConnector));
        }
        else
        {
            Logger.Write(LogType.DEBUG, $"{_serverName} received internal packet from peer {connection.RemoteServerName}: {line}", nameof(InternalPeerConnector));
        }

        await _callbacks.NotifyPeerPacketReceivedAsync(connection, connection.RemoteServerName, line, cancellationToken);
    }
}
