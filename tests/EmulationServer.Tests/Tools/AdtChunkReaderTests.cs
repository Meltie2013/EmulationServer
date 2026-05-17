using System.Text;
using EmulationServer.Tools.Extraction.Formats.Adt;

namespace EmulationServer.Tests.Tools;

public sealed class AdtChunkReaderTests
{
    [Fact]
    public void ReadAdtFourCC_NormalizesReversedAdtChunkMagic()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("KNCM");

        string fourCC = AdtChunkReader.ReadAdtFourCC(bytes, 0);

        Assert.Equal("MCNK", fourCC);
    }

    [Fact]
    public void ReadTopLevelChunks_ReturnsNormalizedFourCC()
    {
        byte[] data =
        [
            (byte)'N', (byte)'I', (byte)'C', (byte)'M',
            0, 0, 0, 0,
        ];

        IReadOnlyList<AdtChunk> chunks = AdtChunkReader.ReadTopLevelChunks(data);

        AdtChunk chunk = Assert.Single(chunks);
        Assert.Equal("MCIN", chunk.FourCC);
        Assert.Equal(0, chunk.Offset);
        Assert.Equal(0, chunk.Size);
        Assert.Equal(8, chunk.DataOffset);
    }
}
