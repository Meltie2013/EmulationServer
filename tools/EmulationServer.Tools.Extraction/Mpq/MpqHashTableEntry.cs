
using System.Buffers.Binary;

namespace EmulationServer.Tools.Extraction.Mpq;

internal readonly struct MpqHashTableEntry
{
    public MpqHashTableEntry(uint nameHashA, uint nameHashB, ushort locale, ushort platform, uint blockIndex)
    {
        NameHashA = nameHashA;
        NameHashB = nameHashB;
        Locale = locale;
        Platform = platform;
        BlockIndex = blockIndex;
    }

    public uint NameHashA { get; }

    public uint NameHashB { get; }

    public ushort Locale { get; }

    public ushort Platform { get; }

    public uint BlockIndex { get; }

    public bool IsEmpty => BlockIndex == 0xFFFFFFFF;

    public bool IsDeleted => BlockIndex == 0xFFFFFFFE;

    public static MpqHashTableEntry Read(ReadOnlySpan<byte> data)
    {
        return new MpqHashTableEntry(
            BinaryPrimitives.ReadUInt32LittleEndian(data[0..4]),
            BinaryPrimitives.ReadUInt32LittleEndian(data[4..8]),
            BinaryPrimitives.ReadUInt16LittleEndian(data[8..10]),
            BinaryPrimitives.ReadUInt16LittleEndian(data[10..12]),
            BinaryPrimitives.ReadUInt32LittleEndian(data[12..16]));
    }
}
