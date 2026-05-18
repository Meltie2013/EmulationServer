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
using EmulationServer.Tools.Extraction.Formats.Maps;
using EmulationServer.Tools.Extraction.Formats.Maps.Conversion;

/**
  * File overview: tools/EmulationServer.Tools.Extraction/Formats/Adt/AdtTileReader.cs
  * This file belongs to the developer tooling for data extraction, validation, and diagnostics portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Tools.Extraction.Formats.Adt;

/**
  * Represents the adt tile reader component in the developer tooling for data extraction, validation, and diagnostics area.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public static class AdtTileReader
{
    private const int CellsPerGrid = 16;
    private const int CellSize = 8;
    private const int GridSize = 128;

    private const int ChunkHeaderSize = 8;
    private const int McnkHeaderSize = 128;
    private const int McnkOffsetHeight = 0x14;
    private const int McnkOffsetAreaId = 0x34;
    private const int McnkOffsetHoles = 0x3C;
    private const int McnkOffsetLiquid = 0x60;
    private const int McnkOffsetLiquidSize = 0x64;
    private const int McnkOffsetPosition = 0x68;

    private const int McvtHeightCount = 145;
    private const int MclqLiquidVertexCount = 9 * 9;
    private const int MclqLiquidVertexSize = 8;
    private const int MclqHeightBoundsSize = 8;
    private const int MclqLiquidVertexDataOffset = MclqHeightBoundsSize;
    private const int MclqFlagsOffset = MclqLiquidVertexDataOffset + MclqLiquidVertexCount * MclqLiquidVertexSize;
    private const int MclqFlagsSize = 8 * 8;
    private const int MclqMinimumDataSize = MclqFlagsOffset + MclqFlagsSize;

    private const int Mh2oHeaderSize = 12;
    private const int Mh2oInstanceSize = 24;

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of AdtTileReader and keeps this workflow isolated from the caller.
      */
    public static AdtTile Read(string path, LiquidTypeIndex? liquidTypes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        byte[] data = File.ReadAllBytes(path);
        IReadOnlyList<AdtChunk> topLevelChunks = AdtChunkReader.ReadTopLevelChunks(data);
        IReadOnlyList<AdtChunk> terrainChunks = ReadMcnkChunks(data, topLevelChunks);

        List<AdtCell> cells = [];

        foreach (AdtChunk chunk in terrainChunks)
        {
            cells.Add(ReadCell(data, chunk));
        }

        if (cells.Count == 0)
        {
            throw new AdtFormatException($"{path} does not contain any MCNK chunks.");
        }

        AdtLiquidData liquid = ReadLiquidData(data, terrainChunks, topLevelChunks, liquidTypes ?? LiquidTypeIndex.Empty);

        return new AdtTile(path, cells, liquid);
    }

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of AdtTileReader and keeps this workflow isolated from the caller.
      */
    private static IReadOnlyList<AdtChunk> ReadMcnkChunks(ReadOnlySpan<byte> data, IReadOnlyList<AdtChunk> topLevelChunks)
    {
        List<AdtChunk> chunks = [];

        AdtChunk? mcin = FindChunk(topLevelChunks, "MCIN");

        if (mcin.HasValue && mcin.Value.Size >= CellsPerGrid * CellsPerGrid * 16)
        {
            AdtChunk mcinChunk = mcin.Value;
            ReadOnlySpan<byte> mcinData = data.Slice(mcinChunk.DataOffset, mcinChunk.Size);

            for (int i = 0; i < CellsPerGrid * CellsPerGrid; i++)
            {
                int entryOffset = i * 16;

                uint mcnkOffset = BinaryPrimitives.ReadUInt32LittleEndian(mcinData.Slice(entryOffset, sizeof(uint)));
                uint mcnkSize = BinaryPrimitives.ReadUInt32LittleEndian(mcinData.Slice(entryOffset + 4, sizeof(uint)));

                if (mcnkOffset == 0 || mcnkSize == 0 || mcnkOffset > int.MaxValue)
                {
                    continue;
                }

                int chunkOffset = checked((int)mcnkOffset);

                if (chunkOffset < 0 || chunkOffset + ChunkHeaderSize > data.Length)
                {
                    continue;
                }

                string fourCC = AdtChunkReader.ReadAdtFourCC(data, chunkOffset);

                if (!string.Equals(fourCC, "MCNK", StringComparison.Ordinal))
                {
                    continue;
                }

                int chunkSize = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(chunkOffset + 4, sizeof(uint))));
                int dataOffset = chunkOffset + ChunkHeaderSize;

                if (chunkSize <= 0 || dataOffset + chunkSize > data.Length)
                {
                    continue;
                }

                chunks.Add(new AdtChunk("MCNK", chunkOffset, chunkSize, dataOffset));
            }
        }

        if (chunks.Count > 0)
        {
            return chunks;
        }

        return topLevelChunks
            .Where(chunk => string.Equals(chunk.FourCC, "MCNK", StringComparison.Ordinal))
            .ToArray();
    }

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of AdtTileReader and keeps this workflow isolated from the caller.
      */
    private static AdtCell ReadCell(ReadOnlySpan<byte> data, AdtChunk chunk)
    {
        ReadOnlySpan<byte> cell = data.Slice(chunk.DataOffset, chunk.Size);

        if (cell.Length < McnkHeaderSize)
        {
            throw new AdtFormatException("MCNK chunk is smaller than the expected 128-byte header.");
        }

        uint flags = ReadUInt32(cell, 0x00);
        int indexX = checked((int)ReadUInt32(cell, 0x04));
        int indexY = checked((int)ReadUInt32(cell, 0x08));
        uint offsetHeight = ReadUInt32(cell, McnkOffsetHeight);
        uint areaId = ReadUInt32(cell, McnkOffsetAreaId);
        ushort holes = unchecked((ushort)ReadUInt32(cell, McnkOffsetHoles));
        float baseHeight = ReadBaseHeight(cell);
        float[] heights = ReadHeights(data, chunk, offsetHeight);

        return new AdtCell
        {
            IndexX = indexX,
            IndexY = indexY,
            Flags = flags,
            AreaId = areaId,
            Holes = holes,
            BaseHeight = baseHeight,
            Heights = heights,
        };
    }

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of AdtTileReader and keeps this workflow isolated from the caller.
      */
    private static AdtLiquidData ReadLiquidData(
        ReadOnlySpan<byte> data,
        IReadOnlyList<AdtChunk> terrainChunks,
        IReadOnlyList<AdtChunk> topLevelChunks,
        LiquidTypeIndex liquidTypes)
    {
        AdtLiquidData liquid = new();

        ReadMclqLiquid(data, terrainChunks, liquid);
        ReadMh2oLiquid(data, topLevelChunks, liquid, liquidTypes);

        return liquid;
    }

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of AdtTileReader and keeps this workflow isolated from the caller.
      */
    private static void ReadMclqLiquid(ReadOnlySpan<byte> data, IReadOnlyList<AdtChunk> terrainChunks, AdtLiquidData liquid)
    {
        foreach (AdtChunk chunk in terrainChunks)
        {
            ReadOnlySpan<byte> cell = data.Slice(chunk.DataOffset, chunk.Size);

            if (cell.Length < McnkHeaderSize)
            {
                continue;
            }

            int cellX = checked((int)ReadUInt32(cell, 0x04));
            int cellY = checked((int)ReadUInt32(cell, 0x08));

            if (!IsValidCellIndex(cellX) || !IsValidCellIndex(cellY))
            {
                continue;
            }

            uint offsetLiquid = ReadUInt32(cell, McnkOffsetLiquid);
            uint liquidSize = ReadUInt32(cell, McnkOffsetLiquidSize);

            if (offsetLiquid == 0 || liquidSize <= ChunkHeaderSize || liquidSize > int.MaxValue)
            {
                continue;
            }

            if (!TryReadMclqData(data, chunk, offsetLiquid, checked((int)liquidSize), out ReadOnlySpan<byte> mclq))
            {
                continue;
            }

            if (mclq.Length < MclqMinimumDataSize)
            {
                continue;
            }

            liquid.MclqCells++;

            int visibleCount = 0;

            for (int y = 0; y < CellSize; y++)
            {
                int cy = cellY * CellSize + y;

                for (int x = 0; x < CellSize; x++)
                {
                    int cx = cellX * CellSize + x;
                    byte flag = mclq[MclqFlagsOffset + y * CellSize + x];

                    if (flag == 0x0F)
                    {
                        continue;
                    }

                    liquid.MarkVisible(cy, cx);
                    visibleCount++;

                    if ((flag & 0x80) != 0)
                    {
                        liquid.Flags[cellY, cellX] |= MapFormatConstants.MapLiquidTypeDarkWater;
                    }
                }
            }

            uint cellFlags = ReadUInt32(cell, 0x00);

            if ((cellFlags & (1u << 2)) != 0)
            {
                liquid.Entry[cellY, cellX] = 1;
                liquid.Flags[cellY, cellX] |= MapFormatConstants.MapLiquidTypeWater;
            }

            if ((cellFlags & (1u << 3)) != 0)
            {
                liquid.Entry[cellY, cellX] = 2;
                liquid.Flags[cellY, cellX] |= MapFormatConstants.MapLiquidTypeOcean;
            }

            if ((cellFlags & (1u << 4)) != 0)
            {
                liquid.Entry[cellY, cellX] = 3;
                liquid.Flags[cellY, cellX] |= MapFormatConstants.MapLiquidTypeMagma;
            }

            if ((cellFlags & (1u << 5)) != 0)
            {
                liquid.Entry[cellY, cellX] = 4;
                liquid.Flags[cellY, cellX] |= MapFormatConstants.MapLiquidTypeSlime;
            }

            // Some old ADTs contain visible MCLQ data but weak/unknown liquid flags.
            // Do not silently discard visible liquid. Fall back to normal water.
            if (visibleCount > 0 && liquid.Flags[cellY, cellX] == MapFormatConstants.MapLiquidTypeNoWater)
            {
                liquid.Entry[cellY, cellX] = 1;
                liquid.Flags[cellY, cellX] = MapFormatConstants.MapLiquidTypeWater;
            }

            if (visibleCount == 0)
            {
                continue;
            }

            for (int y = 0; y <= CellSize; y++)
            {
                int cy = cellY * CellSize + y;

                for (int x = 0; x <= CellSize; x++)
                {
                    int cx = cellX * CellSize + x;
                    int vertexOffset = MclqLiquidVertexDataOffset + (y * (CellSize + 1) + x) * MclqLiquidVertexSize;

                    if (vertexOffset + MclqLiquidVertexSize > mclq.Length)
                    {
                        continue;
                    }

                    liquid.Heights[cy, cx] = BinaryPrimitives.ReadSingleLittleEndian(mclq.Slice(vertexOffset + 4, sizeof(float)));
                }
            }
        }
    }

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of AdtTileReader and keeps this workflow isolated from the caller.
      */
    private static void ReadMh2oLiquid(
        ReadOnlySpan<byte> data,
        IReadOnlyList<AdtChunk> topLevelChunks,
        AdtLiquidData liquid,
        LiquidTypeIndex liquidTypes)
    {
        AdtChunk? mh2oChunk = FindChunk(topLevelChunks, "MH2O");

        if (!mh2oChunk.HasValue)
        {
            return;
        }

        AdtChunk mh2oChunkValue = mh2oChunk.Value;
        ReadOnlySpan<byte> mh2o = data.Slice(mh2oChunkValue.DataOffset, mh2oChunkValue.Size);

        if (mh2o.Length < CellsPerGrid * CellsPerGrid * Mh2oHeaderSize)
        {
            return;
        }

        for (int cellY = 0; cellY < CellsPerGrid; cellY++)
        {
            for (int cellX = 0; cellX < CellsPerGrid; cellX++)
            {
                int cellIndex = cellY * CellsPerGrid + cellX;
                int headerOffset = cellIndex * Mh2oHeaderSize;

                uint instanceOffset = ReadUInt32(mh2o, headerOffset);
                uint layerCount = ReadUInt32(mh2o, headerOffset + 4);

                if (instanceOffset == 0 || layerCount == 0 || instanceOffset > int.MaxValue)
                {
                    continue;
                }

                for (uint layer = 0; layer < layerCount; layer++)
                {
                    int layerOffset = checked((int)instanceOffset + (int)layer * Mh2oInstanceSize);

                    if (layerOffset < 0 || layerOffset + Mh2oInstanceSize > mh2o.Length)
                    {
                        continue;
                    }

                    ushort liquidTypeId = BinaryPrimitives.ReadUInt16LittleEndian(mh2o.Slice(layerOffset, sizeof(ushort)));
                    ushort liquidVertexFormat = BinaryPrimitives.ReadUInt16LittleEndian(mh2o.Slice(layerOffset + 2, sizeof(ushort)));
                    float minimumLiquidLevel = BinaryPrimitives.ReadSingleLittleEndian(mh2o.Slice(layerOffset + 4, sizeof(float)));
                    float maximumLiquidLevel = BinaryPrimitives.ReadSingleLittleEndian(mh2o.Slice(layerOffset + 8, sizeof(float)));
                    byte offsetX = mh2o[layerOffset + 12];
                    byte offsetY = mh2o[layerOffset + 13];
                    byte width = mh2o[layerOffset + 14];
                    byte height = mh2o[layerOffset + 15];
                    uint existsBitmapOffset = BinaryPrimitives.ReadUInt32LittleEndian(mh2o.Slice(layerOffset + 16, sizeof(uint)));
                    uint heightMapOffset = BinaryPrimitives.ReadUInt32LittleEndian(mh2o.Slice(layerOffset + 20, sizeof(uint)));

                    if (width == 0 || height == 0)
                    {
                        continue;
                    }

                    liquid.Mh2oCells++;

                    liquid.Entry[cellY, cellX] = liquidTypeId;
                    liquid.Flags[cellY, cellX] |= liquidTypes.GetMapLiquidFlags(liquidTypeId);

                    ulong existsBitmap = existsBitmapOffset == 0 || existsBitmapOffset + sizeof(ulong) > mh2o.Length
                        ? ulong.MaxValue
                        : BinaryPrimitives.ReadUInt64LittleEndian(mh2o.Slice(checked((int)existsBitmapOffset), sizeof(ulong)));

                    int visibleCount = 0;

                    for (int y = 0; y < height; y++)
                    {
                        int cy = cellY * CellSize + offsetY + y;

                        if (cy < 0 || cy >= GridSize)
                        {
                            existsBitmap >>= width;
                            continue;
                        }

                        for (int x = 0; x < width; x++)
                        {
                            int cx = cellX * CellSize + offsetX + x;
                            bool visible = (existsBitmap & 1UL) != 0;

                            if (visible && cx >= 0 && cx < GridSize)
                            {
                                liquid.MarkVisible(cy, cx);
                                visibleCount++;
                            }

                            existsBitmap >>= 1;
                        }
                    }

                    if (visibleCount == 0 || liquid.Flags[cellY, cellX] == MapFormatConstants.MapLiquidTypeNoWater)
                    {
                        continue;
                    }

                    int heightPosition = 0;

                    for (int y = 0; y <= height; y++)
                    {
                        int cy = cellY * CellSize + offsetY + y;

                        for (int x = 0; x <= width; x++)
                        {
                            int cx = cellX * CellSize + offsetX + x;
                            float liquidHeight = minimumLiquidLevel;

                            if (heightMapOffset != 0 &&
                                TryReadMh2oLiquidHeight(mh2o, heightMapOffset, liquidVertexFormat, heightPosition, minimumLiquidLevel, out float parsedHeight))
                            {
                                liquidHeight = parsedHeight;
                            }

                            if (cy >= 0 && cy <= GridSize && cx >= 0 && cx <= GridSize)
                            {
                                liquid.Heights[cy, cx] = liquidHeight;
                            }

                            heightPosition++;
                        }
                    }
                }
            }
        }
    }

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of AdtTileReader and keeps this workflow isolated from the caller.
      */
    private static float ReadBaseHeight(ReadOnlySpan<byte> cell)
    {
        if (cell.Length < McnkOffsetPosition + 12)
        {
            return 0.0f;
        }

        return ReadSingle(cell, McnkOffsetPosition + 8);
    }

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of AdtTileReader and keeps this workflow isolated from the caller.
      */
    private static float[] ReadHeights(ReadOnlySpan<byte> data, AdtChunk mcnkChunk, uint offsetHeight)
    {
        if (offsetHeight == 0)
        {
            return [];
        }

        if (!TryReadMcnkSubChunk(data, mcnkChunk, offsetHeight, "MCVT", out ReadOnlySpan<byte> mcvt))
        {
            return [];
        }

        int requiredBytes = McvtHeightCount * sizeof(float);

        if (mcvt.Length < requiredBytes)
        {
            return [];
        }

        float[] heights = new float[McvtHeightCount];

        for (int i = 0; i < heights.Length; i++)
        {
            heights[i] = BinaryPrimitives.ReadSingleLittleEndian(mcvt.Slice(i * sizeof(float), sizeof(float)));
        }

        return heights;
    }

    /**
      * Attempts the operation without treating a normal failure as an exceptional condition.
      * The method is part of AdtTileReader and keeps this workflow isolated from the caller.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    private static bool TryReadMclqData(
        ReadOnlySpan<byte> data,
        AdtChunk mcnkChunk,
        uint relativeOffset,
        int liquidSize,
        out ReadOnlySpan<byte> chunkData)
    {
        chunkData = default;

        if (relativeOffset > int.MaxValue || liquidSize <= ChunkHeaderSize)
        {
            return false;
        }

        int offset = checked((int)relativeOffset);

        return TryReadMclqDataAt(data, mcnkChunk.Offset + offset, liquidSize, out chunkData) ||
            TryReadMclqDataAt(data, mcnkChunk.DataOffset + offset, liquidSize, out chunkData);
    }

    /**
      * Attempts the operation without treating a normal failure as an exceptional condition.
      * The method is part of AdtTileReader and keeps this workflow isolated from the caller.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    private static bool TryReadMclqDataAt(
        ReadOnlySpan<byte> data,
        int absoluteOffset,
        int liquidSize,
        out ReadOnlySpan<byte> chunkData)
    {
        chunkData = default;

        if (absoluteOffset < 0 || absoluteOffset >= data.Length)
        {
            return false;
        }

        // Some files/tools expose MCLQ with a normal chunk header.
        if (absoluteOffset + ChunkHeaderSize <= data.Length &&
            string.Equals(AdtChunkReader.ReadAdtFourCC(data, absoluteOffset), "MCLQ", StringComparison.Ordinal))
        {
            int chunkSize = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(absoluteOffset + 4, sizeof(uint))));
            int dataOffset = absoluteOffset + ChunkHeaderSize;

            if (chunkSize >= MclqMinimumDataSize && dataOffset + chunkSize <= data.Length)
            {
                chunkData = data.Slice(dataOffset, chunkSize);
                return true;
            }
        }

        // Vanilla/TBC old liquid is commonly reached directly through the MCNK liquid offset.
        if (absoluteOffset + liquidSize <= data.Length)
        {
            chunkData = data.Slice(absoluteOffset, liquidSize);
            return chunkData.Length >= MclqMinimumDataSize;
        }

        return false;
    }

    /**
      * Attempts the operation without treating a normal failure as an exceptional condition.
      * The method is part of AdtTileReader and keeps this workflow isolated from the caller.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    private static bool TryReadMcnkSubChunk(
        ReadOnlySpan<byte> data,
        AdtChunk mcnkChunk,
        uint relativeOffset,
        string expectedFourCC,
        out ReadOnlySpan<byte> chunkData)
    {
        chunkData = default;

        if (relativeOffset > int.MaxValue)
        {
            return false;
        }

        int offset = checked((int)relativeOffset);

        return TryReadChunkAt(data, mcnkChunk.Offset + offset, expectedFourCC, out chunkData) ||
               TryReadChunkAt(data, mcnkChunk.DataOffset + offset, expectedFourCC, out chunkData);
    }

    /**
      * Attempts the operation without treating a normal failure as an exceptional condition.
      * The method is part of AdtTileReader and keeps this workflow isolated from the caller.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    private static bool TryReadChunkAt(ReadOnlySpan<byte> data, int absoluteOffset, string expectedFourCC, out ReadOnlySpan<byte> chunkData)
    {
        chunkData = default;

        if (absoluteOffset < 0 || absoluteOffset + ChunkHeaderSize > data.Length)
        {
            return false;
        }

        string fourCC = AdtChunkReader.ReadAdtFourCC(data, absoluteOffset);

        if (!string.Equals(fourCC, expectedFourCC, StringComparison.Ordinal))
        {
            return false;
        }

        int chunkSize = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(absoluteOffset + 4, sizeof(uint))));
        int dataOffset = absoluteOffset + ChunkHeaderSize;

        if (chunkSize < 0 || dataOffset + chunkSize > data.Length)
        {
            return false;
        }

        chunkData = data.Slice(dataOffset, chunkSize);
        return true;
    }

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of AdtTileReader and keeps this workflow isolated from the caller.
      */
    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset)
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, sizeof(uint)));
    }

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of AdtTileReader and keeps this workflow isolated from the caller.
      */
    private static float ReadSingle(ReadOnlySpan<byte> data, int offset)
    {
        return BinaryPrimitives.ReadSingleLittleEndian(data.Slice(offset, sizeof(float)));
    }

    /**
      * Finds a matching item in the managed collection and returns the safest available result.
      * The method is part of AdtTileReader and keeps this workflow isolated from the caller.
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

    /**
      * Performs the is valid cell index operation for AdtTileReader.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    private static bool IsValidCellIndex(int value)
    {
        return value >= 0 && value < CellsPerGrid;
    }

    /**
      * Attempts the operation without treating a normal failure as an exceptional condition.
      * The method is part of AdtTileReader and keeps this workflow isolated from the caller.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    private static bool TryReadMh2oLiquidHeight(
        ReadOnlySpan<byte> mh2o,
        uint vertexDataOffset,
        ushort liquidVertexFormat,
        int vertexIndex,
        float fallbackHeight,
        out float height)
    {
        height = fallbackHeight;

        if (vertexDataOffset == 0 || vertexDataOffset > int.MaxValue)
        {
            return false;
        }

        int stride = liquidVertexFormat switch
        {
            0 => 5, // height + depth
            1 => 8, // height + UV
            2 => 1, // depth only, no explicit height
            3 => 9, // height + UV + depth
            _ => 0,
        };

        if (stride == 0 || liquidVertexFormat == 2)
        {
            return false;
        }

        int valueOffset = checked((int)vertexDataOffset + vertexIndex * stride);

        if (valueOffset < 0 || valueOffset + sizeof(float) > mh2o.Length)
        {
            return false;
        }

        height = BinaryPrimitives.ReadSingleLittleEndian(mh2o.Slice(valueOffset, sizeof(float)));
        return float.IsFinite(height);
    }
}
