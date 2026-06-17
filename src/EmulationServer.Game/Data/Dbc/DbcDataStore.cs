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
// File: src/EmulationServer.Game/Data/Dbc/DbcDataStore.cs
// Purpose: Contains DBC data store code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Text;

namespace EmulationServer.Game.Data.Dbc;

// Type: DbcDataStore
// Purpose: Provides DBC data store behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class DbcDataStore
{

    // Field: Stores the record data state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current record data backing value maintained by the owning type.
    private readonly byte[] _recordData;

    // Field: Stores the string block state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current string block backing value maintained by the owning type.
    private readonly byte[] _stringBlock;
    // Field: Stores the uint state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current uint backing value maintained by the owning type.
    private readonly Dictionary<uint, int> _recordIndexById;

    // Field: Stores the field size state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current field size backing value maintained by the owning type.
    private readonly int _fieldSize;

    // Constructor: DbcDataStore
    // Purpose: Initializes a new DbcDataStore instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - path: Path value supplied by the caller for this operation.
    // - header: Header value supplied by the caller for this operation.
    // - byterecordData: Byterecord data value supplied by the caller for this operation.
    // - bytestringBlock: Bytestring block value supplied by the caller for this operation.
    // - recordIndexById: Record index by ID identifier used to select the exact record, object, or runtime owner.
    // - fieldSize: Field size value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to DbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Property: Gets or sets the path value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: path value exposed by the owning type.
    public string Path { get; }

    // Property: Gets or sets the name value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: name value exposed by the owning type.
    public string Name { get; }

    // Property: Gets or sets the header value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: header value exposed by the owning type.
    public DbcHeader Header { get; }

    // Property: Gets or sets the record count value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: record count value exposed by the owning type.
    public int RecordCount => Header.RecordCount;

    // Property: Gets or sets the field count value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: field count value exposed by the owning type.
    public int FieldCount => Header.FieldCount;

    // Method: GetRecord
    // Purpose: Retrieves get record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - index: Index value supplied by the caller for this operation.
    // Returns: Returns the DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to DbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public DbcRecord GetRecord(int index)
    {
        if (index < 0 || index >= Header.RecordCount)
        {
            throw new ArgumentOutOfRangeException(null, index, $"Record index must be between 0 and {Header.RecordCount - 1}.");
        }

        int offset = index * Header.RecordSize;
        return new DbcRecord(_recordData.AsMemory(offset, Header.RecordSize), _stringBlock, Header.FieldCount, _fieldSize);
    }

    // Method: TryGetRecordById
    // Purpose: Attempts to retrieve or parse try get record by ID data without treating normal misses as failures.
    // Parameters:
    // - id: Id value supplied by the caller for this operation.
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns true when try get record by ID succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to DbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: EnumerateRecords
    // Purpose: Executes the enumerate records operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the I enumerable value produced by this operation.
    // Notes: This keeps the operation scoped to DbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public IEnumerable<DbcRecord> EnumerateRecords()
    {
        for (int index = 0; index < Header.RecordCount; index++)
        {
            yield return GetRecord(index);
        }
    }

    // Method: Load
    // Purpose: Retrieves load data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - path: Path value supplied by the caller for this operation.
    // Returns: Returns the DBC data store value produced by this operation.
    // Notes: This keeps the operation scoped to DbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public static DbcDataStore Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using FileStream stream = File.OpenRead(path);
        return Load(stream, path);
    }

    // Method: Load
    // Purpose: Retrieves load data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - stream: Stream value supplied by the caller for this operation.
    // - sourceName: Source name value supplied by the caller for this operation.
    // Returns: Returns the DBC data store value produced by this operation.
    // Notes: This keeps the operation scoped to DbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: BuildRecordIndex
    // Purpose: Builds or writes build record index output for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - byterecords: Byterecords value supplied by the caller for this operation.
    // - header: Header value supplied by the caller for this operation.
    // - bytestringBlock: Bytestring block value supplied by the caller for this operation.
    // - fieldSize: Field size value supplied by the caller for this operation.
    // Returns: Returns the dictionary value produced by this operation.
    // Notes: This keeps the operation scoped to DbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static Dictionary<uint, int> BuildRecordIndex(byte[] records, DbcHeader header, byte[] stringBlock, int fieldSize)
    {
        Dictionary<uint, int> index = new();

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

    // Method: GetGenericFieldSize
    // Purpose: Retrieves get generic field size data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - header: Header value supplied by the caller for this operation.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to DbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static int GetGenericFieldSize(DbcHeader header)
    {
        return header.TryGetUniformFieldSize(out int fieldSize) ? fieldSize : 0;
    }

    // Method: ValidateHeader
    // Purpose: Validates or evaluates validate header rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - header: Header value supplied by the caller for this operation.
    // - sourceName: Source name value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to DbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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
