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
// File: src/WorldServer/Networking/Packets/WorldPacketIO.cs
// Purpose: Contains world packet IO code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Buffers;
using System.Buffers.Binary;
using System.Net.Sockets;

namespace EmulationServer.WorldServer.Networking.Packets;

// Type: WorldPacketIO
// Purpose: Provides world packet IO behavior for the world server gameplay, session, and character runtime layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class WorldPacketIO
{

    // Method: ReadClientPacketAsync
    // Purpose: Retrieves read client packet data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - stream: Stream value supplied by the caller for this operation.
    // - crypt: Crypt value supplied by the caller for this operation.
    // - maximumPacketSize: Maximum packet size value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to WorldPacketIO so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public static async ValueTask<WorldPacket> ReadClientPacketAsync(
        NetworkStream stream,
        WorldHeaderCrypt? crypt,
        int maximumPacketSize,
        CancellationToken cancellationToken)
    {
        byte[] header = ArrayPool<byte>.Shared.Rent(6);
        try
        {
            await ReadExactlyAsync(stream, header.AsMemory(0, 6), cancellationToken);

            ushort packetSize;
            uint opcodeValue;
            int payloadLength;
            {
                Span<byte> headerSpan = header.AsSpan(0, 6);
                if (crypt is not null)
                {
                    crypt.Decrypt(headerSpan);
                }

                packetSize = BinaryPrimitives.ReadUInt16BigEndian(headerSpan[..2]);
                if (packetSize < 4 || packetSize > maximumPacketSize)
                {
                    throw new InvalidDataException($"Invalid client world packet size: {packetSize}.");
                }

                opcodeValue = BinaryPrimitives.ReadUInt32LittleEndian(headerSpan.Slice(2, 4));
                payloadLength = packetSize - 4;
            }

            byte[] payload = new byte[payloadLength];

            if (payloadLength > 0)
            {
                await ReadExactlyAsync(stream, payload, cancellationToken);
            }

            return new WorldPacket((WorldOpcode)opcodeValue, payload);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(header);
        }
    }

    // Method: WriteServerPacketAsync
    // Purpose: Builds or writes write server packet output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - stream: Stream value supplied by the caller for this operation.
    // - opcode: Opcode value supplied by the caller for this operation.
    // - payload: Payload bytes or structured payload consumed by this operation.
    // - crypt: Crypt value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldPacketIO so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

        byte[] frame = RentServerFrame(opcode, payload, crypt, (ushort)packetSize, out int frameLength);
        try
        {
            await stream.WriteAsync(frame.AsMemory(0, frameLength), cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(frame);
        }
    }

    // Method: RentServerFrame
    // Purpose: Executes the rent server frame operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - opcode: Opcode value supplied by the caller for this operation.
    // - payload: Payload bytes or structured payload consumed by this operation.
    // - crypt: Crypt value supplied by the caller for this operation.
    // - packetSize: Packet size value supplied by the caller for this operation.
    // - frameLength: Frame length value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketIO so callers do not duplicate validation, protocol, or persistence rules.
    private static byte[] RentServerFrame(
        WorldOpcode opcode,
        ReadOnlyMemory<byte> payload,
        WorldHeaderCrypt? crypt,
        ushort packetSize,
        out int frameLength)
    {
        frameLength = 4 + payload.Length;
        byte[] frame = ArrayPool<byte>.Shared.Rent(frameLength);

        Span<byte> header = frame.AsSpan(0, 4);
        BinaryPrimitives.WriteUInt16BigEndian(header[..2], packetSize);
        BinaryPrimitives.WriteUInt16LittleEndian(header.Slice(2, 2), (ushort)opcode);

        if (crypt is not null)
        {
            crypt.Encrypt(header);
        }

        if (payload.Length > 0)
        {
            payload.Span.CopyTo(frame.AsSpan(4, payload.Length));
        }

        return frame;
    }

    // Method: ReadExactlyAsync
    // Purpose: Retrieves read exactly data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - stream: Stream value supplied by the caller for this operation.
    // - bytebuffer: Bytebuffer value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldPacketIO so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static ValueTask ReadExactlyAsync(NetworkStream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        return ReadExactlyAsync(stream, buffer.AsMemory(), cancellationToken);
    }

    // Method: ReadExactlyAsync
    // Purpose: Retrieves read exactly data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - stream: Stream value supplied by the caller for this operation.
    // - buffer: Buffer bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldPacketIO so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async ValueTask ReadExactlyAsync(NetworkStream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            int received = await stream.ReadAsync(buffer.Slice(offset), cancellationToken);
            if (received == 0)
            {
                throw new EndOfStreamException("World client disconnected.");
            }

            offset += received;
        }
    }
}
