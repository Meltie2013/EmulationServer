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
// File: src/EmulationServer.Network/Networking/Sessions/RealmSession.cs
// Purpose: Contains realm session code for the packet serialization, socket transport, and protocol framing layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Buffers;
using System.Net.Sockets;

using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Network.Networking.Sessions;

// Type: RealmSession
// Purpose: Provides realm session behavior for the packet serialization, socket transport, and protocol framing layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class RealmSession
{

    // Constant: Defines the receive buffer size constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed receive buffer size value used anywhere this rule or protocol value is needed.
    private const int ReceiveBufferSize = 4096;

    // Field: Stores the client state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current client backing value maintained by the owning type.
    private readonly TcpClient _client;

    // Field: Stores the stream state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current stream backing value maintained by the owning type.
    private readonly NetworkStream _stream;

    // Field: Stores the session processor state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current session processor backing value maintained by the owning type.
    private readonly IRealmSessionProcessor? _sessionProcessor;

    private readonly CancellationTokenSource _disconnectCancellation = new();

    // Field: Stores the remote end point state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current remote end point backing value maintained by the owning type.
    private readonly string _remoteEndPoint;

    // Field: Stores the disconnect requested state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current disconnect requested backing value maintained by the owning type.
    private int _disconnectRequested;

    // Method: NewGuid
    // Purpose: Executes the new GUID operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters: none.
    // Returns: Returns the GUID ID { get; } = guid. value produced by this operation.
    // Notes: This keeps the operation scoped to RealmSession so callers do not duplicate validation, protocol, or persistence rules.
    public Guid Id { get; } = Guid.NewGuid();

    // Constructor: RealmSession
    // Purpose: Initializes a new RealmSession instance with dependencies and values required by the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - client: Client value supplied by the caller for this operation.
    // - sessionProcessor: Session processor value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmSession so callers do not duplicate validation, protocol, or persistence rules.
    public RealmSession(TcpClient client, IRealmSessionProcessor? sessionProcessor = null)
    {
        _client = client ?? throw new ArgumentNullException();
        _stream = _client.GetStream();
        _sessionProcessor = sessionProcessor;
        _remoteEndPoint = _client.Client.RemoteEndPoint?.ToString() ?? "unknown endpoint";
    }

    // Method: ProcessAsync
    // Purpose: Executes the process operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        Logger.Write(LogType.NETWORK, $"Started processing session for {_remoteEndPoint}", "RealmSession");

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

        }
        catch (EndOfStreamException exception)
        {
            Logger.Write(LogType.NETWORK, exception.Message, "RealmSession");
        }
        catch (IOException exception)
        {
            Logger.Write(LogType.NETWORK, $"Connection closed for {_remoteEndPoint}: {exception.Message}", "RealmSession");
        }
        catch (SocketException exception)
        {
            Logger.Write(LogType.NETWORK, $"Socket closed for {_remoteEndPoint}: {exception.SocketErrorCode}", "RealmSession");
        }
        catch (ObjectDisposedException) when (IsDisconnectRequested)
        {

        }
        catch (Exception exception)
        {
            Logger.Write(LogType.CRITICAL, exception.ToString(), "RealmSession");
        }
        finally
        {
            await DisconnectAsync();
        }
    }

    // Method: DisconnectAsync
    // Purpose: Executes the disconnect operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters: none.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task DisconnectAsync()
    {
        if (Interlocked.Exchange(ref _disconnectRequested, 1) == 1)
        {
            return;
        }

        Logger.Write(LogType.NETWORK, $"Ending session for {_remoteEndPoint}", "RealmSession");

        try
        {
            _disconnectCancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {

        }

        try
        {
            await _stream.FlushAsync(CancellationToken.None);
        }
        catch
        {

        }

        try
        {
            _client.Client.Shutdown(SocketShutdown.Send);
        }
        catch (SocketException)
        {

        }
        catch (ObjectDisposedException)
        {

        }

        _stream.Dispose();
        _client.Dispose();
        _disconnectCancellation.Dispose();
    }

    // Method: ProcessRawDebugSessionAsync
    // Purpose: Executes the process raw debug session operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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
                    Logger.Write(LogType.NETWORK, $"Client disconnected from {_remoteEndPoint}", "RealmSession");
                    break;
                }

                Logger.Write(LogType.DEBUG, $"Received {received} byte(s) from {_remoteEndPoint}", "RealmSession");
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    // Method: Read
    // Purpose: Retrieves read data for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - _disconnectRequested: Disconnect requested value supplied by the caller for this operation.
    // Returns: Returns the bool is disconnect requested => volatile. value produced by this operation.
    // Notes: This keeps the operation scoped to RealmSession so callers do not duplicate validation, protocol, or persistence rules.
    private bool IsDisconnectRequested => Volatile.Read(ref _disconnectRequested) == 1;
}
