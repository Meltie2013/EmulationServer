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
// File: src/EmulationServer.Game/Data/Dbc/Maps/MapDbcDataStore.cs
// Purpose: Contains map DBC data store code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Game.Data.Dbc.Maps;

// Type: MapDbcDataStore
// Purpose: Provides map DBC data store behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class MapDbcDataStore
{

    // Constructor: MapDbcDataStore
    // Purpose: Initializes a new MapDbcDataStore instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Constructor: MapDbcDataStore
    // Purpose: Initializes a new MapDbcDataStore instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - maps: Maps value supplied by the caller for this operation.
    // - areas: Areas value supplied by the caller for this operation.
    // - areaTriggers: Area triggers value supplied by the caller for this operation.
    // - worldMapAreas: World map areas value supplied by the caller for this operation.
    // - worldMapContinents: World map continents value supplied by the caller for this operation.
    // - worldMapOverlays: World map overlays value supplied by the caller for this operation.
    // - areasByMap: Areas by map value supplied by the caller for this operation.
    // - triggersByMap: Triggers by map value supplied by the caller for this operation.
    // - worldMapAreasByContinent: World map areas by continent value supplied by the caller for this operation.
    // - overlaysByWorldMapArea: Overlays by world map area value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    public static MapDbcDataStore Empty { get; } = new();

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, MapDbcRecord> Maps { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, AreaTableDbcRecord> Areas { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, AreaTriggerDbcRecord> AreaTriggers { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, WorldMapAreaDbcRecord> WorldMapAreas { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, WorldMapContinentDbcRecord> WorldMapContinents { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, WorldMapOverlayDbcRecord> WorldMapOverlays { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, IReadOnlyList<AreaTableDbcRecord>> AreasByMap { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, IReadOnlyList<AreaTriggerDbcRecord>> TriggersByMap { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, IReadOnlyList<WorldMapAreaDbcRecord>> WorldMapAreasByContinent { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, IReadOnlyList<WorldMapOverlayDbcRecord>> OverlaysByWorldMapArea { get; }

    // Method: FromDbcStores
    // Purpose: Executes the from DBC stores operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - dbcStores: Dbc stores value supplied by the caller for this operation.
    // - ownerName: Owner name value supplied by the caller for this operation.
    // Returns: Returns the map DBC data store value produced by this operation.
    // Notes: This keeps the operation scoped to MapDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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
            string.Join(Environment.NewLine,
                $"{ownerName}: map DBC loaded:",
                $"  Map.dbc: {mapData.Maps.Count}",
                $"  AreaTable.dbc: {mapData.Areas.Count}",
                $"  AreaTrigger.dbc: {mapData.AreaTriggers.Count}",
                $"  WorldMapArea.dbc: {mapData.WorldMapAreas.Count}",
                $"  WorldMapContinent.dbc: {mapData.WorldMapContinents.Count}",
                $"  WorldMapOverlay.dbc: {mapData.WorldMapOverlays.Count}"),
            "MapDbcDataStore");

        return mapData;
    }

    // Method: TryGetMap
    // Purpose: Attempts to retrieve or parse try get map data without treating normal misses as failures.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - map: Map value supplied by the caller for this operation.
    // Returns: Returns true when try get map succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to MapDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetMap(int mapId, out MapDbcRecord map)
    {
        return Maps.TryGetValue(mapId, out map!);
    }

    // Method: GetAreasForMap
    // Purpose: Retrieves get areas for map data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to MapDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyList<AreaTableDbcRecord> GetAreasForMap(int mapId)
    {
        return AreasByMap.TryGetValue(mapId, out IReadOnlyList<AreaTableDbcRecord>? areas)
            ? areas
            : [];
    }

    // Method: GetTriggersForMap
    // Purpose: Retrieves get triggers for map data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to MapDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyList<AreaTriggerDbcRecord> GetTriggersForMap(int mapId)
    {
        return TriggersByMap.TryGetValue(mapId, out IReadOnlyList<AreaTriggerDbcRecord>? triggers)
            ? triggers
            : [];
    }

    // Method: GetContinentsForMap
    // Purpose: Retrieves get continents for map data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to MapDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyList<WorldMapContinentDbcRecord> GetContinentsForMap(int mapId)
    {
        return WorldMapContinents.Values
            .Where(continent => continent.MapId == mapId)
            .ToArray();
    }

    // Method: DescribeMap
    // Purpose: Executes the describe map operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to MapDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: TRecord
    // Purpose: Executes the T record operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - dbcStores: Dbc stores value supplied by the caller for this operation.
    // - fileName: File name value supplied by the caller for this operation.
    // - ownerName: Owner name value supplied by the caller for this operation.
    // - requiredFieldCount: Required field count value supplied by the caller for this operation.
    // - readRecord: Read record value supplied by the caller for this operation.
    // Returns: Returns the dictionary load indexed< value produced by this operation.
    // Notes: This keeps the operation scoped to MapDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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
            Logger.Write(LogType.WARNING, $"{ownerName} did not load {fileName}; typed map data from that file will be unavailable.", "MapDbcDataStore");
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

    // Method: ValidateFieldCount
    // Purpose: Validates or evaluates validate field count rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - store: Store value supplied by the caller for this operation.
    // - fileName: File name value supplied by the caller for this operation.
    // - requiredFieldCount: Required field count value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static void ValidateFieldCount(DbcDataStore store, string fileName, int requiredFieldCount)
    {
        if (store.FieldCount < requiredFieldCount)
        {
            throw new DbcFormatException($"{fileName} has {store.FieldCount} field(s), but the typed map DBC reader requires at least {requiredFieldCount} field(s).");
        }
    }

    // Method: TRecord
    // Purpose: Executes the T record operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the int get record id< value produced by this operation.
    // Notes: This keeps the operation scoped to MapDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadMapRecord
    // Purpose: Retrieves read map record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the map DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to MapDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadAreaRecord
    // Purpose: Retrieves read area record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the area table DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to MapDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadAreaTriggerRecord
    // Purpose: Retrieves read area trigger record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the area trigger DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to MapDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadWorldMapAreaRecord
    // Purpose: Retrieves read world map area record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the world map area DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to MapDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadWorldMapContinentRecord
    // Purpose: Retrieves read world map continent record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the world map continent DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to MapDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadWorldMapOverlayRecord
    // Purpose: Retrieves read world map overlay record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the world map overlay DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to MapDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: GroupByMapId
    // Purpose: Executes the group by map ID operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - records: Records value supplied by the caller for this operation.
    // Returns: Returns the I read only dictionary> value produced by this operation.
    // Notes: This keeps the operation scoped to MapDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static IReadOnlyDictionary<int, IReadOnlyList<AreaTableDbcRecord>> GroupByMapId(IEnumerable<AreaTableDbcRecord> records)
    {
        return records
            .GroupBy(record => record.MapId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<AreaTableDbcRecord>)group.OrderBy(record => record.Id).ToArray());
    }

    // Method: GroupByMapId
    // Purpose: Executes the group by map ID operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - records: Records value supplied by the caller for this operation.
    // Returns: Returns the I read only dictionary> value produced by this operation.
    // Notes: This keeps the operation scoped to MapDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static IReadOnlyDictionary<int, IReadOnlyList<AreaTriggerDbcRecord>> GroupByMapId(IEnumerable<AreaTriggerDbcRecord> records)
    {
        return records
            .GroupBy(record => record.MapId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<AreaTriggerDbcRecord>)group.OrderBy(record => record.Id).ToArray());
    }

    // Method: GroupByWorldMapContinentId
    // Purpose: Executes the group by world map continent ID operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - records: Records value supplied by the caller for this operation.
    // Returns: Returns the I read only dictionary> value produced by this operation.
    // Notes: This keeps the operation scoped to MapDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static IReadOnlyDictionary<int, IReadOnlyList<WorldMapAreaDbcRecord>> GroupByWorldMapContinentId(IEnumerable<WorldMapAreaDbcRecord> records)
    {
        return records
            .GroupBy(record => record.WorldMapContinentId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<WorldMapAreaDbcRecord>)group.OrderBy(record => record.Id).ToArray());
    }

    // Method: GroupByWorldMapAreaId
    // Purpose: Executes the group by world map area ID operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - records: Records value supplied by the caller for this operation.
    // Returns: Returns the I read only dictionary> value produced by this operation.
    // Notes: This keeps the operation scoped to MapDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static IReadOnlyDictionary<int, IReadOnlyList<WorldMapOverlayDbcRecord>> GroupByWorldMapAreaId(IEnumerable<WorldMapOverlayDbcRecord> records)
    {
        return records
            .GroupBy(record => record.WorldMapAreaId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<WorldMapOverlayDbcRecord>)group.OrderBy(record => record.Id).ToArray());
    }

    // Method: ReadInt32
    // Purpose: Retrieves read int32 data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // - fieldIndex: Field index value supplied by the caller for this operation.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to MapDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static int ReadInt32(DbcRecord record, int fieldIndex)
    {
        return record.GetInt32(fieldIndex);
    }

    // Method: ReadSingle
    // Purpose: Retrieves read single data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // - fieldIndex: Field index value supplied by the caller for this operation.
    // Returns: Returns the float value produced by this operation.
    // Notes: This keeps the operation scoped to MapDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static float ReadSingle(DbcRecord record, int fieldIndex)
    {
        return record.GetSingle(fieldIndex);
    }

    // Method: ReadString
    // Purpose: Retrieves read string data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // - fieldIndex: Field index value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to MapDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static string ReadString(DbcRecord record, int fieldIndex)
    {
        return record.GetString(fieldIndex).Trim('\0', ' ', '\t', '\r', '\n');
    }
}
