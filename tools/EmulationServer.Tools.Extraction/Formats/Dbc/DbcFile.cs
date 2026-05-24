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
  * File overview: tools/EmulationServer.Tools.Extraction/Formats/Dbc/DbcFile.cs
  * Documents the DbcFile source file in the client data extraction and conversion tooling area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Tools.Extraction.Formats.Dbc;

/**
  * Owns the dbc file behavior for the client data extraction and conversion tooling layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class DbcFile
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

    /**
      * Initializes a new DbcFile instance with the dependencies required by the client data extraction and conversion tooling workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: header, recordData, stringBlock.
      */
    private DbcFile(DbcHeader header, byte[] recordData, byte[] stringBlock)
    {
        Header = header;
        _recordData = recordData;
        _stringBlock = stringBlock;
    }

    /**
      * Gets or stores the header value used by DbcFile.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public DbcHeader Header { get; }

    /**
      * Gets or stores the record count value used by DbcFile.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int RecordCount => Header.RecordCount;

    /**
      * Gets or stores the field count value used by DbcFile.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int FieldCount => Header.FieldCount;

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of DbcFile and keeps this workflow isolated from the caller.
      */
    public DbcRecord GetRecord(int index)
    {
        if (index < 0 || index >= Header.RecordCount)
        {
            throw new ArgumentOutOfRangeException(null, index, $"Record index must be between 0 and {Header.RecordCount - 1}.");
        }

        int offset = index * Header.RecordSize;
        return new DbcRecord(_recordData.AsMemory(offset, Header.RecordSize), _stringBlock, Header.FieldCount);
    }

    /**
      * Performs the enumerate records operation for the client data extraction and conversion tooling workflow.
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
      * The method is part of DbcFile and keeps this workflow isolated from the caller.
      */
    public static DbcFile Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using FileStream stream = File.OpenRead(path);
        return Load(stream, path);
    }

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of DbcFile and keeps this workflow isolated from the caller.
      */
    public static DbcFile Load(Stream stream, string sourceName = "DBC stream")
    {
        ArgumentNullException.ThrowIfNull(stream);

        using BinaryReader reader = new(stream, Encoding.UTF8, leaveOpen: true);

        string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
        int recordCount = reader.ReadInt32();
        int fieldCount = reader.ReadInt32();
        int recordSize = reader.ReadInt32();
        int stringBlockSize = reader.ReadInt32();

        DbcHeader header = new(magic, recordCount, fieldCount, recordSize, stringBlockSize);
        ValidateHeader(header, sourceName);

        long recordBytes = checked((long)recordCount * recordSize);
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

        return new DbcFile(header, records, stringBlock);
    }

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of DbcFile and keeps this workflow isolated from the caller.
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

        if (header.RecordSize != header.FieldCount * sizeof(uint))
        {
            throw new DbcFormatException($"{sourceName} has record size {header.RecordSize}, but field count {header.FieldCount} requires {header.FieldCount * sizeof(uint)} byte(s).");
        }

        if (header.StringBlockSize < 0)
        {
            throw new DbcFormatException($"{sourceName} has invalid string block size {header.StringBlockSize}.");
        }
    }
}
