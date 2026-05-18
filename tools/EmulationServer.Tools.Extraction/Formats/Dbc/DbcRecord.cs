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

namespace EmulationServer.Tools.Extraction.Formats.Dbc;

public readonly struct DbcRecord
{
    private readonly ReadOnlyMemory<byte> _recordData;
    private readonly ReadOnlyMemory<byte> _stringBlock;

    internal DbcRecord(ReadOnlyMemory<byte> recordData, ReadOnlyMemory<byte> stringBlock, int fieldCount)
    {
        _recordData = recordData;
        _stringBlock = stringBlock;
        FieldCount = fieldCount;
    }

    public int FieldCount { get; }

    public uint GetUInt32(int fieldIndex)
    {
        ValidateFieldIndex(fieldIndex);
        ReadOnlySpan<byte> field = _recordData.Span.Slice(fieldIndex * sizeof(uint), sizeof(uint));
        return BinaryPrimitives.ReadUInt32LittleEndian(field);
    }

    public int GetInt32(int fieldIndex)
    {
        return unchecked((int)GetUInt32(fieldIndex));
    }

    public float GetSingle(int fieldIndex)
    {
        return BitConverter.Int32BitsToSingle(GetInt32(fieldIndex));
    }

    public string GetString(int fieldIndex)
    {
        uint offset = GetUInt32(fieldIndex);
        return GetStringAtOffset(offset);
    }

    public string GetStringAtOffset(uint offset)
    {
        ReadOnlySpan<byte> strings = _stringBlock.Span;

        if (offset >= strings.Length)
        {
            throw new DbcFormatException($"DBC string offset {offset} is outside the string block length {strings.Length}.");
        }

        ReadOnlySpan<byte> text = strings[(int)offset..];
        int terminator = text.IndexOf((byte)0);

        if (terminator < 0)
        {
            throw new DbcFormatException($"DBC string offset {offset} does not contain a null-terminated string.");
        }

        return Encoding.UTF8.GetString(text[..terminator]);
    }

    private void ValidateFieldIndex(int fieldIndex)
    {
        if (fieldIndex < 0 || fieldIndex >= FieldCount)
        {
            throw new ArgumentOutOfRangeException(nameof(fieldIndex), fieldIndex, $"Field index must be between 0 and {FieldCount - 1}.");
        }
    }
}
