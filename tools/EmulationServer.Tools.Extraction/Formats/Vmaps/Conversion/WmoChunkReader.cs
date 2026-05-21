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

using System.Buffers.Binary;
using System.Text;

/**
  * File overview: tools/EmulationServer.Tools.Extraction/Formats/Vmaps/Conversion/WmoChunkReader.cs
  * This file provides low-level WMO chunk scanning helpers used by the vmap converter.
  */

namespace EmulationServer.Tools.Extraction.Formats.Vmaps.Conversion;

/**
  * Reads WMO chunk headers without interpreting the full WMO model format.
  * The converter only needs a small set of chunks, but a reusable scanner keeps the parsing code compact and testable.
  */
public static class WmoChunkReader
{
    private const int ChunkHeaderSize = 8;

    /**
      * Reads top-level chunks from a WMO file.
      * The method tolerates trailing bytes by stopping when the next full chunk header cannot be read.
      */
    public static IReadOnlyList<WmoChunk> ReadTopLevelChunks(ReadOnlySpan<byte> data)
    {
        List<WmoChunk> chunks = [];
        int offset = 0;

        while (offset + ChunkHeaderSize <= data.Length)
        {
            string fourCC = ReadWmoFourCC(data, offset);
            int size = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset + 4, sizeof(uint))));
            int dataOffset = offset + ChunkHeaderSize;
            int nextOffset = dataOffset + size;

            if (size < 0 || nextOffset < dataOffset || nextOffset > data.Length)
            {
                break;
            }

            chunks.Add(new WmoChunk(fourCC, offset, size, dataOffset));
            offset = nextOffset;
        }

        return chunks;
    }

    /**
      * Attempts to find the first chunk with the specified FourCC.
      */
    public static bool TryFind(IReadOnlyList<WmoChunk> chunks, string fourCC, out WmoChunk chunk)
    {
        foreach (WmoChunk candidate in chunks)
        {
            if (string.Equals(candidate.FourCC, fourCC, StringComparison.Ordinal))
            {
                chunk = candidate;
                return true;
            }
        }

        chunk = default;
        return false;
    }

    /**
      * Reads a WMO FourCC value.
      * WoW chunk identifiers are stored as little-endian integer values, so the display order is reversed.
      */
    public static string ReadWmoFourCC(ReadOnlySpan<byte> data, int offset)
    {
        if (offset < 0 || offset + 4 > data.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, "FourCC offset is outside the WMO data.");
        }

        Span<byte> bytes = stackalloc byte[4];
        bytes[0] = data[offset + 3];
        bytes[1] = data[offset + 2];
        bytes[2] = data[offset + 1];
        bytes[3] = data[offset];
        return Encoding.ASCII.GetString(bytes);
    }
}
