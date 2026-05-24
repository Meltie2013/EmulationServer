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

namespace EmulationServer.WorldServer.Networking.Packets;

public sealed class WorldPacketReader
{
    private readonly byte[] _buffer;
    private int _offset;

    public WorldPacketReader(byte[] buffer)
    {
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    }

    public int Remaining => _buffer.Length - _offset;

    public byte ReadUInt8()
    {
        EnsureAvailable(1);
        return _buffer[_offset++];
    }

    public ushort ReadUInt16()
    {
        EnsureAvailable(2);
        ushort value = BinaryPrimitives.ReadUInt16LittleEndian(_buffer.AsSpan(_offset, 2));
        _offset += 2;
        return value;
    }

    public uint ReadUInt32()
    {
        EnsureAvailable(4);
        uint value = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.AsSpan(_offset, 4));
        _offset += 4;
        return value;
    }

    public ulong ReadUInt64()
    {
        EnsureAvailable(8);
        ulong value = BinaryPrimitives.ReadUInt64LittleEndian(_buffer.AsSpan(_offset, 8));
        _offset += 8;
        return value;
    }

    public float ReadFloat()
    {
        EnsureAvailable(4);
        float value = BinaryPrimitives.ReadSingleLittleEndian(_buffer.AsSpan(_offset, 4));
        _offset += 4;
        return value;
    }

    public byte[] ReadBytes(int length)
    {
        EnsureAvailable(length);
        byte[] value = _buffer.AsSpan(_offset, length).ToArray();
        _offset += length;
        return value;
    }

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

    private void EnsureAvailable(int count)
    {
        if (count < 0 || Remaining < count)
        {
            throw new InvalidDataException("World packet payload ended before the expected field was available.");
        }
    }
}
