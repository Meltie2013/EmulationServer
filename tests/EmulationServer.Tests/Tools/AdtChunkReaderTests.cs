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
