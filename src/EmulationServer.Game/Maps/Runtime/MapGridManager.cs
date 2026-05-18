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

namespace EmulationServer.Game.Maps.Runtime;

public sealed class MapGridManager
{
    private readonly MapServiceDefinition _definition;
    private readonly string _mapsDirectory;
    private readonly MapGridLoadingMode _loadingMode;
    private readonly bool _keepLoaded;
    private readonly TimeSpan _idleUnloadDelay;
    private readonly ConcurrentDictionary<MapTileKey, LoadedMapGrid> _loadedGrids = new();

    public MapGridManager(
        MapServiceDefinition definition,
        string mapsDirectory,
        MapGridLoadingMode loadingMode,
        bool keepLoaded,
        TimeSpan idleUnloadDelay)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _mapsDirectory = string.IsNullOrWhiteSpace(mapsDirectory)
            ? throw new ArgumentException("Maps directory is required.", nameof(mapsDirectory))
            : Path.GetFullPath(mapsDirectory);
        _loadingMode = loadingMode;
        _keepLoaded = keepLoaded;
        _idleUnloadDelay = idleUnloadDelay;
    }

    public int LoadedGridCount => _loadedGrids.Count;

    public IReadOnlyCollection<MapTileKey> LoadedGridKeys => _loadedGrids.Keys.ToArray();

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

        Logger.Write(LogType.NETWORK, $"Unloaded {unloaded} map grid(s) for '{_definition.Name}'. Reason: {reason}", nameof(MapGridManager));
        return unloaded;
    }

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
                Logger.Write(LogType.TRACE, $"Unloaded idle map grid {FormatKey(key)} for '{_definition.Name}'.", nameof(MapGridManager));
            }
        }
    }

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

        Logger.Write(LogType.SUCCESS, $"Preloaded {loaded} map grid(s) for '{_definition.Name}' from '{_mapsDirectory}'.", nameof(MapGridManager));
    }

    private LoadedMapGrid LoadGrid(MapTileKey key, bool markUsed)
    {
        LoadedMapGrid grid = _loadedGrids.GetOrAdd(key, static (tileKey, state) =>
        {
            string path = state.ResolveTilePath(tileKey);
            MapTileDataStore tile = MapTileDataStore.Load(path);
            Logger.Write(LogType.TRACE, $"Loaded map grid {FormatKey(tileKey)} for '{state._definition.Name}'.", nameof(MapGridManager));
            return new LoadedMapGrid(tile);
        }, this);

        if (markUsed)
        {
            grid.Touch();
        }

        return grid;
    }

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

    private IEnumerable<string> EnumerateMapTileFilesForMap()
    {
        string prefix = _definition.MapId.ToString("000", CultureInfo.InvariantCulture);
        return Directory.EnumerateFiles(_mapsDirectory, $"{prefix}*.map", SearchOption.AllDirectories)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase);
    }

    private static string FormatKey(MapTileKey key)
    {
        return $"MapId={key.MapId}, TileX={key.TileX}, TileY={key.TileY}";
    }
}
