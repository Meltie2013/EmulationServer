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


/**
 * File overview: tools/EmulationServer.Tools.Extraction/Formats/Vmaps/Conversion/WmoRootReader.cs
 * Documents the WmoRootReader source file in the client data extraction and conversion tooling area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Tools.Extraction.Formats.Vmaps.Conversion;

/**
  * Reads root WMO metadata such as group count.
  * Detailed material, portal, light, and doodad data is intentionally left for later gameplay-specific conversion passes.
  */
public static class WmoRootReader
{
    /**
      * Reads a root WMO file and returns the number of group files it references.
      */
    public static WmoRootInfo Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        byte[] data = File.ReadAllBytes(path);
        IReadOnlyList<WmoChunk> chunks = WmoChunkReader.ReadTopLevelChunks(data);

        if (!WmoChunkReader.TryFind(chunks, "MOHD", out WmoChunk mohd) || mohd.Size < 8)
        {
            return new WmoRootInfo(0);
        }

        ReadOnlySpan<byte> mohdData = data.AsSpan(mohd.DataOffset, mohd.Size);
        int groupCount = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(mohdData.Slice(4, sizeof(uint))));
        return new WmoRootInfo(groupCount);
    }
}
