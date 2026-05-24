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
using System.Globalization;

using EmulationServer.Game.Data.Maps;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

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
public sealed class MapGridManager
{
    /**
      * Holds the private definition state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly MapServiceDefinition _definition;
    /**
      * Holds the private maps directory state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly string _mapsDirectory;
    /**
      * Holds the private loading mode state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly MapGridLoadingMode _loadingMode;
    /**
      * Holds the private keep loaded state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly bool _keepLoaded;
    /**
      * Holds the private idle unload delay state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly TimeSpan _idleUnloadDelay;
    private readonly ConcurrentDictionary<MapTileKey, LoadedMapGrid> _loadedGrids = new();

    /**
      * Initializes a new MapGridManager instance with the dependencies required by the runtime map-player state tracking workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: definition, mapsDirectory, loadingMode, keepLoaded, idleUnloadDelay.
      */
    public MapGridManager(
        MapServiceDefinition definition,
        string mapsDirectory,
        MapGridLoadingMode loadingMode,
        bool keepLoaded,
        TimeSpan idleUnloadDelay)
    {
        _definition = definition ?? throw new ArgumentNullException();
        _mapsDirectory = string.IsNullOrWhiteSpace(mapsDirectory)
            ? throw new ArgumentException("Maps directory is required.")
            : Path.GetFullPath(mapsDirectory);
        _loadingMode = loadingMode;
        _keepLoaded = keepLoaded;
        _idleUnloadDelay = idleUnloadDelay;
    }

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
    public async Task InitializeAsync(IEnumerable<MapTileKey> startupGrids, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_mapsDirectory))
        {
            throw new DirectoryNotFoundException($"Map tile directory was not found: {_mapsDirectory}");
        }

        if (_loadingMode == MapGridLoadingMode.Preload)
        {
            await PreloadAllTilesForMapAsync(cancellationToken);
            return;
        }

        foreach (MapTileKey key in startupGrids.Where(key => key.MapId == _definition.MapId))
        {
            cancellationToken.ThrowIfCancellationRequested();
            LoadGrid(key, markUsed: true);
        }
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

        try
        {
            grid = LoadGrid(key, markUsed: true);
            return true;
        }
        catch (FileNotFoundException)
        {
            grid = null!;
            return false;
        }
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
        if (_keepLoaded || _idleUnloadDelay <= TimeSpan.Zero)
        {
            return;
        }

        DateTimeOffset cutoff = DateTimeOffset.UtcNow - _idleUnloadDelay;
        foreach ((MapTileKey key, LoadedMapGrid grid) in _loadedGrids.ToArray())
        {
            if (grid.LastUsedUtc > cutoff)
            {
                continue;
            }

            if (_loadedGrids.TryRemove(key, out _))
            {
                Logger.Write(LogType.TRACE, $"Unloaded idle map grid {FormatKey(key)} for '{_definition.Name}'.", "MapGridManager");
            }
        }
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
        foreach (string path in EnumerateMapTileFilesForMap())
        {
            cancellationToken.ThrowIfCancellationRequested();
            MapTileDataStore tile = MapTileDataStore.Load(path);
            _loadedGrids[tile.Key] = new LoadedMapGrid(tile);
            loaded++;

            if (loaded % 64 == 0)
            {
                await Task.Yield();
            }
        }

        Logger.Write(LogType.SUCCESS, $"Preloaded {loaded} map grid(s) for '{_definition.Name}' from '{_mapsDirectory}'.", "MapGridManager");
    }

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of MapGridManager and keeps this workflow isolated from the caller.
      */
    private LoadedMapGrid LoadGrid(MapTileKey key, bool markUsed)
    {
        LoadedMapGrid grid = _loadedGrids.GetOrAdd(key, static (tileKey, state) =>
        {
            string path = state.ResolveTilePath(tileKey);
            MapTileDataStore tile = MapTileDataStore.Load(path);
            Logger.Write(LogType.TRACE, $"Loaded map grid {FormatKey(tileKey)} for '{state._definition.Name}'.", "MapGridManager");
            return new LoadedMapGrid(tile);
        }, this);

        if (markUsed)
        {
            grid.Touch();
        }

        return grid;
    }

    /**
      * Resolves the tile path value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: key.
      */
    private string ResolveTilePath(MapTileKey key)
    {
        string fileName = string.Create(CultureInfo.InvariantCulture, $"{key.MapId:000}{key.TileX:00}{key.TileY:00}.map");
        string directPath = Path.Combine(_mapsDirectory, fileName);
        if (File.Exists(directPath))
        {
            return directPath;
        }

        string nestedPath = Path.Combine(_mapsDirectory, key.MapId.ToString(CultureInfo.InvariantCulture), fileName);
        if (File.Exists(nestedPath))
        {
            return nestedPath;
        }

        throw new FileNotFoundException($"Map grid file was not found for {FormatKey(key)}. Checked '{directPath}' and '{nestedPath}'.", fileName);
    }

    /**
      * Performs the enumerate map tile files for map operation for the runtime map-player state tracking workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      */
    private IEnumerable<string> EnumerateMapTileFilesForMap()
    {
        string prefix = _definition.MapId.ToString("000", CultureInfo.InvariantCulture);
        return Directory.EnumerateFiles(_mapsDirectory, $"{prefix}*.map", SearchOption.AllDirectories)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase);
    }

    /**
      * Formats runtime values into a stable human-readable message for logging or diagnostics.
      * The method is part of MapGridManager and keeps this workflow isolated from the caller.
      */
    private static string FormatKey(MapTileKey key)
    {
        return $"MapId={key.MapId}, TileX={key.TileX}, TileY={key.TileY}";
    }
}
