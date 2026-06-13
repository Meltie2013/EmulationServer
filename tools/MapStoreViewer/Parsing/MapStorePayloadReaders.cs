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

using EmulationServer.Shared.Data.MapStore;
using MapStoreViewer.Scene;

namespace MapStoreViewer.Parsing;

/**
  * Reads mapstore payloads into viewer-only scene records.
  */
public static class MapStorePayloadReaders
{
    public static TerrainScene ReadTerrain(MapStoreFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        using MemoryStream stream = new(file.Payload, writable: false);
        using BinaryReader reader = new(stream);

        string magic = MapStoreBinaryPrimitives.ReadFourCC(reader, "terrain payload magic");
        if (!string.Equals(magic, MapStorePayloadConstants.TerrainMagic, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"{file.Path} has invalid terrain payload magic '{magic}'.");
        }

        uint areaSize = reader.ReadUInt32();
        uint heightSize = reader.ReadUInt32();
        uint holesSize = reader.ReadUInt32();
        long expectedPayloadSize = 16L + areaSize + heightSize + holesSize;
        if (expectedPayloadSize != file.Payload.Length)
        {
            throw new InvalidDataException($"{file.Path} has invalid terrain payload size. Header declares {expectedPayloadSize} byte(s), payload contains {file.Payload.Length} byte(s).");
        }

        byte[] areaBytes = reader.ReadBytes(checked((int)areaSize));
        byte[] heightBytes = reader.ReadBytes(checked((int)heightSize));
        byte[] holesBytes = reader.ReadBytes(checked((int)holesSize));

        (ushort areaFlags, ushort gridAreaFlag, _) = ReadAreaSection(areaBytes, file.Path);
        (uint heightFlags, float gridHeight, float gridMaxHeight, float[]? v9Heights) = ReadHeightSection(heightBytes, file.Path);
        ushort[] holes = ReadHolesSection(holesBytes, file.Path);

        float minimumHeight = gridHeight;
        float maximumHeight = gridMaxHeight;
        if (v9Heights is { Length: > 0 })
        {
            minimumHeight = v9Heights.Min();
            maximumHeight = v9Heights.Max();
        }

        return new TerrainScene(
            file.Header.Build,
            areaFlags,
            gridAreaFlag,
            heightFlags,
            gridHeight,
            gridMaxHeight,
            minimumHeight,
            maximumHeight,
            v9Heights,
            holes,
            (areaFlags & MapStorePayloadConstants.MapAreaNoArea) == 0,
            (heightFlags & MapStorePayloadConstants.MapHeightNoHeight) == 0 && v9Heights is not null,
            holes.Any(static value => value != 0));
    }

    public static LiquidScene ReadLiquid(MapStoreFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        using MemoryStream stream = new(file.Payload, writable: false);
        using BinaryReader reader = new(stream);

        string payloadMagic = MapStoreBinaryPrimitives.ReadFourCC(reader, "liquid payload magic");
        if (!string.Equals(payloadMagic, MapStorePayloadConstants.LiquidPayloadMagic, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"{file.Path} has invalid liquid payload magic '{payloadMagic}'.");
        }

        uint liquidSectionSize = reader.ReadUInt32();
        if (8L + liquidSectionSize != file.Payload.Length)
        {
            throw new InvalidDataException($"{file.Path} has invalid liquid payload size. Header declares {liquidSectionSize} byte(s), payload contains {file.Payload.Length} byte(s).");
        }

        if (liquidSectionSize == 0)
        {
            return new LiquidScene(file.Header.Build, false, 0, 0, 0, 0, 0, 0, 0.0f, 0.0f, 0.0f, null, null, null);
        }

        string sectionMagic = MapStoreBinaryPrimitives.ReadFourCC(reader, "liquid section magic");
        if (!string.Equals(sectionMagic, MapStorePayloadConstants.LiquidSectionMagic, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"{file.Path} has invalid liquid section magic '{sectionMagic}'.");
        }

        ushort flags = reader.ReadUInt16();
        ushort liquidType = reader.ReadUInt16();
        byte offsetX = reader.ReadByte();
        byte offsetY = reader.ReadByte();
        byte width = reader.ReadByte();
        byte height = reader.ReadByte();
        float liquidLevel = reader.ReadSingle();

        ushort[]? liquidTypeIds = null;
        byte[]? liquidFlags = null;
        if ((flags & MapStorePayloadConstants.MapLiquidNoType) == 0)
        {
            liquidTypeIds = new ushort[MapStorePayloadConstants.AreaCellCount];
            for (int i = 0; i < liquidTypeIds.Length; i++)
            {
                liquidTypeIds[i] = reader.ReadUInt16();
            }

            liquidFlags = new byte[MapStorePayloadConstants.AreaCellCount];
            for (int i = 0; i < liquidFlags.Length; i++)
            {
                liquidFlags[i] = reader.ReadByte();
            }
        }

        float[]? liquidHeights = null;
        if ((flags & MapStorePayloadConstants.MapLiquidNoHeight) == 0)
        {
            int heightSampleCount = checked(width * height);
            liquidHeights = new float[heightSampleCount];
            for (int i = 0; i < liquidHeights.Length; i++)
            {
                liquidHeights[i] = reader.ReadSingle();
            }
        }

        if (stream.Position != stream.Length)
        {
            throw new InvalidDataException($"{file.Path} has {stream.Length - stream.Position} unread liquid payload byte(s).");
        }

        float minimumHeight = liquidHeights is { Length: > 0 } ? liquidHeights.Min() : liquidLevel;
        float maximumHeight = liquidHeights is { Length: > 0 } ? liquidHeights.Max() : liquidLevel;
        return new LiquidScene(file.Header.Build, true, flags, liquidType, offsetX, offsetY, width, height, liquidLevel, minimumHeight, maximumHeight, liquidTypeIds, liquidFlags, liquidHeights);
    }

    public static (ushort Build, uint Version, IReadOnlyList<CollisionPlacementScene> Placements) ReadCollisionPlacements(MapStoreFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        using MemoryStream stream = new(file.Payload, writable: false);
        using BinaryReader reader = new(stream);

        string magic = MapStoreBinaryPrimitives.ReadAscii(reader, MapStorePayloadConstants.CollisionPayloadMagic.Length, "collision payload magic");
        if (!string.Equals(magic, MapStorePayloadConstants.CollisionPayloadMagic, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"{file.Path} has invalid collision payload magic '{magic}'.");
        }

        uint version = reader.ReadUInt32();
        ushort build = reader.ReadUInt16();
        uint mapId = reader.ReadUInt32();
        int tileX = reader.ReadInt32();
        int tileY = reader.ReadInt32();
        int placementCount = reader.ReadInt32();

        if (mapId != file.Header.MapId || tileX != file.Header.TileX || tileY != file.Header.TileY)
        {
            string expectedKey = MapStoreFileNames.FormatTileKey(file.Header.MapId, file.Header.TileX, file.Header.TileY);
            string actualKey = MapStoreFileNames.FormatTileKey(mapId, tileX, tileY);
            throw new InvalidDataException($"{file.Path} has mismatched inner collision key. Expected {expectedKey}, got {actualKey}.");
        }

        if (build != file.Header.Build)
        {
            throw new InvalidDataException($"{file.Path} has mismatched inner collision build. Outer={file.Header.Build}, Inner={build}.");
        }

        if (placementCount < 0)
        {
            throw new InvalidDataException($"{file.Path} has invalid negative collision placement count {placementCount}.");
        }

        List<CollisionPlacementScene> placements = new(placementCount);
        for (int i = 0; i < placementCount; i++)
        {
            string modelKey = MapStoreBinaryPrimitives.ReadUtf8String(reader, file.Path, "collision model key");
            string normalizedPath = MapStoreBinaryPrimitives.ReadUtf8String(reader, file.Path, "collision model path");
            uint uniqueId = reader.ReadUInt32();
            Vector3Scene position = ReadVector(reader);
            Vector3Scene rotation = ReadVector(reader);
            BoundsScene bounds = new(ReadVector(reader), ReadVector(reader));
            uint flags = reader.ReadUInt32();
            ushort doodadSet = reader.ReadUInt16();
            ushort nameSet = reader.ReadUInt16();

            placements.Add(new CollisionPlacementScene(modelKey, normalizedPath, uniqueId, position, rotation, bounds, flags, doodadSet, nameSet, ModelLoaded: false));
        }

        if (stream.Position != stream.Length)
        {
            throw new InvalidDataException($"{file.Path} has {stream.Length - stream.Position} unread collision payload byte(s).");
        }

        return (build, version, placements);
    }

    public static NavmeshScene ReadNavmesh(MapStoreFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        using MemoryStream stream = new(file.Payload, writable: false);
        using BinaryReader reader = new(stream);

        string magic = MapStoreBinaryPrimitives.ReadFourCC(reader, "navmesh payload magic");
        if (!string.Equals(magic, MapStorePayloadConstants.NavmeshPayloadMagic, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"{file.Path} has invalid navmesh payload magic '{magic}'.");
        }

        uint polygonCount = reader.ReadUInt32();
        uint connectionCount = reader.ReadUInt32();
        if (stream.Position != stream.Length)
        {
            throw new InvalidDataException($"{file.Path} has {stream.Length - stream.Position} unread navmesh payload byte(s).");
        }

        return new NavmeshScene(file.Header.Build, polygonCount, connectionCount, polygonCount > 0);
    }

    private static (ushort Flags, ushort GridArea, ushort[] AreaGrid) ReadAreaSection(byte[] section, string path)
    {
        if (section.Length < 8)
        {
            throw new InvalidDataException($"{path} has an invalid area section size of {section.Length} byte(s).");
        }

        using MemoryStream stream = new(section, writable: false);
        using BinaryReader reader = new(stream);

        string magic = MapStoreBinaryPrimitives.ReadFourCC(reader, "area section magic");
        if (!string.Equals(magic, MapStorePayloadConstants.AreaMagic, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"{path} has invalid area section magic '{magic}'.");
        }

        ushort flags = reader.ReadUInt16();
        ushort gridArea = reader.ReadUInt16();
        ushort[] areaGrid = new ushort[MapStorePayloadConstants.AreaCellCount];

        if ((flags & MapStorePayloadConstants.MapAreaNoArea) != 0)
        {
            Array.Fill(areaGrid, gridArea);
            if (stream.Position != stream.Length)
            {
                throw new InvalidDataException($"{path} has extra bytes after constant area section data.");
            }

            return (flags, gridArea, areaGrid);
        }

        int expectedSize = 8 + MapStorePayloadConstants.AreaCellCount * sizeof(ushort);
        if (section.Length != expectedSize)
        {
            throw new InvalidDataException($"{path} has invalid area grid size. Expected {expectedSize} byte(s), got {section.Length} byte(s).");
        }

        for (int i = 0; i < areaGrid.Length; i++)
        {
            areaGrid[i] = reader.ReadUInt16();
        }

        return (flags, gridArea, areaGrid);
    }

    private static (uint Flags, float GridHeight, float GridMaxHeight, float[]? V9) ReadHeightSection(byte[] section, string path)
    {
        if (section.Length < 16)
        {
            throw new InvalidDataException($"{path} has an invalid height section size of {section.Length} byte(s).");
        }

        using MemoryStream stream = new(section, writable: false);
        using BinaryReader reader = new(stream);

        string magic = MapStoreBinaryPrimitives.ReadFourCC(reader, "height section magic");
        if (!string.Equals(magic, MapStorePayloadConstants.HeightMagic, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"{path} has invalid height section magic '{magic}'.");
        }

        uint flags = reader.ReadUInt32();
        float gridHeight = reader.ReadSingle();
        float gridMaxHeight = reader.ReadSingle();

        if ((flags & MapStorePayloadConstants.MapHeightNoHeight) != 0)
        {
            if (stream.Position != stream.Length)
            {
                throw new InvalidDataException($"{path} has extra bytes after constant height section data.");
            }

            return (flags, gridHeight, gridMaxHeight, null);
        }

        if ((flags & (MapStorePayloadConstants.MapHeightAsInt16 | MapStorePayloadConstants.MapHeightAsInt8)) != 0)
        {
            throw new InvalidDataException($"{path} uses compressed height flags 0x{flags:X8}, but the viewer currently expects float height grids.");
        }

        int expectedSize = 16 + (MapStorePayloadConstants.V9VertexCount + MapStorePayloadConstants.V8VertexCount) * sizeof(float);
        if (section.Length != expectedSize)
        {
            throw new InvalidDataException($"{path} has invalid height grid size. Expected {expectedSize} byte(s), got {section.Length} byte(s).");
        }

        float[] v9 = new float[MapStorePayloadConstants.V9VertexCount];
        for (int i = 0; i < v9.Length; i++)
        {
            v9[i] = reader.ReadSingle();
        }

        long v8Bytes = MapStorePayloadConstants.V8VertexCount * sizeof(float);
        stream.Position += v8Bytes;
        if (stream.Position != stream.Length)
        {
            throw new InvalidDataException($"{path} has extra bytes after height grid data.");
        }

        return (flags, gridHeight, gridMaxHeight, v9);
    }

    private static ushort[] ReadHolesSection(byte[] section, string path)
    {
        ushort[] holes = new ushort[MapStorePayloadConstants.AreaCellCount];
        if (section.Length == 0)
        {
            return holes;
        }

        int expectedSize = MapStorePayloadConstants.AreaCellCount * sizeof(ushort);
        if (section.Length != expectedSize)
        {
            throw new InvalidDataException($"{path} has invalid holes section size. Expected {expectedSize} byte(s), got {section.Length} byte(s).");
        }

        using MemoryStream stream = new(section, writable: false);
        using BinaryReader reader = new(stream);

        for (int i = 0; i < holes.Length; i++)
        {
            holes[i] = reader.ReadUInt16();
        }

        return holes;
    }

    private static Vector3Scene ReadVector(BinaryReader reader)
    {
        return new Vector3Scene(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }
}
