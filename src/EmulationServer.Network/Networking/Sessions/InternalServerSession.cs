
using System.Buffers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

using EmulationServer.Network.Configuration;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Network.Networking.Sessions;

public sealed class InternalServerSession
{
    private const int ReceiveBufferSize = 4096;
    private const int MaximumRegistrationLineLength = 512;

    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly CancellationTokenSource _disconnectCancellation = new();
    private readonly InternalNetworkSettings _settings;
    private readonly string _remoteEndPoint;

    private int _disconnectRequested;

    public Guid Id { get; } = Guid.NewGuid();

    public InternalServerSession(InternalNetworkSettings settings, TcpClient client)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _settings = settings;
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _stream = _client.GetStream();
        _remoteEndPoint = _client.Client.RemoteEndPoint?.ToString() ?? "unknown endpoint";
    }

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        Logger.Write(LogType.NETWORK, $"{_settings.ServerName} accepted internal session from {_remoteEndPoint}. Awaiting server registration...", nameof(InternalServerSession));

        using CancellationTokenSource linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _disconnectCancellation.Token);

        string? remoteServerName = null;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(ReceiveBufferSize);

        try
        {
            remoteServerName = await ReadAndValidateRegistrationAsync(linkedCancellation.Token);
            Logger.Write(LogType.NETWORK, $"{_settings.ServerName} registered internal server '{remoteServerName}' from {_remoteEndPoint}.", nameof(InternalServerSession));

            byte[] accepted = Encoding.UTF8.GetBytes($"REGISTERED {_settings.ServerName}\n");
            await _stream.WriteAsync(accepted.AsMemory(0, accepted.Length), linkedCancellation.Token);

            while (!linkedCancellation.Token.IsCancellationRequested)
            {
                int received = await _stream.ReadAsync(buffer.AsMemory(0, ReceiveBufferSize), linkedCancellation.Token);
                if (received == 0)
                {
                    Logger.Write(LogType.NETWORK, $"Internal server '{remoteServerName}' disconnected from {_remoteEndPoint}.", nameof(InternalServerSession));
                    break;
                }

                string preview = Encoding.UTF8.GetString(buffer, 0, received).Trim();
                Logger.Write(LogType.DEBUG, $"{_settings.ServerName} received {received} internal byte(s) from {remoteServerName}: {preview}", nameof(InternalServerSession));
            }
        }
        catch (UnauthorizedAccessException exception)
        {
            Logger.Write(LogType.WARNING, $"Rejected internal registration from {_remoteEndPoint}: {exception.Message}", nameof(InternalServerSession));

            try
            {
                byte[] rejected = Encoding.UTF8.GetBytes("REJECTED AuthenticationFailed\n");
                await _stream.WriteAsync(rejected.AsMemory(0, rejected.Length), CancellationToken.None);
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
            ArrayPool<byte>.Shared.Return(buffer);
            await DisconnectAsync();
        }
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
        _disconnectCancellation.Dispose();

        return Task.CompletedTask;
    }

    private async Task<string> ReadAndValidateRegistrationAsync(CancellationToken cancellationToken)
    {
        string line = await ReadLineAsync(_stream, MaximumRegistrationLineLength, cancellationToken);
        string[] parts = line.Split(' ', 3, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 3 || !string.Equals(parts[0], "REGISTER", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Missing or invalid registration command.");
        }

        string remoteServerName = parts[1];
        string registrationKey = parts[2];

        if (string.IsNullOrWhiteSpace(remoteServerName))
        {
            throw new UnauthorizedAccessException("Missing remote server name.");
        }

        if (!RegistrationKeysMatch(_settings.RegistrationKey, registrationKey))
        {
            throw new UnauthorizedAccessException($"Invalid registration key for server '{remoteServerName}'.");
        }

        return remoteServerName;
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
            throw new UnauthorizedAccessException("Registration command is too long.");
        }

        return Encoding.UTF8.GetString(lineBuffer.ToArray()).Trim();
    }

    private static bool RegistrationKeysMatch(string expected, string actual)
    {
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
        byte[] actualBytes = Encoding.UTF8.GetBytes(actual);

        return expectedBytes.Length == actualBytes.Length &&
            CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    private bool IsDisconnectRequested => Volatile.Read(ref _disconnectRequested) == 1;
}
