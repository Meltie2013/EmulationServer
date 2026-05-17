
using System.Net.Sockets;

using EmulationServer.Network.Configuration;
using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Health;
using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Network.Networking.Sessions;

public sealed class InternalServerSession
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly CancellationTokenSource _disconnectCancellation = new();
    private readonly InternalNetworkSettings _settings;
    private readonly InternalNetworkCallbacks _callbacks;
    private readonly string _remoteEndPoint;

    private long _lastPacketReceivedUtcTicks;
    private int _disconnectRequested;

    public Guid Id { get; } = Guid.NewGuid();

    public string? RemoteServerName { get; private set; }

    public DateTimeOffset LastPacketReceivedUtc => new(Interlocked.Read(ref _lastPacketReceivedUtcTicks), TimeSpan.Zero);

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(RemoteServerName);

    public InternalServerSession(
        InternalNetworkSettings settings,
        TcpClient client,
        InternalNetworkCallbacks? callbacks = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _settings = settings;
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _callbacks = callbacks ?? InternalNetworkCallbacks.Empty;
        _stream = _client.GetStream();
        _remoteEndPoint = _client.Client.RemoteEndPoint?.ToString() ?? "unknown endpoint";
        _lastPacketReceivedUtcTicks = DateTimeOffset.UtcNow.Ticks;
    }

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        Logger.Write(LogType.NETWORK, $"{_settings.ServerName} accepted internal session from {_remoteEndPoint}. Requesting server pass-key...", nameof(InternalServerSession));

        using CancellationTokenSource linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _disconnectCancellation.Token);

        string? remoteServerName = null;
        InternalLatencyMonitor? latencyMonitor = null;

        try
        {
            remoteServerName = await RequestAndValidateAuthenticationAsync(linkedCancellation.Token);
            RemoteServerName = remoteServerName;
            MarkPacketReceived();

            Logger.Write(LogType.NETWORK, $"{_settings.ServerName} authenticated internal server '{remoteServerName}' from {_remoteEndPoint}.", nameof(InternalServerSession));

            await InternalProtocol.WriteLineAsync(
                _stream,
                _sendLock,
                $"{InternalProtocol.AuthenticationAccepted} {_settings.ServerName}",
                linkedCancellation.Token);

            await _callbacks.NotifyServerAuthenticatedAsync(this, remoteServerName, linkedCancellation.Token);

            latencyMonitor = new InternalLatencyMonitor(
                _settings.ServerName,
                remoteServerName,
                _stream,
                _sendLock,
                _settings.LatencyReportInterval,
                _settings.PingTimeout);

            latencyMonitor.Start(linkedCancellation.Token);

            while (!linkedCancellation.Token.IsCancellationRequested)
            {
                string? line = await InternalProtocol.ReadLineAsync(
                    _stream,
                    InternalProtocol.MaximumPacketLineLength,
                    linkedCancellation.Token);

                if (line is null)
                {
                    Logger.Write(LogType.NETWORK, $"Internal server '{remoteServerName}' disconnected from {_remoteEndPoint}.", nameof(InternalServerSession));
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                MarkPacketReceived();
                await _callbacks.NotifyPacketReceivedAsync(this, remoteServerName, line, linkedCancellation.Token);
                await ProcessPacketAsync(remoteServerName, line, latencyMonitor, linkedCancellation.Token);
            }
        }
        catch (UnauthorizedAccessException exception)
        {
            Logger.Write(LogType.WARNING, $"Rejected internal authentication from {_remoteEndPoint}: {exception.Message}", nameof(InternalServerSession));

            try
            {
                await InternalProtocol.WriteLineAsync(
                    _stream,
                    _sendLock,
                    $"{InternalProtocol.AuthenticationRejected} AuthenticationFailed",
                    CancellationToken.None);
            }
            catch (Exception writeException) when (writeException is IOException or SocketException or ObjectDisposedException)
            {
                // The remote connection may already be gone. The rejection was still logged above.
            }
        }
        catch (OperationCanceledException) when (linkedCancellation.Token.IsCancellationRequested)
        {
            // Expected during server shutdown or explicit session disconnect.
        }
        catch (IOException exception)
        {
            Logger.Write(LogType.NETWORK, $"Internal connection closed for {_remoteEndPoint}: {exception.Message}", nameof(InternalServerSession));
        }
        catch (SocketException exception)
        {
            Logger.Write(LogType.NETWORK, $"Internal socket closed for {_remoteEndPoint}: {exception.SocketErrorCode}", nameof(InternalServerSession));
        }
        catch (ObjectDisposedException) when (IsDisconnectRequested)
        {
            // Expected when the socket is disposed during shutdown.
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.CRITICAL, exception.ToString(), nameof(InternalServerSession));
        }
        finally
        {
            if (latencyMonitor is not null)
            {
                await latencyMonitor.DisposeAsync();
            }

            if (!string.IsNullOrWhiteSpace(remoteServerName))
            {
                try
                {
                    await _callbacks.NotifyServerDisconnectedAsync(this, remoteServerName, CancellationToken.None);
                }
                catch (Exception exception)
                {
                    Logger.Write(LogType.CRITICAL, exception.ToString(), nameof(InternalServerSession));
                }
            }

            await DisconnectAsync();
        }
    }

    public async Task SendPacketAsync(string packet, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(packet))
        {
            return;
        }

        await InternalProtocol.WriteLineAsync(
            _stream,
            _sendLock,
            packet,
            cancellationToken);
    }

    public Task DisconnectAsync()
    {
        if (Interlocked.Exchange(ref _disconnectRequested, 1) == 1)
        {
            return Task.CompletedTask;
        }

        Logger.Write(LogType.NETWORK, $"Ending internal session for {_remoteEndPoint}.", nameof(InternalServerSession));

        try
        {
            _disconnectCancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Ignore; shutdown is already in progress or complete.
        }

        try
        {
            _client.Client.Shutdown(SocketShutdown.Both);
        }
        catch (SocketException)
        {
            // The remote side may have already closed/reset the connection.
        }
        catch (ObjectDisposedException)
        {
            // The socket may have already been disposed.
        }

        _stream.Dispose();
        _client.Dispose();
        _sendLock.Dispose();
        _disconnectCancellation.Dispose();

        return Task.CompletedTask;
    }

    private async Task<string> RequestAndValidateAuthenticationAsync(CancellationToken cancellationToken)
    {
        await InternalProtocol.WriteLineAsync(
            _stream,
            _sendLock,
            $"{InternalProtocol.AuthenticationChallenge} {_settings.ServerName}",
            cancellationToken);

        string? line = await InternalProtocol.ReadLineAsync(
            _stream,
            InternalProtocol.MaximumAuthenticationLineLength,
            cancellationToken);

        if (line is null)
        {
            throw new UnauthorizedAccessException("Remote server disconnected before sending authentication response.");
        }

        string[] parts = line.Split(' ', 3, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 3 || !string.Equals(parts[0], InternalProtocol.AuthenticationResponse, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Missing or invalid authentication response packet.");
        }

        string remoteServerName = parts[1];
        string registrationKey = parts[2];

        if (string.IsNullOrWhiteSpace(remoteServerName))
        {
            throw new UnauthorizedAccessException("Missing remote server name.");
        }

        if (!InternalProtocol.RegistrationKeysMatch(_settings.RegistrationKey, registrationKey))
        {
            throw new UnauthorizedAccessException($"Invalid registration key for server '{remoteServerName}'.");
        }

        return remoteServerName;
    }

    private async Task ProcessPacketAsync(
        string remoteServerName,
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
            Logger.Write(LogType.TRACE, $"{_settings.ServerName} received PING packet from {remoteServerName}.", nameof(InternalServerSession));
            await latencyMonitor.RespondToPingAsync(parts[1], cancellationToken);
            return;
        }

        if (parts.Length >= 2 && string.Equals(parts[0], InternalProtocol.Pong, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.TRACE, $"{_settings.ServerName} received PONG packet from {remoteServerName}.", nameof(InternalServerSession));
            latencyMonitor.RecordPong(parts[1]);
            return;
        }

        if (parts.Length >= 2 && string.Equals(parts[0], InternalProtocol.ShutdownRequest, StringComparison.OrdinalIgnoreCase))
        {
            string reason = parts.Length == 3 ? parts[2] : "No reason provided.";
            await _callbacks.NotifyShutdownRequestedAsync(parts[1], reason, cancellationToken);
            return;
        }
    }

    private void MarkPacketReceived()
    {
        Interlocked.Exchange(ref _lastPacketReceivedUtcTicks, DateTimeOffset.UtcNow.Ticks);
    }

    private bool IsDisconnectRequested => Volatile.Read(ref _disconnectRequested) == 1;
}
