
using System.Buffers.Binary;
using System.Text;

namespace EmulationServer.RealmServer.Auth;

public sealed class ByteWriter
{
    private readonly List<byte> _buffer = [];

    public int Count => _buffer.Count;

    public void WriteUInt8(byte value)
    {
        _buffer.Add(value);
    }

    public void WriteUInt16(ushort value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
        _buffer.AddRange(buffer.ToArray());
    }

    public void WriteUInt32(uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
        _buffer.AddRange(buffer.ToArray());
    }

    public void WriteFloat(float value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteSingleLittleEndian(buffer, value);
        _buffer.AddRange(buffer.ToArray());
    }

    public void WriteCString(string value)
    {
        _buffer.AddRange(Encoding.UTF8.GetBytes(value));
        _buffer.Add(0);
    }

    public void WriteBytes(ReadOnlySpan<byte> value)
    {
        _buffer.AddRange(value.ToArray());
    }

    public byte[] ToArray()
    {
        return [.. _buffer];
    }
}
