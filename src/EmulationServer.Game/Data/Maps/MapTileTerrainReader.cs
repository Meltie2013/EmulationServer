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
// File: src/EmulationServer.Game/Data/Maps/MapTileTerrainReader.cs
// Purpose: Contains map tile terrain reader code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Shared.Data.MapStore;

namespace EmulationServer.Game.Data.Maps;

// Type: MapTileTerrainReader
// Purpose: Provides map tile terrain reader behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class MapTileTerrainReader
{
    // Method: Read
    // Purpose: Retrieves read data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - file: File value supplied by the caller for this operation.
    // Returns: Returns the map tile terrain data value produced by this operation.
    // Notes: This keeps the operation scoped to MapTileTerrainReader so callers do not duplicate validation, protocol, or persistence rules.
    public static MapTileTerrainData Read(MapStoreFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        using MemoryStream stream = new(file.Payload, writable: false);
        using BinaryReader reader = new(stream);

        string magic = MapStoreBinaryPrimitives.ReadFourCC(reader, "terrain FourCC value");
        if (!string.Equals(magic, MapStorePayloadConstants.TerrainMagic, StringComparison.Ordinal))
        {
            throw new MapFormatException($"{file.Path} has invalid terrain payload magic '{magic}'.");
        }

        uint areaSize = reader.ReadUInt32();
        uint heightSize = reader.ReadUInt32();
        uint holesSize = reader.ReadUInt32();
        long expectedPayloadSize = 16L + areaSize + heightSize + holesSize;
        if (expectedPayloadSize != file.Payload.Length)
        {
            throw new MapFormatException($"{file.Path} has invalid terrain payload size. Header declares {expectedPayloadSize} byte(s), payload contains {file.Payload.Length} byte(s).");
        }

        byte[] areaBytes = reader.ReadBytes(checked((int)areaSize));
        byte[] heightBytes = reader.ReadBytes(checked((int)heightSize));
        byte[] holesBytes = reader.ReadBytes(checked((int)holesSize));

        (ushort areaFlags, ushort gridAreaFlag, ushort[] areaGrid) = ReadAreaSection(areaBytes, file.Path);
        (uint heightFlags, float gridHeight, float gridMaxHeight, float[]? v9, float[]? v8) = ReadHeightSection(heightBytes, file.Path);
        ushort[] holes = ReadHolesSection(holesBytes, file.Path);

        MapTileKey key = new(file.Header.MapId, file.Header.TileX, file.Header.TileY);
        return new MapTileTerrainData(key, file.Header.Build, areaFlags, gridAreaFlag, areaGrid, heightFlags, gridHeight, gridMaxHeight, v9, v8, holes);
    }

    // Method: static
    // Purpose: Executes the static operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - Flags: Flags value supplied by the caller for this operation.
    // - GridArea: Grid area value supplied by the caller for this operation.
    // - ushortAreaGrid: Ushort area grid value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapTileTerrainReader so callers do not duplicate validation, protocol, or persistence rules.
    private static (ushort Flags, ushort GridArea, ushort[] AreaGrid) ReadAreaSection(byte[] section, string path)
    {
        if (section.Length < 8)
        {
            throw new MapFormatException($"{path} has an invalid area section size of {section.Length} byte(s).");
        }

        using MemoryStream stream = new(section, writable: false);
        using BinaryReader reader = new(stream);

        string magic = MapStoreBinaryPrimitives.ReadFourCC(reader, "area section FourCC value");
        if (!string.Equals(magic, MapStorePayloadConstants.AreaMagic, StringComparison.Ordinal))
        {
            throw new MapFormatException($"{path} has invalid area section magic '{magic}'.");
        }

        ushort flags = reader.ReadUInt16();
        ushort gridArea = reader.ReadUInt16();
        ushort[] areaGrid = new ushort[MapStorePayloadConstants.AreaCellCount];

        if ((flags & MapStorePayloadConstants.MapAreaNoArea) != 0)
        {
            Array.Fill(areaGrid, gridArea);
            if (stream.Position != stream.Length)
            {
                throw new MapFormatException($"{path} has extra bytes after constant area section data.");
            }

            return (flags, gridArea, areaGrid);
        }

        int expectedSize = 8 + MapStorePayloadConstants.AreaCellCount * sizeof(ushort);
        if (section.Length != expectedSize)
        {
            throw new MapFormatException($"{path} has invalid area grid size. Expected {expectedSize} byte(s), got {section.Length} byte(s).");
        }

        for (int i = 0; i < areaGrid.Length; i++)
        {
            areaGrid[i] = reader.ReadUInt16();
        }

        return (flags, gridArea, areaGrid);
    }

    // Method: static
    // Purpose: Executes the static operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - Flags: Flags value supplied by the caller for this operation.
    // - GridHeight: Grid height value supplied by the caller for this operation.
    // - GridMaxHeight: Grid max height value supplied by the caller for this operation.
    // - V9: V9 value supplied by the caller for this operation.
    // - V8: V8 value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapTileTerrainReader so callers do not duplicate validation, protocol, or persistence rules.
    private static (uint Flags, float GridHeight, float GridMaxHeight, float[]? V9, float[]? V8) ReadHeightSection(byte[] section, string path)
    {
        if (section.Length < 16)
        {
            throw new MapFormatException($"{path} has an invalid height section size of {section.Length} byte(s).");
        }

        using MemoryStream stream = new(section, writable: false);
        using BinaryReader reader = new(stream);

        string magic = MapStoreBinaryPrimitives.ReadFourCC(reader, "height section FourCC value");
        if (!string.Equals(magic, MapStorePayloadConstants.HeightMagic, StringComparison.Ordinal))
        {
            throw new MapFormatException($"{path} has invalid height section magic '{magic}'.");
        }

        uint flags = reader.ReadUInt32();
        float gridHeight = reader.ReadSingle();
        float gridMaxHeight = reader.ReadSingle();

        if ((flags & MapStorePayloadConstants.MapHeightNoHeight) != 0)
        {
            if (stream.Position != stream.Length)
            {
                throw new MapFormatException($"{path} has extra bytes after constant height section data.");
            }

            return (flags, gridHeight, gridMaxHeight, null, null);
        }

        if ((flags & (MapStorePayloadConstants.MapHeightAsInt16 | MapStorePayloadConstants.MapHeightAsInt8)) != 0)
        {
            throw new MapFormatException($"{path} uses compressed height flags 0x{flags:X8}, but the runtime reader currently expects float height grids.");
        }

        int expectedSize = 16 + (MapStorePayloadConstants.V9VertexCount + MapStorePayloadConstants.V8VertexCount) * sizeof(float);
        if (section.Length != expectedSize)
        {
            throw new MapFormatException($"{path} has invalid height grid size. Expected {expectedSize} byte(s), got {section.Length} byte(s).");
        }

        float[] v9 = new float[MapStorePayloadConstants.V9VertexCount];
        for (int i = 0; i < v9.Length; i++)
        {
            v9[i] = reader.ReadSingle();
        }

        float[] v8 = new float[MapStorePayloadConstants.V8VertexCount];
        for (int i = 0; i < v8.Length; i++)
        {
            v8[i] = reader.ReadSingle();
        }

        return (flags, gridHeight, gridMaxHeight, v9, v8);
    }

    // Method: ReadHolesSection
    // Purpose: Retrieves read holes section data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - bytesection: Bytesection value supplied by the caller for this operation.
    // - path: Path value supplied by the caller for this operation.
    // Returns: Returns the ushort[] value produced by this operation.
    // Notes: This keeps the operation scoped to MapTileTerrainReader so callers do not duplicate validation, protocol, or persistence rules.
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
            throw new MapFormatException($"{path} has invalid holes section size. Expected {expectedSize} byte(s), got {section.Length} byte(s).");
        }

        using MemoryStream stream = new(section, writable: false);
        using BinaryReader reader = new(stream);

        for (int i = 0; i < holes.Length; i++)
        {
            holes[i] = reader.ReadUInt16();
        }

        return holes;
    }
}
