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

using System.Net;
using System.Net.Sockets;


/**
 * File overview: src/EmulationServer.Network/Networking/Sessions/RealmSessionContext.cs
 * Documents the RealmSessionContext source file in the internal server networking, packet framing, and peer/session lifecycle area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Network.Networking.Sessions;

/**
 * Owns the realm session context behavior for the internal server networking, packet framing, and peer/session lifecycle layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public sealed class RealmSessionContext
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
     * Initializes a new RealmSessionContext instance with the dependencies required by the internal server networking, packet framing, and peer/session lifecycle workflow.
     * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
     * Inputs used by this operation: sessionId, client, stream.
     */
    public RealmSessionContext(Guid sessionId, TcpClient client, NetworkStream stream)
    {
        Id = sessionId;
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));

        RemoteEndPoint = _client.Client.RemoteEndPoint?.ToString() ?? "unknown endpoint";
        RemoteAddress = (_client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "0.0.0.0";
    }

    /**
      * Gets or stores the id value used by RealmSessionContext.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public Guid Id { get; }

    /**
      * Gets or stores the remote end point value used by RealmSessionContext.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string RemoteEndPoint { get; }

    /**
      * Gets or stores the remote address value used by RealmSessionContext.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string RemoteAddress { get; }

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of RealmSessionContext and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async ValueTask<byte> ReadByteAsync(CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[1];
        await ReadExactlyAsync(buffer, cancellationToken);
        return buffer[0];
    }

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of RealmSessionContext and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async ValueTask<byte[]> ReadBytesAsync(int length, CancellationToken cancellationToken)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Read length cannot be negative.");
        }

        byte[] buffer = new byte[length];
        await ReadExactlyAsync(buffer, cancellationToken);
        return buffer;
    }

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of RealmSessionContext and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
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

    /**
     * Writes write data to the target packet, stream, or persistent store.
     * The method keeps binary layout and serialization rules centralized for easier packet review and compatibility fixes.
     * Inputs used by this operation: data, cancellationToken.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
     */
    public ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        return _stream.WriteAsync(data, cancellationToken);
    }
}
