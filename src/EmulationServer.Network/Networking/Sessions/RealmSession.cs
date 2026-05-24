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

using System.Buffers;
using System.Net.Sockets;

using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;


/**
 * File overview: src/EmulationServer.Network/Networking/Sessions/RealmSession.cs
 * Documents the RealmSession source file in the internal server networking, packet framing, and peer/session lifecycle area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Network.Networking.Sessions;

/**
 * Owns the realm session behavior for the internal server networking, packet framing, and peer/session lifecycle layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public sealed class RealmSession
{
    /**
     * Defines the constant value for receive buffer size.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    private const int ReceiveBufferSize = 4096;

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
     * Holds the private session processor state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly IRealmSessionProcessor? _sessionProcessor;
    /**
     * Holds the private disconnect cancellation state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly CancellationTokenSource _disconnectCancellation = new();
    /**
     * Holds the private remote end point state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly string _remoteEndPoint;

    /**
     * Holds the private disconnect requested state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private int _disconnectRequested;

    /**
      * Gets or stores the id value used by RealmSession.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public Guid Id { get; } = Guid.NewGuid();

    /**
     * Initializes a new RealmSession instance with the dependencies required by the internal server networking, packet framing, and peer/session lifecycle workflow.
     * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
     * Inputs used by this operation: client, sessionProcessor.
     */
    public RealmSession(TcpClient client, IRealmSessionProcessor? sessionProcessor = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _stream = _client.GetStream();
        _sessionProcessor = sessionProcessor;
        _remoteEndPoint = _client.Client.RemoteEndPoint?.ToString() ?? "unknown endpoint";
    }

    /**
      * Processes incoming data and dispatches it to the correct subsystem handler.
      * The method is part of RealmSession and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        Logger.Write(LogType.NETWORK, $"Started processing session for {_remoteEndPoint}", nameof(RealmSession));

        using CancellationTokenSource linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _disconnectCancellation.Token);

        try
        {
            if (_sessionProcessor is not null)
            {
                RealmSessionContext context = new(Id, _client, _stream);
                await _sessionProcessor.ProcessAsync(context, linkedCancellation.Token);
                return;
            }

            await ProcessRawDebugSessionAsync(linkedCancellation.Token);
        }
        catch (OperationCanceledException) when (linkedCancellation.Token.IsCancellationRequested)
        {
            // Expected during server shutdown or explicit session disconnect.
        }
        catch (EndOfStreamException exception)
        {
            Logger.Write(LogType.NETWORK, exception.Message, nameof(RealmSession));
        }
        catch (IOException exception)
        {
            Logger.Write(LogType.NETWORK, $"Connection closed for {_remoteEndPoint}: {exception.Message}", nameof(RealmSession));
        }
        catch (SocketException exception)
        {
            Logger.Write(LogType.NETWORK, $"Socket closed for {_remoteEndPoint}: {exception.SocketErrorCode}", nameof(RealmSession));
        }
        catch (ObjectDisposedException) when (IsDisconnectRequested)
        {
            // Expected when the socket is disposed during shutdown.
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.CRITICAL, exception.ToString(), nameof(RealmSession));
        }
        finally
        {
            await DisconnectAsync();
        }
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

        Logger.Write(LogType.NETWORK, $"Ending session for {_remoteEndPoint}", nameof(RealmSession));

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

    /**
      * Processes incoming data and dispatches it to the correct subsystem handler.
      * The method is part of RealmSession and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task ProcessRawDebugSessionAsync(CancellationToken cancellationToken)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(ReceiveBufferSize);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                int received = await _stream.ReadAsync(buffer.AsMemory(0, ReceiveBufferSize), cancellationToken);
                if (received == 0)
                {
                    Logger.Write(LogType.NETWORK, $"Client disconnected from {_remoteEndPoint}", nameof(RealmSession));
                    break;
                }

                Logger.Write(LogType.DEBUG, $"Received {received} byte(s) from {_remoteEndPoint}", nameof(RealmSession));
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /**
      * Gets or stores the is disconnect requested value used by RealmSession.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    private bool IsDisconnectRequested => Volatile.Read(ref _disconnectRequested) == 1;
}
