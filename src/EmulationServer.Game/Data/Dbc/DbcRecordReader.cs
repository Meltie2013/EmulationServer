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
// File: src/EmulationServer.Game/Data/Dbc/DbcRecordReader.cs
// Purpose: Contains DBC record reader code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Buffers.Binary;

namespace EmulationServer.Game.Data.Dbc;

// Type: DbcRecordReader
// Purpose: Provides DBC record reader behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
internal static class DbcRecordReader
{

    // Method: ValidateFieldCount
    // Purpose: Validates or evaluates validate field count rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - store: Store value supplied by the caller for this operation.
    // - fileName: File name value supplied by the caller for this operation.
    // - requiredFieldCount: Required field count value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to DbcRecordReader so callers do not duplicate validation, protocol, or persistence rules.
    public static void ValidateFieldCount(DbcDataStore store, string fileName, int requiredFieldCount)
    {
        ArgumentNullException.ThrowIfNull(store);

        if (store.FieldCount < requiredFieldCount)
        {
            throw new DbcFormatException($"{fileName} has {store.FieldCount} field(s), but the typed DBC reader requires at least {requiredFieldCount} field(s).");
        }
    }

    // Method: ValidateRecordSize
    // Purpose: Validates or evaluates validate record size rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - store: Store value supplied by the caller for this operation.
    // - fileName: File name value supplied by the caller for this operation.
    // - requiredRecordSize: Required record size value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to DbcRecordReader so callers do not duplicate validation, protocol, or persistence rules.
    public static void ValidateRecordSize(DbcDataStore store, string fileName, int requiredRecordSize)
    {
        ArgumentNullException.ThrowIfNull(store);

        if (store.Header.RecordSize < requiredRecordSize)
        {
            throw new DbcFormatException($"{fileName} has {store.Header.RecordSize} byte record(s), but the typed DBC reader requires at least {requiredRecordSize} byte(s).");
        }
    }

    // Method: ReadInt32
    // Purpose: Retrieves read int32 data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // - fieldIndex: Field index value supplied by the caller for this operation.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to DbcRecordReader so callers do not duplicate validation, protocol, or persistence rules.
    public static int ReadInt32(DbcRecord record, int fieldIndex)
    {
        return record.GetInt32(fieldIndex);
    }

    // Method: ReadUInt32
    // Purpose: Retrieves read U int32 data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // - fieldIndex: Field index value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to DbcRecordReader so callers do not duplicate validation, protocol, or persistence rules.
    public static uint ReadUInt32(DbcRecord record, int fieldIndex)
    {
        return record.GetUInt32(fieldIndex);
    }

    // Method: ReadSingle
    // Purpose: Retrieves read single data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // - fieldIndex: Field index value supplied by the caller for this operation.
    // Returns: Returns the float value produced by this operation.
    // Notes: This keeps the operation scoped to DbcRecordReader so callers do not duplicate validation, protocol, or persistence rules.
    public static float ReadSingle(DbcRecord record, int fieldIndex)
    {
        return record.GetSingle(fieldIndex);
    }

    // Method: ReadString
    // Purpose: Retrieves read string data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // - fieldIndex: Field index value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to DbcRecordReader so callers do not duplicate validation, protocol, or persistence rules.
    public static string ReadString(DbcRecord record, int fieldIndex)
    {
        return CleanString(record.GetString(fieldIndex));
    }

    // Method: ReadByteAtOffset
    // Purpose: Retrieves read byte at offset data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // - byteOffset: Byte offset value supplied by the caller for this operation.
    // Returns: Returns the byte value produced by this operation.
    // Notes: This keeps the operation scoped to DbcRecordReader so callers do not duplicate validation, protocol, or persistence rules.
    public static byte ReadByteAtOffset(DbcRecord record, int byteOffset)
    {
        ReadOnlySpan<byte> data = record.GetRawData();
        EnsureOffset(data, byteOffset, sizeof(byte));
        return data[byteOffset];
    }

    // Method: ReadInt32AtOffset
    // Purpose: Retrieves read int32 at offset data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // - byteOffset: Byte offset value supplied by the caller for this operation.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to DbcRecordReader so callers do not duplicate validation, protocol, or persistence rules.
    public static int ReadInt32AtOffset(DbcRecord record, int byteOffset)
    {
        return unchecked((int)ReadUInt32AtOffset(record, byteOffset));
    }

    // Method: ReadUInt32AtOffset
    // Purpose: Retrieves read U int32 at offset data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // - byteOffset: Byte offset value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to DbcRecordReader so callers do not duplicate validation, protocol, or persistence rules.
    public static uint ReadUInt32AtOffset(DbcRecord record, int byteOffset)
    {
        ReadOnlySpan<byte> data = record.GetRawData();
        EnsureOffset(data, byteOffset, sizeof(uint));
        return BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(byteOffset, sizeof(uint)));
    }

    // Method: CleanString
    // Purpose: Executes the clean string operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to DbcRecordReader so callers do not duplicate validation, protocol, or persistence rules.
    public static string CleanString(string value)
    {
        return value.Trim('\0', ' ', '\t', '\r', '\n');
    }

    // Method: EnsureOffset
    // Purpose: Validates or evaluates ensure offset rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - data: Data bytes or structured payload consumed by this operation.
    // - byteOffset: Byte offset value supplied by the caller for this operation.
    // - width: Width value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to DbcRecordReader so callers do not duplicate validation, protocol, or persistence rules.
    private static void EnsureOffset(ReadOnlySpan<byte> data, int byteOffset, int width)
    {
        if (byteOffset < 0 || width <= 0 || byteOffset + width > data.Length)
        {
            throw new DbcFormatException($"DBC raw byte read at offset {byteOffset} with width {width} exceeds record length {data.Length}.");
        }
    }
}
