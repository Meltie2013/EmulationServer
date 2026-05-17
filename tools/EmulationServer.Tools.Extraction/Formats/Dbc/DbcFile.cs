
using System.Text;

namespace EmulationServer.Tools.Extraction.Formats.Dbc;

public sealed class DbcFile
{
    private readonly byte[] _recordData;
    private readonly byte[] _stringBlock;

    private DbcFile(DbcHeader header, byte[] recordData, byte[] stringBlock)
    {
        Header = header;
        _recordData = recordData;
        _stringBlock = stringBlock;
    }

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
        return new DbcRecord(_recordData.AsMemory(offset, Header.RecordSize), _stringBlock, Header.FieldCount);
    }

    public IEnumerable<DbcRecord> EnumerateRecords()
    {
        for (int index = 0; index < Header.RecordCount; index++)
        {
            yield return GetRecord(index);
        }
    }

    public static DbcFile Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using FileStream stream = File.OpenRead(path);
        return Load(stream, path);
    }

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
