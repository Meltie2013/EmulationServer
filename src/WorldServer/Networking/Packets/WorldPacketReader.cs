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
// File: src/WorldServer/Networking/Packets/WorldPacketReader.cs
// Purpose: Contains world packet reader code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Buffers.Binary;
using System.Text;

namespace EmulationServer.WorldServer.Networking.Packets;

// Type: WorldPacketReader
// Purpose: Provides world packet reader behavior for the world server gameplay, session, and character runtime layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class WorldPacketReader
{

    // Field: Stores the buffer state used by the world server gameplay, session, and character runtime layer.
    // Value: current buffer backing value maintained by the owning type.
    private readonly ReadOnlyMemory<byte> _buffer;

    // Field: Stores the offset state used by the world server gameplay, session, and character runtime layer.
    // Value: current offset backing value maintained by the owning type.
    private int _offset;

    // Constructor: WorldPacketReader
    // Purpose: Initializes a new WorldPacketReader instance with dependencies and values required by the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - bytebuffer: Bytebuffer value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketReader so callers do not duplicate validation, protocol, or persistence rules.
    public WorldPacketReader(byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        _buffer = buffer;
    }

    // Constructor: WorldPacketReader
    // Purpose: Initializes a new WorldPacketReader instance with dependencies and values required by the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - buffer: Buffer bytes or structured payload consumed by this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketReader so callers do not duplicate validation, protocol, or persistence rules.
    public WorldPacketReader(ReadOnlyMemory<byte> buffer)
    {
        _buffer = buffer;
    }

    // Property: Gets or sets the remaining value used by the world server gameplay, session, and character runtime layer.
    // Value: remaining value exposed by the owning type.
    public int Remaining => _buffer.Length - _offset;

    // Method: ReadUInt8
    // Purpose: Retrieves read U int8 data for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the byte value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketReader so callers do not duplicate validation, protocol, or persistence rules.
    public byte ReadUInt8()
    {
        EnsureAvailable(1);
        return _buffer.Span[_offset++];
    }

    // Method: ReadUInt16
    // Purpose: Retrieves read U int16 data for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the ushort value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketReader so callers do not duplicate validation, protocol, or persistence rules.
    public ushort ReadUInt16()
    {
        EnsureAvailable(2);
        ushort value = BinaryPrimitives.ReadUInt16LittleEndian(_buffer.Span.Slice(_offset, 2));
        _offset += 2;
        return value;
    }

    // Method: ReadUInt32
    // Purpose: Retrieves read U int32 data for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketReader so callers do not duplicate validation, protocol, or persistence rules.
    public uint ReadUInt32()
    {
        EnsureAvailable(4);
        uint value = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.Span.Slice(_offset, 4));
        _offset += 4;
        return value;
    }

    // Method: ReadUInt64
    // Purpose: Retrieves read U int64 data for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the ulong value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketReader so callers do not duplicate validation, protocol, or persistence rules.
    public ulong ReadUInt64()
    {
        EnsureAvailable(8);
        ulong value = BinaryPrimitives.ReadUInt64LittleEndian(_buffer.Span.Slice(_offset, 8));
        _offset += 8;
        return value;
    }

    // Method: ReadFloat
    // Purpose: Retrieves read float data for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the float value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketReader so callers do not duplicate validation, protocol, or persistence rules.
    public float ReadFloat()
    {
        EnsureAvailable(4);
        float value = BinaryPrimitives.ReadSingleLittleEndian(_buffer.Span.Slice(_offset, 4));
        _offset += 4;
        return value;
    }

    // Method: ReadBytes
    // Purpose: Retrieves read bytes data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - length: Length value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketReader so callers do not duplicate validation, protocol, or persistence rules.
    public byte[] ReadBytes(int length)
    {
        EnsureAvailable(length);
        byte[] value = _buffer.Span.Slice(_offset, length).ToArray();
        _offset += length;
        return value;
    }

    // Method: ReadCString
    // Purpose: Retrieves read C string data for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketReader so callers do not duplicate validation, protocol, or persistence rules.
    public string ReadCString()
    {
        ReadOnlySpan<byte> remaining = _buffer.Span[_offset..];
        int terminatorOffset = remaining.IndexOf((byte)0);
        if (terminatorOffset < 0)
        {
            throw new InvalidDataException("CString terminator was not found in world packet payload.");
        }

        string value = Encoding.UTF8.GetString(remaining[..terminatorOffset]);
        _offset += terminatorOffset + 1;
        return value;
    }

    // Method: EnsureAvailable
    // Purpose: Validates or evaluates ensure available rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - count: Count value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketReader so callers do not duplicate validation, protocol, or persistence rules.
    private void EnsureAvailable(int count)
    {
        if (count < 0 || Remaining < count)
        {
            throw new InvalidDataException("World packet payload ended before the expected field was available.");
        }
    }
}
