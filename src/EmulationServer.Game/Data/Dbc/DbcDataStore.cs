using System.Text;

namespace EmulationServer.Game.Data.Dbc;

public sealed class DbcDataStore
{
    private readonly byte[] _recordData;
    private readonly byte[] _stringBlock;
    private readonly Dictionary<uint, int> _recordIndexById;
    private readonly int _fieldSize;

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

    public string Path { get; }

    public string Name { get; }

    public DbcHeader Header { get; }

    public int RecordCount => Header.RecordCount;

    public int FieldCount => Header.FieldCount;

    public DbcRecord GetRecord(int index)
    {
        if (index < 0 || index >= Header.RecordCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, $"Record index must be between 0 and {Header.RecordCount - 1}.");
        }

        int offset = index * Header.RecordSize;
        return new DbcRecord(_recordData.AsMemory(offset, Header.RecordSize), _stringBlock, Header.FieldCount, _fieldSize);
    }

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

    public IEnumerable<DbcRecord> EnumerateRecords()
    {
        for (int index = 0; index < Header.RecordCount; index++)
        {
            yield return GetRecord(index);
        }
    }

    public static DbcDataStore Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using FileStream stream = File.OpenRead(path);
        return Load(stream, path);
    }

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

    private static int GetGenericFieldSize(DbcHeader header)
    {
        return header.TryGetUniformFieldSize(out int fieldSize) ? fieldSize : 0;
    }

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
