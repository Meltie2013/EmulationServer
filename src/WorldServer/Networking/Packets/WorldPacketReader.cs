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
 * File overview: src/WorldServer/Networking/Packets/WorldPacketReader.cs
 * Documents the WorldPacketReader source file in the World of Warcraft packet opcode, reader, writer, and builder support area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.WorldServer.Networking.Packets;

/**
 * Owns the world packet reader behavior for the World of Warcraft packet opcode, reader, writer, and builder support layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public sealed class WorldPacketReader
{
    /**
     * Holds the private buffer state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly byte[] _buffer;
    /**
     * Holds the private offset state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private int _offset;

    /**
     * Initializes a new WorldPacketReader instance with the dependencies required by the World of Warcraft packet opcode, reader, writer, and builder support workflow.
     * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
     * Inputs used by this operation: buffer.
     */
    public WorldPacketReader(byte[] buffer)
    {
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    }

    /**
     * Stores the default remaining value used when the caller does not supply an override.
     * Centralizing the default keeps configuration and packet behavior consistent across the server process.
     */
    public int Remaining => _buffer.Length - _offset;

    /**
     * Parses read u int 8 input into the strongly typed server representation.
     * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
     */
    public byte ReadUInt8()
    {
        EnsureAvailable(1);
        return _buffer[_offset++];
    }

    /**
     * Parses read u int 16 input into the strongly typed server representation.
     * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
     */
    public ushort ReadUInt16()
    {
        EnsureAvailable(2);
        ushort value = BinaryPrimitives.ReadUInt16LittleEndian(_buffer.AsSpan(_offset, 2));
        _offset += 2;
        return value;
    }

    /**
     * Parses read u int 32 input into the strongly typed server representation.
     * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
     */
    public uint ReadUInt32()
    {
        EnsureAvailable(4);
        uint value = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.AsSpan(_offset, 4));
        _offset += 4;
        return value;
    }

    /**
     * Parses read u int 64 input into the strongly typed server representation.
     * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
     */
    public ulong ReadUInt64()
    {
        EnsureAvailable(8);
        ulong value = BinaryPrimitives.ReadUInt64LittleEndian(_buffer.AsSpan(_offset, 8));
        _offset += 8;
        return value;
    }

    /**
     * Parses read float input into the strongly typed server representation.
     * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
     */
    public float ReadFloat()
    {
        EnsureAvailable(4);
        float value = BinaryPrimitives.ReadSingleLittleEndian(_buffer.AsSpan(_offset, 4));
        _offset += 4;
        return value;
    }

    /**
     * Parses read bytes input into the strongly typed server representation.
     * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
     * Inputs used by this operation: length.
     */
    public byte[] ReadBytes(int length)
    {
        EnsureAvailable(length);
        byte[] value = _buffer.AsSpan(_offset, length).ToArray();
        _offset += length;
        return value;
    }

    /**
     * Parses read c string input into the strongly typed server representation.
     * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
     */
    public string ReadCString()
    {
        int terminator = Array.IndexOf(_buffer, (byte)0, _offset);
        if (terminator < 0)
        {
            throw new InvalidDataException("CString terminator was not found in world packet payload.");
        }

        string value = Encoding.UTF8.GetString(_buffer, _offset, terminator - _offset);
        _offset = terminator + 1;
        return value;
    }

    /**
     * Validates ensure available state before it is used by another server component.
     * Validation failures are raised as close to the source as possible so configuration, packet, and data problems are easier to diagnose.
     * Inputs used by this operation: count.
     */
    private void EnsureAvailable(int count)
    {
        if (count < 0 || Remaining < count)
        {
            throw new InvalidDataException("World packet payload ended before the expected field was available.");
        }
    }
}
