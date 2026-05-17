using System.Buffers.Binary;
using System.Text;

namespace EmulationServer.Tools.Extraction.Formats.Adt;

public static class AdtChunkReader
{
    public static IReadOnlyList<AdtChunk> ReadTopLevelChunks(ReadOnlySpan<byte> data)
    {
        List<AdtChunk> chunks = [];
        int offset = 0;

        while (offset + 8 <= data.Length)
        {
            string fourCC = ReadAdtFourCC(data, offset);
            int size = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset + 4, 4)));
            int dataOffset = offset + 8;
            int nextOffset = dataOffset + size;

            if (size < 0 || nextOffset < dataOffset || nextOffset > data.Length)
            {
                break;
            }

            chunks.Add(new AdtChunk(fourCC, offset, size, dataOffset));
            offset = nextOffset;
        }

        return chunks;
    }

    public static IReadOnlyList<AdtChunk> ReadNestedChunks(ReadOnlySpan<byte> data, int offset, int size)
    {
        if (offset < 0 || size < 0 || offset + size > data.Length)
        {
            return [];
        }

        List<AdtChunk> chunks = [];
        int current = offset;
        int end = offset + size;

        while (current + 8 <= end)
        {
            string fourCC = ReadAdtFourCC(data, current);
            int chunkSize = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(current + 4, 4)));
            int dataOffset = current + 8;
            int nextOffset = dataOffset + chunkSize;

            if (chunkSize < 0 || nextOffset < dataOffset || nextOffset > end)
            {
                break;
            }

            chunks.Add(new AdtChunk(fourCC, current, chunkSize, dataOffset));
            current = nextOffset;
        }

        return chunks;
    }

    public static string ReadAdtFourCC(ReadOnlySpan<byte> data, int offset)
    {
        if (offset < 0 || offset + 4 > data.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, "FourCC offset is outside the ADT data.");
        }

        Span<byte> bytes = stackalloc byte[4];
        bytes[0] = data[offset + 3];
        bytes[1] = data[offset + 2];
        bytes[2] = data[offset + 1];
        bytes[3] = data[offset];
        return Encoding.ASCII.GetString(bytes);
    }
}
