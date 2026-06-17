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
// File: src/EmulationServer.Game/Data/Dbc/DbcRecord.cs
// Purpose: Contains DBC record code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Buffers.Binary;
using System.Text;

namespace EmulationServer.Game.Data.Dbc;

// Type: DbcRecord
// Purpose: Represents the DBC record value type used by the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public readonly struct DbcRecord
{

    // Field: Stores the record data state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current record data backing value maintained by the owning type.
    private readonly ReadOnlyMemory<byte> _recordData;

    // Field: Stores the string block state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current string block backing value maintained by the owning type.
    private readonly ReadOnlyMemory<byte> _stringBlock;

    // Field: Stores the field size state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current field size backing value maintained by the owning type.
    private readonly int _fieldSize;

    // Constructor: DbcRecord
    // Purpose: Initializes a new DbcRecord instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - recordData: Record data value supplied by the caller for this operation.
    // - stringBlock: String block value supplied by the caller for this operation.
    // - fieldCount: Field count value supplied by the caller for this operation.
    // - fieldSize: Field size value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to DbcRecord so callers do not duplicate validation, protocol, or persistence rules.
    internal DbcRecord(ReadOnlyMemory<byte> recordData, ReadOnlyMemory<byte> stringBlock, int fieldCount, int fieldSize)
    {
        _recordData = recordData;
        _stringBlock = stringBlock;
        FieldCount = fieldCount;
        _fieldSize = fieldSize;
    }

    // Property: Gets or sets the field count value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: field count value exposed by the owning type.
    public int FieldCount { get; }

    // Method: GetUInt32
    // Purpose: Retrieves get U int32 data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the uint ID => value produced by this operation.
    // Notes: This keeps the operation scoped to DbcRecord so callers do not duplicate validation, protocol, or persistence rules.
    public uint Id => GetUInt32(0);

    // Method: GetUInt8
    // Purpose: Retrieves get U int8 data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - fieldIndex: Field index value supplied by the caller for this operation.
    // Returns: Returns the byte value produced by this operation.
    // Notes: This keeps the operation scoped to DbcRecord so callers do not duplicate validation, protocol, or persistence rules.
    public byte GetUInt8(int fieldIndex)
    {
        ValidateFieldIndex(fieldIndex);
        EnsureFieldSize(fieldIndex, sizeof(byte));

        return _recordData.Span[GetFieldOffset(fieldIndex)];
    }

    // Method: GetUInt16
    // Purpose: Retrieves get U int16 data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - fieldIndex: Field index value supplied by the caller for this operation.
    // Returns: Returns the ushort value produced by this operation.
    // Notes: This keeps the operation scoped to DbcRecord so callers do not duplicate validation, protocol, or persistence rules.
    public ushort GetUInt16(int fieldIndex)
    {
        ValidateFieldIndex(fieldIndex);
        EnsureFieldSize(fieldIndex, sizeof(ushort));

        return BinaryPrimitives.ReadUInt16LittleEndian(
            _recordData.Span.Slice(GetFieldOffset(fieldIndex), sizeof(ushort)));
    }

    // Method: GetUInt32
    // Purpose: Retrieves get U int32 data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - fieldIndex: Field index value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to DbcRecord so callers do not duplicate validation, protocol, or persistence rules.
    public uint GetUInt32(int fieldIndex)
    {
        ValidateFieldIndex(fieldIndex);

        int offset = GetFieldOffset(fieldIndex);
        ReadOnlySpan<byte> record = _recordData.Span;

        return _fieldSize switch
        {
            sizeof(byte) => record[offset],
            sizeof(ushort) => BinaryPrimitives.ReadUInt16LittleEndian(record.Slice(offset, sizeof(ushort))),
            sizeof(uint) => BinaryPrimitives.ReadUInt32LittleEndian(record.Slice(offset, sizeof(uint))),
            _ => throw new DbcFormatException(
                $"DBC field {fieldIndex} cannot be read generically because this record uses a mixed or unknown field layout.")
        };
    }

    // Method: GetInt32
    // Purpose: Retrieves get int32 data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - fieldIndex: Field index value supplied by the caller for this operation.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to DbcRecord so callers do not duplicate validation, protocol, or persistence rules.
    public int GetInt32(int fieldIndex)
    {
        return unchecked((int)GetUInt32(fieldIndex));
    }

    // Method: GetSingle
    // Purpose: Retrieves get single data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - fieldIndex: Field index value supplied by the caller for this operation.
    // Returns: Returns the float value produced by this operation.
    // Notes: This keeps the operation scoped to DbcRecord so callers do not duplicate validation, protocol, or persistence rules.
    public float GetSingle(int fieldIndex)
    {
        ValidateFieldIndex(fieldIndex);
        EnsureFieldSize(fieldIndex, sizeof(uint));

        return BitConverter.Int32BitsToSingle(unchecked((int)GetUInt32(fieldIndex)));
    }

    // Method: GetString
    // Purpose: Retrieves get string data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - fieldIndex: Field index value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to DbcRecord so callers do not duplicate validation, protocol, or persistence rules.
    public string GetString(int fieldIndex)
    {
        ValidateFieldIndex(fieldIndex);
        EnsureFieldSize(fieldIndex, sizeof(uint));

        return GetStringAtOffset(GetUInt32(fieldIndex));
    }

    // Method: GetStringAtOffset
    // Purpose: Retrieves get string at offset data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - offset: Offset value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to DbcRecord so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: GetRawData
    // Purpose: Retrieves get raw data data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the read only span value produced by this operation.
    // Notes: This keeps the operation scoped to DbcRecord so callers do not duplicate validation, protocol, or persistence rules.
    public ReadOnlySpan<byte> GetRawData()
    {
        return _recordData.Span;
    }

    // Method: GetFieldOffset
    // Purpose: Retrieves get field offset data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - fieldIndex: Field index value supplied by the caller for this operation.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to DbcRecord so callers do not duplicate validation, protocol, or persistence rules.
    private int GetFieldOffset(int fieldIndex)
    {
        if (_fieldSize <= 0)
        {
            throw new DbcFormatException(
                $"DBC field {fieldIndex} cannot be read generically because this record uses a mixed or unknown field layout.");
        }

        return fieldIndex * _fieldSize;
    }

    // Method: ValidateFieldIndex
    // Purpose: Validates or evaluates validate field index rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - fieldIndex: Field index value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to DbcRecord so callers do not duplicate validation, protocol, or persistence rules.
    private void ValidateFieldIndex(int fieldIndex)
    {
        if (fieldIndex < 0 || fieldIndex >= FieldCount)
        {
            throw new ArgumentOutOfRangeException(null, fieldIndex, $"Field index must be between 0 and {FieldCount - 1}.");
        }
    }

    // Method: EnsureFieldSize
    // Purpose: Validates or evaluates ensure field size rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - fieldIndex: Field index value supplied by the caller for this operation.
    // - minimumFieldSize: Minimum field size value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to DbcRecord so callers do not duplicate validation, protocol, or persistence rules.
    private void EnsureFieldSize(int fieldIndex, int minimumFieldSize)
    {
        if (_fieldSize < minimumFieldSize)
        {
            throw new DbcFormatException(
                $"DBC field {fieldIndex} is {_fieldSize} byte(s), but this read requires {minimumFieldSize} byte(s).");
        }
    }
}
