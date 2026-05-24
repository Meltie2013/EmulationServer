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
using System.Text;

/**
 * File overview: src/WorldServer/Networking/Packets/WorldPacketWriter.cs
 * Documents the WorldPacketWriter source file in the World of Warcraft packet opcode, reader, writer, and builder support area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.WorldServer.Networking.Packets;

/**
 * Owns the world packet writer behavior for the World of Warcraft packet opcode, reader, writer, and builder support layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public sealed class WorldPacketWriter
{
    /**
     * Holds the private buffer state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly List<byte> _buffer = [];

    /**
     * Stores the default count value used when the caller does not supply an override.
     * Centralizing the default keeps configuration and packet behavior consistent across the server process.
     */
    public int Count => _buffer.Count;

    /**
     * Writes write u int 8 data to the target packet, stream, or persistent store.
     * The method keeps binary layout and serialization rules centralized for easier packet review and compatibility fixes.
     * Inputs used by this operation: value.
     */
    public void WriteUInt8(byte value)
    {
        _buffer.Add(value);
    }

    /**
     * Writes write u int 16 data to the target packet, stream, or persistent store.
     * The method keeps binary layout and serialization rules centralized for easier packet review and compatibility fixes.
     * Inputs used by this operation: value.
     */
    public void WriteUInt16(ushort value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
        _buffer.AddRange(buffer.ToArray());
    }

    /**
     * Writes write u int 32 data to the target packet, stream, or persistent store.
     * The method keeps binary layout and serialization rules centralized for easier packet review and compatibility fixes.
     * Inputs used by this operation: value.
     */
    public void WriteUInt32(uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
        _buffer.AddRange(buffer.ToArray());
    }

    /**
     * Writes write u int 64 data to the target packet, stream, or persistent store.
     * The method keeps binary layout and serialization rules centralized for easier packet review and compatibility fixes.
     * Inputs used by this operation: value.
     */
    public void WriteUInt64(ulong value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
        _buffer.AddRange(buffer.ToArray());
    }

    /**
     * Writes write float data to the target packet, stream, or persistent store.
     * The method keeps binary layout and serialization rules centralized for easier packet review and compatibility fixes.
     * Inputs used by this operation: value.
     */
    public void WriteFloat(float value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteSingleLittleEndian(buffer, value);
        _buffer.AddRange(buffer.ToArray());
    }

    /**
     * Writes write c string data to the target packet, stream, or persistent store.
     * The method keeps binary layout and serialization rules centralized for easier packet review and compatibility fixes.
     * Inputs used by this operation: value.
     */
    public void WriteCString(string value)
    {
        _buffer.AddRange(Encoding.UTF8.GetBytes(value));
        _buffer.Add(0);
    }

    /**
     * Writes write bytes data to the target packet, stream, or persistent store.
     * The method keeps binary layout and serialization rules centralized for easier packet review and compatibility fixes.
     * Inputs used by this operation: value.
     */
    public void WriteBytes(ReadOnlySpan<byte> value)
    {
        _buffer.AddRange(value.ToArray());
    }

    /**
     * Performs the to array operation for the World of Warcraft packet opcode, reader, writer, and builder support workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     */
    public byte[] ToArray()
    {
        return [.. _buffer];
    }
}
