
using System.Text;
using EmulationServer.Tools.Extraction.Formats.Adt;
using EmulationServer.Tools.Extraction.Formats.Maps;

namespace EmulationServer.Tools.Extraction.Formats.Maps.Conversion;

public sealed class MangosMapTileConverter
{
    private const int CellsPerGrid = MapFormatConstants.AdtCellsPerGrid;
    private const int CellSize = 8;
    private const int GridSize = MapFormatConstants.AdtGridSize;
    private const int HeaderSize = MapFormatConstants.MapFileHeaderSize;
    private const float MinimumStoredHeight = -500.0f;

    private readonly AreaTableIndex _areaTable;
    private readonly LiquidTypeIndex _liquidTypes;

    public MangosMapTileConverter(AreaTableIndex areaTable, LiquidTypeIndex liquidTypes)
    {
        _areaTable = areaTable ?? throw new ArgumentNullException(nameof(areaTable));
        _liquidTypes = liquidTypes ?? throw new ArgumentNullException(nameof(liquidTypes));
    }

    public MapTileConversionReport Convert(string adtPath, string outputPath, uint build)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adtPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        AdtTile tile = AdtTileReader.Read(adtPath, _liquidTypes);
        TileData tileData = BuildTileData(tile);

        byte[] areaSection = BuildAreaSection(tileData.AreaFlags);
        byte[] heightSection = BuildHeightSection(tileData.V9, tileData.V8);
        byte[] liquidSection = BuildLiquidSection(tileData);
        byte[] holesSection = BuildHolesSection(tileData.Holes);

        uint areaOffset = HeaderSize;
        uint areaSize = checked((uint)areaSection.Length);

        uint heightOffset = checked(areaOffset + areaSize);
        uint heightSize = checked((uint)heightSection.Length);

        uint liquidOffset = liquidSection.Length > 0
            ? checked(heightOffset + heightSize)
            : 0;

        uint liquidSize = checked((uint)liquidSection.Length);

        uint holesOffset = holesSection.Length > 0
            ? checked((liquidOffset != 0 ? liquidOffset + liquidSize : heightOffset + heightSize))
            : 0;

        uint holesSize = checked((uint)holesSection.Length);

        string? parentDirectory = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrWhiteSpace(parentDirectory))
        {
            Directory.CreateDirectory(parentDirectory);
        }

        using FileStream stream = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        using BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: false);

        WriteFourCC(writer, MapFormatConstants.MapMagic);
        WriteFourCC(writer, MapFormatConstants.VersionMagic);
        writer.Write(build);
        writer.Write(areaOffset);
        writer.Write(areaSize);
        writer.Write(heightOffset);
        writer.Write(heightSize);
        writer.Write(liquidOffset);
        writer.Write(liquidSize);
        writer.Write(holesOffset);
        writer.Write(holesSize);

        writer.Write(areaSection);
        writer.Write(heightSection);

        if (liquidSection.Length > 0)
        {
            writer.Write(liquidSection);
        }

        if (holesSection.Length > 0)
        {
            writer.Write(holesSection);
        }

        writer.Flush();

        return new MapTileConversionReport(
            liquidSection.Length > 0,
            tile.Liquid.MclqCells,
            tile.Liquid.Mh2oCells,
            CountLiquidCells(tileData.LiquidFlags),
            CountVisibleLiquidTiles(tileData.LiquidShow),
            liquidSection.Length);
    }

    private TileData BuildTileData(AdtTile tile)
    {
        ushort[,] areaFlags = new ushort[CellsPerGrid, CellsPerGrid];
        ushort[,] holes = new ushort[CellsPerGrid, CellsPerGrid];
        float[,] v9 = new float[GridSize + 1, GridSize + 1];
        float[,] v8 = new float[GridSize, GridSize];

        Fill(areaFlags, ushort.MaxValue);
        Fill(v9, MinimumStoredHeight);
        Fill(v8, MinimumStoredHeight);

        foreach (AdtCell cell in tile.Cells)
        {
            if (!IsValidCellIndex(cell.IndexX) || !IsValidCellIndex(cell.IndexY))
            {
                continue;
            }

            int cellX = cell.IndexX;
            int cellY = cell.IndexY;

            areaFlags[cellY, cellX] = _areaTable.GetAreaFlag(cell.AreaId);
            holes[cellY, cellX] = cell.Holes;

            for (int y = 0; y <= CellSize; y++)
            {
                int cy = cellY * CellSize + y;

                for (int x = 0; x <= CellSize; x++)
                {
                    int cx = cellX * CellSize + x;
                    v9[cy, cx] = cell.BaseHeight;
                }
            }

            for (int y = 0; y < CellSize; y++)
            {
                int cy = cellY * CellSize + y;

                for (int x = 0; x < CellSize; x++)
                {
                    int cx = cellX * CellSize + x;
                    v8[cy, cx] = cell.BaseHeight;
                }
            }

            if (!cell.HasHeights)
            {
                continue;
            }

            for (int y = 0; y <= CellSize; y++)
            {
                int cy = cellY * CellSize + y;

                for (int x = 0; x <= CellSize; x++)
                {
                    int cx = cellX * CellSize + x;
                    v9[cy, cx] += cell.Heights[y * (CellSize * 2 + 1) + x];
                }
            }

            for (int y = 0; y < CellSize; y++)
            {
                int cy = cellY * CellSize + y;

                for (int x = 0; x < CellSize; x++)
                {
                    int cx = cellX * CellSize + x;
                    v8[cy, cx] += cell.Heights[y * (CellSize * 2 + 1) + CellSize + 1 + x];
                }
            }
        }

        return new TileData(
            areaFlags,
            holes,
            v9,
            v8,
            tile.Liquid.Entry,
            tile.Liquid.Flags,
            tile.Liquid.Show,
            tile.Liquid.Heights);
    }

    private static byte[] BuildAreaSection(ushort[,] areaFlags)
    {
        ushort firstArea = areaFlags[0, 0];
        bool fullAreaData = false;

        for (int y = 0; y < CellsPerGrid && !fullAreaData; y++)
        {
            for (int x = 0; x < CellsPerGrid; x++)
            {
                if (areaFlags[y, x] != firstArea)
                {
                    fullAreaData = true;
                    break;
                }
            }
        }

        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: true);

        WriteFourCC(writer, MapFormatConstants.AreaMagic);

        if (fullAreaData)
        {
            writer.Write((ushort)0);
            writer.Write((ushort)0);

            for (int y = 0; y < CellsPerGrid; y++)
            {
                for (int x = 0; x < CellsPerGrid; x++)
                {
                    writer.Write(areaFlags[y, x]);
                }
            }
        }
        else
        {
            writer.Write(MapFormatConstants.MapAreaNoArea);
            writer.Write(firstArea);
        }

        writer.Flush();
        return stream.ToArray();
    }

    private static byte[] BuildHeightSection(float[,] v9, float[,] v8)
    {
        float minimum = float.PositiveInfinity;
        float maximum = float.NegativeInfinity;

        foreach (float height in v8)
        {
            minimum = Math.Min(minimum, height);
            maximum = Math.Max(maximum, height);
        }

        foreach (float height in v9)
        {
            minimum = Math.Min(minimum, height);
            maximum = Math.Max(maximum, height);
        }

        if (!float.IsFinite(minimum) || !float.IsFinite(maximum))
        {
            minimum = MinimumStoredHeight;
            maximum = MinimumStoredHeight;
        }

        if (minimum < MinimumStoredHeight)
        {
            ClampMinimum(v9, MinimumStoredHeight);
            ClampMinimum(v8, MinimumStoredHeight);
            minimum = MinimumStoredHeight;
            maximum = Math.Max(maximum, MinimumStoredHeight);
        }

        uint flags = Math.Abs(maximum - minimum) < 0.0001f
            ? MapFormatConstants.MapHeightNoHeight
            : 0;

        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: true);

        WriteFourCC(writer, MapFormatConstants.HeightMagic);
        writer.Write(flags);
        writer.Write(minimum);
        writer.Write(maximum);

        if ((flags & MapFormatConstants.MapHeightNoHeight) == 0)
        {
            WriteFloatGrid(writer, v9, GridSize + 1, GridSize + 1);
            WriteFloatGrid(writer, v8, GridSize, GridSize);
        }

        writer.Flush();
        return stream.ToArray();
    }

    private static byte[] BuildLiquidSection(TileData tileData)
    {
        ushort firstLiquidEntry = tileData.LiquidEntry[0, 0];
        byte firstLiquidFlag = tileData.LiquidFlags[0, 0];

        bool fullType = false;

        for (int y = 0; y < CellsPerGrid && !fullType; y++)
        {
            for (int x = 0; x < CellsPerGrid; x++)
            {
                if (tileData.LiquidEntry[y, x] != firstLiquidEntry || tileData.LiquidFlags[y, x] != firstLiquidFlag)
                {
                    fullType = true;
                    break;
                }
            }
        }

        if (firstLiquidFlag == MapFormatConstants.MapLiquidTypeNoWater && !fullType)
        {
            return Array.Empty<byte>();
        }

        int minX = GridSize;
        int minY = GridSize;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < GridSize; y++)
        {
            for (int x = 0; x < GridSize; x++)
            {
                if (!tileData.LiquidShow[y, x])
                {
                    continue;
                }

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        if (maxX < minX || maxY < minY)
        {
            return Array.Empty<byte>();
        }

        byte offsetX = checked((byte)minX);
        byte offsetY = checked((byte)minY);
        byte width = checked((byte)(maxX - minX + 2));
        byte height = checked((byte)(maxY - minY + 2));

        float minimum = float.PositiveInfinity;
        float maximum = float.NegativeInfinity;

        for (int y = offsetY; y < offsetY + height && y <= GridSize; y++)
        {
            for (int x = offsetX; x < offsetX + width && x <= GridSize; x++)
            {
                float value = tileData.LiquidHeights[y, x];

                minimum = Math.Min(minimum, value);
                maximum = Math.Max(maximum, value);
            }
        }

        if (!float.IsFinite(minimum) || !float.IsFinite(maximum))
        {
            minimum = MinimumStoredHeight;
            maximum = MinimumStoredHeight;
        }

        ushort flags = 0;

        if (!fullType)
        {
            flags |= MapFormatConstants.MapLiquidNoType;
        }

        if (Math.Abs(maximum - minimum) < 0.001f)
        {
            flags |= MapFormatConstants.MapLiquidNoHeight;
        }

        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: true);

        WriteFourCC(writer, MapFormatConstants.LiquidMagic);
        writer.Write(flags);
        writer.Write(fullType ? (ushort)0 : firstLiquidEntry);
        writer.Write(offsetX);
        writer.Write(offsetY);
        writer.Write(width);
        writer.Write(height);
        writer.Write(minimum);

        if ((flags & MapFormatConstants.MapLiquidNoType) == 0)
        {
            for (int y = 0; y < CellsPerGrid; y++)
            {
                for (int x = 0; x < CellsPerGrid; x++)
                {
                    writer.Write(tileData.LiquidEntry[y, x]);
                }
            }

            for (int y = 0; y < CellsPerGrid; y++)
            {
                for (int x = 0; x < CellsPerGrid; x++)
                {
                    writer.Write(tileData.LiquidFlags[y, x]);
                }
            }
        }

        if ((flags & MapFormatConstants.MapLiquidNoHeight) == 0)
        {
            for (int y = offsetY; y < offsetY + height && y <= GridSize; y++)
            {
                for (int x = offsetX; x < offsetX + width && x <= GridSize; x++)
                {
                    writer.Write(tileData.LiquidHeights[y, x]);
                }
            }
        }

        writer.Flush();
        return stream.ToArray();
    }
    private static byte[] BuildHolesSection(ushort[,] holes)
    {
        bool hasHoles = false;

        foreach (ushort hole in holes)
        {
            if (hole != 0)
            {
                hasHoles = true;
                break;
            }
        }

        if (!hasHoles)
        {
            return Array.Empty<byte>();
        }

        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: true);

        for (int y = 0; y < CellsPerGrid; y++)
        {
            for (int x = 0; x < CellsPerGrid; x++)
            {
                writer.Write(holes[y, x]);
            }
        }

        writer.Flush();
        return stream.ToArray();
    }

    private static void WriteFloatGrid(BinaryWriter writer, float[,] values, int height, int width)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                writer.Write(values[y, x]);
            }
        }
    }

    private static void Fill(float[,] values, float value)
    {
        for (int y = 0; y < values.GetLength(0); y++)
        {
            for (int x = 0; x < values.GetLength(1); x++)
            {
                values[y, x] = value;
            }
        }
    }

    private static void Fill(ushort[,] values, ushort value)
    {
        for (int y = 0; y < values.GetLength(0); y++)
        {
            for (int x = 0; x < values.GetLength(1); x++)
            {
                values[y, x] = value;
            }
        }
    }

    private static void ClampMinimum(float[,] values, float minimum)
    {
        for (int y = 0; y < values.GetLength(0); y++)
        {
            for (int x = 0; x < values.GetLength(1); x++)
            {
                if (values[y, x] < minimum)
                {
                    values[y, x] = minimum;
                }
            }
        }
    }

    private static bool IsValidCellIndex(int value)
    {
        return value >= 0 && value < CellsPerGrid;
    }

    private static void WriteFourCC(BinaryWriter writer, string value)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);

        if (bytes.Length != 4)
        {
            throw new ArgumentException("FourCC values must be exactly four bytes.", nameof(value));
        }

        writer.Write(bytes);
    }

    private static int CountVisibleLiquidTiles(bool[,] values)
    {
        int count = 0;

        foreach (bool value in values)
        {
            if (value)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountLiquidCells(byte[,] flags)
    {
        int count = 0;

        foreach (byte value in flags)
        {
            if (value != MapFormatConstants.MapLiquidTypeNoWater)
            {
                count++;
            }
        }

        return count;
    }

    private sealed record TileData(
        ushort[,] AreaFlags,
        ushort[,] Holes,
        float[,] V9,
        float[,] V8,
        ushort[,] LiquidEntry,
        byte[,] LiquidFlags,
        bool[,] LiquidShow,
        float[,] LiquidHeights);
}
