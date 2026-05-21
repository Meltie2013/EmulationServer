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

using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

/**
  * File overview: src/EmulationServer.Game/Data/Dbc/Maps/MapDbcDataStore.cs
  * This file converts raw DBC tables into typed map metadata used by map services, routing, status output, and validation.
  */

namespace EmulationServer.Game.Data.Dbc.Maps;

/**
  * Owns typed map-related DBC data and precomputed indexes so runtime systems can query map metadata without parsing raw rows.
  */
public sealed class MapDbcDataStore
{
    /**
      * Creates an empty typed map-data store for disabled DBC loading paths.
      */
    private MapDbcDataStore()
    {
        Maps = new Dictionary<int, MapDbcRecord>();
        Areas = new Dictionary<int, AreaTableDbcRecord>();
        AreaTriggers = new Dictionary<int, AreaTriggerDbcRecord>();
        WorldMapAreas = new Dictionary<int, WorldMapAreaDbcRecord>();
        WorldMapContinents = new Dictionary<int, WorldMapContinentDbcRecord>();
        WorldMapOverlays = new Dictionary<int, WorldMapOverlayDbcRecord>();
        AreasByMap = new Dictionary<int, IReadOnlyList<AreaTableDbcRecord>>();
        TriggersByMap = new Dictionary<int, IReadOnlyList<AreaTriggerDbcRecord>>();
        WorldMapAreasByContinent = new Dictionary<int, IReadOnlyList<WorldMapAreaDbcRecord>>();
        OverlaysByWorldMapArea = new Dictionary<int, IReadOnlyList<WorldMapOverlayDbcRecord>>();
    }

    /**
      * Creates a typed map-data store from all parsed records and indexes.
      */
    private MapDbcDataStore(
        IReadOnlyDictionary<int, MapDbcRecord> maps,
        IReadOnlyDictionary<int, AreaTableDbcRecord> areas,
        IReadOnlyDictionary<int, AreaTriggerDbcRecord> areaTriggers,
        IReadOnlyDictionary<int, WorldMapAreaDbcRecord> worldMapAreas,
        IReadOnlyDictionary<int, WorldMapContinentDbcRecord> worldMapContinents,
        IReadOnlyDictionary<int, WorldMapOverlayDbcRecord> worldMapOverlays,
        IReadOnlyDictionary<int, IReadOnlyList<AreaTableDbcRecord>> areasByMap,
        IReadOnlyDictionary<int, IReadOnlyList<AreaTriggerDbcRecord>> triggersByMap,
        IReadOnlyDictionary<int, IReadOnlyList<WorldMapAreaDbcRecord>> worldMapAreasByContinent,
        IReadOnlyDictionary<int, IReadOnlyList<WorldMapOverlayDbcRecord>> overlaysByWorldMapArea)
    {
        Maps = maps;
        Areas = areas;
        AreaTriggers = areaTriggers;
        WorldMapAreas = worldMapAreas;
        WorldMapContinents = worldMapContinents;
        WorldMapOverlays = worldMapOverlays;
        AreasByMap = areasByMap;
        TriggersByMap = triggersByMap;
        WorldMapAreasByContinent = worldMapAreasByContinent;
        OverlaysByWorldMapArea = overlaysByWorldMapArea;
    }

    /**
      * Gets an empty typed map-data store for servers that run with DBC loading disabled.
      */
    public static MapDbcDataStore Empty { get; } = new();

    /**
      * Gets all Map.dbc records indexed by map id.
      */
    public IReadOnlyDictionary<int, MapDbcRecord> Maps { get; }

    /**
      * Gets all AreaTable.dbc records indexed by area id.
      */
    public IReadOnlyDictionary<int, AreaTableDbcRecord> Areas { get; }

    /**
      * Gets all AreaTrigger.dbc records indexed by trigger id.
      */
    public IReadOnlyDictionary<int, AreaTriggerDbcRecord> AreaTriggers { get; }

    /**
      * Gets all WorldMapArea.dbc records indexed by world-map area id.
      */
    public IReadOnlyDictionary<int, WorldMapAreaDbcRecord> WorldMapAreas { get; }

    /**
      * Gets all WorldMapContinent.dbc records indexed by world-map continent id.
      */
    public IReadOnlyDictionary<int, WorldMapContinentDbcRecord> WorldMapContinents { get; }

    /**
      * Gets all WorldMapOverlay.dbc records indexed by overlay id.
      */
    public IReadOnlyDictionary<int, WorldMapOverlayDbcRecord> WorldMapOverlays { get; }

    /**
      * Gets areas grouped by their owning map id.
      */
    public IReadOnlyDictionary<int, IReadOnlyList<AreaTableDbcRecord>> AreasByMap { get; }

    /**
      * Gets area triggers grouped by their owning map id.
      */
    public IReadOnlyDictionary<int, IReadOnlyList<AreaTriggerDbcRecord>> TriggersByMap { get; }

    /**
      * Gets world-map display areas grouped by their world-map continent id.
      */
    public IReadOnlyDictionary<int, IReadOnlyList<WorldMapAreaDbcRecord>> WorldMapAreasByContinent { get; }

    /**
      * Gets world-map overlays grouped by their owning world-map area id.
      */
    public IReadOnlyDictionary<int, IReadOnlyList<WorldMapOverlayDbcRecord>> OverlaysByWorldMapArea { get; }

    /**
      * Converts the supplied raw DBC stores into a typed map-data store and validates the expected schemas from the CSV references.
      */
    public static MapDbcDataStore FromDbcStores(IReadOnlyDictionary<string, DbcDataStore> dbcStores, string ownerName)
    {
        ArgumentNullException.ThrowIfNull(dbcStores);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerName);

        Dictionary<int, MapDbcRecord> maps = LoadIndexed(dbcStores, MapDbcFileNames.Map, ownerName, 42, ReadMapRecord);
        Dictionary<int, AreaTableDbcRecord> areas = LoadIndexed(dbcStores, MapDbcFileNames.AreaTable, ownerName, 25, ReadAreaRecord);
        Dictionary<int, AreaTriggerDbcRecord> triggers = LoadIndexed(dbcStores, MapDbcFileNames.AreaTrigger, ownerName, 10, ReadAreaTriggerRecord);
        Dictionary<int, WorldMapAreaDbcRecord> worldMapAreas = LoadIndexed(dbcStores, MapDbcFileNames.WorldMapArea, ownerName, 8, ReadWorldMapAreaRecord);
        Dictionary<int, WorldMapContinentDbcRecord> worldMapContinents = LoadIndexed(dbcStores, MapDbcFileNames.WorldMapContinent, ownerName, 13, ReadWorldMapContinentRecord);
        Dictionary<int, WorldMapOverlayDbcRecord> worldMapOverlays = LoadIndexed(dbcStores, MapDbcFileNames.WorldMapOverlay, ownerName, 17, ReadWorldMapOverlayRecord);

        MapDbcDataStore mapData = new(
            maps,
            areas,
            triggers,
            worldMapAreas,
            worldMapContinents,
            worldMapOverlays,
            GroupByMapId(areas.Values),
            GroupByMapId(triggers.Values),
            GroupByWorldMapContinentId(worldMapAreas.Values),
            GroupByWorldMapAreaId(worldMapOverlays.Values));

        Logger.Write(
            LogType.SUCCESS,
            $"{ownerName} typed map DBC data loaded: maps={mapData.Maps.Count}, areas={mapData.Areas.Count}, triggers={mapData.AreaTriggers.Count}, worldMapAreas={mapData.WorldMapAreas.Count}, continents={mapData.WorldMapContinents.Count}, overlays={mapData.WorldMapOverlays.Count}.",
            nameof(MapDbcDataStore));

        return mapData;
    }

    /**
      * Attempts to get one Map.dbc record by map id.
      */
    public bool TryGetMap(int mapId, out MapDbcRecord map)
    {
        return Maps.TryGetValue(mapId, out map!);
    }

    /**
      * Returns all AreaTable rows assigned to the supplied map id.
      */
    public IReadOnlyList<AreaTableDbcRecord> GetAreasForMap(int mapId)
    {
        return AreasByMap.TryGetValue(mapId, out IReadOnlyList<AreaTableDbcRecord>? areas)
            ? areas
            : [];
    }

    /**
      * Returns all AreaTrigger rows assigned to the supplied map id.
      */
    public IReadOnlyList<AreaTriggerDbcRecord> GetTriggersForMap(int mapId)
    {
        return TriggersByMap.TryGetValue(mapId, out IReadOnlyList<AreaTriggerDbcRecord>? triggers)
            ? triggers
            : [];
    }

    /**
      * Returns all WorldMapContinent rows associated with the supplied map id.
      */
    public IReadOnlyList<WorldMapContinentDbcRecord> GetContinentsForMap(int mapId)
    {
        return WorldMapContinents.Values
            .Where(continent => continent.MapId == mapId)
            .ToArray();
    }

    /**
      * Builds a short description used by map info commands and startup logs.
      */
    public string DescribeMap(int mapId)
    {
        if (!TryGetMap(mapId, out MapDbcRecord map))
        {
            return $"MapId={mapId} is not present in Map.dbc.";
        }

        int areaCount = GetAreasForMap(mapId).Count;
        int triggerCount = GetTriggersForMap(mapId).Count;
        int continentCount = GetContinentsForMap(mapId).Count;

        return $"{map.DisplayName} (MapId={map.Id}, Type={map.Type}, Areas={areaCount}, Triggers={triggerCount}, Continents={continentCount})";
    }

    /**
      * Reads one DBC file into typed records indexed by record id.
      */
    private static Dictionary<int, TRecord> LoadIndexed<TRecord>(
        IReadOnlyDictionary<string, DbcDataStore> dbcStores,
        string fileName,
        string ownerName,
        int requiredFieldCount,
        Func<DbcRecord, TRecord> readRecord)
        where TRecord : notnull
    {
        Dictionary<int, TRecord> records = [];
        if (!dbcStores.TryGetValue(fileName, out DbcDataStore? store))
        {
            Logger.Write(LogType.WARNING, $"{ownerName} did not load {fileName}; typed map data from that file will be unavailable.", nameof(MapDbcDataStore));
            return records;
        }

        ValidateFieldCount(store, fileName, requiredFieldCount);

        foreach (DbcRecord record in store.EnumerateRecords())
        {
            TRecord typedRecord = readRecord(record);
            records[GetRecordId(typedRecord)] = typedRecord;
        }

        return records;
    }

    /**
      * Validates that the loaded DBC schema matches the CSV layout used for the typed reader.
      */
    private static void ValidateFieldCount(DbcDataStore store, string fileName, int requiredFieldCount)
    {
        if (store.FieldCount < requiredFieldCount)
        {
            throw new DbcFormatException($"{fileName} has {store.FieldCount} field(s), but the typed map DBC reader requires at least {requiredFieldCount} field(s).");
        }
    }

    /**
      * Returns the typed record id without forcing every record type through an interface.
      */
    private static int GetRecordId<TRecord>(TRecord record)
    {
        return record switch
        {
            MapDbcRecord map => map.Id,
            AreaTableDbcRecord area => area.Id,
            AreaTriggerDbcRecord trigger => trigger.Id,
            WorldMapAreaDbcRecord worldMapArea => worldMapArea.Id,
            WorldMapContinentDbcRecord continent => continent.Id,
            WorldMapOverlayDbcRecord overlay => overlay.Id,
            _ => throw new InvalidOperationException($"Unsupported map DBC record type {typeof(TRecord).Name}.")
        };
    }

    /**
      * Reads one Map.dbc row using the field order from the supplied CSV export.
      */
    private static MapDbcRecord ReadMapRecord(DbcRecord record)
    {
        return new MapDbcRecord(
            ReadInt32(record, 0),
            ReadString(record, 1),
            ReadInt32(record, 2),
            ReadInt32(record, 3) != 0,
            ReadString(record, 4),
            ReadInt32(record, 13),
            ReadInt32(record, 14),
            ReadInt32(record, 15),
            ReadInt32(record, 19),
            ReadInt32(record, 38),
            ReadInt32(record, 39),
            ReadString(record, 40),
            ReadSingle(record, 41));
    }

    /**
      * Reads one AreaTable.dbc row using the field order from the supplied CSV export.
      */
    private static AreaTableDbcRecord ReadAreaRecord(DbcRecord record)
    {
        return new AreaTableDbcRecord(
            ReadInt32(record, 0),
            ReadInt32(record, 1),
            ReadInt32(record, 2),
            ReadInt32(record, 3),
            ReadInt32(record, 4),
            ReadInt32(record, 5),
            ReadInt32(record, 6),
            ReadInt32(record, 7),
            ReadInt32(record, 8),
            ReadInt32(record, 9),
            ReadInt32(record, 10),
            ReadString(record, 11),
            ReadInt32(record, 20),
            ReadInt32(record, 21),
            ReadSingle(record, 22),
            ReadSingle(record, 23),
            ReadInt32(record, 24));
    }

    /**
      * Reads one AreaTrigger.dbc row using the field order from the supplied CSV export.
      */
    private static AreaTriggerDbcRecord ReadAreaTriggerRecord(DbcRecord record)
    {
        return new AreaTriggerDbcRecord(
            ReadInt32(record, 0),
            ReadInt32(record, 1),
            ReadSingle(record, 2),
            ReadSingle(record, 3),
            ReadSingle(record, 4),
            ReadSingle(record, 5),
            ReadSingle(record, 6),
            ReadSingle(record, 7),
            ReadSingle(record, 8),
            ReadSingle(record, 9));
    }

    /**
      * Reads one WorldMapArea.dbc row using the field order from the supplied CSV export.
      */
    private static WorldMapAreaDbcRecord ReadWorldMapAreaRecord(DbcRecord record)
    {
        return new WorldMapAreaDbcRecord(
            ReadInt32(record, 0),
            ReadInt32(record, 1),
            ReadInt32(record, 2),
            ReadString(record, 3),
            ReadSingle(record, 4),
            ReadSingle(record, 5),
            ReadSingle(record, 6),
            ReadSingle(record, 7));
    }

    /**
      * Reads one WorldMapContinent.dbc row using the field order from the supplied CSV export.
      */
    private static WorldMapContinentDbcRecord ReadWorldMapContinentRecord(DbcRecord record)
    {
        return new WorldMapContinentDbcRecord(
            ReadInt32(record, 0),
            ReadInt32(record, 1),
            ReadInt32(record, 2),
            ReadInt32(record, 3),
            ReadInt32(record, 4),
            ReadInt32(record, 5),
            ReadSingle(record, 6),
            ReadSingle(record, 7),
            ReadSingle(record, 8),
            ReadSingle(record, 9),
            ReadSingle(record, 10),
            ReadSingle(record, 11),
            ReadSingle(record, 12));
    }

    /**
      * Reads one WorldMapOverlay.dbc row using the field order from the supplied CSV export.
      */
    private static WorldMapOverlayDbcRecord ReadWorldMapOverlayRecord(DbcRecord record)
    {
        int[] areaIds =
        [
            ReadInt32(record, 2),
            ReadInt32(record, 3),
            ReadInt32(record, 4),
            ReadInt32(record, 5),
        ];

        return new WorldMapOverlayDbcRecord(
            ReadInt32(record, 0),
            ReadInt32(record, 1),
            areaIds.Where(areaId => areaId > 0).Distinct().ToArray(),
            ReadInt32(record, 6),
            ReadInt32(record, 7),
            ReadString(record, 8),
            ReadInt32(record, 9),
            ReadInt32(record, 10),
            ReadInt32(record, 11),
            ReadInt32(record, 12),
            ReadInt32(record, 13),
            ReadInt32(record, 14),
            ReadInt32(record, 15),
            ReadInt32(record, 16));
    }

    /**
      * Groups AreaTable records by map id for map service startup summaries and future area lookups.
      */
    private static IReadOnlyDictionary<int, IReadOnlyList<AreaTableDbcRecord>> GroupByMapId(IEnumerable<AreaTableDbcRecord> records)
    {
        return records
            .GroupBy(record => record.MapId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<AreaTableDbcRecord>)group.OrderBy(record => record.Id).ToArray());
    }

    /**
      * Groups AreaTrigger records by map id for future portal and trigger processing.
      */
    private static IReadOnlyDictionary<int, IReadOnlyList<AreaTriggerDbcRecord>> GroupByMapId(IEnumerable<AreaTriggerDbcRecord> records)
    {
        return records
            .GroupBy(record => record.MapId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<AreaTriggerDbcRecord>)group.OrderBy(record => record.Id).ToArray());
    }

    /**
      * Groups world-map area records by continent id so continent map services can quickly inspect available area overlays.
      */
    private static IReadOnlyDictionary<int, IReadOnlyList<WorldMapAreaDbcRecord>> GroupByWorldMapContinentId(IEnumerable<WorldMapAreaDbcRecord> records)
    {
        return records
            .GroupBy(record => record.WorldMapContinentId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<WorldMapAreaDbcRecord>)group.OrderBy(record => record.Id).ToArray());
    }

    /**
      * Groups overlay records by world-map area id for future map and minimap lookup features.
      */
    private static IReadOnlyDictionary<int, IReadOnlyList<WorldMapOverlayDbcRecord>> GroupByWorldMapAreaId(IEnumerable<WorldMapOverlayDbcRecord> records)
    {
        return records
            .GroupBy(record => record.WorldMapAreaId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<WorldMapOverlayDbcRecord>)group.OrderBy(record => record.Id).ToArray());
    }

    /**
      * Reads a signed integer field from a raw DBC record.
      */
    private static int ReadInt32(DbcRecord record, int fieldIndex)
    {
        return record.GetInt32(fieldIndex);
    }

    /**
      * Reads a floating-point field from a raw DBC record.
      */
    private static float ReadSingle(DbcRecord record, int fieldIndex)
    {
        return record.GetSingle(fieldIndex);
    }

    /**
      * Reads a string field from a raw DBC record and trims null-only or whitespace-only values.
      */
    private static string ReadString(DbcRecord record, int fieldIndex)
    {
        return record.GetString(fieldIndex).Trim('\0', ' ', '\t', '\r', '\n');
    }
}
