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

using System.Globalization;

using EmulationServer.Database.Interfaces;
using EmulationServer.Game.Data.Maps;

using MySqlConnector;

/**
  * File overview: src/EmulationServer.Game/WorldData/WorldTemplateRepository.cs
  * Documents the WorldTemplateRepository source file in the world database template loading and cache construction area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.WorldData;

/**
  * Owns the world template repository behavior for the world database template loading and cache construction layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class WorldTemplateRepository
{
    /**
      * Holds the private database service state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly IDatabaseService _databaseService;

    /**
      * Initializes a new WorldTemplateRepository instance with the dependencies required by the world database template loading and cache construction workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: databaseService.
      */
    public WorldTemplateRepository(IDatabaseService databaseService)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException();
    }

    /**
      * Loads the startup world data cache from persistent storage.
      * item_template is loaded in full here so item lookups and item query responses use the in-memory world cache instead of runtime table reads.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async Task<WorldTemplateDataStore> LoadAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PlayerCreateInfoRecord> playerCreateInfo = await LoadPlayerCreateInfoAsync(cancellationToken);
        IReadOnlyList<ItemTemplateRecord> itemTemplates = await LoadItemTemplatesAsync(cancellationToken);
        IReadOnlyList<PlayerLevelStatsRecord> playerLevelStats = await LoadPlayerLevelStatsAsync(cancellationToken);
        IReadOnlyList<PlayerClassLevelStatsRecord> playerClassLevelStats = await LoadPlayerClassLevelStatsAsync(cancellationToken);
        IReadOnlyList<PlayerLevelExperienceRecord> playerLevelExperience = await LoadPlayerLevelExperienceAsync(cancellationToken);
        IReadOnlyList<PlayerCreateActionRecord> playerCreateActions = await LoadPlayerCreateActionsAsync(cancellationToken);
        IReadOnlyList<PlayerCreateItemRecord> playerCreateItems = await LoadPlayerCreateItemsAsync(cancellationToken);
        IReadOnlyList<PlayerCreateSpellRecord> playerCreateSpells = await LoadPlayerCreateSpellsAsync(cancellationToken);
        IReadOnlyList<GameObjectTemplateRecord> gameObjectTemplates = await LoadGameObjectTemplatesAsync(cancellationToken);
        IReadOnlyList<GameObjectSpawnRecord> gameObjectSpawns = await LoadGameObjectSpawnsAsync(cancellationToken);
        IReadOnlyList<CreatureTemplateRecord> creatureTemplates = await LoadCreatureTemplatesAsync(cancellationToken);
        IReadOnlyList<CreatureSpawnRecord> creatureSpawns = await LoadCreatureSpawnsAsync(cancellationToken);

        return new WorldTemplateDataStore(
            playerCreateInfo,
            itemTemplates,
            playerLevelStats,
            playerClassLevelStats,
            playerLevelExperience,
            playerCreateActions,
            playerCreateItems,
            playerCreateSpells,
            gameObjectTemplates,
            gameObjectSpawns,
            creatureTemplates,
            creatureSpawns);
    }

    /**
      * Loads load player create info information from configuration, files, or persistent storage.
      * The method normalizes external input before returning it so the rest of the server can work with validated, strongly typed data.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async Task<IReadOnlyList<PlayerCreateInfoRecord>> LoadPlayerCreateInfoAsync(CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT `race`, `class`, `map`, `zone`, `position_x`, `position_y`, `position_z`, `orientation`
            FROM `playercreateinfo`;
            """;

        List<PlayerCreateInfoRecord> records = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new PlayerCreateInfoRecord(
                Convert.ToByte(reader.GetValue(0), CultureInfo.InvariantCulture),
                Convert.ToByte(reader.GetValue(1), CultureInfo.InvariantCulture),
                Convert.ToUInt16(reader.GetValue(2), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(3), CultureInfo.InvariantCulture),
                Convert.ToSingle(reader.GetValue(4), CultureInfo.InvariantCulture),
                Convert.ToSingle(reader.GetValue(5), CultureInfo.InvariantCulture),
                Convert.ToSingle(reader.GetValue(6), CultureInfo.InvariantCulture),
                Convert.ToSingle(reader.GetValue(7), CultureInfo.InvariantCulture)));
        }

        return records;
    }

    /**
      * Loads every item_template row into the startup world cache.
      * Runtime item systems resolve item stats, damage, spells, armor, resistances, durability, and tooltip data from this cache.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async Task<IReadOnlyList<ItemTemplateRecord>> LoadItemTemplatesAsync(CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

        command.CommandText = $"""
            SELECT {ItemTemplateSelectColumns}
            FROM `item_template`;
            """;

        List<ItemTemplateRecord> records = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(ReadItemTemplateRecord(reader));
        }

        return records;
    }

    /**
      * Loads load player level stats information from configuration, files, or persistent storage.
      * The method normalizes external input before returning it so the rest of the server can work with validated, strongly typed data.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async Task<IReadOnlyList<PlayerLevelStatsRecord>> LoadPlayerLevelStatsAsync(CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        if (!await TableExistsAsync(connection, "player_levelstats", cancellationToken))
        {
            return Array.Empty<PlayerLevelStatsRecord>();
        }

        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT `race`, `class`, `level`, `str`, `agi`, `sta`, `inte`, `spi`
            FROM `player_levelstats`;
            """;

        List<PlayerLevelStatsRecord> records = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new PlayerLevelStatsRecord(
                Convert.ToByte(reader.GetValue(0), CultureInfo.InvariantCulture),
                Convert.ToByte(reader.GetValue(1), CultureInfo.InvariantCulture),
                Convert.ToByte(reader.GetValue(2), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(3), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(4), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(5), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(6), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(7), CultureInfo.InvariantCulture)));
        }

        return records;
    }

    /**
      * Loads load player class level stats information from configuration, files, or persistent storage.
      * The method normalizes external input before returning it so the rest of the server can work with validated, strongly typed data.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async Task<IReadOnlyList<PlayerClassLevelStatsRecord>> LoadPlayerClassLevelStatsAsync(CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        if (!await TableExistsAsync(connection, "player_classlevelstats", cancellationToken))
        {
            return Array.Empty<PlayerClassLevelStatsRecord>();
        }

        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT `class`, `level`, `basehp`, `basemana`
            FROM `player_classlevelstats`;
            """;

        List<PlayerClassLevelStatsRecord> records = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new PlayerClassLevelStatsRecord(
                Convert.ToByte(reader.GetValue(0), CultureInfo.InvariantCulture),
                Convert.ToByte(reader.GetValue(1), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(2), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(3), CultureInfo.InvariantCulture)));
        }

        return records;
    }

    /**
      * Loads load player level experience information from configuration, files, or persistent storage.
      * The method normalizes external input before returning it so the rest of the server can work with validated, strongly typed data.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async Task<IReadOnlyList<PlayerLevelExperienceRecord>> LoadPlayerLevelExperienceAsync(CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        if (!await TableExistsAsync(connection, "player_xp_for_level", cancellationToken))
        {
            return Array.Empty<PlayerLevelExperienceRecord>();
        }

        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT `lvl`, `xp_for_next_level`
            FROM `player_xp_for_level`;
            """;

        List<PlayerLevelExperienceRecord> records = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new PlayerLevelExperienceRecord(
                Convert.ToByte(reader.GetValue(0), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(1), CultureInfo.InvariantCulture)));
        }

        return records;
    }

    /**
      * Loads load player create actions information from configuration, files, or persistent storage.
      * The method normalizes external input before returning it so the rest of the server can work with validated, strongly typed data.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async Task<IReadOnlyList<PlayerCreateActionRecord>> LoadPlayerCreateActionsAsync(CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        if (!await TableExistsAsync(connection, "playercreateinfo_action", cancellationToken))
        {
            return Array.Empty<PlayerCreateActionRecord>();
        }

        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT `race`, `class`, `button`, `action`, `type`
            FROM `playercreateinfo_action`;
            """;

        List<PlayerCreateActionRecord> records = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new PlayerCreateActionRecord(
                Convert.ToByte(reader.GetValue(0), CultureInfo.InvariantCulture),
                Convert.ToByte(reader.GetValue(1), CultureInfo.InvariantCulture),
                Convert.ToByte(reader.GetValue(2), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(3), CultureInfo.InvariantCulture),
                Convert.ToByte(reader.GetValue(4), CultureInfo.InvariantCulture)));
        }

        return records;
    }

    /**
      * Loads load player create items information from configuration, files, or persistent storage.
      * The method normalizes external input before returning it so the rest of the server can work with validated, strongly typed data.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async Task<IReadOnlyList<PlayerCreateItemRecord>> LoadPlayerCreateItemsAsync(CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        if (!await TableExistsAsync(connection, "playercreateinfo_item", cancellationToken))
        {
            return Array.Empty<PlayerCreateItemRecord>();
        }

        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT `race`, `class`, `itemid`, `amount`
            FROM `playercreateinfo_item`;
            """;

        List<PlayerCreateItemRecord> records = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new PlayerCreateItemRecord(
                Convert.ToByte(reader.GetValue(0), CultureInfo.InvariantCulture),
                Convert.ToByte(reader.GetValue(1), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(2), CultureInfo.InvariantCulture),
                Convert.ToByte(reader.GetValue(3), CultureInfo.InvariantCulture)));
        }

        return records;
    }

    /**
      * Loads load player create spells information from configuration, files, or persistent storage.
      * The method normalizes external input before returning it so the rest of the server can work with validated, strongly typed data.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async Task<IReadOnlyList<PlayerCreateSpellRecord>> LoadPlayerCreateSpellsAsync(CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        if (!await TableExistsAsync(connection, "playercreateinfo_spell", cancellationToken))
        {
            return Array.Empty<PlayerCreateSpellRecord>();
        }

        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT `race`, `class`, `Spell`, `Note`
            FROM `playercreateinfo_spell`;
            """;

        List<PlayerCreateSpellRecord> records = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new PlayerCreateSpellRecord(
                Convert.ToByte(reader.GetValue(0), CultureInfo.InvariantCulture),
                Convert.ToByte(reader.GetValue(1), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(2), CultureInfo.InvariantCulture),
                reader.IsDBNull(3) ? string.Empty : reader.GetString(3)));
        }

        return records;
    }

    /**
      * Loads every gameobject_template row into the startup world cache.
      * The schema mirrors Mangos Zero and keeps all 24 data fields available for type-specific object behavior later.
      */
    public async Task<IReadOnlyList<GameObjectTemplateRecord>> LoadGameObjectTemplatesAsync(CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        if (!await TableExistsAsync(connection, "gameobject_template", cancellationToken))
        {
            return Array.Empty<GameObjectTemplateRecord>();
        }

        bool hasScriptName = await ColumnExistsAsync(connection, "gameobject_template", "ScriptName", cancellationToken);

        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT {FormatGameObjectTemplateSelectColumns(hasScriptName)}
            FROM `gameobject_template`;
            """;

        List<GameObjectTemplateRecord> records = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(ReadGameObjectTemplateRecord(reader));
        }

        return records;
    }

    /**
      * Loads every gameobject spawn row into the startup world cache.
      * zoneId and areaId are read when present, or safely defaulted while older databases are being migrated.
      */
    public async Task<IReadOnlyList<GameObjectSpawnRecord>> LoadGameObjectSpawnsAsync(CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        if (!await TableExistsAsync(connection, "gameobject", cancellationToken))
        {
            return Array.Empty<GameObjectSpawnRecord>();
        }

        bool hasZoneId = await ColumnExistsAsync(connection, "gameobject", "zoneId", cancellationToken);
        bool hasAreaId = await ColumnExistsAsync(connection, "gameobject", "areaId", cancellationToken);

        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT `guid`, `id`, `map`, {FormatOptionalColumnSelect(hasZoneId, "zoneId")}, {FormatOptionalColumnSelect(hasAreaId, "areaId")},
                   `position_x`, `position_y`, `position_z`, `orientation`,
                   `rotation0`, `rotation1`, `rotation2`, `rotation3`,
                   `spawntimesecs`, `animprogress`, `state`
            FROM `gameobject`
            ORDER BY `map`, `guid`;
            """;

        List<GameObjectSpawnRecord> records = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(ReadGameObjectSpawnRecord(reader));
        }

        return records;
    }

    /**
      * Loads gameobject rows for one map. Used by map start/restart control flows so object DB edits can be picked up without rebooting WorldServer.
      */
    public async Task<IReadOnlyList<GameObjectSpawnRecord>> LoadGameObjectSpawnsForMapAsync(ushort mapId, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        if (!await TableExistsAsync(connection, "gameobject", cancellationToken))
        {
            return Array.Empty<GameObjectSpawnRecord>();
        }

        bool hasZoneId = await ColumnExistsAsync(connection, "gameobject", "zoneId", cancellationToken);
        bool hasAreaId = await ColumnExistsAsync(connection, "gameobject", "areaId", cancellationToken);

        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT `guid`, `id`, `map`, {FormatOptionalColumnSelect(hasZoneId, "zoneId")}, {FormatOptionalColumnSelect(hasAreaId, "areaId")},
                   `position_x`, `position_y`, `position_z`, `orientation`,
                   `rotation0`, `rotation1`, `rotation2`, `rotation3`,
                   `spawntimesecs`, `animprogress`, `state`
            FROM `gameobject`
            WHERE `map` = @map
            ORDER BY `guid`;
            """;
        command.Parameters.AddWithValue("@map", mapId);

        List<GameObjectSpawnRecord> records = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(ReadGameObjectSpawnRecord(reader));
        }

        return records;
    }

    /**
      * Resolves zoneId/areaId per gameobject guid from world coordinates and persists changed values back to the world database.
      * This method deliberately works from the spawn position, not grouped counts, so each row receives its own area identifiers.
      */
    public async Task<GameObjectAreaEnrichmentResult> ResolveAndPersistGameObjectAreasAsync(
        IEnumerable<GameObjectSpawnRecord> spawns,
        MapStoreAreaLookupService areaLookup,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spawns);
        ArgumentNullException.ThrowIfNull(areaLookup);

        List<GameObjectSpawnRecord> enrichedSpawns = [];
        List<GameObjectSpawnRecord> changedSpawns = [];
        Dictionary<string, int> sourceCounts = new(StringComparer.OrdinalIgnoreCase);
        int resolvedCount = 0;
        int unresolvedCount = 0;

        foreach (GameObjectSpawnRecord spawn in spawns)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!areaLookup.TryResolve(spawn.Map, spawn.PositionX, spawn.PositionY, out WorldAreaLookupResult lookup) || !lookup.IsResolved)
            {
                enrichedSpawns.Add(spawn);
                unresolvedCount++;
                continue;
            }

            resolvedCount++;
            sourceCounts[lookup.Source] = sourceCounts.TryGetValue(lookup.Source, out int count) ? count + 1 : 1;

            GameObjectSpawnRecord enrichedSpawn = spawn with
            {
                ZoneId = lookup.ZoneId,
                AreaId = lookup.AreaId,
            };

            enrichedSpawns.Add(enrichedSpawn);

            if (spawn.ZoneId != lookup.ZoneId || spawn.AreaId != lookup.AreaId)
            {
                changedSpawns.Add(enrichedSpawn);
            }
        }

        int persistedCount = changedSpawns.Count == 0
            ? 0
            : await PersistGameObjectAreaIdsAsync(changedSpawns, cancellationToken);

        return new GameObjectAreaEnrichmentResult(
            enrichedSpawns,
            resolvedCount,
            unresolvedCount,
            changedSpawns.Count,
            persistedCount,
            sourceCounts);
    }

    /**
      * Persists changed gameobject zone/area identifiers by guid.
      */
    private async Task<int> PersistGameObjectAreaIdsAsync(IReadOnlyList<GameObjectSpawnRecord> changedSpawns, CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        if (!await TableExistsAsync(connection, "gameobject", cancellationToken) ||
            !await ColumnExistsAsync(connection, "gameobject", "zoneId", cancellationToken) ||
            !await ColumnExistsAsync(connection, "gameobject", "areaId", cancellationToken))
        {
            return 0;
        }

        await using MySqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE `gameobject`
            SET `zoneId` = @zoneId,
                `areaId` = @areaId
            WHERE `guid` = @guid
              AND (`zoneId` <> @zoneId OR `areaId` <> @areaId);
            """;

        MySqlParameter guidParameter = command.Parameters.Add("@guid", MySqlDbType.UInt32);
        MySqlParameter zoneParameter = command.Parameters.Add("@zoneId", MySqlDbType.UInt32);
        MySqlParameter areaParameter = command.Parameters.Add("@areaId", MySqlDbType.UInt32);

        int updated = 0;
        foreach (GameObjectSpawnRecord spawn in changedSpawns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            guidParameter.Value = spawn.Guid;
            zoneParameter.Value = spawn.ZoneId;
            areaParameter.Value = spawn.AreaId;
            updated += await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return updated;
    }

    /**
      * Loads every creature_template row into the startup world cache.
      */
    public async Task<IReadOnlyList<CreatureTemplateRecord>> LoadCreatureTemplatesAsync(CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        if (!await TableExistsAsync(connection, "creature_template", cancellationToken))
        {
            return Array.Empty<CreatureTemplateRecord>();
        }

        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT {CreatureTemplateSelectColumns}
            FROM `creature_template`;
            """;

        List<CreatureTemplateRecord> records = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(ReadCreatureTemplateRecord(reader));
        }

        return records;
    }

    /**
      * Loads every creature spawn row into the startup world cache.
      */
    public async Task<IReadOnlyList<CreatureSpawnRecord>> LoadCreatureSpawnsAsync(CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        if (!await TableExistsAsync(connection, "creature", cancellationToken))
        {
            return Array.Empty<CreatureSpawnRecord>();
        }

        bool hasZoneId = await ColumnExistsAsync(connection, "creature", "zoneId", cancellationToken);
        bool hasAreaId = await ColumnExistsAsync(connection, "creature", "areaId", cancellationToken);

        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT `guid`, `id`, `map`, {FormatOptionalColumnSelect(hasZoneId, "zoneId")}, {FormatOptionalColumnSelect(hasAreaId, "areaId")},
                   `modelid`, `equipment_id`, `position_x`, `position_y`, `position_z`, `orientation`,
                   `spawntimesecs`, `spawndist`, `currentwaypoint`, `curhealth`, `curmana`, `DeathState`, `MovementType`
            FROM `creature`
            ORDER BY `map`, `guid`;
            """;

        List<CreatureSpawnRecord> records = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(ReadCreatureSpawnRecord(reader));
        }

        return records;
    }

    /**
      * Loads creature rows for one map. Used by map start/restart control flows so creature DB edits can be picked up without rebooting WorldServer.
      */
    public async Task<IReadOnlyList<CreatureSpawnRecord>> LoadCreatureSpawnsForMapAsync(ushort mapId, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        if (!await TableExistsAsync(connection, "creature", cancellationToken))
        {
            return Array.Empty<CreatureSpawnRecord>();
        }

        bool hasZoneId = await ColumnExistsAsync(connection, "creature", "zoneId", cancellationToken);
        bool hasAreaId = await ColumnExistsAsync(connection, "creature", "areaId", cancellationToken);

        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT `guid`, `id`, `map`, {FormatOptionalColumnSelect(hasZoneId, "zoneId")}, {FormatOptionalColumnSelect(hasAreaId, "areaId")},
                   `modelid`, `equipment_id`, `position_x`, `position_y`, `position_z`, `orientation`,
                   `spawntimesecs`, `spawndist`, `currentwaypoint`, `curhealth`, `curmana`, `DeathState`, `MovementType`
            FROM `creature`
            WHERE `map` = @map
            ORDER BY `guid`;
            """;
        command.Parameters.AddWithValue("@map", mapId);

        List<CreatureSpawnRecord> records = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(ReadCreatureSpawnRecord(reader));
        }

        return records;
    }

    /**
      * Resolves zoneId/areaId per creature guid from world coordinates and persists changed values back to the world database.
      */
    public async Task<CreatureAreaEnrichmentResult> ResolveAndPersistCreatureAreasAsync(
        IEnumerable<CreatureSpawnRecord> spawns,
        MapStoreAreaLookupService areaLookup,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spawns);
        ArgumentNullException.ThrowIfNull(areaLookup);

        List<CreatureSpawnRecord> enrichedSpawns = [];
        List<CreatureSpawnRecord> changedSpawns = [];
        Dictionary<string, int> sourceCounts = new(StringComparer.OrdinalIgnoreCase);
        int resolvedCount = 0;
        int unresolvedCount = 0;

        foreach (CreatureSpawnRecord spawn in spawns)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!areaLookup.TryResolve(spawn.Map, spawn.PositionX, spawn.PositionY, out WorldAreaLookupResult lookup) || !lookup.IsResolved)
            {
                enrichedSpawns.Add(spawn);
                unresolvedCount++;
                continue;
            }

            resolvedCount++;
            sourceCounts[lookup.Source] = sourceCounts.TryGetValue(lookup.Source, out int count) ? count + 1 : 1;

            CreatureSpawnRecord enrichedSpawn = spawn with
            {
                ZoneId = lookup.ZoneId,
                AreaId = lookup.AreaId,
            };

            enrichedSpawns.Add(enrichedSpawn);

            if (spawn.ZoneId != lookup.ZoneId || spawn.AreaId != lookup.AreaId)
            {
                changedSpawns.Add(enrichedSpawn);
            }
        }

        int persistedCount = changedSpawns.Count == 0
            ? 0
            : await PersistCreatureAreaIdsAsync(changedSpawns, cancellationToken);

        return new CreatureAreaEnrichmentResult(
            enrichedSpawns,
            resolvedCount,
            unresolvedCount,
            changedSpawns.Count,
            persistedCount,
            sourceCounts);
    }

    /**
      * Persists changed creature zone/area identifiers by guid.
      */
    private async Task<int> PersistCreatureAreaIdsAsync(IReadOnlyList<CreatureSpawnRecord> changedSpawns, CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        if (!await TableExistsAsync(connection, "creature", cancellationToken) ||
            !await ColumnExistsAsync(connection, "creature", "zoneId", cancellationToken) ||
            !await ColumnExistsAsync(connection, "creature", "areaId", cancellationToken))
        {
            return 0;
        }

        await using MySqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE `creature`
            SET `zoneId` = @zoneId,
                `areaId` = @areaId
            WHERE `guid` = @guid
              AND (`zoneId` <> @zoneId OR `areaId` <> @areaId);
            """;

        MySqlParameter guidParameter = command.Parameters.Add("@guid", MySqlDbType.UInt32);
        MySqlParameter zoneParameter = command.Parameters.Add("@zoneId", MySqlDbType.UInt32);
        MySqlParameter areaParameter = command.Parameters.Add("@areaId", MySqlDbType.UInt32);

        int updated = 0;
        foreach (CreatureSpawnRecord spawn in changedSpawns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            guidParameter.Value = spawn.Guid;
            zoneParameter.Value = spawn.ZoneId;
            areaParameter.Value = spawn.AreaId;
            updated += await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return updated;
    }


    /**
      * Resolves the player create info value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: race, characterClass, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async Task<PlayerCreateInfoRecord?> GetPlayerCreateInfoAsync(byte race, byte characterClass, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT `race`, `class`, `map`, `zone`, `position_x`, `position_y`, `position_z`, `orientation`
            FROM `playercreateinfo`
            WHERE `race` = @race AND `class` = @class
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@race", race);
        command.Parameters.AddWithValue("@class", characterClass);

        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new PlayerCreateInfoRecord(
            Convert.ToByte(reader.GetValue(0), CultureInfo.InvariantCulture),
            Convert.ToByte(reader.GetValue(1), CultureInfo.InvariantCulture),
            Convert.ToUInt16(reader.GetValue(2), CultureInfo.InvariantCulture),
            Convert.ToUInt32(reader.GetValue(3), CultureInfo.InvariantCulture),
            Convert.ToSingle(reader.GetValue(4), CultureInfo.InvariantCulture),
            Convert.ToSingle(reader.GetValue(5), CultureInfo.InvariantCulture),
            Convert.ToSingle(reader.GetValue(6), CultureInfo.InvariantCulture),
            Convert.ToSingle(reader.GetValue(7), CultureInfo.InvariantCulture));
    }

    public async Task<IReadOnlyDictionary<uint, ItemTemplateRecord>> GetItemTemplatesAsync(IEnumerable<uint> itemEntries, CancellationToken cancellationToken = default)
    {
        uint[] entries = itemEntries.Where(entry => entry != 0).Distinct().ToArray();
        if (entries.Length == 0)
        {
            return new Dictionary<uint, ItemTemplateRecord>();
        }

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

        List<string> parameterNames = [];
        for (int index = 0; index < entries.Length; index++)
        {
            string parameterName = $"@entry{index}";
            parameterNames.Add(parameterName);
            command.Parameters.AddWithValue(parameterName, entries[index]);
        }

        command.CommandText = $"""
            SELECT {ItemTemplateSelectColumns}
            FROM `item_template`
            WHERE `entry` IN ({string.Join(',', parameterNames)});
            """;

        Dictionary<uint, ItemTemplateRecord> result = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            ItemTemplateRecord record = ReadItemTemplateRecord(reader);
            result[record.Entry] = record;
        }

        return result;
    }

    private const string CreatureTemplateSelectColumns = """
        `Entry`, `Name`, COALESCE(`SubName`, '') AS `SubName`, `MinLevel`, `MaxLevel`,
        `ModelId1`, `ModelId2`, `ModelId3`, `ModelId4`, `FactionAlliance`, `FactionHorde`, `Scale`,
        `Family`, `CreatureType`, `InhabitType`, COALESCE(`RegenerateStats`, 0) AS `RegenerateStats`, `RacialLeader`,
        `NpcFlags`, `UnitFlags`, `DynamicFlags`, `ExtraFlags`, `CreatureTypeFlags`, `SpeedWalk`, `SpeedRun`,
        `UnitClass`, `Rank`, `HealthMultiplier`, `PowerMultiplier`, `DamageMultiplier`, `DamageVariance`, `ArmorMultiplier`,
        `ExperienceMultiplier`, `MinLevelHealth`, `MaxLevelHealth`, `MinLevelMana`, `MaxLevelMana`, `MinMeleeDmg`,
        `MaxMeleeDmg`, `MinRangedDmg`, `MaxRangedDmg`, `Armor`, `MeleeAttackPower`, `RangedAttackPower`,
        `MeleeBaseAttackTime`, `RangedBaseAttackTime`, `DamageSchool`, `MinLootGold`, `MaxLootGold`, `LootId`,
        `PickpocketLootId`, `SkinningLootId`, `KillCredit1`, `KillCredit2`, `MechanicImmuneMask`, `SchoolImmuneMask`,
        `ResistanceHoly`, `ResistanceFire`, `ResistanceNature`, `ResistanceFrost`, `ResistanceShadow`, `ResistanceArcane`,
        `SpellListId`, `PetSpellDataId`, `MovementType`, `TrainerType`, `TrainerSpell`, `TrainerClass`, `TrainerRace`,
        `TrainerTemplateId`, `VendorTemplateId`, `GossipMenuId`, `EquipmentTemplateId`, `Civilian`, COALESCE(`AIName`, '') AS `AIName`
        """;

    /**
      * Parses one creature_template row into the immutable template cache record.
      */
    private static CreatureTemplateRecord ReadCreatureTemplateRecord(MySqlDataReader reader)
    {
        int index = 0;
        return new CreatureTemplateRecord(
            Entry: ReadUInt32(reader, index++),
            Name: ReadString(reader, index++),
            SubName: ReadString(reader, index++),
            MinLevel: ReadByte(reader, index++),
            MaxLevel: ReadByte(reader, index++),
            ModelId1: ReadUInt32(reader, index++),
            ModelId2: ReadUInt32(reader, index++),
            ModelId3: ReadUInt32(reader, index++),
            ModelId4: ReadUInt32(reader, index++),
            FactionAlliance: ReadUInt16(reader, index++),
            FactionHorde: ReadUInt16(reader, index++),
            Scale: ReadSingle(reader, index++),
            Family: ReadSByte(reader, index++),
            CreatureType: ReadByte(reader, index++),
            InhabitType: ReadByte(reader, index++),
            RegenerateStats: ReadByte(reader, index++),
            RacialLeader: ReadByte(reader, index++),
            NpcFlags: ReadUInt32(reader, index++),
            UnitFlags: ReadUInt32(reader, index++),
            DynamicFlags: ReadUInt32(reader, index++),
            ExtraFlags: ReadUInt32(reader, index++),
            CreatureTypeFlags: ReadUInt32(reader, index++),
            SpeedWalk: ReadSingle(reader, index++),
            SpeedRun: ReadSingle(reader, index++),
            UnitClass: ReadByte(reader, index++),
            Rank: ReadByte(reader, index++),
            HealthMultiplier: ReadSingle(reader, index++),
            PowerMultiplier: ReadSingle(reader, index++),
            DamageMultiplier: ReadSingle(reader, index++),
            DamageVariance: ReadSingle(reader, index++),
            ArmorMultiplier: ReadSingle(reader, index++),
            ExperienceMultiplier: ReadSingle(reader, index++),
            MinLevelHealth: ReadUInt32(reader, index++),
            MaxLevelHealth: ReadUInt32(reader, index++),
            MinLevelMana: ReadUInt32(reader, index++),
            MaxLevelMana: ReadUInt32(reader, index++),
            MinMeleeDamage: ReadSingle(reader, index++),
            MaxMeleeDamage: ReadSingle(reader, index++),
            MinRangedDamage: ReadSingle(reader, index++),
            MaxRangedDamage: ReadSingle(reader, index++),
            Armor: ReadUInt32(reader, index++),
            MeleeAttackPower: ReadUInt32(reader, index++),
            RangedAttackPower: ReadUInt16(reader, index++),
            MeleeBaseAttackTime: ReadUInt32(reader, index++),
            RangedBaseAttackTime: ReadUInt32(reader, index++),
            DamageSchool: ReadSByte(reader, index++),
            MinLootGold: ReadUInt32(reader, index++),
            MaxLootGold: ReadUInt32(reader, index++),
            LootId: ReadUInt32(reader, index++),
            PickpocketLootId: ReadUInt32(reader, index++),
            SkinningLootId: ReadUInt32(reader, index++),
            KillCredit1: ReadUInt32(reader, index++),
            KillCredit2: ReadUInt32(reader, index++),
            MechanicImmuneMask: ReadUInt32(reader, index++),
            SchoolImmuneMask: ReadUInt32(reader, index++),
            ResistanceHoly: ReadInt16(reader, index++),
            ResistanceFire: ReadInt16(reader, index++),
            ResistanceNature: ReadInt16(reader, index++),
            ResistanceFrost: ReadInt16(reader, index++),
            ResistanceShadow: ReadInt16(reader, index++),
            ResistanceArcane: ReadInt16(reader, index++),
            SpellListId: ReadUInt32(reader, index++),
            PetSpellDataId: ReadUInt32(reader, index++),
            MovementType: ReadByte(reader, index++),
            TrainerType: ReadSByte(reader, index++),
            TrainerSpell: ReadUInt32(reader, index++),
            TrainerClass: ReadByte(reader, index++),
            TrainerRace: ReadByte(reader, index++),
            TrainerTemplateId: ReadUInt32(reader, index++),
            VendorTemplateId: ReadUInt32(reader, index++),
            GossipMenuId: ReadUInt32(reader, index++),
            EquipmentTemplateId: ReadUInt32(reader, index++),
            Civilian: ReadByte(reader, index++),
            AIName: ReadString(reader, index++));
    }

    /**
      * Parses one creature spawn row into the immutable spawn cache record.
      */
    private static CreatureSpawnRecord ReadCreatureSpawnRecord(MySqlDataReader reader)
    {
        int index = 0;
        return new CreatureSpawnRecord(
            ReadUInt32(reader, index++),
            ReadUInt32(reader, index++),
            ReadUInt16(reader, index++),
            ReadUInt32(reader, index++),
            ReadUInt32(reader, index++),
            ReadUInt32(reader, index++),
            ReadInt32(reader, index++),
            ReadSingle(reader, index++),
            ReadSingle(reader, index++),
            ReadSingle(reader, index++),
            ReadSingle(reader, index++),
            ReadUInt32(reader, index++),
            ReadSingle(reader, index++),
            ReadUInt32(reader, index++),
            ReadUInt32(reader, index++),
            ReadUInt32(reader, index++),
            ReadByte(reader, index++),
            ReadByte(reader, index++));
    }

    private const string GameObjectTemplateSelectColumns = """
        `entry`, `type`, `displayId`, `name`, `faction`, `flags`, `size`,
        `data0`, `data1`, `data2`, `data3`, `data4`, `data5`, `data6`, `data7`,
        `data8`, `data9`, `data10`, `data11`, `data12`, `data13`, `data14`, `data15`,
        `data16`, `data17`, `data18`, `data19`, `data20`, `data21`, `data22`, `data23`,
        `mingold`, `maxgold`
        """;

    private static string FormatGameObjectTemplateSelectColumns(bool hasScriptName)
    {
        return hasScriptName
            ? $"{GameObjectTemplateSelectColumns}, `ScriptName`"
            : $"{GameObjectTemplateSelectColumns}, '' AS `ScriptName`";
    }

    /**
      * Parses one gameobject_template row into the immutable template cache record.
      */
    private static GameObjectTemplateRecord ReadGameObjectTemplateRecord(MySqlDataReader reader)
    {
        int index = 0;
        uint entry = ReadUInt32(reader, index++);
        byte type = ReadByte(reader, index++);
        uint displayId = ReadUInt32(reader, index++);
        string name = ReadString(reader, index++);
        ushort faction = ReadUInt16(reader, index++);
        uint flags = ReadUInt32(reader, index++);
        float size = ReadSingle(reader, index++);

        List<uint> dataFields = [];
        for (int dataIndex = 0; dataIndex < GameObjectTemplateRecord.DataFieldCount; dataIndex++)
        {
            dataFields.Add(ReadUInt32(reader, index++));
        }

        return new GameObjectTemplateRecord(
            entry,
            type,
            displayId,
            name,
            faction,
            flags,
            size,
            dataFields,
            ReadUInt32(reader, index++),
            ReadUInt32(reader, index++),
            ReadString(reader, index++));
    }

    /**
      * Parses one gameobject spawn row into the immutable spawn cache record.
      */
    private static GameObjectSpawnRecord ReadGameObjectSpawnRecord(MySqlDataReader reader)
    {
        int index = 0;
        return new GameObjectSpawnRecord(
            ReadUInt32(reader, index++),
            ReadUInt32(reader, index++),
            ReadUInt16(reader, index++),
            ReadUInt32(reader, index++),
            ReadUInt32(reader, index++),
            ReadSingle(reader, index++),
            ReadSingle(reader, index++),
            ReadSingle(reader, index++),
            ReadSingle(reader, index++),
            ReadSingle(reader, index++),
            ReadSingle(reader, index++),
            ReadSingle(reader, index++),
            ReadSingle(reader, index++),
            ReadInt32(reader, index++),
            ReadByte(reader, index++),
            ReadByte(reader, index++));
    }

    private static string FormatOptionalColumnSelect(bool columnExists, string columnName)
    {
        return columnExists
            ? $"`{columnName}`"
            : $"0 AS `{columnName}`";
    }

    private const string ItemTemplateSelectColumns = """
        `entry`, `class`, `subclass`, `name`, `displayid`, `Quality`, `Flags`, `BuyCount`, `BuyPrice`, `SellPrice`,
        `InventoryType`, `AllowableClass`, `AllowableRace`, `ItemLevel`, `RequiredLevel`, `RequiredSkill`, `RequiredSkillRank`,
        `requiredspell`, `requiredhonorrank`, `RequiredCityRank`, `RequiredReputationFaction`, `RequiredReputationRank`,
        `maxcount`, `stackable`, `ContainerSlots`,
        `stat_type1`, `stat_value1`, `stat_type2`, `stat_value2`, `stat_type3`, `stat_value3`, `stat_type4`, `stat_value4`,
        `stat_type5`, `stat_value5`, `stat_type6`, `stat_value6`, `stat_type7`, `stat_value7`, `stat_type8`, `stat_value8`,
        `stat_type9`, `stat_value9`, `stat_type10`, `stat_value10`,
        `dmg_min1`, `dmg_max1`, `dmg_type1`, `dmg_min2`, `dmg_max2`, `dmg_type2`, `dmg_min3`, `dmg_max3`, `dmg_type3`,
        `dmg_min4`, `dmg_max4`, `dmg_type4`, `dmg_min5`, `dmg_max5`, `dmg_type5`,
        `armor`, `holy_res`, `fire_res`, `nature_res`, `frost_res`, `shadow_res`, `arcane_res`, `delay`, `ammo_type`, `RangedModRange`,
        `spellid_1`, `spelltrigger_1`, `spellcharges_1`, `spellppmRate_1`, `spellcooldown_1`, `spellcategory_1`, `spellcategorycooldown_1`,
        `spellid_2`, `spelltrigger_2`, `spellcharges_2`, `spellppmRate_2`, `spellcooldown_2`, `spellcategory_2`, `spellcategorycooldown_2`,
        `spellid_3`, `spelltrigger_3`, `spellcharges_3`, `spellppmRate_3`, `spellcooldown_3`, `spellcategory_3`, `spellcategorycooldown_3`,
        `spellid_4`, `spelltrigger_4`, `spellcharges_4`, `spellppmRate_4`, `spellcooldown_4`, `spellcategory_4`, `spellcategorycooldown_4`,
        `spellid_5`, `spelltrigger_5`, `spellcharges_5`, `spellppmRate_5`, `spellcooldown_5`, `spellcategory_5`, `spellcategorycooldown_5`,
        `bonding`, `description`, `PageText`, `LanguageID`, `PageMaterial`, `startquest`, `lockid`, `Material`, `sheath`,
        `RandomProperty`, `block`, `itemset`, `MaxDurability`, `area`, `Map`, `BagFamily`, `DisenchantID`, `FoodType`,
        `minMoneyLoot`, `maxMoneyLoot`, `Duration`, `ExtraFlags`
        """;

    /**
      * Parses one item_template row into the full immutable template cache record.
      */
    private static ItemTemplateRecord ReadItemTemplateRecord(MySqlDataReader reader)
    {
        int index = 0;

        uint entry = ReadUInt32(reader, index++);
        byte itemClass = ReadByte(reader, index++);
        byte subClass = ReadByte(reader, index++);
        string name = ReadString(reader, index++);
        uint displayId = ReadUInt32(reader, index++);
        byte quality = ReadByte(reader, index++);
        uint flags = ReadUInt32(reader, index++);
        byte buyCount = ReadByte(reader, index++);
        uint buyPrice = ReadUInt32(reader, index++);
        uint sellPrice = ReadUInt32(reader, index++);
        byte inventoryType = ReadByte(reader, index++);
        int allowableClass = ReadInt32(reader, index++);
        int allowableRace = ReadInt32(reader, index++);
        byte itemLevel = ReadByte(reader, index++);
        byte requiredLevel = ReadByte(reader, index++);
        ushort requiredSkill = ReadUInt16(reader, index++);
        ushort requiredSkillRank = ReadUInt16(reader, index++);
        uint requiredSpell = ReadUInt32(reader, index++);
        uint requiredHonorRank = ReadUInt32(reader, index++);
        uint requiredCityRank = ReadUInt32(reader, index++);
        ushort requiredReputationFaction = ReadUInt16(reader, index++);
        ushort requiredReputationRank = ReadUInt16(reader, index++);
        ushort maxCount = ReadUInt16(reader, index++);
        ushort stackable = ReadUInt16(reader, index++);
        byte containerSlots = ReadByte(reader, index++);

        List<ItemTemplateStatRecord> stats = [];
        for (int statIndex = 0; statIndex < 10; statIndex++)
        {
            stats.Add(new ItemTemplateStatRecord(ReadByte(reader, index++), ReadInt32(reader, index++)));
        }

        List<ItemTemplateDamageRecord> damages = [];
        for (int damageIndex = 0; damageIndex < 5; damageIndex++)
        {
            damages.Add(new ItemTemplateDamageRecord(ReadSingle(reader, index++), ReadSingle(reader, index++), ReadByte(reader, index++)));
        }

        ushort armor = ReadUInt16(reader, index++);
        byte holyResistance = ReadByte(reader, index++);
        byte fireResistance = ReadByte(reader, index++);
        byte natureResistance = ReadByte(reader, index++);
        byte frostResistance = ReadByte(reader, index++);
        byte shadowResistance = ReadByte(reader, index++);
        byte arcaneResistance = ReadByte(reader, index++);
        ushort delay = ReadUInt16(reader, index++);
        byte ammoType = ReadByte(reader, index++);
        float rangedModRange = ReadSingle(reader, index++);

        List<ItemTemplateSpellRecord> spells = [];
        for (int spellIndex = 0; spellIndex < 5; spellIndex++)
        {
            spells.Add(new ItemTemplateSpellRecord(
                ReadUInt32(reader, index++),
                ReadByte(reader, index++),
                ReadInt32(reader, index++),
                ReadSingle(reader, index++),
                ReadInt32(reader, index++),
                ReadUInt16(reader, index++),
                ReadInt32(reader, index++)));
        }

        return new ItemTemplateRecord(
            entry,
            itemClass,
            subClass,
            name,
            displayId,
            quality,
            flags,
            buyCount,
            buyPrice,
            sellPrice,
            inventoryType,
            allowableClass,
            allowableRace,
            itemLevel,
            requiredLevel,
            requiredSkill,
            requiredSkillRank,
            requiredSpell,
            requiredHonorRank,
            requiredCityRank,
            requiredReputationFaction,
            requiredReputationRank,
            maxCount,
            stackable,
            containerSlots,
            stats,
            damages,
            armor,
            holyResistance,
            fireResistance,
            natureResistance,
            frostResistance,
            shadowResistance,
            arcaneResistance,
            delay,
            ammoType,
            rangedModRange,
            spells,
            ReadByte(reader, index++),
            ReadString(reader, index++),
            ReadUInt32(reader, index++),
            ReadByte(reader, index++),
            ReadByte(reader, index++),
            ReadUInt32(reader, index++),
            ReadUInt32(reader, index++),
            ReadSByte(reader, index++),
            ReadByte(reader, index++),
            ReadUInt32(reader, index++),
            ReadUInt32(reader, index++),
            ReadUInt32(reader, index++),
            ReadUInt32(reader, index++),
            ReadUInt32(reader, index++),
            ReadInt32(reader, index++),
            ReadInt32(reader, index++),
            ReadUInt32(reader, index++),
            ReadByte(reader, index++),
            ReadUInt32(reader, index++),
            ReadUInt32(reader, index++),
            ReadUInt32(reader, index++),
            ReadByte(reader, index++));
    }

    private static string ReadString(MySqlDataReader reader, int index)
    {
        return reader.IsDBNull(index) ? string.Empty : reader.GetString(index);
    }

    private static byte ReadByte(MySqlDataReader reader, int index)
    {
        return Convert.ToByte(reader.GetValue(index), CultureInfo.InvariantCulture);
    }

    private static sbyte ReadSByte(MySqlDataReader reader, int index)
    {
        return Convert.ToSByte(reader.GetValue(index), CultureInfo.InvariantCulture);
    }

    private static ushort ReadUInt16(MySqlDataReader reader, int index)
    {
        return Convert.ToUInt16(reader.GetValue(index), CultureInfo.InvariantCulture);
    }

    private static uint ReadUInt32(MySqlDataReader reader, int index)
    {
        return Convert.ToUInt32(reader.GetValue(index), CultureInfo.InvariantCulture);
    }

    private static int ReadInt32(MySqlDataReader reader, int index)
    {
        return Convert.ToInt32(reader.GetValue(index), CultureInfo.InvariantCulture);
    }

    private static short ReadInt16(MySqlDataReader reader, int index)
    {
        return Convert.ToInt16(reader.GetValue(index), CultureInfo.InvariantCulture);
    }

    private static float ReadSingle(MySqlDataReader reader, int index)
    {
        return Convert.ToSingle(reader.GetValue(index), CultureInfo.InvariantCulture);
    }

    /**
      * Checks whether a table column exists before optional migration fields are selected.
      */
    private static async Task<bool> ColumnExistsAsync(MySqlConnection connection, string tableName, string columnName, CancellationToken cancellationToken)
    {
        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT 1
            FROM `information_schema`.`COLUMNS`
            WHERE `TABLE_SCHEMA` = DATABASE()
              AND `TABLE_NAME` = @tableName
              AND `COLUMN_NAME` = @columnName
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@tableName", tableName);
        command.Parameters.AddWithValue("@columnName", columnName);

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    /**
      * Performs the table exists operation for the world database template loading and cache construction workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: connection, tableName, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private static async Task<bool> TableExistsAsync(MySqlConnection connection, string tableName, CancellationToken cancellationToken)
    {
        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT 1
            FROM `information_schema`.`TABLES`
            WHERE `TABLE_SCHEMA` = DATABASE()
              AND `TABLE_NAME` = @tableName
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@tableName", tableName);

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }
}
