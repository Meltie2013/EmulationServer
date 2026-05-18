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
  * This file belongs to the network session lifecycle and packet dispatch portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Network.Networking.Sessions;

/**
  * Represents the realm session context component in the network session lifecycle and packet dispatch area.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public sealed class RealmSessionContext
{
    /**
      * Stores the client dependency or runtime value for RealmSessionContext.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly TcpClient _client;
    /**
      * Stores the stream dependency or runtime value for RealmSessionContext.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly NetworkStream _stream;

    /**
      * Creates a new RealmSessionContext instance and stores the dependencies required by the component.
      * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
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
      * Writes the supplied data to the target destination using the project protocol or file format.
      * The method is part of RealmSessionContext and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        return _stream.WriteAsync(data, cancellationToken);
    }
}
