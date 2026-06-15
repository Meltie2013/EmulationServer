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

using EmulationServer.Game.Data.Dbc.Maps;
using EmulationServer.Shared.Data.MapStore;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Game.Data.Maps;

/**
  * Resolves world-space coordinates to AreaTable ids for spawned world objects.
  * The resolver prefers exact ADT area flags from extracted terrain bins, then falls back to WorldMapArea/WorldMapOverlay rectangles.
  */
public sealed class MapStoreAreaLookupService
{
    private const float TileSize = 1600.0f / 3.0f;
    private const float MapHalfGrid = 32.0f;
    private const float WorldMapPixelWidth = 1002.0f;
    private const float WorldMapPixelHeight = 668.0f;

    private readonly string _mapStoreRootDirectory;
    private readonly MapDbcDataStore _mapData;
    private readonly Dictionary<ushort, uint> _areaIdsByExplorationFlag;
    private readonly Dictionary<MapTileKey, MapTileTerrainData?> _terrainCache = [];
    private readonly HashSet<MapTileKey> _missingTerrainTiles = [];

    public MapStoreAreaLookupService(string mapStoreRootDirectory, MapDbcDataStore mapData)
    {
        if (string.IsNullOrWhiteSpace(mapStoreRootDirectory))
        {
            throw new ArgumentException("Mapstore root directory is required.", nameof(mapStoreRootDirectory));
        }

        _mapStoreRootDirectory = Path.GetFullPath(mapStoreRootDirectory);
        _mapData = mapData ?? throw new ArgumentNullException(nameof(mapData));
        _areaIdsByExplorationFlag = BuildAreaFlagReverseLookup(mapData);
    }

    /**
      * Attempts to resolve one map coordinate to a non-zero ZoneId/AreaId pair.
      */
    public bool TryResolve(uint mapId, float worldX, float worldY, out WorldAreaLookupResult result)
    {
        if (TryResolveFromTerrain(mapId, worldX, worldY, out result))
        {
            return true;
        }

        if (TryResolveFromWorldMapArea(mapId, worldX, worldY, out result))
        {
            return true;
        }

        if (TryResolveFromAreaTableFallback(mapId, out result))
        {
            return true;
        }

        result = default;
        return false;
    }

    private bool TryResolveFromTerrain(uint mapId, float worldX, float worldY, out WorldAreaLookupResult result)
    {
        result = default;
        if (!TryConvertWorldToTile(mapId, worldX, worldY, out MapTileKey key, out float gridX, out float gridY))
        {
            return false;
        }

        MapTileTerrainData? terrain = GetTerrainOrDefault(key);
        if (terrain is null)
        {
            return false;
        }

        int sampleX = Math.Clamp((int)MathF.Floor(gridX), 0, MapStorePayloadConstants.GridSize - 1);
        int sampleY = Math.Clamp((int)MathF.Floor(gridY), 0, MapStorePayloadConstants.GridSize - 1);
        int cellX = Math.Clamp(sampleX / 8, 0, MapStorePayloadConstants.CellsPerGrid - 1);
        int cellY = Math.Clamp(sampleY / 8, 0, MapStorePayloadConstants.CellsPerGrid - 1);
        ushort areaFlag = terrain.AreaGrid[cellY * MapStorePayloadConstants.CellsPerGrid + cellX];

        if (areaFlag == ushort.MaxValue || !_areaIdsByExplorationFlag.TryGetValue(areaFlag, out uint areaId) || areaId == 0)
        {
            return false;
        }

        uint zoneId = GetZoneId(areaId);
        if (zoneId == 0)
        {
            return false;
        }

        result = new WorldAreaLookupResult(zoneId, areaId, "terrain");
        return true;
    }

    private MapTileTerrainData? GetTerrainOrDefault(MapTileKey key)
    {
        if (_terrainCache.TryGetValue(key, out MapTileTerrainData? cachedTerrain))
        {
            return cachedTerrain;
        }

        if (_missingTerrainTiles.Contains(key))
        {
            return null;
        }

        string terrainPath = MapStoreFileNames.GetTileFilePath(_mapStoreRootDirectory, key.MapId, key.TileX, key.TileY, MapStoreDataKind.Terrain);
        if (!File.Exists(terrainPath))
        {
            _missingTerrainTiles.Add(key);
            return null;
        }

        try
        {
            MapStoreFile terrainFile = MapStoreBinary.ReadFile(terrainPath, MapStoreDataKind.Terrain);
            ValidateTerrainKey(key, terrainFile);
            cachedTerrain = MapTileTerrainReader.Read(terrainFile);
            _terrainCache[key] = cachedTerrain;
            return cachedTerrain;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or MapFormatException)
        {
            _terrainCache[key] = null;
            Logger.Write(LogType.WARNING, $"Unable to read terrain area data for {key}: {exception.Message}", "MapStoreAreaLookupService");
            return null;
        }
    }

    private static void ValidateTerrainKey(MapTileKey key, MapStoreFile terrainFile)
    {
        if (terrainFile.Header.MapId != key.MapId || terrainFile.Header.TileX != key.TileX || terrainFile.Header.TileY != key.TileY)
        {
            throw new MapFormatException(
                $"{terrainFile.Path} has mismatched mapstore key. " +
                $"Expected {key}, got {MapStoreFileNames.FormatTileKey(terrainFile.Header.MapId, terrainFile.Header.TileX, terrainFile.Header.TileY)}.");
        }
    }

    private bool TryResolveFromWorldMapArea(uint mapId, float worldX, float worldY, out WorldAreaLookupResult result)
    {
        result = default;
        WorldMapAreaDbcRecord? worldMapArea = _mapData.WorldMapAreas.Values
            .Where(area => area.WorldMapContinentId >= 0 && unchecked((uint)area.WorldMapContinentId) == mapId && area.AreaTableId > 0 && ContainsWorldPosition(area, worldX, worldY))
            .OrderBy(AreaRectangleSize)
            .FirstOrDefault();

        if (worldMapArea is null)
        {
            return false;
        }

        uint zoneId = GetZoneId(unchecked((uint)worldMapArea.AreaTableId));
        if (zoneId == 0)
        {
            return false;
        }

        uint areaId = TryResolveOverlayArea(worldMapArea, worldX, worldY, out uint overlayAreaId)
            ? overlayAreaId
            : unchecked((uint)worldMapArea.AreaTableId);

        if (areaId == 0)
        {
            areaId = zoneId;
        }

        result = new WorldAreaLookupResult(zoneId, areaId, "world-map-area");
        return true;
    }

    private bool TryResolveOverlayArea(WorldMapAreaDbcRecord worldMapArea, float worldX, float worldY, out uint areaId)
    {
        areaId = 0;
        if (!_mapData.OverlaysByWorldMapArea.TryGetValue(worldMapArea.Id, out IReadOnlyList<WorldMapOverlayDbcRecord>? overlays))
        {
            return false;
        }

        float mapPixelX = ConvertWorldYToMapPixelX(worldMapArea, worldY);
        float mapPixelY = ConvertWorldXToMapPixelY(worldMapArea, worldX);

        WorldMapOverlayDbcRecord? overlay = overlays
            .Where(candidate => candidate.AreaTableIds.Count > 0 && ContainsOverlayPixel(candidate, mapPixelX, mapPixelY))
            .OrderBy(OverlayRectangleSize)
            .FirstOrDefault();

        if (overlay is null)
        {
            return false;
        }

        int candidateAreaId = overlay.AreaTableIds.FirstOrDefault(candidate => candidate > 0 && _mapData.Areas.ContainsKey(candidate));
        if (candidateAreaId <= 0)
        {
            return false;
        }

        areaId = unchecked((uint)candidateAreaId);
        return true;
    }

    private bool TryResolveFromAreaTableFallback(uint mapId, out WorldAreaLookupResult result)
    {
        result = default;
        AreaTableDbcRecord? area = _mapData.GetAreasForMap(unchecked((int)mapId))
            .Where(candidate => candidate.Id > 0 && candidate.ParentAreaTableId == 0)
            .OrderBy(candidate => candidate.Id)
            .FirstOrDefault()
            ?? _mapData.GetAreasForMap(unchecked((int)mapId))
                .Where(candidate => candidate.Id > 0)
                .OrderBy(candidate => candidate.Id)
                .FirstOrDefault();

        if (area is null)
        {
            return false;
        }

        uint areaId = unchecked((uint)area.Id);
        uint zoneId = GetZoneId(areaId);
        if (zoneId == 0)
        {
            zoneId = areaId;
        }

        result = new WorldAreaLookupResult(zoneId, areaId, "area-table-fallback");
        return true;
    }

    private uint GetZoneId(uint areaId)
    {
        if (areaId == 0 || !_mapData.Areas.TryGetValue(unchecked((int)areaId), out AreaTableDbcRecord? area))
        {
            return 0;
        }

        if (area.ParentAreaTableId > 0 && _mapData.Areas.ContainsKey(area.ParentAreaTableId))
        {
            return unchecked((uint)area.ParentAreaTableId);
        }

        return unchecked((uint)area.Id);
    }

    private static bool TryConvertWorldToTile(uint mapId, float worldX, float worldY, out MapTileKey key, out float gridX, out float gridY)
    {
        key = default;
        gridX = 0.0f;
        gridY = 0.0f;

        if (!float.IsFinite(worldX) || !float.IsFinite(worldY))
        {
            return false;
        }

        int tileX = (int)MathF.Floor(MapHalfGrid - worldY / TileSize);
        int tileY = (int)MathF.Floor(MapHalfGrid - worldX / TileSize);
        if (tileX < 0 || tileX > 63 || tileY < 0 || tileY > 63)
        {
            return false;
        }

        float localX = (MapHalfGrid - tileX) * TileSize - worldY;
        float localY = (MapHalfGrid - tileY) * TileSize - worldX;
        gridX = Math.Clamp((localX / TileSize) * MapStorePayloadConstants.GridSize, 0.0f, MapStorePayloadConstants.GridSize - 0.0001f);
        gridY = Math.Clamp((localY / TileSize) * MapStorePayloadConstants.GridSize, 0.0f, MapStorePayloadConstants.GridSize - 0.0001f);
        key = new MapTileKey(mapId, unchecked((byte)tileX), unchecked((byte)tileY));
        return true;
    }

    private static bool ContainsWorldPosition(WorldMapAreaDbcRecord area, float worldX, float worldY)
    {
        float minX = MathF.Min(area.LocationTop, area.LocationBottom);
        float maxX = MathF.Max(area.LocationTop, area.LocationBottom);
        float minY = MathF.Min(area.LocationLeft, area.LocationRight);
        float maxY = MathF.Max(area.LocationLeft, area.LocationRight);

        return worldX >= minX && worldX <= maxX && worldY >= minY && worldY <= maxY;
    }

    private static float AreaRectangleSize(WorldMapAreaDbcRecord area)
    {
        return MathF.Abs(area.LocationTop - area.LocationBottom) * MathF.Abs(area.LocationLeft - area.LocationRight);
    }

    private static float ConvertWorldYToMapPixelX(WorldMapAreaDbcRecord area, float worldY)
    {
        float left = area.LocationLeft;
        float right = area.LocationRight;
        if (MathF.Abs(left - right) < float.Epsilon)
        {
            return 0.0f;
        }

        return ((left - worldY) / (left - right)) * WorldMapPixelWidth;
    }

    private static float ConvertWorldXToMapPixelY(WorldMapAreaDbcRecord area, float worldX)
    {
        float top = area.LocationTop;
        float bottom = area.LocationBottom;
        if (MathF.Abs(top - bottom) < float.Epsilon)
        {
            return 0.0f;
        }

        return ((top - worldX) / (top - bottom)) * WorldMapPixelHeight;
    }

    private static bool ContainsOverlayPixel(WorldMapOverlayDbcRecord overlay, float mapPixelX, float mapPixelY)
    {
        int minX = Math.Min(overlay.HitRectLeft, overlay.HitRectRight);
        int maxX = Math.Max(overlay.HitRectLeft, overlay.HitRectRight);
        int minY = Math.Min(overlay.HitRectTop, overlay.HitRectBottom);
        int maxY = Math.Max(overlay.HitRectTop, overlay.HitRectBottom);

        return mapPixelX >= minX && mapPixelX <= maxX && mapPixelY >= minY && mapPixelY <= maxY;
    }

    private static int OverlayRectangleSize(WorldMapOverlayDbcRecord overlay)
    {
        return Math.Abs(overlay.HitRectRight - overlay.HitRectLeft) * Math.Abs(overlay.HitRectBottom - overlay.HitRectTop);
    }

    private static Dictionary<ushort, uint> BuildAreaFlagReverseLookup(MapDbcDataStore mapData)
    {
        Dictionary<ushort, uint> result = [];
        foreach (AreaTableDbcRecord area in mapData.Areas.Values.OrderBy(area => area.Id))
        {
            if (area.AreaBit <= 0 || area.AreaBit > ushort.MaxValue)
            {
                continue;
            }

            ushort flag = unchecked((ushort)area.AreaBit);
            result.TryAdd(flag, unchecked((uint)area.Id));
        }

        return result;
    }
}
