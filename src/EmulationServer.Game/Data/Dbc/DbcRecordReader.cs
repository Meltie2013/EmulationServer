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

/**
  * File overview: src/EmulationServer.Game/Data/Dbc/DbcRecordReader.cs
  * This file centralizes common typed DBC read helpers used by WorldServer runtime data stores.
  */

namespace EmulationServer.Game.Data.Dbc;

/**
  * Provides safe typed reads from generic DBC rows, including mixed-layout rows such as CharStartOutfit.dbc.
  */
internal static class DbcRecordReader
{
    /**
      * Validates that a DBC store has enough fields for a typed reader.
      */
    public static void ValidateFieldCount(DbcDataStore store, string fileName, int requiredFieldCount)
    {
        ArgumentNullException.ThrowIfNull(store);

        if (store.FieldCount < requiredFieldCount)
        {
            throw new DbcFormatException($"{fileName} has {store.FieldCount} field(s), but the typed DBC reader requires at least {requiredFieldCount} field(s).");
        }
    }

    /**
      * Validates that a DBC store has enough record bytes for a mixed-layout typed reader.
      */
    public static void ValidateRecordSize(DbcDataStore store, string fileName, int requiredRecordSize)
    {
        ArgumentNullException.ThrowIfNull(store);

        if (store.Header.RecordSize < requiredRecordSize)
        {
            throw new DbcFormatException($"{fileName} has {store.Header.RecordSize} byte record(s), but the typed DBC reader requires at least {requiredRecordSize} byte(s).");
        }
    }

    /**
      * Reads a signed 32-bit integer field from a uniform DBC row.
      */
    public static int ReadInt32(DbcRecord record, int fieldIndex)
    {
        return record.GetInt32(fieldIndex);
    }

    /**
      * Reads an unsigned 32-bit integer field from a uniform DBC row.
      */
    public static uint ReadUInt32(DbcRecord record, int fieldIndex)
    {
        return record.GetUInt32(fieldIndex);
    }

    /**
      * Reads a floating-point field from a uniform DBC row.
      */
    public static float ReadSingle(DbcRecord record, int fieldIndex)
    {
        return record.GetSingle(fieldIndex);
    }

    /**
      * Reads a localized string field from a uniform DBC row and trims null or whitespace-only values.
      */
    public static string ReadString(DbcRecord record, int fieldIndex)
    {
        return CleanString(record.GetString(fieldIndex));
    }

    /**
      * Reads a byte from a raw DBC row offset. This is required for mixed byte/int layouts.
      */
    public static byte ReadByteAtOffset(DbcRecord record, int byteOffset)
    {
        ReadOnlySpan<byte> data = record.GetRawData();
        EnsureOffset(data, byteOffset, sizeof(byte));
        return data[byteOffset];
    }

    /**
      * Reads a signed 32-bit value from a raw DBC row offset. This is required for mixed byte/int layouts.
      */
    public static int ReadInt32AtOffset(DbcRecord record, int byteOffset)
    {
        return unchecked((int)ReadUInt32AtOffset(record, byteOffset));
    }

    /**
      * Reads an unsigned 32-bit value from a raw DBC row offset. This is required for mixed byte/int layouts.
      */
    public static uint ReadUInt32AtOffset(DbcRecord record, int byteOffset)
    {
        ReadOnlySpan<byte> data = record.GetRawData();
        EnsureOffset(data, byteOffset, sizeof(uint));
        return BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(byteOffset, sizeof(uint)));
    }

    /**
      * Trims DBC strings without losing meaningful internal whitespace.
      */
    public static string CleanString(string value)
    {
        return value.Trim('\0', ' ', '\t', '\r', '\n');
    }

    private static void EnsureOffset(ReadOnlySpan<byte> data, int byteOffset, int width)
    {
        if (byteOffset < 0 || width <= 0 || byteOffset + width > data.Length)
        {
            throw new DbcFormatException($"DBC raw byte read at offset {byteOffset} with width {width} exceeds record length {data.Length}.");
        }
    }
}
