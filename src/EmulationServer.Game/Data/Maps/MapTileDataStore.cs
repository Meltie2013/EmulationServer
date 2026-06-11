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

/**
  * File overview: src/EmulationServer.Game/Data/Maps/MapTileDataStore.cs
  * Documents the MapTileDataStore source file in the extracted map data loading and map tile lookup area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Data.Maps;

/**
  * Loads extracted mapstore tile files and converts each compile-required runtime payload into typed queryable data.
  * Terrain, liquid, collision, and navmesh files are required by default unless a dedicated compile-time feature symbol disables one.
  */
public sealed class MapTileDataStore
{
    private MapTileDataStore(
        string mapStoreRootDirectory,
        MapTileKey key,
        IReadOnlyList<MapStoreFileHeader> fileHeaders,
        IReadOnlyList<string> filePaths,
        MapTileTerrainData terrain,
        MapTileLiquidData liquid,
        MapTileCollisionData collision,
        MapTileNavmeshData navmesh)
    {
        MapStoreRootDirectory = mapStoreRootDirectory;
        Key = key;
        FileHeaders = fileHeaders;
        FilePaths = filePaths;
        Terrain = terrain;
        Liquid = liquid;
        Collision = collision;
        Navmesh = navmesh;
        TerrainQueries = new MapTileTerrainQueryService(Terrain);
        LiquidQueries = new MapTileLiquidQueryService(Liquid);
        CollisionQueries = new MapTileCollisionQueryService(Collision);
        NavmeshQueries = new MapTileNavmeshQueryService(Navmesh);
    }

    /**
      * Gets the root mapstore directory used to load this tile.
      */
    public string MapStoreRootDirectory { get; }

    /**
      * Gets the map/tile key represented by this loaded tile.
      */
    public MapTileKey Key { get; }

    /**
      * Gets the validated mapstore headers used to build the typed tile payloads.
      */
    public IReadOnlyList<MapStoreFileHeader> FileHeaders { get; }

    /**
      * Gets the physical mapstore component paths used to build the typed tile payloads.
      */
    public IReadOnlyList<string> FilePaths { get; }

    /**
      * Gets parsed terrain data for this tile.
      */
    public MapTileTerrainData Terrain { get; }

    /**
      * Gets parsed liquid data for this tile.
      */
    public MapTileLiquidData Liquid { get; }

    /**
      * Gets parsed collision placement data for this tile.
      */
    public MapTileCollisionData Collision { get; }

    /**
      * Gets parsed navmesh metadata for this tile.
      */
    public MapTileNavmeshData Navmesh { get; }

    /**
      * Gets terrain query helpers for this tile.
      */
    public MapTileTerrainQueryService TerrainQueries { get; }

    /**
      * Gets liquid query helpers for this tile.
      */
    public MapTileLiquidQueryService LiquidQueries { get; }

    /**
      * Gets collision query helpers for this tile.
      */
    public MapTileCollisionQueryService CollisionQueries { get; }

    /**
      * Gets navmesh query helpers for this tile. Real path queries intentionally return false until navmesh payload generation exists.
      */
    public MapTileNavmeshQueryService NavmeshQueries { get; }

    /**
      * Loads a complete mapstore tile using a terrain file path as the discovery point.
      */
    public static MapTileDataStore Load(string terrainPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(terrainPath);

        if (!TryParseTerrainTilePath(terrainPath, out string mapStoreRootDirectory, out MapTileKey key))
        {
            throw new MapFormatException($"Mapstore terrain file '{terrainPath}' must use: mapstore/maps/<mapId:000>/tiles/<tileX:00>_<tileY:00>.terrain.bin.");
        }

        return Load(mapStoreRootDirectory, key);
    }

    /**
      * Loads a complete mapstore tile from the supplied mapstore root and tile key.
      */
    public static MapTileDataStore Load(string mapStoreRootDirectory, MapTileKey key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapStoreRootDirectory);

        string fullRoot = Path.GetFullPath(mapStoreRootDirectory);
        List<MapStoreFileHeader> fileHeaders = [];
        List<string> filePaths = [];

        MapStoreFile? terrainFile = LoadRequiredIfEnabled(fullRoot, key, MapStoreDataKind.Terrain, fileHeaders, filePaths);
        MapStoreFile? liquidFile = LoadRequiredIfEnabled(fullRoot, key, MapStoreDataKind.Liquid, fileHeaders, filePaths);
        MapStoreFile? collisionFile = LoadRequiredIfEnabled(fullRoot, key, MapStoreDataKind.Collision, fileHeaders, filePaths);
        MapStoreFile? navmeshFile = LoadRequiredIfEnabled(fullRoot, key, MapStoreDataKind.Navmesh, fileHeaders, filePaths);

        MapTileTerrainData terrain = terrainFile is null
            ? MapTileTerrainData.CreateDisabled(key)
            : MapTileTerrainReader.Read(terrainFile);
        MapTileLiquidData liquid = liquidFile is null
            ? MapTileLiquidData.CreateDisabled(key)
            : MapTileLiquidReader.Read(liquidFile);
        MapTileCollisionData collision = collisionFile is null
            ? MapTileCollisionData.CreateDisabled(key)
            : MapTileCollisionReader.Read(collisionFile);
        MapTileNavmeshData navmesh = navmeshFile is null
            ? MapTileNavmeshData.CreateDisabled(key)
            : MapTileNavmeshReader.Read(navmeshFile);

        return new MapTileDataStore(
            fullRoot,
            key,
            fileHeaders.ToArray(),
            filePaths.ToArray(),
            terrain,
            liquid,
            collision,
            navmesh);
    }

    /**
      * Tries to parse a terrain file path into the mapstore root directory and tile key.
      */
    public static bool TryParseTerrainTilePath(string terrainPath, out string mapStoreRootDirectory, out MapTileKey key)
    {
        mapStoreRootDirectory = string.Empty;
        key = default;

        string fullPath = Path.GetFullPath(terrainPath);
        if (!MapStoreFileNames.TryParseTileFileName(Path.GetFileName(fullPath), out byte tileX, out byte tileY, out MapStoreDataKind kind) || kind != MapStoreDataKind.Terrain)
        {
            return false;
        }

        DirectoryInfo? tilesDirectory = Directory.GetParent(fullPath);
        DirectoryInfo? mapDirectory = tilesDirectory?.Parent;
        DirectoryInfo? mapsDirectory = mapDirectory?.Parent;
        DirectoryInfo? rootDirectory = mapsDirectory?.Parent;

        if (tilesDirectory is null ||
            mapDirectory is null ||
            mapsDirectory is null ||
            rootDirectory is null ||
            !string.Equals(tilesDirectory.Name, MapStoreFileNames.TilesDirectoryName, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(mapsDirectory.Name, MapStoreFileNames.MapsDirectoryName, StringComparison.OrdinalIgnoreCase) ||
            !MapStoreFileNames.TryParseMapDirectoryName(mapDirectory.Name, out uint mapId))
        {
            return false;
        }

        mapStoreRootDirectory = rootDirectory.FullName;
        key = new MapTileKey(mapId, tileX, tileY);
        return true;
    }

    /**
      * Loads one required mapstore file when that component is enabled by the current build.
      */
    private static MapStoreFile? LoadRequiredIfEnabled(
        string mapStoreRootDirectory,
        MapTileKey key,
        MapStoreDataKind kind,
        List<MapStoreFileHeader> fileHeaders,
        List<string> filePaths)
    {
        if (!MapStoreRuntimeFeatures.IsEnabled(kind))
        {
            return null;
        }

        string path = MapStoreFileNames.GetTileFilePath(mapStoreRootDirectory, key.MapId, key.TileX, key.TileY, kind);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Required mapstore {kind} file was not found for tile {key}: {path}", path);
        }

        MapStoreFile file = MapStoreBinary.ReadFile(path, kind);
        ValidateComponentKey(key, file);
        fileHeaders.Add(file.Header);
        filePaths.Add(file.Path);
        return file;
    }

    /**
      * Validates that a loaded component belongs to the requested tile key.
      */
    private static void ValidateComponentKey(MapTileKey key, MapStoreFile file)
    {
        if (file.Header.MapId != key.MapId || file.Header.TileX != key.TileX || file.Header.TileY != key.TileY)
        {
            throw new MapFormatException(
                $"{file.Path} has mismatched mapstore key. " +
                $"Expected {key}, " +
                $"got {MapStoreFileNames.FormatTileKey(file.Header.MapId, file.Header.TileX, file.Header.TileY)}.");
        }
    }
}
