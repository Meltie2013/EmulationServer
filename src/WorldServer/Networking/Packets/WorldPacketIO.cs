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

using System.Buffers.Binary;
using System.Net.Sockets;

/**
 * File overview: src/WorldServer/Networking/Packets/WorldPacketIO.cs
 * Documents the WorldPacketIO source file in the World of Warcraft packet opcode, reader, writer, and builder support area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.WorldServer.Networking.Packets;

/**
 * Owns the world packet io behavior for the World of Warcraft packet opcode, reader, writer, and builder support layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public static class WorldPacketIO
{
    /**
     * Parses read client packet input into the strongly typed server representation.
     * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
     * Inputs used by this operation: stream, crypt, maximumPacketSize, cancellationToken.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
     */
    public static async ValueTask<WorldPacket> ReadClientPacketAsync(
        NetworkStream stream,
        WorldHeaderCrypt? crypt,
        int maximumPacketSize,
        CancellationToken cancellationToken)
    {
        byte[] header = new byte[6];
        await ReadExactlyAsync(stream, header, cancellationToken);

        if (crypt is not null)
        {
            crypt.Decrypt(header);
        }

        ushort packetSize = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(0, 2));
        if (packetSize < 4 || packetSize > maximumPacketSize)
        {
            throw new InvalidDataException($"Invalid client world packet size: {packetSize}.");
        }

        uint opcodeValue = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(2, 4));
        int payloadLength = packetSize - 4;
        byte[] payload = new byte[payloadLength];

        if (payloadLength > 0)
        {
            await ReadExactlyAsync(stream, payload, cancellationToken);
        }

        return new WorldPacket((WorldOpcode)opcodeValue, payload);
    }

    /**
     * Writes write server packet data to the target packet, stream, or persistent store.
     * The method keeps binary layout and serialization rules centralized for easier packet review and compatibility fixes.
     * Inputs used by this operation: stream, opcode, payload, crypt, cancellationToken.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
     */
    public static async ValueTask WriteServerPacketAsync(
        NetworkStream stream,
        WorldOpcode opcode,
        ReadOnlyMemory<byte> payload,
        WorldHeaderCrypt? crypt,
        CancellationToken cancellationToken)
    {
        int packetSize = payload.Length + 2;
        if (packetSize > ushort.MaxValue)
        {
            throw new InvalidOperationException($"Server world packet is too large: {packetSize}.");
        }

        byte[] header = new byte[4];
        BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(0, 2), (ushort)packetSize);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(2, 2), (ushort)opcode);

        if (crypt is not null)
        {
            crypt.Encrypt(header);
        }

        await stream.WriteAsync(header, cancellationToken);
        if (payload.Length > 0)
        {
            await stream.WriteAsync(payload, cancellationToken);
        }
    }

    /**
     * Parses read exactly input into the strongly typed server representation.
     * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
     * Inputs used by this operation: stream, buffer, cancellationToken.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
     */
    private static async ValueTask ReadExactlyAsync(NetworkStream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            int received = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken);
            if (received == 0)
            {
                throw new EndOfStreamException("World client disconnected.");
            }

            offset += received;
        }
    }
}
