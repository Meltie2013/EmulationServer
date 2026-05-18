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
  * File overview: tools/EmulationServer.Tools.Extraction/Formats/Dbc/DbcRecord.cs
  * This file belongs to the developer tooling for data extraction, validation, and diagnostics portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Tools.Extraction.Formats.Dbc;

/**
  * Represents the dbc record component in the developer tooling for data extraction, validation, and diagnostics area.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public readonly struct DbcRecord
{
    /**
      * Stores the record data dependency or runtime value for DbcRecord.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly ReadOnlyMemory<byte> _recordData;
    /**
      * Stores the string block dependency or runtime value for DbcRecord.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly ReadOnlyMemory<byte> _stringBlock;

    /**
      * Creates a new DbcRecord instance and stores the dependencies required by the component.
      * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
      */
    internal DbcRecord(ReadOnlyMemory<byte> recordData, ReadOnlyMemory<byte> stringBlock, int fieldCount)
    {
        _recordData = recordData;
        _stringBlock = stringBlock;
        FieldCount = fieldCount;
    }

    /**
      * Gets or stores the field count value used by DbcRecord.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int FieldCount { get; }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of DbcRecord and keeps this workflow isolated from the caller.
      */
    public uint GetUInt32(int fieldIndex)
    {
        ValidateFieldIndex(fieldIndex);
        ReadOnlySpan<byte> field = _recordData.Span.Slice(fieldIndex * sizeof(uint), sizeof(uint));
        return BinaryPrimitives.ReadUInt32LittleEndian(field);
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of DbcRecord and keeps this workflow isolated from the caller.
      */
    public int GetInt32(int fieldIndex)
    {
        return unchecked((int)GetUInt32(fieldIndex));
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of DbcRecord and keeps this workflow isolated from the caller.
      */
    public float GetSingle(int fieldIndex)
    {
        return BitConverter.Int32BitsToSingle(GetInt32(fieldIndex));
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of DbcRecord and keeps this workflow isolated from the caller.
      */
    public string GetString(int fieldIndex)
    {
        uint offset = GetUInt32(fieldIndex);
        return GetStringAtOffset(offset);
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of DbcRecord and keeps this workflow isolated from the caller.
      */
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

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of DbcRecord and keeps this workflow isolated from the caller.
      */
    private void ValidateFieldIndex(int fieldIndex)
    {
        if (fieldIndex < 0 || fieldIndex >= FieldCount)
        {
            throw new ArgumentOutOfRangeException(nameof(fieldIndex), fieldIndex, $"Field index must be between 0 and {FieldCount - 1}.");
        }
    }
}
