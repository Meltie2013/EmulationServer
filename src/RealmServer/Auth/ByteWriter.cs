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
  * File overview: src/RealmServer/Auth/ByteWriter.cs
  * This file belongs to the realm authentication, build validation, and realm list packet creation portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.RealmServer.Auth;

/**
  * Represents the byte writer component in the realm authentication, build validation, and realm list packet creation area.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public sealed class ByteWriter
{
    /**
      * Stores the buffer dependency or runtime value for ByteWriter.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly List<byte> _buffer = [];

    /**
      * Gets or stores the count value used by ByteWriter.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int Count => _buffer.Count;

    /**
      * Writes the supplied data to the target destination using the project protocol or file format.
      * The method is part of ByteWriter and keeps this workflow isolated from the caller.
      */
    public void WriteUInt8(byte value)
    {
        _buffer.Add(value);
    }

    /**
      * Writes the supplied data to the target destination using the project protocol or file format.
      * The method is part of ByteWriter and keeps this workflow isolated from the caller.
      */
    public void WriteUInt16(ushort value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
        _buffer.AddRange(buffer.ToArray());
    }

    /**
      * Writes the supplied data to the target destination using the project protocol or file format.
      * The method is part of ByteWriter and keeps this workflow isolated from the caller.
      */
    public void WriteUInt32(uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
        _buffer.AddRange(buffer.ToArray());
    }

    /**
      * Writes the supplied data to the target destination using the project protocol or file format.
      * The method is part of ByteWriter and keeps this workflow isolated from the caller.
      */
    public void WriteFloat(float value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteSingleLittleEndian(buffer, value);
        _buffer.AddRange(buffer.ToArray());
    }

    /**
      * Writes the supplied data to the target destination using the project protocol or file format.
      * The method is part of ByteWriter and keeps this workflow isolated from the caller.
      */
    public void WriteCString(string value)
    {
        _buffer.AddRange(Encoding.UTF8.GetBytes(value));
        _buffer.Add(0);
    }

    /**
      * Writes the supplied data to the target destination using the project protocol or file format.
      * The method is part of ByteWriter and keeps this workflow isolated from the caller.
      */
    public void WriteBytes(ReadOnlySpan<byte> value)
    {
        _buffer.AddRange(value.ToArray());
    }

    /**
      * Performs the to array operation for ByteWriter.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
    public byte[] ToArray()
    {
        return [.. _buffer];
    }
}
