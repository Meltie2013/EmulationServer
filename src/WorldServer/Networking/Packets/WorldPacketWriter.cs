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
// File: src/WorldServer/Networking/Packets/WorldPacketWriter.cs
// Purpose: Contains world packet writer code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Buffers.Binary;
using System.Text;

namespace EmulationServer.WorldServer.Networking.Packets;

// Type: WorldPacketWriter
// Purpose: Provides world packet writer behavior for the world server gameplay, session, and character runtime layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class WorldPacketWriter
{

    // Field: Stores the buffer state used by the world server gameplay, session, and character runtime layer.
    // Value: current buffer backing value maintained by the owning type.
    private readonly List<byte> _buffer = [];

    // Property: Gets or sets the count value used by the world server gameplay, session, and character runtime layer.
    // Value: count value exposed by the owning type.
    public int Count => _buffer.Count;

    // Method: WriteUInt8
    // Purpose: Builds or writes write U int8 output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketWriter so callers do not duplicate validation, protocol, or persistence rules.
    public void WriteUInt8(byte value)
    {
        _buffer.Add(value);
    }

    // Method: WriteUInt16
    // Purpose: Builds or writes write U int16 output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketWriter so callers do not duplicate validation, protocol, or persistence rules.
    public void WriteUInt16(ushort value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
        _buffer.AddRange(buffer.ToArray());
    }

    // Method: WriteUInt32
    // Purpose: Builds or writes write U int32 output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketWriter so callers do not duplicate validation, protocol, or persistence rules.
    public void WriteUInt32(uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
        _buffer.AddRange(buffer.ToArray());
    }

    // Method: WriteUInt64
    // Purpose: Builds or writes write U int64 output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketWriter so callers do not duplicate validation, protocol, or persistence rules.
    public void WriteUInt64(ulong value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
        _buffer.AddRange(buffer.ToArray());
    }

    // Method: WriteFloat
    // Purpose: Builds or writes write float output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketWriter so callers do not duplicate validation, protocol, or persistence rules.
    public void WriteFloat(float value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteSingleLittleEndian(buffer, value);
        _buffer.AddRange(buffer.ToArray());
    }

    // Method: WriteCString
    // Purpose: Builds or writes write C string output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketWriter so callers do not duplicate validation, protocol, or persistence rules.
    public void WriteCString(string value)
    {
        _buffer.AddRange(Encoding.UTF8.GetBytes(value));
        _buffer.Add(0);
    }

    // Method: WriteBytes
    // Purpose: Builds or writes write bytes output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketWriter so callers do not duplicate validation, protocol, or persistence rules.
    public void WriteBytes(ReadOnlySpan<byte> value)
    {
        _buffer.AddRange(value.ToArray());
    }

    // Method: ToArray
    // Purpose: Executes the to array operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketWriter so callers do not duplicate validation, protocol, or persistence rules.
    public byte[] ToArray()
    {
        return [.. _buffer];
    }
}
