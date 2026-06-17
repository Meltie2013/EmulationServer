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
// File: src/EmulationServer.Game/Maps/Runtime/MapGridManager.cs
// Purpose: Contains map grid manager code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Collections.Concurrent;
using EmulationServer.Game.Data.Maps;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.Shared.Data.MapStore;

namespace EmulationServer.Game.Maps.Runtime;

// Type: MapGridManager
// Purpose: Provides map grid manager behavior for the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - definition: Definition value supplied by the caller for this operation.
// - mapsDirectory: Maps directory value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class MapGridManager(
    MapServiceDefinition definition,
    string mapsDirectory)
{

    // Method: ArgumentNullException
    // Purpose: Executes the argument null exception operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the map service definition definition = definition ?? throw new value produced by this operation.
    // Notes: This keeps the operation scoped to MapGridManager so callers do not duplicate validation, protocol, or persistence rules.
    private readonly MapServiceDefinition _definition = definition ?? throw new ArgumentNullException();

    // Method: IsNullOrWhiteSpace
    // Purpose: Validates or evaluates is null or white space rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - mapsDirectory: Maps directory value supplied by the caller for this operation.
    // Returns: Returns the string maps directory = string. value produced by this operation.
    // Notes: This keeps the operation scoped to MapGridManager so callers do not duplicate validation, protocol, or persistence rules.
    private readonly string _mapsDirectory = string.IsNullOrWhiteSpace(mapsDirectory)
        ? throw new ArgumentException("Maps directory is required.")
        : Path.GetFullPath(mapsDirectory);
    private readonly ConcurrentDictionary<MapTileKey, LoadedMapGrid> _loadedGrids = new();

    // Property: Gets or sets the loaded grid count value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: loaded grid count value exposed by the owning type.
    public int LoadedGridCount => _loadedGrids.Count;

    // Method: ToArray
    // Purpose: Executes the to array operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the I read only collection loaded grid keys => loaded grids.keys. value produced by this operation.
    // Notes: This keeps the operation scoped to MapGridManager so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyCollection<MapTileKey> LoadedGridKeys => [.. _loadedGrids.Keys];

    // Method: InitializeAsync
    // Purpose: Controls the initialize lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MapGridManager so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_mapsDirectory))
        {
            throw new DirectoryNotFoundException($"Map tile directory was not found: {_mapsDirectory}");
        }

        await PreloadAllTilesForMapAsync(cancellationToken);
    }

    // Method: TryGetGrid
    // Purpose: Attempts to retrieve or parse try get grid data without treating normal misses as failures.
    // Parameters:
    // - tileX: Tile X value supplied by the caller for this operation.
    // - tileY: Tile Y value supplied by the caller for this operation.
    // - grid: Grid value supplied by the caller for this operation.
    // Returns: Returns true when try get grid succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to MapGridManager so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetGrid(byte tileX, byte tileY, out LoadedMapGrid grid)
    {
        MapTileKey key = new((uint)_definition.MapId, tileX, tileY);
        if (_loadedGrids.TryGetValue(key, out grid!))
        {
            grid.Touch();
            return true;
        }

        grid = null!;
        return false;
    }

    // Method: TryGetTerrainHeight
    // Purpose: Attempts to retrieve or parse try get terrain height data without treating normal misses as failures.
    // Parameters:
    // - tileX: Tile X value supplied by the caller for this operation.
    // - tileY: Tile Y value supplied by the caller for this operation.
    // - gridX: Grid X value supplied by the caller for this operation.
    // - gridY: Grid Y value supplied by the caller for this operation.
    // - height: Height value supplied by the caller for this operation.
    // Returns: Returns true when try get terrain height succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to MapGridManager so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetTerrainHeight(byte tileX, byte tileY, float gridX, float gridY, out float height)
    {
        height = 0.0f;
        if (!TryGetGrid(tileX, tileY, out LoadedMapGrid grid))
        {
            return false;
        }

        height = grid.Tile.TerrainQueries.SampleHeight(gridX, gridY);
        return true;
    }

    // Method: TryGetAreaFlag
    // Purpose: Attempts to retrieve or parse try get area flag data without treating normal misses as failures.
    // Parameters:
    // - tileX: Tile X value supplied by the caller for this operation.
    // - tileY: Tile Y value supplied by the caller for this operation.
    // - cellX: Cell X value supplied by the caller for this operation.
    // - cellY: Cell Y value supplied by the caller for this operation.
    // - areaFlag: Area flag value supplied by the caller for this operation.
    // Returns: Returns true when try get area flag succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to MapGridManager so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetAreaFlag(byte tileX, byte tileY, int cellX, int cellY, out ushort areaFlag)
    {
        areaFlag = 0;
        if (!TryGetGrid(tileX, tileY, out LoadedMapGrid grid))
        {
            return false;
        }

        areaFlag = grid.Tile.TerrainQueries.GetAreaFlag(cellX, cellY);
        return true;
    }

    // Method: TryGetLiquidInfo
    // Purpose: Attempts to retrieve or parse try get liquid info data without treating normal misses as failures.
    // Parameters:
    // - tileX: Tile X value supplied by the caller for this operation.
    // - tileY: Tile Y value supplied by the caller for this operation.
    // - gridX: Grid X value supplied by the caller for this operation.
    // - gridY: Grid Y value supplied by the caller for this operation.
    // - liquidInfo: Liquid info value supplied by the caller for this operation.
    // Returns: Returns true when try get liquid info succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to MapGridManager so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetLiquidInfo(byte tileX, byte tileY, float gridX, float gridY, out MapTileLiquidInfo liquidInfo)
    {
        liquidInfo = default;
        if (!TryGetGrid(tileX, tileY, out LoadedMapGrid grid))
        {
            return false;
        }

        return grid.Tile.LiquidQueries.TryGetLiquidInfo(gridX, gridY, out liquidInfo);
    }

    // Method: TryGetCollisionPlacements
    // Purpose: Attempts to retrieve or parse try get collision placements data without treating normal misses as failures.
    // Parameters:
    // - tileX: Tile X value supplied by the caller for this operation.
    // - tileY: Tile Y value supplied by the caller for this operation.
    // - point: Point value supplied by the caller for this operation.
    // - placements: Placements value supplied by the caller for this operation.
    // Returns: Returns true when try get collision placements succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to MapGridManager so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetCollisionPlacements(byte tileX, byte tileY, MapTileVector3 point, out IReadOnlyList<MapTileCollisionPlacement> placements)
    {
        placements = [];
        if (!TryGetGrid(tileX, tileY, out LoadedMapGrid grid))
        {
            return false;
        }

        placements = grid.Tile.CollisionQueries.FindPlacementsContaining(point);
        return true;
    }

    // Method: UnloadAllGrids
    // Purpose: Executes the unload all grids operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - reason: Reason value supplied by the caller for this operation.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to MapGridManager so callers do not duplicate validation, protocol, or persistence rules.
    public int UnloadAllGrids(string reason)
    {
        int unloaded = 0;
        foreach (MapTileKey key in _loadedGrids.Keys.ToArray())
        {
            if (_loadedGrids.TryRemove(key, out _))
            {
                unloaded++;
            }
        }

        Logger.Write(LogType.NETWORK, $"Unloaded {unloaded} map grid(s) for '{_definition.Name}'. Reason: {reason}", "MapGridManager");
        return unloaded;
    }

    // Method: UnloadIdleGrids
    // Purpose: Executes the unload idle grids operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapGridManager so callers do not duplicate validation, protocol, or persistence rules.
    public void UnloadIdleGrids()
    {

    }

    // Method: PreloadAllTilesForMapAsync
    // Purpose: Executes the preload all tiles for map operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MapGridManager so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task PreloadAllTilesForMapAsync(CancellationToken cancellationToken)
    {
        int loaded = 0;
        foreach (MapTileKey key in EnumerateMapTileKeysForMap())
        {
            cancellationToken.ThrowIfCancellationRequested();
            MapTileDataStore tile = MapTileDataStore.Load(_mapsDirectory, key);
            _loadedGrids[tile.Key] = new LoadedMapGrid(tile);
            loaded++;

            if (loaded % 64 == 0)
            {
                await Task.Yield();
            }
        }

        if (loaded == 0)
        {
            throw new InvalidDataException($"No extracted mapstore tiles were found for '{_definition.Name}' in '{_mapsDirectory}'.");
        }

        Logger.Write(LogType.SUCCESS, $"Preloaded {loaded} map grid(s) for '{_definition.Name}' from '{_mapsDirectory}'. Mapstore policy: {MapStoreRuntimeFeatures.FormatPolicy()}.", "MapGridManager");
    }

    // Method: EnumerateMapTileKeysForMap
    // Purpose: Executes the enumerate map tile keys for map operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the I enumerable value produced by this operation.
    // Notes: This keeps the operation scoped to MapGridManager so callers do not duplicate validation, protocol, or persistence rules.
    private IEnumerable<MapTileKey> EnumerateMapTileKeysForMap()
    {
        string indexPath = MapStoreFileNames.GetIndexPath(_mapsDirectory, (uint)_definition.MapId);
        if (!File.Exists(indexPath))
        {
            throw new FileNotFoundException($"Required mapstore index file was not found for map {_definition.MapId:D3}: {indexPath}", indexPath);
        }

        MapStoreMapIndex index = MapStoreMapIndexReader.Read(indexPath, (uint)_definition.MapId);
        foreach (MapStoreMapIndexRecord record in index.Records.OrderBy(record => record.Key.TileX).ThenBy(record => record.Key.TileY))
        {
            ValidateIndexRecord(record, indexPath);
            yield return record.Key;
        }
    }

    // Method: ValidateIndexRecord
    // Purpose: Validates or evaluates validate index record rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // - indexPath: Index path value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapGridManager so callers do not duplicate validation, protocol, or persistence rules.
    private static void ValidateIndexRecord(MapStoreMapIndexRecord record, string indexPath)
    {
        MapStoreTileDataFlags missingFlags = MapStoreRuntimeFeatures.RequiredFlags & ~record.DataFlags;
        if (missingFlags != MapStoreTileDataFlags.None)
        {
            throw new InvalidDataException($"{indexPath} reports tile {record.Key} is missing required mapstore data: {missingFlags}.");
        }
    }
}
