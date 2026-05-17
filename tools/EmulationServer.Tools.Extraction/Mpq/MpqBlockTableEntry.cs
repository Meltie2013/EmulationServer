
using System.Buffers.Binary;

namespace EmulationServer.Tools.Extraction.Mpq;

internal readonly struct MpqBlockTableEntry
{
    public MpqBlockTableEntry(uint filePosition, uint compressedSize, uint fileSize, MpqFileFlags flags)
    {
        FilePosition = filePosition;
        CompressedSize = compressedSize;
        FileSize = fileSize;
        Flags = flags;
    }

    public uint FilePosition { get; }

    public uint CompressedSize { get; }

    public uint FileSize { get; }

    public MpqFileFlags Flags { get; }

    public bool Exists => Flags.HasFlag(MpqFileFlags.Exists) && !Flags.HasFlag(MpqFileFlags.DeleteMarker);

    public static MpqBlockTableEntry Read(ReadOnlySpan<byte> data)
    {
        return new MpqBlockTableEntry(
            BinaryPrimitives.ReadUInt32LittleEndian(data[0..4]),
            BinaryPrimitives.ReadUInt32LittleEndian(data[4..8]),
            BinaryPrimitives.ReadUInt32LittleEndian(data[8..12]),
            (MpqFileFlags)BinaryPrimitives.ReadUInt32LittleEndian(data[12..16]));
    }
}
