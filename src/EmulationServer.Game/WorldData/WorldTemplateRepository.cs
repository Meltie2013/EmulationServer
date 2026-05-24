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
      * Loads load information from configuration, files, or persistent storage.
      * The method normalizes external input before returning it so the rest of the server can work with validated, strongly typed data.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
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

        return new WorldTemplateDataStore(
            playerCreateInfo,
            itemTemplates,
            playerLevelStats,
            playerClassLevelStats,
            playerLevelExperience,
            playerCreateActions,
            playerCreateItems,
            playerCreateSpells);
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
      * Loads load item templates information from configuration, files, or persistent storage.
      * The method normalizes external input before returning it so the rest of the server can work with validated, strongly typed data.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async Task<IReadOnlyList<ItemTemplateRecord>> LoadItemTemplatesAsync(CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT `entry`, `class`, `subclass`, `name`, `displayid`, `Flags`, `InventoryType`, `MaxDurability`
            FROM `item_template`;
            """;

        List<ItemTemplateRecord> records = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new ItemTemplateRecord(
                Convert.ToUInt32(reader.GetValue(0), CultureInfo.InvariantCulture),
                Convert.ToByte(reader.GetValue(1), CultureInfo.InvariantCulture),
                Convert.ToByte(reader.GetValue(2), CultureInfo.InvariantCulture),
                reader.GetString(3),
                Convert.ToUInt32(reader.GetValue(4), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(5), CultureInfo.InvariantCulture),
                Convert.ToByte(reader.GetValue(6), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(7), CultureInfo.InvariantCulture)));
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
            SELECT `entry`, `class`, `subclass`, `name`, `displayid`, `Flags`, `InventoryType`, `MaxDurability`
            FROM `item_template`
            WHERE `entry` IN ({string.Join(',', parameterNames)});
            """;

        Dictionary<uint, ItemTemplateRecord> result = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            ItemTemplateRecord record = new(
                Convert.ToUInt32(reader.GetValue(0), CultureInfo.InvariantCulture),
                Convert.ToByte(reader.GetValue(1), CultureInfo.InvariantCulture),
                Convert.ToByte(reader.GetValue(2), CultureInfo.InvariantCulture),
                reader.GetString(3),
                Convert.ToUInt32(reader.GetValue(4), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(5), CultureInfo.InvariantCulture),
                Convert.ToByte(reader.GetValue(6), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(7), CultureInfo.InvariantCulture));

            result[record.Entry] = record;
        }

        return result;
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
