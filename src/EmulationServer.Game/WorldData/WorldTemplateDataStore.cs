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

using EmulationServer.Game.Players;

/**
  * File overview: src/EmulationServer.Game/WorldData/WorldTemplateDataStore.cs
  * Documents the WorldTemplateDataStore source file in the world database template loading and cache construction area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

using EmulationServer.Game.Formulas;

namespace EmulationServer.Game.WorldData;

/**
  * Owns the world template data store behavior for the world database template loading and cache construction layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class WorldTemplateDataStore
{
    /**
      * Exposes the empty value to callers that need this runtime or configuration data.
      * The property keeps the public surface strongly typed and documents which part of the server workflow owns the value.
      */
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

    private readonly Dictionary<(byte Race, byte Class), PlayerCreateInfoRecord> _playerCreateInfo;
    private readonly Dictionary<uint, ItemTemplateRecord> _itemTemplates;
    private readonly Dictionary<(byte Race, byte Class, byte Level), PlayerLevelStatsRecord> _playerLevelStats;
    private readonly Dictionary<(byte Class, byte Level), PlayerClassLevelStatsRecord> _playerClassLevelStats;
    private readonly Dictionary<byte, PlayerLevelExperienceRecord> _playerLevelExperience;
    private readonly Dictionary<(byte Race, byte Class), IReadOnlyList<PlayerCreateActionRecord>> _playerCreateActions;
    private readonly Dictionary<(byte Race, byte Class), IReadOnlyList<PlayerCreateItemRecord>> _playerCreateItems;
    private readonly Dictionary<(byte Race, byte Class), IReadOnlyList<PlayerCreateSpellRecord>> _playerCreateSpells;
    private readonly Dictionary<uint, GameObjectTemplateRecord> _gameObjectTemplates;
    private readonly Dictionary<uint, GameObjectSpawnRecord> _gameObjectSpawns;
    private readonly Dictionary<ushort, IReadOnlyList<GameObjectSpawnRecord>> _gameObjectSpawnsByMap;
    private readonly Dictionary<(ushort Map, uint ZoneId), IReadOnlyList<GameObjectSpawnRecord>> _gameObjectSpawnsByZone;
    private readonly Dictionary<(ushort Map, uint AreaId), IReadOnlyList<GameObjectSpawnRecord>> _gameObjectSpawnsByArea;
    private readonly Dictionary<uint, CreatureTemplateRecord> _creatureTemplates;
    private readonly Dictionary<uint, CreatureSpawnRecord> _creatureSpawns;
    private readonly Dictionary<ushort, IReadOnlyList<CreatureSpawnRecord>> _creatureSpawnsByMap;
    private readonly Dictionary<(ushort Map, uint ZoneId), IReadOnlyList<CreatureSpawnRecord>> _creatureSpawnsByZone;
    private readonly Dictionary<(ushort Map, uint AreaId), IReadOnlyList<CreatureSpawnRecord>> _creatureSpawnsByArea;

    /**
      * Initializes a new WorldTemplateDataStore instance with the dependencies required by the world database template loading and cache construction workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: playerCreateInfo, itemTemplates, playerLevelStats, playerClassLevelStats, playerLevelExperience, playerCreateActions....
      */
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

    public IReadOnlyDictionary<(byte Race, byte Class), PlayerCreateInfoRecord> PlayerCreateInfo => _playerCreateInfo;

    public IReadOnlyDictionary<uint, ItemTemplateRecord> ItemTemplates => _itemTemplates;

    public IReadOnlyDictionary<(byte Race, byte Class, byte Level), PlayerLevelStatsRecord> PlayerLevelStats => _playerLevelStats;

    public IReadOnlyDictionary<(byte Class, byte Level), PlayerClassLevelStatsRecord> PlayerClassLevelStats => _playerClassLevelStats;

    public IReadOnlyDictionary<byte, PlayerLevelExperienceRecord> PlayerLevelExperience => _playerLevelExperience;

    public IReadOnlyDictionary<(byte Race, byte Class), IReadOnlyList<PlayerCreateActionRecord>> PlayerCreateActions => _playerCreateActions;

    public IReadOnlyDictionary<(byte Race, byte Class), IReadOnlyList<PlayerCreateItemRecord>> PlayerCreateItems => _playerCreateItems;

    public IReadOnlyDictionary<(byte Race, byte Class), IReadOnlyList<PlayerCreateSpellRecord>> PlayerCreateSpells => _playerCreateSpells;

    public IReadOnlyDictionary<uint, GameObjectTemplateRecord> GameObjectTemplates => _gameObjectTemplates;

    public IReadOnlyDictionary<uint, GameObjectSpawnRecord> GameObjectSpawns => _gameObjectSpawns;

    public IReadOnlyDictionary<ushort, IReadOnlyList<GameObjectSpawnRecord>> GameObjectSpawnsByMap => _gameObjectSpawnsByMap;

    public IReadOnlyDictionary<uint, CreatureTemplateRecord> CreatureTemplates => _creatureTemplates;

    public IReadOnlyDictionary<uint, CreatureSpawnRecord> CreatureSpawns => _creatureSpawns;

    public IReadOnlyDictionary<ushort, IReadOnlyList<CreatureSpawnRecord>> CreatureSpawnsByMap => _creatureSpawnsByMap;

    /**
      * Stores the default player level stats count value used when the caller does not supply an override.
      * Centralizing the default keeps configuration and packet behavior consistent across the server process.
      */
    public int PlayerLevelStatsCount => _playerLevelStats.Count;

    /**
      * Stores the default player class level stats count value used when the caller does not supply an override.
      * Centralizing the default keeps configuration and packet behavior consistent across the server process.
      */
    public int PlayerClassLevelStatsCount => _playerClassLevelStats.Count;

    /**
      * Stores the default player level experience count value used when the caller does not supply an override.
      * Centralizing the default keeps configuration and packet behavior consistent across the server process.
      */
    public int PlayerLevelExperienceCount => _playerLevelExperience.Count;

    /**
      * Stores the default player create action count value used when the caller does not supply an override.
      * Centralizing the default keeps configuration and packet behavior consistent across the server process.
      */
    public int PlayerCreateActionCount => _playerCreateActions.Values.Sum(records => records.Count);

    /**
      * Stores the default player create item count value used when the caller does not supply an override.
      * Centralizing the default keeps configuration and packet behavior consistent across the server process.
      */
    public int PlayerCreateItemCount => _playerCreateItems.Values.Sum(records => records.Count);

    /**
      * Stores the default player create spell count value used when the caller does not supply an override.
      * Centralizing the default keeps configuration and packet behavior consistent across the server process.
      */
    public int PlayerCreateSpellCount => _playerCreateSpells.Values.Sum(records => records.Count);

    public int GameObjectTemplateCount => _gameObjectTemplates.Count;

    public int GameObjectSpawnCount => _gameObjectSpawns.Count;

    public int GameObjectSpawnMapCount => _gameObjectSpawnsByMap.Count;

    public int GameObjectSpawnZoneCount => _gameObjectSpawnsByZone.Count;

    public int GameObjectSpawnAreaCount => _gameObjectSpawnsByArea.Count;

    public int CreatureTemplateCount => _creatureTemplates.Count;

    public int CreatureSpawnCount => _creatureSpawns.Count;

    public int CreatureSpawnMapCount => _creatureSpawnsByMap.Count;

    public int CreatureSpawnZoneCount => _creatureSpawnsByZone.Count;

    public int CreatureSpawnAreaCount => _creatureSpawnsByArea.Count;

    /**
      * Tries to resolve the get player create info value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: race, characterClass, createInfo.
      */
    public bool TryGetPlayerCreateInfo(byte race, byte characterClass, out PlayerCreateInfoRecord createInfo)
    {
        return _playerCreateInfo.TryGetValue((race, characterClass), out createInfo!);
    }

    /**
      * Tries to resolve the get item template value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: entry, itemTemplate.
      */
    public bool TryGetItemTemplate(uint entry, out ItemTemplateRecord itemTemplate)
    {
        return _itemTemplates.TryGetValue(entry, out itemTemplate!);
    }

    public bool TryGetGameObjectTemplate(uint entry, out GameObjectTemplateRecord gameObjectTemplate)
    {
        return _gameObjectTemplates.TryGetValue(entry, out gameObjectTemplate!);
    }

    public GameObjectTemplateRecord? GetGameObjectTemplateOrDefault(uint entry)
    {
        return _gameObjectTemplates.TryGetValue(entry, out GameObjectTemplateRecord? template)
            ? template
            : null;
    }

    public bool TryGetGameObjectSpawn(uint guid, out GameObjectSpawnRecord gameObjectSpawn)
    {
        return _gameObjectSpawns.TryGetValue(guid, out gameObjectSpawn!);
    }

    public bool TryGetCreatureTemplate(uint entry, out CreatureTemplateRecord creatureTemplate)
    {
        return _creatureTemplates.TryGetValue(entry, out creatureTemplate!);
    }

    public CreatureTemplateRecord? GetCreatureTemplateOrDefault(uint entry)
    {
        return _creatureTemplates.TryGetValue(entry, out CreatureTemplateRecord? template)
            ? template
            : null;
    }

    public bool TryGetCreatureSpawn(uint guid, out CreatureSpawnRecord creatureSpawn)
    {
        return _creatureSpawns.TryGetValue(guid, out creatureSpawn!);
    }

    public IReadOnlyList<GameObjectSpawnRecord> GetGameObjectSpawnsForMap(ushort mapId)
    {
        return _gameObjectSpawnsByMap.TryGetValue(mapId, out IReadOnlyList<GameObjectSpawnRecord>? records)
            ? records
            : Array.Empty<GameObjectSpawnRecord>();
    }

    public IReadOnlyList<GameObjectSpawnRecord> GetGameObjectSpawnsForZone(ushort mapId, uint zoneId)
    {
        return _gameObjectSpawnsByZone.TryGetValue((mapId, zoneId), out IReadOnlyList<GameObjectSpawnRecord>? records)
            ? records
            : Array.Empty<GameObjectSpawnRecord>();
    }

    public IReadOnlyList<GameObjectSpawnRecord> GetGameObjectSpawnsForArea(ushort mapId, uint areaId)
    {
        return _gameObjectSpawnsByArea.TryGetValue((mapId, areaId), out IReadOnlyList<GameObjectSpawnRecord>? records)
            ? records
            : Array.Empty<GameObjectSpawnRecord>();
    }

    public IReadOnlyList<CreatureSpawnRecord> GetCreatureSpawnsForMap(ushort mapId)
    {
        return _creatureSpawnsByMap.TryGetValue(mapId, out IReadOnlyList<CreatureSpawnRecord>? records)
            ? records
            : Array.Empty<CreatureSpawnRecord>();
    }

    public IReadOnlyList<CreatureSpawnRecord> GetCreatureSpawnsForZone(ushort mapId, uint zoneId)
    {
        return _creatureSpawnsByZone.TryGetValue((mapId, zoneId), out IReadOnlyList<CreatureSpawnRecord>? records)
            ? records
            : Array.Empty<CreatureSpawnRecord>();
    }

    public IReadOnlyList<CreatureSpawnRecord> GetCreatureSpawnsForArea(ushort mapId, uint areaId)
    {
        return _creatureSpawnsByArea.TryGetValue((mapId, areaId), out IReadOnlyList<CreatureSpawnRecord>? records)
            ? records
            : Array.Empty<CreatureSpawnRecord>();
    }

    /**
      * Tries to resolve the get player level stats value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: race, characterClass, level, levelStats.
      */
    public bool TryGetPlayerLevelStats(byte race, byte characterClass, byte level, out PlayerLevelStatsRecord levelStats)
    {
        return _playerLevelStats.TryGetValue((race, characterClass, level), out levelStats!);
    }

    /**
      * Tries to resolve the get player class level stats value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: characterClass, level, classLevelStats.
      */
    public bool TryGetPlayerClassLevelStats(byte characterClass, byte level, out PlayerClassLevelStatsRecord classLevelStats)
    {
        return _playerClassLevelStats.TryGetValue((characterClass, level), out classLevelStats!);
    }

    /**
      * Resolves the player create actions value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: race, characterClass.
      */
    public IReadOnlyList<PlayerCreateActionRecord> GetPlayerCreateActions(byte race, byte characterClass)
    {
        return _playerCreateActions.TryGetValue((race, characterClass), out IReadOnlyList<PlayerCreateActionRecord>? records)
            ? records
            : Array.Empty<PlayerCreateActionRecord>();
    }

    /**
      * Resolves the player create items value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: race, characterClass.
      */
    public IReadOnlyList<PlayerCreateItemRecord> GetPlayerCreateItems(byte race, byte characterClass)
    {
        return _playerCreateItems.TryGetValue((race, characterClass), out IReadOnlyList<PlayerCreateItemRecord>? records)
            ? records
            : Array.Empty<PlayerCreateItemRecord>();
    }

    /**
      * Resolves the player create spells value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: race, characterClass.
      */
    public IReadOnlyList<PlayerCreateSpellRecord> GetPlayerCreateSpells(byte race, byte characterClass)
    {
        return _playerCreateSpells.TryGetValue((race, characterClass), out IReadOnlyList<PlayerCreateSpellRecord>? records)
            ? records
            : Array.Empty<PlayerCreateSpellRecord>();
    }

    /**
      * Resolves the next level experience value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: level.
      */
    public uint GetNextLevelExperience(byte level)
    {
        byte safeLevel = level == 0 ? (byte)1 : level;
        if (_playerLevelExperience.TryGetValue(safeLevel, out PlayerLevelExperienceRecord? record) && record.ExperienceForNextLevel != 0)
        {
            return record.ExperienceForNextLevel;
        }

        return ExperienceFormula.GetFallbackNextLevelExperience(safeLevel);
    }

    /**
      * Builds the build base player stats result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: race, characterClass, level.
      */
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

    /**
      * Rebuilds the immutable world cache with the same templates and a fully replaced game object spawn set.
      * This is used after startup coordinate enrichment so map/zone/area indexes reflect persisted per-guid locations.
      */
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

    /**
      * Rebuilds the immutable world cache with refreshed game object templates and one refreshed map's spawn rows.
      * This is used by map restart/start control paths so DB edits to gameobject rows can be observed without rebooting WorldServer.
      */
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

    /**
      * Rebuilds the immutable world cache with the same templates and a fully replaced creature spawn set.
      */
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

    /**
      * Rebuilds the immutable world cache with refreshed creature templates and one refreshed map's spawn rows.
      */
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

    /**
      * Builds the build fallback next level experience result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: level.
      */
    private static uint BuildFallbackNextLevelExperience(byte level)
    {
        return ExperienceFormula.GetFallbackNextLevelExperience(level);
    }

    /**
      * Performs the static operation for the world database template loading and cache construction workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: Strength, Agility, Stamina, Intellect, level.
      */
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
