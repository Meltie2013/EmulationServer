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
  * File overview: tools/EmulationServer.Tools.Extraction/Formats/Vmaps/Conversion/WmoGroupReader.cs
  * This file reads geometry from WMO group files for compact vmap model generation.
  */

namespace EmulationServer.Tools.Extraction.Formats.Vmaps.Conversion;

/**
  * Converts a client WMO group file into vertex and triangle lists.
  * Only the geometry needed for early vmap collision and line-of-sight testing is extracted at this stage.
  */
public static class WmoGroupReader
{
    private const int MogpHeaderSize = 68;

    /**
      * Reads one WMO group file.
      * Groups without vertices or triangle indices are returned as empty geometry so callers can skip them cleanly.
      */
    public static WmoGroupGeometry Read(string path, int groupIndex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        byte[] data = File.ReadAllBytes(path);
        IReadOnlyList<WmoChunk> chunks = WmoChunkReader.ReadTopLevelChunks(data);
        uint flags = 0;
        VmapBounds bounds = VmapBounds.Empty;

        ReadOnlySpan<byte> geometryData = data;
        IReadOnlyList<WmoChunk> geometryChunks = chunks;

        if (WmoChunkReader.TryFind(chunks, "MOGP", out WmoChunk mogp))
        {
            ReadOnlySpan<byte> mogpData = data.AsSpan(mogp.DataOffset, mogp.Size);
            ReadMogpHeader(mogpData, out flags, out bounds);
            geometryData = GetMogpGeometryData(mogpData);
            geometryChunks = WmoChunkReader.ReadTopLevelChunks(geometryData);
        }

        IReadOnlyList<VmapVector3> vertices = WmoChunkReader.TryFind(geometryChunks, "MOVT", out WmoChunk movt)
            ? ReadVertices(geometryData.Slice(movt.DataOffset, movt.Size))
            : [];

        IReadOnlyList<int> indices = WmoChunkReader.TryFind(geometryChunks, "MOVI", out WmoChunk movi)
            ? ReadIndices(geometryData.Slice(movi.DataOffset, movi.Size), vertices.Count)
            : [];

        if (bounds == VmapBounds.Empty && vertices.Count > 0)
        {
            bounds = VmapBounds.FromVertices(vertices);
        }

        return new WmoGroupGeometry(groupIndex, flags, bounds, vertices, indices);
    }

    /**
      * Returns the nested chunk area inside a MOGP payload.
      * WMO group files store the group header first, then place MOVT, MOVI, and related geometry chunks inside the MOGP body.
      */
    private static ReadOnlySpan<byte> GetMogpGeometryData(ReadOnlySpan<byte> mogpData)
    {
        return mogpData.Length > MogpHeaderSize
            ? mogpData[MogpHeaderSize..]
            : ReadOnlySpan<byte>.Empty;
    }

    /**
      * Reads group flags and bounds from the MOGP header.
      */
    private static void ReadMogpHeader(ReadOnlySpan<byte> data, out uint flags, out VmapBounds bounds)
    {
        flags = 0;
        bounds = VmapBounds.Empty;

        if (data.Length >= 12)
        {
            flags = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(8, sizeof(uint)));
        }

        if (data.Length >= 36)
        {
            bounds = new VmapBounds(
                new VmapVector3(ReadSingle(data, 12), ReadSingle(data, 16), ReadSingle(data, 20)),
                new VmapVector3(ReadSingle(data, 24), ReadSingle(data, 28), ReadSingle(data, 32)));
        }
    }

    /**
      * Reads MOVT vertex data from a WMO group.
      */
    private static IReadOnlyList<VmapVector3> ReadVertices(ReadOnlySpan<byte> data)
    {
        int count = data.Length / 12;
        List<VmapVector3> vertices = new(count);

        for (int i = 0; i < count; i++)
        {
            int offset = i * 12;
            vertices.Add(new VmapVector3(ReadSingle(data, offset), ReadSingle(data, offset + 4), ReadSingle(data, offset + 8)));
        }

        return vertices;
    }

    /**
      * Reads MOVI triangle indices from a WMO group and drops incomplete or invalid triangles.
      */
    private static IReadOnlyList<int> ReadIndices(ReadOnlySpan<byte> data, int vertexCount)
    {
        int rawCount = data.Length / sizeof(ushort);
        List<int> indices = new(rawCount);

        for (int i = 0; i + 2 < rawCount; i += 3)
        {
            int first = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(i * sizeof(ushort), sizeof(ushort)));
            int second = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice((i + 1) * sizeof(ushort), sizeof(ushort)));
            int third = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice((i + 2) * sizeof(ushort), sizeof(ushort)));

            if (first >= vertexCount || second >= vertexCount || third >= vertexCount)
            {
                continue;
            }

            indices.Add(first);
            indices.Add(second);
            indices.Add(third);
        }

        return indices;
    }

    /**
      * Reads a little-endian single precision float from a span.
      */
    private static float ReadSingle(ReadOnlySpan<byte> data, int offset)
    {
        return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(data.Slice(offset, sizeof(int))));
    }
}
