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
// File: src/EmulationServer.Game/Data/Maps/MapTileDataStore.cs
// Purpose: Contains map tile data store code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Shared.Data.MapStore;

namespace EmulationServer.Game.Data.Maps;

// Type: MapTileDataStore
// Purpose: Provides map tile data store behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class MapTileDataStore
{
    // Constructor: MapTileDataStore
    // Purpose: Initializes a new MapTileDataStore instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - mapStoreRootDirectory: Map store root directory value supplied by the caller for this operation.
    // - key: Key value supplied by the caller for this operation.
    // - fileHeaders: File headers value supplied by the caller for this operation.
    // - filePaths: File paths value supplied by the caller for this operation.
    // - terrain: Terrain value supplied by the caller for this operation.
    // - liquid: Liquid value supplied by the caller for this operation.
    // - collision: Collision value supplied by the caller for this operation.
    // - navmesh: Navmesh value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapTileDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Property: Gets or sets the map store root directory value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: map store root directory value exposed by the owning type.
    public string MapStoreRootDirectory { get; }

    // Property: Gets or sets the key value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: key value exposed by the owning type.
    public MapTileKey Key { get; }

    // Property: Gets or sets the file headers value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: file headers value exposed by the owning type.
    public IReadOnlyList<MapStoreFileHeader> FileHeaders { get; }

    // Property: Gets or sets the file paths value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: file paths value exposed by the owning type.
    public IReadOnlyList<string> FilePaths { get; }

    // Property: Gets or sets the terrain value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: terrain value exposed by the owning type.
    public MapTileTerrainData Terrain { get; }

    // Property: Gets or sets the liquid value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: liquid value exposed by the owning type.
    public MapTileLiquidData Liquid { get; }

    // Property: Gets or sets the collision value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: collision value exposed by the owning type.
    public MapTileCollisionData Collision { get; }

    // Property: Gets or sets the navmesh value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: navmesh value exposed by the owning type.
    public MapTileNavmeshData Navmesh { get; }

    // Property: Gets or sets the terrain queries value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: terrain queries value exposed by the owning type.
    public MapTileTerrainQueryService TerrainQueries { get; }

    // Property: Gets or sets the liquid queries value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: liquid queries value exposed by the owning type.
    public MapTileLiquidQueryService LiquidQueries { get; }

    // Property: Gets or sets the collision queries value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: collision queries value exposed by the owning type.
    public MapTileCollisionQueryService CollisionQueries { get; }

    // Property: Gets or sets the navmesh queries value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: navmesh queries value exposed by the owning type.
    public MapTileNavmeshQueryService NavmeshQueries { get; }

    // Method: Load
    // Purpose: Retrieves load data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - terrainPath: Terrain path value supplied by the caller for this operation.
    // Returns: Returns the map tile data store value produced by this operation.
    // Notes: This keeps the operation scoped to MapTileDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public static MapTileDataStore Load(string terrainPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(terrainPath);

        if (!TryParseTerrainTilePath(terrainPath, out string mapStoreRootDirectory, out MapTileKey key))
        {
            throw new MapFormatException($"Mapstore terrain file '{terrainPath}' must use: mapstore/maps/<mapId:000>/tiles/<tileX:00>_<tileY:00>.terrain.bin.");
        }

        return Load(mapStoreRootDirectory, key);
    }

    // Method: Load
    // Purpose: Retrieves load data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - mapStoreRootDirectory: Map store root directory value supplied by the caller for this operation.
    // - key: Key value supplied by the caller for this operation.
    // Returns: Returns the map tile data store value produced by this operation.
    // Notes: This keeps the operation scoped to MapTileDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: TryParseTerrainTilePath
    // Purpose: Attempts to retrieve or parse try parse terrain tile path data without treating normal misses as failures.
    // Parameters:
    // - terrainPath: Terrain path value supplied by the caller for this operation.
    // - mapStoreRootDirectory: Map store root directory value supplied by the caller for this operation.
    // - key: Key value supplied by the caller for this operation.
    // Returns: Returns true when try parse terrain tile path succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to MapTileDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: LoadRequiredIfEnabled
    // Purpose: Retrieves load required if enabled data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - mapStoreRootDirectory: Map store root directory value supplied by the caller for this operation.
    // - key: Key value supplied by the caller for this operation.
    // - kind: Kind value supplied by the caller for this operation.
    // - fileHeaders: File headers value supplied by the caller for this operation.
    // - filePaths: File paths value supplied by the caller for this operation.
    // Returns: Returns the map store file? value produced by this operation.
    // Notes: This keeps the operation scoped to MapTileDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ValidateComponentKey
    // Purpose: Validates or evaluates validate component key rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - key: Key value supplied by the caller for this operation.
    // - file: File value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapTileDataStore so callers do not duplicate validation, protocol, or persistence rules.
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
