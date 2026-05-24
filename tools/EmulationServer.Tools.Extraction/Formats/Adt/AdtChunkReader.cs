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
 * File overview: tools/EmulationServer.Tools.Extraction/Formats/Adt/AdtChunkReader.cs
 * Documents the AdtChunkReader source file in the client data extraction and conversion tooling area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Tools.Extraction.Formats.Adt;

/**
 * Owns the adt chunk reader behavior for the client data extraction and conversion tooling layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public static class AdtChunkReader
{
    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of AdtChunkReader and keeps this workflow isolated from the caller.
      */
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

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of AdtChunkReader and keeps this workflow isolated from the caller.
      */
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

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of AdtChunkReader and keeps this workflow isolated from the caller.
      */
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
