
using System.Net.Sockets;
using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.WorldServer.Configuration;

namespace EmulationServer.WorldServer.Internal;

public sealed class WorldRealmStatusReporter : IAsyncDisposable
{
    private readonly RealmStatusSettings _settings;
    private readonly string _registrationKey;
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    private CancellationTokenSource? _stopCancellation;
    private Task? _reportTask;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private int _started;
    private int _activeConnections;

    public WorldRealmStatusReporter(RealmStatusSettings settings, string registrationKey)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        if (string.IsNullOrWhiteSpace(registrationKey))
        {
            throw new ArgumentException("Registration key is required.", nameof(registrationKey));
        }

        _settings.Validate();
        _registrationKey = registrationKey;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_settings.Enabled)
        {
            Logger.Write(LogType.NETWORK, "WorldServer realm status reporting is disabled.", nameof(WorldRealmStatusReporter));
            return Task.CompletedTask;
        }

        if (Interlocked.Exchange(ref _started, 1) == 1)
        {
            throw new InvalidOperationException("WorldServer realm status reporter has already been started.");
        }

        _stopCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _reportTask = Task.Run(() => RunAsync(_stopCancellation.Token), CancellationToken.None);

        Logger.Write(LogType.NETWORK, $"WorldServer realm status reporter started for realm {_settings.RealmId}.", nameof(WorldRealmStatusReporter));

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _started, 0) == 0)
        {
            return;
        }

        try
        {
            if (_stream is not null)
            {
                await SendRealmStatusAsync(false, 0, cancellationToken);
            }
        }
        catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
        {
            Logger.Write(LogType.WARNING, $"Unable to send offline realm status before shutdown: {exception.Message}", nameof(WorldRealmStatusReporter));
        }

        if (_stopCancellation is not null)
        {
            await _stopCancellation.CancelAsync();
        }

        if (_reportTask is not null)
        {
            try
            {
                await _reportTask;
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
        }

        CleanupConnection();

        _stopCancellation?.Dispose();
        _stopCancellation = null;
        _reportTask = null;

        Logger.Write(LogType.NETWORK, "WorldServer realm status reporter stopped.", nameof(WorldRealmStatusReporter));
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        _sendLock.Dispose();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndAuthenticateAsync(cancellationToken);

                Task receiveTask = ProcessRealmServerPacketsAsync(cancellationToken);
                Task statusTask = SendRealmStatusLoopAsync(cancellationToken);

                Task completedTask = await Task.WhenAny(receiveTask, statusTask);
                await completedTask;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                Logger.Write(LogType.WARNING, $"WorldServer could not update RealmServer status: {exception.Message}", nameof(WorldRealmStatusReporter));
                CleanupConnection();

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private async Task SendRealmStatusLoopAsync(CancellationToken cancellationToken)
    {
        await SendRealmStatusAsync(true, Volatile.Read(ref _activeConnections), cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(_settings.UpdateInterval, cancellationToken);
            await SendRealmStatusAsync(true, Volatile.Read(ref _activeConnections), cancellationToken);
        }
    }

    private async Task ProcessRealmServerPacketsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            NetworkStream stream = _stream ?? throw new IOException("RealmServer connection is not available.");

            string? line = await InternalProtocol.ReadLineAsync(
                stream,
                InternalProtocol.MaximumPacketLineLength,
                cancellationToken);

            if (line is null)
            {
                throw new IOException("RealmServer disconnected from WorldServer realm status reporter.");
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            await ProcessRealmServerPacketAsync(line, cancellationToken);
        }
    }

    private async Task ProcessRealmServerPacketAsync(string line, CancellationToken cancellationToken)
    {
        string[] parts = line.Split(' ', 3, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            return;
        }

        if (parts.Length >= 2 && string.Equals(parts[0], InternalProtocol.Ping, StringComparison.OrdinalIgnoreCase))
        {
            await SendPongAsync(parts[1], cancellationToken);
            return;
        }

        if (parts.Length >= 2 && string.Equals(parts[0], InternalProtocol.Pong, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.DEBUG, $"WorldServer received latency pong {parts[1]} from RealmServer.", nameof(WorldRealmStatusReporter));
            return;
        }

        if (parts.Length >= 2 && string.Equals(parts[0], InternalProtocol.ShutdownRequest, StringComparison.OrdinalIgnoreCase))
        {
            string reason = parts.Length == 3 ? parts[2] : "No reason provided.";
            Logger.Write(LogType.WARNING, $"WorldServer received shutdown request from {parts[1]}: {reason}", nameof(WorldRealmStatusReporter));
            return;
        }

        Logger.Write(LogType.DEBUG, $"WorldServer received RealmServer internal packet: {line}", nameof(WorldRealmStatusReporter));
    }

    private async Task SendPongAsync(string pingId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(pingId) || _stream is null)
        {
            return;
        }

        await InternalProtocol.WriteLineAsync(
            _stream,
            _sendLock,
            $"{InternalProtocol.Pong} {pingId}",
            cancellationToken);
    }

    private async Task ConnectAndAuthenticateAsync(CancellationToken cancellationToken)
    {
        CleanupConnection();

        _client = new TcpClient
        {
            NoDelay = true,
            ReceiveBufferSize = 8192,
            SendBufferSize = 8192,
        };

        Logger.Write(LogType.NETWORK, $"WorldServer connecting to RealmServer internal listener at {_settings.RealmServerHost}:{_settings.RealmServerPort}...", nameof(WorldRealmStatusReporter));

        await _client.ConnectAsync(_settings.RealmServerHost, _settings.RealmServerPort, cancellationToken);
        _stream = _client.GetStream();

        string? challenge = await InternalProtocol.ReadLineAsync(
            _stream,
            InternalProtocol.MaximumAuthenticationLineLength,
            cancellationToken);

        if (challenge is null)
        {
            throw new InvalidOperationException("RealmServer disconnected before authentication challenge.");
        }

        string[] challengeParts = challenge.Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (challengeParts.Length != 2 || !string.Equals(challengeParts[0], InternalProtocol.AuthenticationChallenge, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("RealmServer sent an invalid authentication challenge.");
        }

        await InternalProtocol.WriteLineAsync(
            _stream,
            _sendLock,
            $"{InternalProtocol.AuthenticationResponse} WorldServer {_registrationKey}",
            cancellationToken);

        string? response = await InternalProtocol.ReadLineAsync(
            _stream,
            InternalProtocol.MaximumAuthenticationLineLength,
            cancellationToken);

        if (response is null)
        {
            throw new InvalidOperationException("RealmServer disconnected before accepting authentication.");
        }

        if (!response.StartsWith($"{InternalProtocol.AuthenticationAccepted} ", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("RealmServer rejected WorldServer authentication.");
        }

        Logger.Write(LogType.NETWORK, "WorldServer authenticated with RealmServer internal listener.", nameof(WorldRealmStatusReporter));
    }

    private async Task SendRealmStatusAsync(bool online, int activeConnections, CancellationToken cancellationToken)
    {
        if (_stream is null)
        {
            return;
        }

        int safeActiveConnections = Math.Max(0, activeConnections);
        int safeMaxConnections = Math.Max(1, _settings.MaxConnections);
        string state = online ? "online" : "offline";

        string packet = $"REALM_STATUS {_settings.RealmId} {state} {safeActiveConnections} {safeMaxConnections}";

        await InternalProtocol.WriteLineAsync(
            _stream,
            _sendLock,
            packet,
            cancellationToken);

        Logger.Write(LogType.NETWORK, $"WorldServer sent realm status: {packet}", nameof(WorldRealmStatusReporter));
    }

    private void CleanupConnection()
    {
        try
        {
            _stream?.Dispose();
        }
        catch
        {
            // Ignore cleanup exceptions.
        }

        try
        {
            _client?.Dispose();
        }
        catch
        {
            // Ignore cleanup exceptions.
        }

        _stream = null;
        _client = null;
    }
}
