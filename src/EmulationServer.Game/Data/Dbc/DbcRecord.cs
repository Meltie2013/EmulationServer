using System.Buffers.Binary;
using System.Text;

namespace EmulationServer.Game.Data.Dbc;

public readonly struct DbcRecord
{
    private readonly ReadOnlyMemory<byte> _recordData;
    private readonly ReadOnlyMemory<byte> _stringBlock;
    private readonly int _fieldSize;

    internal DbcRecord(ReadOnlyMemory<byte> recordData, ReadOnlyMemory<byte> stringBlock, int fieldCount, int fieldSize)
    {
        _recordData = recordData;
        _stringBlock = stringBlock;
        FieldCount = fieldCount;
        _fieldSize = fieldSize;
    }

    public int FieldCount { get; }

    public uint Id => GetUInt32(0);

    public byte GetUInt8(int fieldIndex)
    {
        ValidateFieldIndex(fieldIndex);
        EnsureFieldSize(fieldIndex, sizeof(byte));

        return _recordData.Span[GetFieldOffset(fieldIndex)];
    }

    public ushort GetUInt16(int fieldIndex)
    {
        ValidateFieldIndex(fieldIndex);
        EnsureFieldSize(fieldIndex, sizeof(ushort));

        return BinaryPrimitives.ReadUInt16LittleEndian(
            _recordData.Span.Slice(GetFieldOffset(fieldIndex), sizeof(ushort)));
    }

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

    public int GetInt32(int fieldIndex)
    {
        return unchecked((int)GetUInt32(fieldIndex));
    }

    public float GetSingle(int fieldIndex)
    {
        ValidateFieldIndex(fieldIndex);
        EnsureFieldSize(fieldIndex, sizeof(uint));

        return BitConverter.Int32BitsToSingle(unchecked((int)GetUInt32(fieldIndex)));
    }

    public string GetString(int fieldIndex)
    {
        ValidateFieldIndex(fieldIndex);
        EnsureFieldSize(fieldIndex, sizeof(uint));

        return GetStringAtOffset(GetUInt32(fieldIndex));
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

    public ReadOnlySpan<byte> GetRawData()
    {
        return _recordData.Span;
    }

    private int GetFieldOffset(int fieldIndex)
    {
        if (_fieldSize <= 0)
        {
            throw new DbcFormatException(
                $"DBC field {fieldIndex} cannot be read generically because this record uses a mixed or unknown field layout.");
        }

        return fieldIndex * _fieldSize;
    }

    private void ValidateFieldIndex(int fieldIndex)
    {
        if (fieldIndex < 0 || fieldIndex >= FieldCount)
        {
            throw new ArgumentOutOfRangeException(nameof(fieldIndex), fieldIndex, $"Field index must be between 0 and {FieldCount - 1}.");
        }
    }

    private void EnsureFieldSize(int fieldIndex, int minimumFieldSize)
    {
        if (_fieldSize < minimumFieldSize)
        {
            throw new DbcFormatException(
                $"DBC field {fieldIndex} is {_fieldSize} byte(s), but this read requires {minimumFieldSize} byte(s).");
        }
    }
}
