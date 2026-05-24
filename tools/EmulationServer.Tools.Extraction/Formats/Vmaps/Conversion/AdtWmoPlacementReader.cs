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
using EmulationServer.Tools.Extraction.Formats.Adt;


/**
 * File overview: tools/EmulationServer.Tools.Extraction/Formats/Vmaps/Conversion/AdtWmoPlacementReader.cs
 * Documents the AdtWmoPlacementReader source file in the client data extraction and conversion tooling area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Tools.Extraction.Formats.Vmaps.Conversion;

/**
  * Extracts WMO placement records from ADT MWMO and MODF chunks.
  * MWMO stores the referenced WMO file names and MODF stores where each WMO appears inside the map tile.
  */
public static class AdtWmoPlacementReader
{
    /**
     * Defines the constant value for modf record size.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    private const int ModfRecordSize = 64;

    /**
      * Reads all WMO placement records from one ADT file.
      */
    public static IReadOnlyList<VmapPlacement> Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        byte[] data = File.ReadAllBytes(path);
        IReadOnlyList<AdtChunk> chunks = AdtChunkReader.ReadTopLevelChunks(data);

        AdtChunk? mwmo = FindChunk(chunks, "MWMO");
        AdtChunk? modf = FindChunk(chunks, "MODF");

        if (!mwmo.HasValue || !modf.HasValue)
        {
            return [];
        }

        Dictionary<uint, string> modelNames = ReadModelNames(data.AsSpan(mwmo.Value.DataOffset, mwmo.Value.Size));
        return ReadPlacements(data.AsSpan(modf.Value.DataOffset, modf.Value.Size), modelNames);
    }

    private static Dictionary<uint, string> ReadModelNames(ReadOnlySpan<byte> data)
    {
        Dictionary<uint, string> names = [];
        int offset = 0;

        while (offset < data.Length)
        {
            int end = offset;

            while (end < data.Length && data[end] != 0)
            {
                end++;
            }

            if (end > offset)
            {
                string value = Encoding.UTF8.GetString(data.Slice(offset, end - offset));
                names[(uint)offset] = value;
            }

            offset = end + 1;
        }

        return names;
    }

    /**
      * Reads MODF records and resolves their model-name offsets through the MWMO string block.
      */
    private static IReadOnlyList<VmapPlacement> ReadPlacements(ReadOnlySpan<byte> data, IReadOnlyDictionary<uint, string> modelNames)
    {
        int count = data.Length / ModfRecordSize;
        List<VmapPlacement> placements = new(count);

        for (int i = 0; i < count; i++)
        {
            ReadOnlySpan<byte> record = data.Slice(i * ModfRecordSize, ModfRecordSize);
            uint nameOffset = BinaryPrimitives.ReadUInt32LittleEndian(record.Slice(0, sizeof(uint)));

            if (!modelNames.TryGetValue(nameOffset, out string? modelPath) || string.IsNullOrWhiteSpace(modelPath))
            {
                continue;
            }

            uint uniqueId = BinaryPrimitives.ReadUInt32LittleEndian(record.Slice(4, sizeof(uint)));
            VmapVector3 position = ReadVector(record, 8);
            VmapVector3 rotation = ReadVector(record, 20);
            VmapBounds bounds = new(ReadVector(record, 32), ReadVector(record, 44));
            uint flags = BinaryPrimitives.ReadUInt32LittleEndian(record.Slice(56, sizeof(uint)));
            ushort doodadSet = BinaryPrimitives.ReadUInt16LittleEndian(record.Slice(60, sizeof(ushort)));
            ushort nameSet = BinaryPrimitives.ReadUInt16LittleEndian(record.Slice(62, sizeof(ushort)));

            placements.Add(new VmapPlacement(
                VmapModelName.FromRelativePath(modelPath),
                uniqueId,
                position,
                rotation,
                bounds,
                flags,
                doodadSet,
                nameSet));
        }

        return placements;
    }

    /**
      * Reads a three-component float vector from a record.
      */
    private static VmapVector3 ReadVector(ReadOnlySpan<byte> data, int offset)
    {
        return new VmapVector3(ReadSingle(data, offset), ReadSingle(data, offset + 4), ReadSingle(data, offset + 8));
    }

    /**
      * Reads a little-endian single precision float from a span.
      */
    private static float ReadSingle(ReadOnlySpan<byte> data, int offset)
    {
        return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(data.Slice(offset, sizeof(int))));
    }

    /**
      * Finds a top-level ADT chunk by FourCC.
      */
    private static AdtChunk? FindChunk(IReadOnlyList<AdtChunk> chunks, string fourCC)
    {
        foreach (AdtChunk chunk in chunks)
        {
            if (string.Equals(chunk.FourCC, fourCC, StringComparison.Ordinal))
            {
                return chunk;
            }
        }

        return null;
    }
}
