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

using System.Net.Sockets;

using EmulationServer.Network.Configuration;
using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Health;
using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;


/**
 * File overview: src/EmulationServer.Network/Networking/Sessions/InternalServerSession.cs
 * Documents the InternalServerSession source file in the internal server networking, packet framing, and peer/session lifecycle area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Network.Networking.Sessions;

/**
  * Represents one authenticated internal server connection and dispatches received protocol packets.
  * It stores per-connection runtime state and provides the operations needed by session handlers.
  */
public sealed class InternalServerSession
{
    /**
     * Holds the private client state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly TcpClient _client;
    /**
     * Holds the private stream state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly NetworkStream _stream;
    /**
     * Holds the private reader state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly InternalProtocolReader _reader;
    /**
     * Holds the private send lock state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    /**
     * Holds the private disconnect cancellation state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly CancellationTokenSource _disconnectCancellation = new();
    /**
     * Holds the private settings state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly InternalNetworkSettings _settings;
    /**
     * Holds the private callbacks state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly InternalNetworkCallbacks _callbacks;
    /**
     * Holds the private remote end point state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly string _remoteEndPoint;

    /**
     * Holds the private last packet received utc ticks state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private long _lastPacketReceivedUtcTicks;
    /**
     * Holds the private disconnect requested state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private int _disconnectRequested;

    /**
      * Gets or stores the id value used by InternalServerSession.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public Guid Id { get; } = Guid.NewGuid();

    /**
      * Gets or stores the remote server name value used by InternalServerSession.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string? RemoteServerName { get; private set; }

    /**
      * Gets or stores the last packet received utc value used by InternalServerSession.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public DateTimeOffset LastPacketReceivedUtc => new(Interlocked.Read(ref _lastPacketReceivedUtcTicks), TimeSpan.Zero);

    /**
      * Gets or stores the is authenticated value used by InternalServerSession.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(RemoteServerName);

    /**
     * Initializes a new InternalServerSession instance with the dependencies required by the internal server networking, packet framing, and peer/session lifecycle workflow.
     * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
     * Inputs used by this operation: settings, client, callbacks.
     */
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
        _reader = new InternalProtocolReader(_stream);
        _remoteEndPoint = _client.Client.RemoteEndPoint?.ToString() ?? "unknown endpoint";
        _lastPacketReceivedUtcTicks = DateTimeOffset.UtcNow.Ticks;
    }

    /**
      * Processes incoming data and dispatches it to the correct subsystem handler.
      * The method is part of InternalServerSession and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
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
                string? line = await _reader.ReadLineAsync(
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

    /**
      * Sends a protocol message or status update to a connected peer.
      * The method is part of InternalServerSession and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
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

    /**
     * Performs the disconnect operation for the internal server networking, packet framing, and peer/session lifecycle workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
     */
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

        _reader.Dispose();
        _stream.Dispose();
        _client.Dispose();
        _sendLock.Dispose();
        _disconnectCancellation.Dispose();

        return Task.CompletedTask;
    }

    /**
     * Performs the request and validate authentication operation for the internal server networking, packet framing, and peer/session lifecycle workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: cancellationToken.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
     */
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

    /**
      * Processes incoming data and dispatches it to the correct subsystem handler.
      * The method is part of InternalServerSession and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
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

    /**
     * Performs the mark packet received operation for the internal server networking, packet framing, and peer/session lifecycle workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     */
    private void MarkPacketReceived()
    {
        Interlocked.Exchange(ref _lastPacketReceivedUtcTicks, DateTimeOffset.UtcNow.Ticks);
    }

    /**
      * Gets or stores the is disconnect requested value used by InternalServerSession.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    private bool IsDisconnectRequested => Volatile.Read(ref _disconnectRequested) == 1;
}
