
using System.Net.Sockets;
using System.Text;

using EmulationServer.Network.Configuration;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Network.Networking.Peers;

public sealed class InternalPeerConnector : IAsyncDisposable
{
    private const int MaximumRegistrationResponseLength = 512;

    private readonly string _serverName;
    private readonly IReadOnlyList<InternalPeerSettings> _peers;
    private readonly string _registrationKey;
    private readonly List<Task> _connectionTasks = [];
    private readonly object _syncRoot = new();

    private CancellationTokenSource? _stopCancellation;
    private int _started;
    private int _stopping;

    public InternalPeerConnector(string serverName, IReadOnlyList<InternalPeerSettings> peers, string registrationKey)
    {
        if (string.IsNullOrWhiteSpace(serverName))
        {
            throw new ArgumentException("Server name is required.", nameof(serverName));
        }

        if (string.IsNullOrWhiteSpace(registrationKey))
        {
            throw new ArgumentException("Registration key is required.", nameof(registrationKey));
        }

        _serverName = serverName;
        _peers = peers ?? throw new ArgumentNullException(nameof(peers));
        _registrationKey = registrationKey;
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
        bool everRegistered = false;
        bool loggedInitialWait = false;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using TcpClient client = new();
                client.NoDelay = true;
                client.ReceiveBufferSize = 8192;
                client.SendBufferSize = 8192;

                if (everRegistered)
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
                await RegisterWithPeerAsync(peer, stream, cancellationToken);

                everRegistered = true;
                Logger.Write(LogType.NETWORK, $"{_serverName} registered with internal peer {peer.Name}.", nameof(InternalPeerConnector));

                byte[] buffer = new byte[4096];
                while (!cancellationToken.IsCancellationRequested)
                {
                    int received = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                    if (received == 0)
                    {
                        break;
                    }

                    string preview = Encoding.UTF8.GetString(buffer, 0, received).Trim();
                    Logger.Write(LogType.DEBUG, $"{_serverName} received {received} internal byte(s) from peer {peer.Name}: {preview}", nameof(InternalPeerConnector));
                }

                Logger.Write(LogType.NETWORK, $"{_serverName} disconnected from internal peer {peer.Name}.", nameof(InternalPeerConnector));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                if (everRegistered)
                {
                    Logger.Write(LogType.WARNING, $"{_serverName} lost or could not reconnect to internal peer {peer.Name} at {peer.Host}:{peer.Port}: {exception.Message}", nameof(InternalPeerConnector));
                }
                else
                {
                    // Keep startup clean: before the first successful registration, the peer may simply not be online yet.
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

    private async Task RegisterWithPeerAsync(InternalPeerSettings peer, NetworkStream stream, CancellationToken cancellationToken)
    {
        byte[] registration = Encoding.UTF8.GetBytes($"REGISTER {_serverName} {_registrationKey}\n");
        await stream.WriteAsync(registration.AsMemory(0, registration.Length), cancellationToken);

        string response = await ReadLineAsync(stream, MaximumRegistrationResponseLength, cancellationToken);
        if (!response.StartsWith("REGISTERED ", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Internal peer {peer.Name} rejected registration.");
        }
    }

    private static async Task<string> ReadLineAsync(NetworkStream stream, int maximumLength, CancellationToken cancellationToken)
    {
        byte[] singleByteBuffer = new byte[1];
        using MemoryStream lineBuffer = new();

        while (lineBuffer.Length < maximumLength)
        {
            int received = await stream.ReadAsync(singleByteBuffer.AsMemory(0, 1), cancellationToken);
            if (received == 0)
            {
                break;
            }

            byte value = singleByteBuffer[0];
            if (value == '\n')
            {
                break;
            }

            if (value != '\r')
            {
                lineBuffer.WriteByte(value);
            }
        }

        if (lineBuffer.Length >= maximumLength)
        {
            throw new InvalidOperationException("Registration response is too long.");
        }

        return Encoding.UTF8.GetString(lineBuffer.ToArray()).Trim();
    }
}
