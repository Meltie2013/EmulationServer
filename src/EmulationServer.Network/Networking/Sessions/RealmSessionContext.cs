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
// File: src/EmulationServer.Network/Networking/Sessions/RealmSessionContext.cs
// Purpose: Contains realm session context code for the packet serialization, socket transport, and protocol framing layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Net;
using System.Net.Sockets;

namespace EmulationServer.Network.Networking.Sessions;

// Type: RealmSessionContext
// Purpose: Provides realm session context behavior for the packet serialization, socket transport, and protocol framing layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class RealmSessionContext
{

    // Field: Stores the client state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current client backing value maintained by the owning type.
    private readonly TcpClient _client;

    // Field: Stores the stream state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current stream backing value maintained by the owning type.
    private readonly NetworkStream _stream;

    // Constructor: RealmSessionContext
    // Purpose: Initializes a new RealmSessionContext instance with dependencies and values required by the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - sessionId: Session ID identifier used to select the exact record, object, or runtime owner.
    // - client: Client value supplied by the caller for this operation.
    // - stream: Stream value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmSessionContext so callers do not duplicate validation, protocol, or persistence rules.
    public RealmSessionContext(Guid sessionId, TcpClient client, NetworkStream stream)
    {
        Id = sessionId;
        _client = client ?? throw new ArgumentNullException();
        _stream = stream ?? throw new ArgumentNullException();

        RemoteEndPoint = _client.Client.RemoteEndPoint?.ToString() ?? "unknown endpoint";
        RemoteAddress = (_client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "0.0.0.0";
    }

    // Property: Gets or sets the ID value used by the packet serialization, socket transport, and protocol framing layer.
    // Value: ID value exposed by the owning type.
    public Guid Id { get; }

    // Property: Gets or sets the remote end point value used by the packet serialization, socket transport, and protocol framing layer.
    // Value: remote end point value exposed by the owning type.
    public string RemoteEndPoint { get; }

    // Property: Gets or sets the remote address value used by the packet serialization, socket transport, and protocol framing layer.
    // Value: remote address value exposed by the owning type.
    public string RemoteAddress { get; }

    // Method: ReadByteAsync
    // Purpose: Retrieves read byte data for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to RealmSessionContext so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async ValueTask<byte> ReadByteAsync(CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[1];
        await ReadExactlyAsync(buffer, cancellationToken);
        return buffer[0];
    }

    // Method: ReadBytesAsync
    // Purpose: Retrieves read bytes data for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - length: Length value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to RealmSessionContext so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async ValueTask<byte[]> ReadBytesAsync(int length, CancellationToken cancellationToken)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(null, "Read length cannot be negative.");
        }

        byte[] buffer = new byte[length];
        await ReadExactlyAsync(buffer, cancellationToken);
        return buffer;
    }

    // Method: ReadExactlyAsync
    // Purpose: Retrieves read exactly data for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - bytebuffer: Bytebuffer value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmSessionContext so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async ValueTask ReadExactlyAsync(byte[] buffer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        int offset = 0;
        while (offset < buffer.Length)
        {
            int received = await _stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken);
            if (received == 0)
            {
                throw new EndOfStreamException($"Client disconnected from {RemoteEndPoint}.");
            }

            offset += received;
        }
    }

    // Method: WriteAsync
    // Purpose: Builds or writes write output for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - data: Data bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmSessionContext so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        await _stream.WriteAsync(data, cancellationToken);
        await _stream.FlushAsync(cancellationToken);
    }

    // Method: AllowTerminalResponseDeliveryAsync
    // Purpose: Executes the allow terminal response delivery operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - deliveryDelay: Delivery delay value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmSessionContext so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public static async Task AllowTerminalResponseDeliveryAsync(TimeSpan deliveryDelay, CancellationToken cancellationToken)
    {
        if (deliveryDelay <= TimeSpan.Zero)
        {
            return;
        }

        try
        {
            await Task.Delay(deliveryDelay, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {

        }
    }
}
