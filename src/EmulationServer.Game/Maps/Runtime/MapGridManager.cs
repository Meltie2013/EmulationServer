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

using System.Collections.Concurrent;
using EmulationServer.Game.Data.Maps;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.Shared.Data.MapStore;

/**
  * File overview: src/EmulationServer.Game/Maps/Runtime/MapGridManager.cs
  * Documents the MapGridManager source file in the runtime map-player state tracking area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Maps.Runtime;

/**
  * Owns loaded map grid tiles for a service and controls whether tiles stay resident or unload when idle.
  * It coordinates a collection of related runtime objects and keeps ownership rules in one place.
  */
public sealed class MapGridManager(
    MapServiceDefinition definition,
    string mapsDirectory)
{
    /**
      * Holds the private definition state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly MapServiceDefinition _definition = definition ?? throw new ArgumentNullException();
    /**
      * Holds the private maps directory state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly string _mapsDirectory = string.IsNullOrWhiteSpace(mapsDirectory)
        ? throw new ArgumentException("Maps directory is required.")
        : Path.GetFullPath(mapsDirectory);
    private readonly ConcurrentDictionary<MapTileKey, LoadedMapGrid> _loadedGrids = new();

    /**
      * Gets or stores the loaded grid count value used by MapGridManager.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int LoadedGridCount => _loadedGrids.Count;

    /**
      * Gets or stores the loaded grid keys value used by MapGridManager.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public IReadOnlyCollection<MapTileKey> LoadedGridKeys => _loadedGrids.Keys.ToArray();

    /**
      * Initializes dependent resources before the service begins normal operation.
      * The method is part of MapGridManager and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_mapsDirectory))
        {
            throw new DirectoryNotFoundException($"Map tile directory was not found: {_mapsDirectory}");
        }

        await PreloadAllTilesForMapAsync(cancellationToken);
    }

    /**
      * Attempts the operation without treating a normal failure as an exceptional condition.
      * The method is part of MapGridManager and keeps this workflow isolated from the caller.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
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

    /**
      * Attempts to sample terrain height from a loaded or loadable tile.
      */
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

    /**
      * Attempts to read the terrain area flag for a local ADT cell.
      */
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

    /**
      * Attempts to read liquid information for a local tile grid coordinate.
      */
    public bool TryGetLiquidInfo(byte tileX, byte tileY, float gridX, float gridY, out MapTileLiquidInfo liquidInfo)
    {
        liquidInfo = default;
        if (!TryGetGrid(tileX, tileY, out LoadedMapGrid grid))
        {
            return false;
        }

        return grid.Tile.LiquidQueries.TryGetLiquidInfo(gridX, gridY, out liquidInfo);
    }

    /**
      * Attempts to return collision placements whose extracted bounds contain the supplied world point.
      */
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

    /**
      * Performs the unload all grids operation for the runtime map-player state tracking workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: reason.
      */
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

    /**
      * Performs the unload idle grids operation for the runtime map-player state tracking workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      */
    public void UnloadIdleGrids()
    {
        // Grids are intentionally kept resident for deterministic runtime behavior.
        // Disabling terrain/liquid/collision/navmesh data requires a compile-time mapstore feature symbol.
    }

    /**
      * Performs the preload all tiles for map operation for the runtime map-player state tracking workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Enumerates map tile keys from the required map.index.bin file.
      * The index is mandatory so startup validates the exact extracted tile set instead of guessing from file names.
      */
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

    /**
      * Validates that the map index says every compile-required tile component exists.
      */
    private static void ValidateIndexRecord(MapStoreMapIndexRecord record, string indexPath)
    {
        MapStoreTileDataFlags missingFlags = MapStoreRuntimeFeatures.RequiredFlags & ~record.DataFlags;
        if (missingFlags != MapStoreTileDataFlags.None)
        {
            throw new InvalidDataException($"{indexPath} reports tile {record.Key} is missing required mapstore data: {missingFlags}.");
        }
    }
}
