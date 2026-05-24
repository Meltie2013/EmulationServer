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

using System.Text;

/**
  * File overview: src/EmulationServer.Game/Data/Dbc/DbcDataStore.cs
  * Documents the DbcDataStore source file in the DBC loading and strongly typed client data records area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Data.Dbc;

/**
  * Loads generic DBC records and exposes raw cell access before typed DBC schemas are implemented.
  * It owns loaded data in memory and provides lookup access to other systems.
  */
public sealed class DbcDataStore
{
    /**
      * Holds the private record data state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly byte[] _recordData;
    /**
      * Holds the private string block state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly byte[] _stringBlock;
    private readonly Dictionary<uint, int> _recordIndexById;
    /**
      * Holds the private field size state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly int _fieldSize;

    /**
      * Initializes a new DbcDataStore instance with the dependencies required by the DBC loading and strongly typed client data records workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: path, header, recordData, stringBlock, recordIndexById, fieldSize.
      */
    private DbcDataStore(
        string path,
        DbcHeader header,
        byte[] recordData,
        byte[] stringBlock,
        Dictionary<uint, int> recordIndexById,
        int fieldSize)
    {
        Path = path;
        Name = System.IO.Path.GetFileName(path);
        Header = header;
        _recordData = recordData;
        _stringBlock = stringBlock;
        _recordIndexById = recordIndexById;
        _fieldSize = fieldSize;
    }

    /**
      * Gets or stores the path value used by DbcDataStore.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string Path { get; }

    /**
      * Gets or stores the name value used by DbcDataStore.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string Name { get; }

    /**
      * Gets or stores the header value used by DbcDataStore.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public DbcHeader Header { get; }

    /**
      * Gets or stores the record count value used by DbcDataStore.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int RecordCount => Header.RecordCount;

    /**
      * Gets or stores the field count value used by DbcDataStore.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int FieldCount => Header.FieldCount;

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of DbcDataStore and keeps this workflow isolated from the caller.
      */
    public DbcRecord GetRecord(int index)
    {
        if (index < 0 || index >= Header.RecordCount)
        {
            throw new ArgumentOutOfRangeException(null, index, $"Record index must be between 0 and {Header.RecordCount - 1}.");
        }

        int offset = index * Header.RecordSize;
        return new DbcRecord(_recordData.AsMemory(offset, Header.RecordSize), _stringBlock, Header.FieldCount, _fieldSize);
    }

    /**
      * Attempts the operation without treating a normal failure as an exceptional condition.
      * The method is part of DbcDataStore and keeps this workflow isolated from the caller.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    public bool TryGetRecordById(uint id, out DbcRecord record)
    {
        if (_recordIndexById.TryGetValue(id, out int index))
        {
            record = GetRecord(index);
            return true;
        }

        record = default;
        return false;
    }

    /**
      * Performs the enumerate records operation for the DBC loading and strongly typed client data records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      */
    public IEnumerable<DbcRecord> EnumerateRecords()
    {
        for (int index = 0; index < Header.RecordCount; index++)
        {
            yield return GetRecord(index);
        }
    }

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of DbcDataStore and keeps this workflow isolated from the caller.
      */
    public static DbcDataStore Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using FileStream stream = File.OpenRead(path);
        return Load(stream, path);
    }

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of DbcDataStore and keeps this workflow isolated from the caller.
      */
    public static DbcDataStore Load(Stream stream, string sourceName)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);

        using BinaryReader reader = new(stream, Encoding.UTF8, leaveOpen: true);

        string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
        int recordCount = reader.ReadInt32();
        int fieldCount = reader.ReadInt32();
        int recordSize = reader.ReadInt32();
        int stringBlockSize = reader.ReadInt32();

        DbcHeader header = new(magic, recordCount, fieldCount, recordSize, stringBlockSize);
        ValidateHeader(header, sourceName);

        long recordBytes = checked((long)recordCount * recordSize);
        if (recordBytes > int.MaxValue)
        {
            throw new DbcFormatException($"{sourceName} is too large to load into memory. Record bytes={recordBytes}.");
        }

        byte[] records = reader.ReadBytes((int)recordBytes);
        if (records.Length != recordBytes)
        {
            throw new DbcFormatException($"{sourceName} ended before all DBC records were read. Expected {recordBytes} byte(s), read {records.Length}.");
        }

        byte[] stringBlock = reader.ReadBytes(stringBlockSize);
        if (stringBlock.Length != stringBlockSize)
        {
            throw new DbcFormatException($"{sourceName} ended before the DBC string block was read. Expected {stringBlockSize} byte(s), read {stringBlock.Length}.");
        }

        int fieldSize = GetGenericFieldSize(header);
        Dictionary<uint, int> recordIndexById = BuildRecordIndex(records, header, stringBlock, fieldSize);
        return new DbcDataStore(sourceName, header, records, stringBlock, recordIndexById, fieldSize);
    }

    private static Dictionary<uint, int> BuildRecordIndex(byte[] records, DbcHeader header, byte[] stringBlock, int fieldSize)
    {
        Dictionary<uint, int> index = new();

        // Some DBC files, such as CharBaseInfo.dbc, are compact helper tables
        // with one-byte fields and no stable four-byte id column. Do not build a
        // misleading id index for those generic raw stores. Typed stores can add
        // the correct lookup keys later.
        if (fieldSize != sizeof(uint) || header.RecordSize < sizeof(uint))
        {
            return index;
        }

        for (int recordIndex = 0; recordIndex < header.RecordCount; recordIndex++)
        {
            int offset = recordIndex * header.RecordSize;
            DbcRecord record = new(records.AsMemory(offset, header.RecordSize), stringBlock, header.FieldCount, fieldSize);
            index.TryAdd(record.Id, recordIndex);
        }

        return index;
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of DbcDataStore and keeps this workflow isolated from the caller.
      */
    private static int GetGenericFieldSize(DbcHeader header)
    {
        return header.TryGetUniformFieldSize(out int fieldSize) ? fieldSize : 0;
    }

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of DbcDataStore and keeps this workflow isolated from the caller.
      */
    private static void ValidateHeader(DbcHeader header, string sourceName)
    {
        if (!string.Equals(header.Magic, DbcHeader.ExpectedMagic, StringComparison.Ordinal))
        {
            throw new DbcFormatException($"{sourceName} has invalid DBC magic '{header.Magic}'. Expected '{DbcHeader.ExpectedMagic}'.");
        }

        if (header.RecordCount < 0)
        {
            throw new DbcFormatException($"{sourceName} has invalid record count {header.RecordCount}.");
        }

        if (header.FieldCount <= 0)
        {
            throw new DbcFormatException($"{sourceName} has invalid field count {header.FieldCount}.");
        }

        if (header.RecordSize <= 0)
        {
            throw new DbcFormatException($"{sourceName} has invalid record size {header.RecordSize}.");
        }

        if (!header.TryGetUniformFieldSize(out _) && header.RecordSize < header.FieldCount)
        {
            throw new DbcFormatException(
                $"{sourceName} has record size {header.RecordSize}, which is too small for {header.FieldCount} field(s).");
        }

        if (header.StringBlockSize < 0)
        {
            throw new DbcFormatException($"{sourceName} has invalid string block size {header.StringBlockSize}.");
        }
    }
}
