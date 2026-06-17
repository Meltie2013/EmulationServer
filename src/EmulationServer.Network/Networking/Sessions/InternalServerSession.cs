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
// File: src/EmulationServer.Network/Networking/Sessions/InternalServerSession.cs
// Purpose: Contains internal server session code for the packet serialization, socket transport, and protocol framing layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Net.Sockets;
using System.Threading.Channels;

using EmulationServer.Network.Configuration;
using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Health;
using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Network.Networking.Sessions;

// Type: InternalServerSession
// Purpose: Provides internal server session behavior for the packet serialization, socket transport, and protocol framing layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class InternalServerSession
{

    // Constant: Defines the internal packet dispatch queue capacity constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed internal packet dispatch queue capacity value used anywhere this rule or protocol value is needed.
    private const int InternalPacketDispatchQueueCapacity = 4096;

    // Type: QueuedInternalPacket
    // Purpose: Represents queued internal packet data passed through the packet serialization, socket transport, and protocol framing layer.
    // Constructor values:
    // - RemoteServerName: Remote server name value supplied by the caller for this operation.
    // - Line: Line value supplied by the caller for this operation.
    // Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
    private readonly record struct QueuedInternalPacket(string RemoteServerName, string Line);

    // Field: Stores the client state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current client backing value maintained by the owning type.
    private readonly TcpClient _client;

    // Field: Stores the stream state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current stream backing value maintained by the owning type.
    private readonly NetworkStream _stream;

    // Field: Stores the reader state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current reader backing value maintained by the owning type.
    private readonly InternalProtocolReader _reader;

    private readonly SemaphoreSlim _sendLock = new(1, 1);

    private readonly CancellationTokenSource _disconnectCancellation = new();

    // Field: Stores the settings state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current settings backing value maintained by the owning type.
    private readonly InternalNetworkSettings _settings;

    // Field: Stores the callbacks state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current callbacks backing value maintained by the owning type.
    private readonly InternalNetworkCallbacks _callbacks;

    // Field: Stores the remote end point state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current remote end point backing value maintained by the owning type.
    private readonly string _remoteEndPoint;

    // Method: QueuedInternalPacket
    // Purpose: Executes the queued internal packet operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - SingleReader: Single reader value supplied by the caller for this operation.
    // Returns: Returns the channel packet dispatch queue = channel.create bounded< value produced by this operation.
    // Notes: This keeps the operation scoped to InternalServerSession so callers do not duplicate validation, protocol, or persistence rules.
    private readonly Channel<QueuedInternalPacket> _packetDispatchQueue = Channel.CreateBounded<QueuedInternalPacket>(new BoundedChannelOptions(InternalPacketDispatchQueueCapacity)
    {
        SingleReader = true,
        SingleWriter = true,
        FullMode = BoundedChannelFullMode.Wait,
        AllowSynchronousContinuations = false,
    });

    // Field: Stores the packet dispatch loop state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current packet dispatch loop backing value maintained by the owning type.
    private Task? _packetDispatchLoop;

    // Field: Stores the last packet received utc ticks state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current last packet received utc ticks backing value maintained by the owning type.
    private long _lastPacketReceivedUtcTicks;

    // Field: Stores the disconnect requested state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current disconnect requested backing value maintained by the owning type.
    private int _disconnectRequested;

    // Method: NewGuid
    // Purpose: Executes the new GUID operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters: none.
    // Returns: Returns the GUID ID { get; } = guid. value produced by this operation.
    // Notes: This keeps the operation scoped to InternalServerSession so callers do not duplicate validation, protocol, or persistence rules.
    public Guid Id { get; } = Guid.NewGuid();

    // Property: Gets or sets the remote server name value used by the packet serialization, socket transport, and protocol framing layer.
    // Value: remote server name value exposed by the owning type.
    public string? RemoteServerName { get; private set; }

    public DateTimeOffset LastPacketReceivedUtc => new(Interlocked.Read(ref _lastPacketReceivedUtcTicks), TimeSpan.Zero);

    // Method: IsNullOrWhiteSpace
    // Purpose: Validates or evaluates is null or white space rules for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - RemoteServerName: Remote server name value supplied by the caller for this operation.
    // Returns: Returns the bool is authenticated => !string. value produced by this operation.
    // Notes: This keeps the operation scoped to InternalServerSession so callers do not duplicate validation, protocol, or persistence rules.
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(RemoteServerName);

    // Constructor: InternalServerSession
    // Purpose: Initializes a new InternalServerSession instance with dependencies and values required by the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - settings: Settings values that control how this operation should run.
    // - client: Client value supplied by the caller for this operation.
    // - callbacks: Callbacks value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to InternalServerSession so callers do not duplicate validation, protocol, or persistence rules.
    public InternalServerSession(
        InternalNetworkSettings settings,
        TcpClient client,
        InternalNetworkCallbacks? callbacks = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _settings = settings;
        _client = client ?? throw new ArgumentNullException();
        _callbacks = callbacks ?? InternalNetworkCallbacks.Empty;
        _stream = _client.GetStream();
        _reader = new InternalProtocolReader(_stream);
        _remoteEndPoint = _client.Client.RemoteEndPoint?.ToString() ?? "unknown endpoint";
        _lastPacketReceivedUtcTicks = DateTimeOffset.UtcNow.Ticks;
    }

    // Method: ProcessAsync
    // Purpose: Executes the process operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalServerSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        Logger.Write(LogType.NETWORK, $"{_settings.ServerName} accepted internal session from {_remoteEndPoint}. Requesting server pass-key...", "InternalServerSession");

        using CancellationTokenSource linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _disconnectCancellation.Token);

        string? remoteServerName = null;
        InternalLatencyMonitor? latencyMonitor = null;

        try
        {
            using CancellationTokenSource authenticationCancellation = CancellationTokenSource.CreateLinkedTokenSource(linkedCancellation.Token);
            authenticationCancellation.CancelAfter(_settings.AuthenticationTimeout);

            try
            {
                remoteServerName = await RequestAndValidateAuthenticationAsync(authenticationCancellation.Token);
            }
            catch (OperationCanceledException) when (!linkedCancellation.Token.IsCancellationRequested)
            {
                throw new UnauthorizedAccessException($"Internal authentication timed out after {_settings.AuthenticationTimeout.TotalSeconds:0.##} second(s).");
            }

            RemoteServerName = remoteServerName;
            MarkPacketReceived();

            Logger.Write(LogType.NETWORK, $"{_settings.ServerName} authenticated internal server '{remoteServerName}' from {_remoteEndPoint}.", "InternalServerSession");

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
                _settings.LatencyLoggingEnabled,
                _settings.LatencyLogInterval,
                _settings.PingTimeout,
                (serverName, latency) => _callbacks.NotifyLatencyMeasured(serverName, latency),
                (serverName, elapsed) => _callbacks.NotifyPingTimedOut(serverName, elapsed));

            latencyMonitor.Start(linkedCancellation.Token);
            StartPacketDispatchLoop(linkedCancellation.Token);

            while (!linkedCancellation.Token.IsCancellationRequested)
            {
                string? line = await _reader.ReadLineAsync(
                    InternalProtocol.MaximumPacketLineLength,
                    linkedCancellation.Token);

                if (line is null)
                {
                    Logger.Write(LogType.NETWORK, $"Internal server '{remoteServerName}' disconnected from {_remoteEndPoint}.", "InternalServerSession");
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                MarkPacketReceived();
                if (await TryProcessControlPacketAsync(remoteServerName, line, latencyMonitor, linkedCancellation.Token))
                {
                    continue;
                }

                await _packetDispatchQueue.Writer.WriteAsync(new QueuedInternalPacket(remoteServerName, line), linkedCancellation.Token);
            }
        }
        catch (UnauthorizedAccessException exception)
        {
            Logger.Write(LogType.WARNING, $"Rejected internal authentication from {_remoteEndPoint}: {exception.Message}", "InternalServerSession");

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

            }
        }
        catch (OperationCanceledException) when (linkedCancellation.Token.IsCancellationRequested)
        {

        }
        catch (IOException exception)
        {
            Logger.Write(LogType.NETWORK, $"Internal connection closed for {_remoteEndPoint}: {exception.Message}", "InternalServerSession");
        }
        catch (SocketException exception)
        {
            Logger.Write(LogType.NETWORK, $"Internal socket closed for {_remoteEndPoint}: {exception.SocketErrorCode}", "InternalServerSession");
        }
        catch (ObjectDisposedException) when (IsDisconnectRequested)
        {

        }
        catch (Exception exception)
        {
            Logger.Write(LogType.CRITICAL, exception.ToString(), "InternalServerSession");
        }
        finally
        {
            _packetDispatchQueue.Writer.TryComplete();
            await StopPacketDispatchLoopAsync();

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
                    Logger.Write(LogType.CRITICAL, exception.ToString(), "InternalServerSession");
                }
            }

            await DisconnectAsync();
        }
    }

    // Method: StartPacketDispatchLoop
    // Purpose: Controls the start packet dispatch loop lifecycle step for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: none.
    // Notes: This keeps the operation scoped to InternalServerSession so callers do not duplicate validation, protocol, or persistence rules.
    private void StartPacketDispatchLoop(CancellationToken cancellationToken)
    {
        _packetDispatchLoop ??= Task.Run(() => ProcessQueuedPacketsAsync(cancellationToken), CancellationToken.None);
    }

    // Method: StopPacketDispatchLoopAsync
    // Purpose: Controls the stop packet dispatch loop lifecycle step for the packet serialization, socket transport, and protocol framing layer.
    // Parameters: none.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalServerSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task StopPacketDispatchLoopAsync()
    {
        Task? loop = _packetDispatchLoop;
        _packetDispatchLoop = null;
        if (loop is null || loop.IsCompleted)
        {
            return;
        }

        try
        {
            Task completedTask = await Task.WhenAny(loop, Task.Delay(TimeSpan.FromSeconds(1)));
            if (completedTask == loop)
            {
                await loop;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    // Method: ProcessQueuedPacketsAsync
    // Purpose: Executes the process queued packets operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalServerSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task ProcessQueuedPacketsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _packetDispatchQueue.Reader.WaitToReadAsync(cancellationToken))
            {
                while (_packetDispatchQueue.Reader.TryRead(out QueuedInternalPacket packet))
                {
                    await _callbacks.NotifyPacketReceivedAsync(this, packet.RemoteServerName, packet.Line, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {

        }
        catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException or InvalidOperationException)
        {
            Logger.Write(LogType.NETWORK, $"Internal packet dispatcher stopped for {_remoteEndPoint}: {exception.Message}", "InternalServerSession");
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.CRITICAL, exception.ToString(), "InternalServerSession");
            await DisconnectAsync();
        }
    }

    // Method: SendPacketAsync
    // Purpose: Handles send packet work for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalServerSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: DisconnectAsync
    // Purpose: Executes the disconnect operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters: none.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalServerSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public Task DisconnectAsync()
    {
        if (Interlocked.Exchange(ref _disconnectRequested, 1) == 1)
        {
            return Task.CompletedTask;
        }

        Logger.Write(LogType.NETWORK, $"Ending internal session for {_remoteEndPoint}.", "InternalServerSession");

        try
        {
            _disconnectCancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {

        }

        try
        {
            _client.Client.Shutdown(SocketShutdown.Both);
        }
        catch (SocketException)
        {

        }
        catch (ObjectDisposedException)
        {

        }

        _reader.Dispose();
        _stream.Dispose();
        _client.Dispose();
        _sendLock.Dispose();
        _disconnectCancellation.Dispose();

        return Task.CompletedTask;
    }

    // Method: RequestAndValidateAuthenticationAsync
    // Purpose: Executes the request and validate authentication operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to InternalServerSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task<string> RequestAndValidateAuthenticationAsync(CancellationToken cancellationToken)
    {
        string challengeNonce = InternalProtocol.CreateAuthenticationNonce();

        await InternalProtocol.WriteLineAsync(
            _stream,
            _sendLock,
            $"{InternalProtocol.AuthenticationChallenge} {_settings.ServerName} {challengeNonce}",
            cancellationToken);

        string? line = await _reader.ReadLineAsync(
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
        string authenticationProof = parts[2];

        if (!InternalProtocol.IsValidServerName(remoteServerName))
        {
            throw new UnauthorizedAccessException($"Invalid remote server name '{remoteServerName}'.");
        }

        if (_settings.AllowedServers.Count > 0 &&
            !_settings.AllowedServers.Any(allowedServer => string.Equals(allowedServer, remoteServerName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new UnauthorizedAccessException($"Server '{remoteServerName}' is not allowed to register with {_settings.ServerName}.");
        }

        if (!InternalProtocol.AuthenticationProofsMatch(
            _settings.RegistrationKey,
            remoteServerName,
            _settings.ServerName,
            challengeNonce,
            authenticationProof))
        {
            throw new UnauthorizedAccessException($"Invalid authentication proof for server '{remoteServerName}'.");
        }

        return remoteServerName;
    }

    // Method: TryProcessControlPacketAsync
    // Purpose: Executes the try process control packet operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - line: Line value supplied by the caller for this operation.
    // - latencyMonitor: Latency monitor value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous Boolean result that is true when try process control packet async succeeds or the requested condition is met.
    // Notes: This keeps the operation scoped to InternalServerSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task<bool> TryProcessControlPacketAsync(
        string remoteServerName,
        string line,
        InternalLatencyMonitor latencyMonitor,
        CancellationToken cancellationToken)
    {
        string[] parts = line.Split(' ', 3, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        if (parts.Length >= 2 && string.Equals(parts[0], InternalProtocol.Ping, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.TRACE, $"{_settings.ServerName} received PING packet from {remoteServerName}.", "InternalServerSession");
            await latencyMonitor.RespondToPingAsync(parts[1], cancellationToken);
            return true;
        }

        if (parts.Length >= 2 && string.Equals(parts[0], InternalProtocol.Pong, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.TRACE, $"{_settings.ServerName} received PONG packet from {remoteServerName}.", "InternalServerSession");
            latencyMonitor.RecordPong(parts[1]);
            return true;
        }

        if (parts.Length >= 2 && string.Equals(parts[0], InternalProtocol.ShutdownRequest, StringComparison.OrdinalIgnoreCase))
        {
            string reason = parts.Length == 3 ? parts[2] : "No reason provided.";
            await _callbacks.NotifyShutdownRequestedAsync(parts[1], reason, cancellationToken);
            return true;
        }

        return false;
    }

    // Method: MarkPacketReceived
    // Purpose: Executes the mark packet received operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to InternalServerSession so callers do not duplicate validation, protocol, or persistence rules.
    private void MarkPacketReceived()
    {
        Interlocked.Exchange(ref _lastPacketReceivedUtcTicks, DateTimeOffset.UtcNow.Ticks);
    }

    // Method: Read
    // Purpose: Retrieves read data for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - _disconnectRequested: Disconnect requested value supplied by the caller for this operation.
    // Returns: Returns the bool is disconnect requested => volatile. value produced by this operation.
    // Notes: This keeps the operation scoped to InternalServerSession so callers do not duplicate validation, protocol, or persistence rules.
    private bool IsDisconnectRequested => Volatile.Read(ref _disconnectRequested) == 1;
}
