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
// File: tests/EmulationServer.Tests/Tools/AdtChunkReaderTests.cs
// Purpose: Contains adt chunk reader tests code for the automated test and verification layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Text;
using EmulationServer.Tools.Extraction.Formats.Adt;

namespace EmulationServer.Tests.Tools;

// Type: AdtChunkReaderTests
// Purpose: Provides adt chunk reader tests behavior for the automated test and verification layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class AdtChunkReaderTests
{

    [Fact]
    // Method: ReadAdtFourCC_NormalizesReversedAdtChunkMagic
    // Purpose: Retrieves read adt four CC normalizes reversed adt chunk magic data for the automated test and verification layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to AdtChunkReaderTests so callers do not duplicate validation, protocol, or persistence rules.
    public void ReadAdtFourCC_NormalizesReversedAdtChunkMagic()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("KNCM");

        string fourCC = AdtChunkReader.ReadAdtFourCC(bytes, 0);

        Assert.Equal("MCNK", fourCC);
    }

    [Fact]
    // Method: ReadTopLevelChunks_ReturnsNormalizedFourCC
    // Purpose: Retrieves read top level chunks returns normalized four CC data for the automated test and verification layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to AdtChunkReaderTests so callers do not duplicate validation, protocol, or persistence rules.
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
