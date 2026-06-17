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
// File: src/EmulationServer.Game/WorldData/WorldTemplateDataStore.cs
// Purpose: Contains world template data store code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Game.Players;

using EmulationServer.Game.Formulas;

namespace EmulationServer.Game.WorldData;

// Type: WorldTemplateDataStore
// Purpose: Provides world template data store behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class WorldTemplateDataStore
{

    public static WorldTemplateDataStore Empty { get; } = new(
        Array.Empty<PlayerCreateInfoRecord>(),
        Array.Empty<ItemTemplateRecord>(),
        Array.Empty<PlayerLevelStatsRecord>(),
        Array.Empty<PlayerClassLevelStatsRecord>(),
        Array.Empty<PlayerLevelExperienceRecord>(),
        Array.Empty<PlayerCreateActionRecord>(),
        Array.Empty<PlayerCreateItemRecord>(),
        Array.Empty<PlayerCreateSpellRecord>(),
        Array.Empty<GameObjectTemplateRecord>(),
        Array.Empty<GameObjectSpawnRecord>(),
        Array.Empty<CreatureTemplateRecord>(),
        Array.Empty<CreatureSpawnRecord>());

    // Constructor: Dictionary
    // Purpose: Executes the dictionary operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - Race: Race value supplied by the caller for this operation.
    // - Class: Class value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private readonly Dictionary<(byte Race, byte Class), PlayerCreateInfoRecord> _playerCreateInfo;
    // Field: Stores the uint state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current uint backing value maintained by the owning type.
    private readonly Dictionary<uint, ItemTemplateRecord> _itemTemplates;
    // Constructor: Dictionary
    // Purpose: Executes the dictionary operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - Race: Race value supplied by the caller for this operation.
    // - Class: Class value supplied by the caller for this operation.
    // - Level: Level value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private readonly Dictionary<(byte Race, byte Class, byte Level), PlayerLevelStatsRecord> _playerLevelStats;
    // Constructor: Dictionary
    // Purpose: Executes the dictionary operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - Class: Class value supplied by the caller for this operation.
    // - Level: Level value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private readonly Dictionary<(byte Class, byte Level), PlayerClassLevelStatsRecord> _playerClassLevelStats;
    // Field: Stores the byte state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current byte backing value maintained by the owning type.
    private readonly Dictionary<byte, PlayerLevelExperienceRecord> _playerLevelExperience;
    // Constructor: Dictionary
    // Purpose: Executes the dictionary operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - Race: Race value supplied by the caller for this operation.
    // - Class: Class value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private readonly Dictionary<(byte Race, byte Class), IReadOnlyList<PlayerCreateActionRecord>> _playerCreateActions;
    // Constructor: Dictionary
    // Purpose: Executes the dictionary operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - Race: Race value supplied by the caller for this operation.
    // - Class: Class value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private readonly Dictionary<(byte Race, byte Class), IReadOnlyList<PlayerCreateItemRecord>> _playerCreateItems;
    // Constructor: Dictionary
    // Purpose: Executes the dictionary operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - Race: Race value supplied by the caller for this operation.
    // - Class: Class value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private readonly Dictionary<(byte Race, byte Class), IReadOnlyList<PlayerCreateSpellRecord>> _playerCreateSpells;
    // Field: Stores the uint state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current uint backing value maintained by the owning type.
    private readonly Dictionary<uint, GameObjectTemplateRecord> _gameObjectTemplates;
    // Field: Stores the uint state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current uint backing value maintained by the owning type.
    private readonly Dictionary<uint, GameObjectSpawnRecord> _gameObjectSpawns;
    // Field: Stores the ushort state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current ushort backing value maintained by the owning type.
    private readonly Dictionary<ushort, IReadOnlyList<GameObjectSpawnRecord>> _gameObjectSpawnsByMap;
    // Constructor: Dictionary
    // Purpose: Executes the dictionary operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - Map: Map value supplied by the caller for this operation.
    // - ZoneId: Zone ID identifier used to select the exact record, object, or runtime owner.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private readonly Dictionary<(ushort Map, uint ZoneId), IReadOnlyList<GameObjectSpawnRecord>> _gameObjectSpawnsByZone;
    // Constructor: Dictionary
    // Purpose: Executes the dictionary operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - Map: Map value supplied by the caller for this operation.
    // - AreaId: Area ID identifier used to select the exact record, object, or runtime owner.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private readonly Dictionary<(ushort Map, uint AreaId), IReadOnlyList<GameObjectSpawnRecord>> _gameObjectSpawnsByArea;
    // Field: Stores the uint state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current uint backing value maintained by the owning type.
    private readonly Dictionary<uint, CreatureTemplateRecord> _creatureTemplates;
    // Field: Stores the uint state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current uint backing value maintained by the owning type.
    private readonly Dictionary<uint, CreatureSpawnRecord> _creatureSpawns;
    // Field: Stores the ushort state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current ushort backing value maintained by the owning type.
    private readonly Dictionary<ushort, IReadOnlyList<CreatureSpawnRecord>> _creatureSpawnsByMap;
    // Constructor: Dictionary
    // Purpose: Executes the dictionary operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - Map: Map value supplied by the caller for this operation.
    // - ZoneId: Zone ID identifier used to select the exact record, object, or runtime owner.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private readonly Dictionary<(ushort Map, uint ZoneId), IReadOnlyList<CreatureSpawnRecord>> _creatureSpawnsByZone;
    // Constructor: Dictionary
    // Purpose: Executes the dictionary operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - Map: Map value supplied by the caller for this operation.
    // - AreaId: Area ID identifier used to select the exact record, object, or runtime owner.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private readonly Dictionary<(ushort Map, uint AreaId), IReadOnlyList<CreatureSpawnRecord>> _creatureSpawnsByArea;

    // Constructor: WorldTemplateDataStore
    // Purpose: Initializes a new WorldTemplateDataStore instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - playerCreateInfo: Player create info value supplied by the caller for this operation.
    // - itemTemplates: Item templates value supplied by the caller for this operation.
    // - playerLevelStats: Player level stats value supplied by the caller for this operation.
    // - playerClassLevelStats: Player class level stats value supplied by the caller for this operation.
    // - playerLevelExperience: Player level experience value supplied by the caller for this operation.
    // - playerCreateActions: Player create actions value supplied by the caller for this operation.
    // - playerCreateItems: Player create items value supplied by the caller for this operation.
    // - playerCreateSpells: Player create spells value supplied by the caller for this operation.
    // - gameObjectTemplates: Game object templates value supplied by the caller for this operation.
    // - gameObjectSpawns: Game object spawns value supplied by the caller for this operation.
    // - creatureTemplates: Creature templates value supplied by the caller for this operation.
    // - creatureSpawns: Creature spawns value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public WorldTemplateDataStore(
        IEnumerable<PlayerCreateInfoRecord> playerCreateInfo,
        IEnumerable<ItemTemplateRecord> itemTemplates,
        IEnumerable<PlayerLevelStatsRecord> playerLevelStats,
        IEnumerable<PlayerClassLevelStatsRecord> playerClassLevelStats,
        IEnumerable<PlayerLevelExperienceRecord> playerLevelExperience,
        IEnumerable<PlayerCreateActionRecord> playerCreateActions,
        IEnumerable<PlayerCreateItemRecord> playerCreateItems,
        IEnumerable<PlayerCreateSpellRecord> playerCreateSpells,
        IEnumerable<GameObjectTemplateRecord> gameObjectTemplates,
        IEnumerable<GameObjectSpawnRecord> gameObjectSpawns,
        IEnumerable<CreatureTemplateRecord>? creatureTemplates = null,
        IEnumerable<CreatureSpawnRecord>? creatureSpawns = null)
    {
        ArgumentNullException.ThrowIfNull(playerCreateInfo);
        ArgumentNullException.ThrowIfNull(itemTemplates);
        ArgumentNullException.ThrowIfNull(playerLevelStats);
        ArgumentNullException.ThrowIfNull(playerClassLevelStats);
        ArgumentNullException.ThrowIfNull(playerLevelExperience);
        ArgumentNullException.ThrowIfNull(playerCreateActions);
        ArgumentNullException.ThrowIfNull(playerCreateItems);
        ArgumentNullException.ThrowIfNull(playerCreateSpells);
        ArgumentNullException.ThrowIfNull(gameObjectTemplates);
        ArgumentNullException.ThrowIfNull(gameObjectSpawns);
        creatureTemplates ??= Array.Empty<CreatureTemplateRecord>();
        creatureSpawns ??= Array.Empty<CreatureSpawnRecord>();

        _playerCreateInfo = playerCreateInfo
            .GroupBy(record => (record.Race, record.Class))
            .ToDictionary(group => group.Key, group => group.First());

        _itemTemplates = itemTemplates
            .GroupBy(record => record.Entry)
            .ToDictionary(group => group.Key, group => group.First());

        _playerLevelStats = playerLevelStats
            .GroupBy(record => (record.Race, record.Class, record.Level))
            .ToDictionary(group => group.Key, group => group.First());

        _playerClassLevelStats = playerClassLevelStats
            .GroupBy(record => (record.Class, record.Level))
            .ToDictionary(group => group.Key, group => group.First());

        _playerLevelExperience = playerLevelExperience
            .GroupBy(record => record.Level)
            .ToDictionary(group => group.Key, group => group.First());

        _playerCreateActions = playerCreateActions
            .GroupBy(record => (record.Race, record.Class))
            .ToDictionary(group => group.Key, group => (IReadOnlyList<PlayerCreateActionRecord>)group.OrderBy(record => record.Button).ToArray());

        _playerCreateItems = playerCreateItems
            .GroupBy(record => (record.Race, record.Class))
            .ToDictionary(group => group.Key, group => (IReadOnlyList<PlayerCreateItemRecord>)group.ToArray());

        _playerCreateSpells = playerCreateSpells
            .GroupBy(record => (record.Race, record.Class))
            .ToDictionary(group => group.Key, group => (IReadOnlyList<PlayerCreateSpellRecord>)group.OrderBy(record => record.SpellId).ToArray());

        _gameObjectTemplates = gameObjectTemplates
            .Where(GameObjectDataValidation.IsLoadableTemplate)
            .GroupBy(record => record.Entry)
            .ToDictionary(group => group.Key, group => group.First());

        HashSet<uint> loadableGameObjectTemplateEntries = _gameObjectTemplates.Keys.ToHashSet();

        _gameObjectSpawns = gameObjectSpawns
            .Where(GameObjectDataValidation.IsLoadableSpawn)
            .Where(record => loadableGameObjectTemplateEntries.Contains(record.Entry))
            .GroupBy(record => record.Guid)
            .ToDictionary(group => group.Key, group => group.First());

        _gameObjectSpawnsByMap = _gameObjectSpawns.Values
            .GroupBy(record => record.Map)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<GameObjectSpawnRecord>)group.OrderBy(record => record.Guid).ToArray());

        _gameObjectSpawnsByZone = _gameObjectSpawns.Values
            .Where(record => record.ZoneId != 0)
            .GroupBy(record => (record.Map, record.ZoneId))
            .ToDictionary(group => group.Key, group => (IReadOnlyList<GameObjectSpawnRecord>)group.OrderBy(record => record.Guid).ToArray());

        _gameObjectSpawnsByArea = _gameObjectSpawns.Values
            .Where(record => record.AreaId != 0)
            .GroupBy(record => (record.Map, record.AreaId))
            .ToDictionary(group => group.Key, group => (IReadOnlyList<GameObjectSpawnRecord>)group.OrderBy(record => record.Guid).ToArray());

        _creatureTemplates = creatureTemplates
            .Where(CreatureDataValidation.IsLoadableTemplate)
            .GroupBy(record => record.Entry)
            .ToDictionary(group => group.Key, group => group.First());

        HashSet<uint> loadableCreatureTemplateEntries = _creatureTemplates.Keys.ToHashSet();

        _creatureSpawns = creatureSpawns
            .Where(CreatureDataValidation.IsLoadableSpawn)
            .Where(record => loadableCreatureTemplateEntries.Contains(record.Entry))
            .GroupBy(record => record.Guid)
            .ToDictionary(group => group.Key, group => group.First());

        _creatureSpawnsByMap = _creatureSpawns.Values
            .GroupBy(record => record.Map)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<CreatureSpawnRecord>)group.OrderBy(record => record.Guid).ToArray());

        _creatureSpawnsByZone = _creatureSpawns.Values
            .Where(record => record.ZoneId != 0)
            .GroupBy(record => (record.Map, record.ZoneId))
            .ToDictionary(group => group.Key, group => (IReadOnlyList<CreatureSpawnRecord>)group.OrderBy(record => record.Guid).ToArray());

        _creatureSpawnsByArea = _creatureSpawns.Values
            .Where(record => record.AreaId != 0)
            .GroupBy(record => (record.Map, record.AreaId))
            .ToDictionary(group => group.Key, group => (IReadOnlyList<CreatureSpawnRecord>)group.OrderBy(record => record.Guid).ToArray());
    }

    // Constructor: IReadOnlyDictionary
    // Purpose: Executes the I read only dictionary operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - Race: Race value supplied by the caller for this operation.
    // - Class: Class value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyDictionary<(byte Race, byte Class), PlayerCreateInfoRecord> PlayerCreateInfo => _playerCreateInfo;

    // Property: Gets or sets the uint value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: uint value exposed by the owning type.
    public IReadOnlyDictionary<uint, ItemTemplateRecord> ItemTemplates => _itemTemplates;

    // Constructor: IReadOnlyDictionary
    // Purpose: Executes the I read only dictionary operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - Race: Race value supplied by the caller for this operation.
    // - Class: Class value supplied by the caller for this operation.
    // - Level: Level value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyDictionary<(byte Race, byte Class, byte Level), PlayerLevelStatsRecord> PlayerLevelStats => _playerLevelStats;

    // Constructor: IReadOnlyDictionary
    // Purpose: Executes the I read only dictionary operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - Class: Class value supplied by the caller for this operation.
    // - Level: Level value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyDictionary<(byte Class, byte Level), PlayerClassLevelStatsRecord> PlayerClassLevelStats => _playerClassLevelStats;

    // Property: Gets or sets the byte value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: byte value exposed by the owning type.
    public IReadOnlyDictionary<byte, PlayerLevelExperienceRecord> PlayerLevelExperience => _playerLevelExperience;

    // Constructor: IReadOnlyDictionary
    // Purpose: Executes the I read only dictionary operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - Race: Race value supplied by the caller for this operation.
    // - Class: Class value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyDictionary<(byte Race, byte Class), IReadOnlyList<PlayerCreateActionRecord>> PlayerCreateActions => _playerCreateActions;

    // Constructor: IReadOnlyDictionary
    // Purpose: Executes the I read only dictionary operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - Race: Race value supplied by the caller for this operation.
    // - Class: Class value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyDictionary<(byte Race, byte Class), IReadOnlyList<PlayerCreateItemRecord>> PlayerCreateItems => _playerCreateItems;

    // Constructor: IReadOnlyDictionary
    // Purpose: Executes the I read only dictionary operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - Race: Race value supplied by the caller for this operation.
    // - Class: Class value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyDictionary<(byte Race, byte Class), IReadOnlyList<PlayerCreateSpellRecord>> PlayerCreateSpells => _playerCreateSpells;

    // Property: Gets or sets the uint value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: uint value exposed by the owning type.
    public IReadOnlyDictionary<uint, GameObjectTemplateRecord> GameObjectTemplates => _gameObjectTemplates;

    // Property: Gets or sets the uint value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: uint value exposed by the owning type.
    public IReadOnlyDictionary<uint, GameObjectSpawnRecord> GameObjectSpawns => _gameObjectSpawns;

    // Property: Gets or sets the ushort value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: ushort value exposed by the owning type.
    public IReadOnlyDictionary<ushort, IReadOnlyList<GameObjectSpawnRecord>> GameObjectSpawnsByMap => _gameObjectSpawnsByMap;

    // Property: Gets or sets the uint value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: uint value exposed by the owning type.
    public IReadOnlyDictionary<uint, CreatureTemplateRecord> CreatureTemplates => _creatureTemplates;

    // Property: Gets or sets the uint value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: uint value exposed by the owning type.
    public IReadOnlyDictionary<uint, CreatureSpawnRecord> CreatureSpawns => _creatureSpawns;

    // Property: Gets or sets the ushort value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: ushort value exposed by the owning type.
    public IReadOnlyDictionary<ushort, IReadOnlyList<CreatureSpawnRecord>> CreatureSpawnsByMap => _creatureSpawnsByMap;

    // Property: Gets or sets the player level stats count value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: player level stats count value exposed by the owning type.
    public int PlayerLevelStatsCount => _playerLevelStats.Count;

    // Property: Gets or sets the player class level stats count value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: player class level stats count value exposed by the owning type.
    public int PlayerClassLevelStatsCount => _playerClassLevelStats.Count;

    // Property: Gets or sets the player level experience count value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: player level experience count value exposed by the owning type.
    public int PlayerLevelExperienceCount => _playerLevelExperience.Count;

    // Method: Sum
    // Purpose: Executes the sum operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - records: Records value supplied by the caller for this operation.
    // Returns: Returns the int player create action count => player create actions.values. value produced by this operation.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public int PlayerCreateActionCount => _playerCreateActions.Values.Sum(records => records.Count);

    // Method: Sum
    // Purpose: Executes the sum operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - records: Records value supplied by the caller for this operation.
    // Returns: Returns the int player create item count => player create items.values. value produced by this operation.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public int PlayerCreateItemCount => _playerCreateItems.Values.Sum(records => records.Count);

    // Method: Sum
    // Purpose: Executes the sum operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - records: Records value supplied by the caller for this operation.
    // Returns: Returns the int player create spell count => player create spells.values. value produced by this operation.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public int PlayerCreateSpellCount => _playerCreateSpells.Values.Sum(records => records.Count);

    // Property: Gets or sets the game object template count value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: game object template count value exposed by the owning type.
    public int GameObjectTemplateCount => _gameObjectTemplates.Count;

    // Property: Gets or sets the game object spawn count value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: game object spawn count value exposed by the owning type.
    public int GameObjectSpawnCount => _gameObjectSpawns.Count;

    // Property: Gets or sets the game object spawn map count value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: game object spawn map count value exposed by the owning type.
    public int GameObjectSpawnMapCount => _gameObjectSpawnsByMap.Count;

    // Property: Gets or sets the game object spawn zone count value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: game object spawn zone count value exposed by the owning type.
    public int GameObjectSpawnZoneCount => _gameObjectSpawnsByZone.Count;

    // Property: Gets or sets the game object spawn area count value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: game object spawn area count value exposed by the owning type.
    public int GameObjectSpawnAreaCount => _gameObjectSpawnsByArea.Count;

    // Property: Gets or sets the creature template count value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: creature template count value exposed by the owning type.
    public int CreatureTemplateCount => _creatureTemplates.Count;

    // Property: Gets or sets the creature spawn count value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: creature spawn count value exposed by the owning type.
    public int CreatureSpawnCount => _creatureSpawns.Count;

    // Property: Gets or sets the creature spawn map count value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: creature spawn map count value exposed by the owning type.
    public int CreatureSpawnMapCount => _creatureSpawnsByMap.Count;

    // Property: Gets or sets the creature spawn zone count value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: creature spawn zone count value exposed by the owning type.
    public int CreatureSpawnZoneCount => _creatureSpawnsByZone.Count;

    // Property: Gets or sets the creature spawn area count value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: creature spawn area count value exposed by the owning type.
    public int CreatureSpawnAreaCount => _creatureSpawnsByArea.Count;

    // Method: TryGetPlayerCreateInfo
    // Purpose: Attempts to retrieve or parse try get player create info data without treating normal misses as failures.
    // Parameters:
    // - race: Race value supplied by the caller for this operation.
    // - characterClass: Character class value supplied by the caller for this operation.
    // - createInfo: Create info value supplied by the caller for this operation.
    // Returns: Returns true when try get player create info succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetPlayerCreateInfo(byte race, byte characterClass, out PlayerCreateInfoRecord createInfo)
    {
        return _playerCreateInfo.TryGetValue((race, characterClass), out createInfo!);
    }

    // Method: TryGetItemTemplate
    // Purpose: Attempts to retrieve or parse try get item template data without treating normal misses as failures.
    // Parameters:
    // - entry: Entry value supplied by the caller for this operation.
    // - itemTemplate: Item template value supplied by the caller for this operation.
    // Returns: Returns true when try get item template succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetItemTemplate(uint entry, out ItemTemplateRecord itemTemplate)
    {
        return _itemTemplates.TryGetValue(entry, out itemTemplate!);
    }

    // Method: TryGetGameObjectTemplate
    // Purpose: Attempts to retrieve or parse try get game object template data without treating normal misses as failures.
    // Parameters:
    // - entry: Entry value supplied by the caller for this operation.
    // - gameObjectTemplate: Game object template value supplied by the caller for this operation.
    // Returns: Returns true when try get game object template succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetGameObjectTemplate(uint entry, out GameObjectTemplateRecord gameObjectTemplate)
    {
        return _gameObjectTemplates.TryGetValue(entry, out gameObjectTemplate!);
    }

    // Method: GetGameObjectTemplateOrDefault
    // Purpose: Retrieves get game object template or default data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - entry: Entry value supplied by the caller for this operation.
    // Returns: Returns the game object template record? value produced by this operation.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public GameObjectTemplateRecord? GetGameObjectTemplateOrDefault(uint entry)
    {
        return _gameObjectTemplates.TryGetValue(entry, out GameObjectTemplateRecord? template)
            ? template
            : null;
    }

    // Method: TryGetGameObjectSpawn
    // Purpose: Attempts to retrieve or parse try get game object spawn data without treating normal misses as failures.
    // Parameters:
    // - guid: Guid identifier used to select the exact record, object, or runtime owner.
    // - gameObjectSpawn: Game object spawn value supplied by the caller for this operation.
    // Returns: Returns true when try get game object spawn succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetGameObjectSpawn(uint guid, out GameObjectSpawnRecord gameObjectSpawn)
    {
        return _gameObjectSpawns.TryGetValue(guid, out gameObjectSpawn!);
    }

    // Method: TryGetCreatureTemplate
    // Purpose: Attempts to retrieve or parse try get creature template data without treating normal misses as failures.
    // Parameters:
    // - entry: Entry value supplied by the caller for this operation.
    // - creatureTemplate: Creature template value supplied by the caller for this operation.
    // Returns: Returns true when try get creature template succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetCreatureTemplate(uint entry, out CreatureTemplateRecord creatureTemplate)
    {
        return _creatureTemplates.TryGetValue(entry, out creatureTemplate!);
    }

    // Method: GetCreatureTemplateOrDefault
    // Purpose: Retrieves get creature template or default data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - entry: Entry value supplied by the caller for this operation.
    // Returns: Returns the creature template record? value produced by this operation.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public CreatureTemplateRecord? GetCreatureTemplateOrDefault(uint entry)
    {
        return _creatureTemplates.TryGetValue(entry, out CreatureTemplateRecord? template)
            ? template
            : null;
    }

    // Method: TryGetCreatureSpawn
    // Purpose: Attempts to retrieve or parse try get creature spawn data without treating normal misses as failures.
    // Parameters:
    // - guid: Guid identifier used to select the exact record, object, or runtime owner.
    // - creatureSpawn: Creature spawn value supplied by the caller for this operation.
    // Returns: Returns true when try get creature spawn succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetCreatureSpawn(uint guid, out CreatureSpawnRecord creatureSpawn)
    {
        return _creatureSpawns.TryGetValue(guid, out creatureSpawn!);
    }

    // Method: GetGameObjectSpawnsForMap
    // Purpose: Retrieves get game object spawns for map data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyList<GameObjectSpawnRecord> GetGameObjectSpawnsForMap(ushort mapId)
    {
        return _gameObjectSpawnsByMap.TryGetValue(mapId, out IReadOnlyList<GameObjectSpawnRecord>? records)
            ? records
            : Array.Empty<GameObjectSpawnRecord>();
    }

    // Method: GetGameObjectSpawnsForZone
    // Purpose: Retrieves get game object spawns for zone data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - zoneId: Zone ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyList<GameObjectSpawnRecord> GetGameObjectSpawnsForZone(ushort mapId, uint zoneId)
    {
        return _gameObjectSpawnsByZone.TryGetValue((mapId, zoneId), out IReadOnlyList<GameObjectSpawnRecord>? records)
            ? records
            : Array.Empty<GameObjectSpawnRecord>();
    }

    // Method: GetGameObjectSpawnsForArea
    // Purpose: Retrieves get game object spawns for area data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - areaId: Area ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyList<GameObjectSpawnRecord> GetGameObjectSpawnsForArea(ushort mapId, uint areaId)
    {
        return _gameObjectSpawnsByArea.TryGetValue((mapId, areaId), out IReadOnlyList<GameObjectSpawnRecord>? records)
            ? records
            : Array.Empty<GameObjectSpawnRecord>();
    }

    // Method: GetCreatureSpawnsForMap
    // Purpose: Retrieves get creature spawns for map data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyList<CreatureSpawnRecord> GetCreatureSpawnsForMap(ushort mapId)
    {
        return _creatureSpawnsByMap.TryGetValue(mapId, out IReadOnlyList<CreatureSpawnRecord>? records)
            ? records
            : Array.Empty<CreatureSpawnRecord>();
    }

    // Method: GetCreatureSpawnsForZone
    // Purpose: Retrieves get creature spawns for zone data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - zoneId: Zone ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyList<CreatureSpawnRecord> GetCreatureSpawnsForZone(ushort mapId, uint zoneId)
    {
        return _creatureSpawnsByZone.TryGetValue((mapId, zoneId), out IReadOnlyList<CreatureSpawnRecord>? records)
            ? records
            : Array.Empty<CreatureSpawnRecord>();
    }

    // Method: GetCreatureSpawnsForArea
    // Purpose: Retrieves get creature spawns for area data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - areaId: Area ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyList<CreatureSpawnRecord> GetCreatureSpawnsForArea(ushort mapId, uint areaId)
    {
        return _creatureSpawnsByArea.TryGetValue((mapId, areaId), out IReadOnlyList<CreatureSpawnRecord>? records)
            ? records
            : Array.Empty<CreatureSpawnRecord>();
    }

    // Method: TryGetPlayerLevelStats
    // Purpose: Attempts to retrieve or parse try get player level stats data without treating normal misses as failures.
    // Parameters:
    // - race: Race value supplied by the caller for this operation.
    // - characterClass: Character class value supplied by the caller for this operation.
    // - level: Level value supplied by the caller for this operation.
    // - levelStats: Level stats value supplied by the caller for this operation.
    // Returns: Returns true when try get player level stats succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetPlayerLevelStats(byte race, byte characterClass, byte level, out PlayerLevelStatsRecord levelStats)
    {
        return _playerLevelStats.TryGetValue((race, characterClass, level), out levelStats!);
    }

    // Method: TryGetPlayerClassLevelStats
    // Purpose: Attempts to retrieve or parse try get player class level stats data without treating normal misses as failures.
    // Parameters:
    // - characterClass: Character class value supplied by the caller for this operation.
    // - level: Level value supplied by the caller for this operation.
    // - classLevelStats: Class level stats value supplied by the caller for this operation.
    // Returns: Returns true when try get player class level stats succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetPlayerClassLevelStats(byte characterClass, byte level, out PlayerClassLevelStatsRecord classLevelStats)
    {
        return _playerClassLevelStats.TryGetValue((characterClass, level), out classLevelStats!);
    }

    // Method: GetPlayerCreateActions
    // Purpose: Retrieves get player create actions data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - race: Race value supplied by the caller for this operation.
    // - characterClass: Character class value supplied by the caller for this operation.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyList<PlayerCreateActionRecord> GetPlayerCreateActions(byte race, byte characterClass)
    {
        return _playerCreateActions.TryGetValue((race, characterClass), out IReadOnlyList<PlayerCreateActionRecord>? records)
            ? records
            : Array.Empty<PlayerCreateActionRecord>();
    }

    // Method: GetPlayerCreateItems
    // Purpose: Retrieves get player create items data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - race: Race value supplied by the caller for this operation.
    // - characterClass: Character class value supplied by the caller for this operation.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyList<PlayerCreateItemRecord> GetPlayerCreateItems(byte race, byte characterClass)
    {
        return _playerCreateItems.TryGetValue((race, characterClass), out IReadOnlyList<PlayerCreateItemRecord>? records)
            ? records
            : Array.Empty<PlayerCreateItemRecord>();
    }

    // Method: GetPlayerCreateSpells
    // Purpose: Retrieves get player create spells data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - race: Race value supplied by the caller for this operation.
    // - characterClass: Character class value supplied by the caller for this operation.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyList<PlayerCreateSpellRecord> GetPlayerCreateSpells(byte race, byte characterClass)
    {
        return _playerCreateSpells.TryGetValue((race, characterClass), out IReadOnlyList<PlayerCreateSpellRecord>? records)
            ? records
            : Array.Empty<PlayerCreateSpellRecord>();
    }

    // Method: GetNextLevelExperience
    // Purpose: Retrieves get next level experience data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - level: Level value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public uint GetNextLevelExperience(byte level)
    {
        byte safeLevel = level == 0 ? (byte)1 : level;
        if (_playerLevelExperience.TryGetValue(safeLevel, out PlayerLevelExperienceRecord? record) && record.ExperienceForNextLevel != 0)
        {
            return record.ExperienceForNextLevel;
        }

        return ExperienceFormula.GetFallbackNextLevelExperience(safeLevel);
    }

    // Method: BuildBasePlayerStats
    // Purpose: Builds or writes build base player stats output for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - race: Race value supplied by the caller for this operation.
    // - characterClass: Character class value supplied by the caller for this operation.
    // - level: Level value supplied by the caller for this operation.
    // Returns: Returns the player stats value produced by this operation.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public PlayerStats BuildBasePlayerStats(byte race, byte characterClass, byte level)
    {
        byte safeLevel = level == 0 ? (byte)1 : level;
        uint health = 80 + ((uint)safeLevel * 20u);
        uint mana = characterClass is 1 or 4 ? 0u : 100 + ((uint)safeLevel * 30u);
        uint rage = characterClass == 1 ? 1000u : 0u;
        uint energy = characterClass == 4 ? 100u : 0u;
        (uint strength, uint agility, uint stamina, uint intellect, uint spirit) = ResolveFallbackAttributes(characterClass, safeLevel);

        if (TryGetPlayerLevelStats(race, characterClass, safeLevel, out PlayerLevelStatsRecord levelStats))
        {
            strength = levelStats.Strength;
            agility = levelStats.Agility;
            stamina = levelStats.Stamina;
            intellect = levelStats.Intellect;
            spirit = levelStats.Spirit;
        }

        if (TryGetPlayerClassLevelStats(characterClass, safeLevel, out PlayerClassLevelStatsRecord classLevelStats))
        {
            health = classLevelStats.BaseHealth == 0 ? health : classLevelStats.BaseHealth;
            mana = classLevelStats.BaseMana;
        }

        uint armor = Math.Max(1u, agility * 2u);
        return new PlayerStats(health, mana, rage, 0, energy, 0, strength, agility, stamina, intellect, spirit, armor);
    }

    // Method: GetItemTemplates
    // Purpose: Retrieves get item templates data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - itemEntries: Item entries value supplied by the caller for this operation.
    // Returns: Returns the I read only dictionary value produced by this operation.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyDictionary<uint, ItemTemplateRecord> GetItemTemplates(IEnumerable<uint> itemEntries)
    {
        ArgumentNullException.ThrowIfNull(itemEntries);

        Dictionary<uint, ItemTemplateRecord> result = [];
        foreach (uint entry in itemEntries)
        {
            if (entry == 0 || result.ContainsKey(entry))
            {
                continue;
            }

            if (_itemTemplates.TryGetValue(entry, out ItemTemplateRecord? template))
            {
                result[entry] = template;
            }
        }

        return result;
    }

    // Method: WithGameObjectSpawns
    // Purpose: Executes the with game object spawns operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - gameObjectSpawns: Game object spawns value supplied by the caller for this operation.
    // Returns: Returns the world template data store value produced by this operation.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public WorldTemplateDataStore WithGameObjectSpawns(IEnumerable<GameObjectSpawnRecord> gameObjectSpawns)
    {
        ArgumentNullException.ThrowIfNull(gameObjectSpawns);

        return new WorldTemplateDataStore(
            _playerCreateInfo.Values,
            _itemTemplates.Values,
            _playerLevelStats.Values,
            _playerClassLevelStats.Values,
            _playerLevelExperience.Values,
            _playerCreateActions.Values.SelectMany(records => records),
            _playerCreateItems.Values.SelectMany(records => records),
            _playerCreateSpells.Values.SelectMany(records => records),
            _gameObjectTemplates.Values,
            gameObjectSpawns,
            _creatureTemplates.Values,
            _creatureSpawns.Values);
    }

    // Method: WithGameObjectDataForMap
    // Purpose: Executes the with game object data for map operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - gameObjectTemplates: Game object templates value supplied by the caller for this operation.
    // - mapGameObjectSpawns: Map game object spawns value supplied by the caller for this operation.
    // Returns: Returns the world template data store value produced by this operation.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public WorldTemplateDataStore WithGameObjectDataForMap(
        ushort mapId,
        IEnumerable<GameObjectTemplateRecord> gameObjectTemplates,
        IEnumerable<GameObjectSpawnRecord> mapGameObjectSpawns)
    {
        ArgumentNullException.ThrowIfNull(gameObjectTemplates);
        ArgumentNullException.ThrowIfNull(mapGameObjectSpawns);

        IEnumerable<GameObjectSpawnRecord> mergedSpawns = _gameObjectSpawns.Values
            .Where(spawn => spawn.Map != mapId)
            .Concat(mapGameObjectSpawns);

        return new WorldTemplateDataStore(
            _playerCreateInfo.Values,
            _itemTemplates.Values,
            _playerLevelStats.Values,
            _playerClassLevelStats.Values,
            _playerLevelExperience.Values,
            _playerCreateActions.Values.SelectMany(records => records),
            _playerCreateItems.Values.SelectMany(records => records),
            _playerCreateSpells.Values.SelectMany(records => records),
            gameObjectTemplates,
            mergedSpawns,
            _creatureTemplates.Values,
            _creatureSpawns.Values);
    }

    // Method: WithCreatureSpawns
    // Purpose: Executes the with creature spawns operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - creatureSpawns: Creature spawns value supplied by the caller for this operation.
    // Returns: Returns the world template data store value produced by this operation.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public WorldTemplateDataStore WithCreatureSpawns(IEnumerable<CreatureSpawnRecord> creatureSpawns)
    {
        ArgumentNullException.ThrowIfNull(creatureSpawns);

        return new WorldTemplateDataStore(
            _playerCreateInfo.Values,
            _itemTemplates.Values,
            _playerLevelStats.Values,
            _playerClassLevelStats.Values,
            _playerLevelExperience.Values,
            _playerCreateActions.Values.SelectMany(records => records),
            _playerCreateItems.Values.SelectMany(records => records),
            _playerCreateSpells.Values.SelectMany(records => records),
            _gameObjectTemplates.Values,
            _gameObjectSpawns.Values,
            _creatureTemplates.Values,
            creatureSpawns);
    }

    // Method: WithCreatureDataForMap
    // Purpose: Executes the with creature data for map operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - creatureTemplates: Creature templates value supplied by the caller for this operation.
    // - mapCreatureSpawns: Map creature spawns value supplied by the caller for this operation.
    // Returns: Returns the world template data store value produced by this operation.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public WorldTemplateDataStore WithCreatureDataForMap(
        ushort mapId,
        IEnumerable<CreatureTemplateRecord> creatureTemplates,
        IEnumerable<CreatureSpawnRecord> mapCreatureSpawns)
    {
        ArgumentNullException.ThrowIfNull(creatureTemplates);
        ArgumentNullException.ThrowIfNull(mapCreatureSpawns);

        IEnumerable<CreatureSpawnRecord> mergedSpawns = _creatureSpawns.Values
            .Where(spawn => spawn.Map != mapId)
            .Concat(mapCreatureSpawns);

        return new WorldTemplateDataStore(
            _playerCreateInfo.Values,
            _itemTemplates.Values,
            _playerLevelStats.Values,
            _playerClassLevelStats.Values,
            _playerLevelExperience.Values,
            _playerCreateActions.Values.SelectMany(records => records),
            _playerCreateItems.Values.SelectMany(records => records),
            _playerCreateSpells.Values.SelectMany(records => records),
            _gameObjectTemplates.Values,
            _gameObjectSpawns.Values,
            creatureTemplates,
            mergedSpawns);
    }

    // Method: BuildFallbackNextLevelExperience
    // Purpose: Builds or writes build fallback next level experience output for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - level: Level value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static uint BuildFallbackNextLevelExperience(byte level)
    {
        return ExperienceFormula.GetFallbackNextLevelExperience(level);
    }

    // Method: static
    // Purpose: Executes the static operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - Strength: Strength value supplied by the caller for this operation.
    // - Agility: Agility value supplied by the caller for this operation.
    // - Stamina: Stamina value supplied by the caller for this operation.
    // - Intellect: Intellect value supplied by the caller for this operation.
    // - Spirit: Spirit value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldTemplateDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static (uint Strength, uint Agility, uint Stamina, uint Intellect, uint Spirit) ResolveFallbackAttributes(byte playerClass, byte level)
    {
        (uint strength, uint agility, uint stamina, uint intellect, uint spirit) = playerClass switch
        {
            1 => (23u, 20u, 22u, 20u, 20u),
            2 => (22u, 20u, 22u, 20u, 20u),
            3 => (20u, 23u, 21u, 20u, 20u),
            4 => (21u, 24u, 20u, 20u, 20u),
            5 => (19u, 20u, 20u, 22u, 23u),
            7 => (21u, 20u, 21u, 21u, 21u),
            8 => (19u, 20u, 19u, 24u, 22u),
            9 => (19u, 20u, 21u, 23u, 22u),
            11 => (21u, 22u, 21u, 22u, 22u),
            _ => (20u, 20u, 20u, 20u, 20u),
        };

        uint levelBonus = Math.Max((uint)level, 1u) - 1u;
        strength += levelBonus;
        agility += levelBonus;
        stamina += levelBonus;
        intellect += playerClass is 1 or 4 ? 0u : levelBonus;
        spirit += levelBonus;
        return (strength, agility, stamina, intellect, spirit);
    }
}
