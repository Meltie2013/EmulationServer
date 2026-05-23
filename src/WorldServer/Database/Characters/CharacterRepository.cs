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
using EmulationServer.Game.Characters;
using EmulationServer.Game.Players;
using EmulationServer.Game.WorldData;

using MySqlConnector;

namespace EmulationServer.WorldServer.Database.Characters;

public sealed class CharacterRepository
{
    private const int CharacterEquipmentSlotCount = 19;
    private const uint AtLoginFirst = 0x20;
    private const int NoEquipmentSlot = -1;
    private readonly IDatabaseService _databaseService;
    private readonly Func<uint, ItemTemplateRecord?> _itemTemplateAccessor;

    public CharacterRepository(IDatabaseService databaseService, Func<uint, ItemTemplateRecord?> itemTemplateAccessor)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _itemTemplateAccessor = itemTemplateAccessor ?? throw new ArgumentNullException(nameof(itemTemplateAccessor));
    }

    public async Task<IReadOnlyList<CharacterListEntry>> GetCharactersForAccountAsync(uint accountId, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT `guid`, `name`, `race`, `class`, `gender`,
                   `playerBytes`, `playerBytes2`, `level`, `xp`, `zone`,
                   `map`, `position_x`, `position_y`, `position_z`,
                   `playerFlags`, `at_login`, `equipmentCache`
            FROM `characters`
            WHERE `account` = @account
            ORDER BY `guid`;
            """;
        command.Parameters.AddWithValue("@account", accountId);

        List<CharacterListRow> rows = [];
        await using (MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new CharacterListRow(
                    Convert.ToUInt32(reader.GetValue(0), CultureInfo.InvariantCulture),
                    reader.GetString(1),
                    Convert.ToByte(reader.GetValue(2), CultureInfo.InvariantCulture),
                    Convert.ToByte(reader.GetValue(3), CultureInfo.InvariantCulture),
                    Convert.ToByte(reader.GetValue(4), CultureInfo.InvariantCulture),
                    Convert.ToByte(reader.GetValue(7), CultureInfo.InvariantCulture),
                    Convert.ToUInt32(reader.GetValue(8), CultureInfo.InvariantCulture),
                    Convert.ToUInt32(reader.GetValue(9), CultureInfo.InvariantCulture),
                    Convert.ToUInt32(reader.GetValue(10), CultureInfo.InvariantCulture),
                    Convert.ToSingle(reader.GetValue(11), CultureInfo.InvariantCulture),
                    Convert.ToSingle(reader.GetValue(12), CultureInfo.InvariantCulture),
                    Convert.ToSingle(reader.GetValue(13), CultureInfo.InvariantCulture),
                    Convert.ToUInt32(reader.GetValue(14), CultureInfo.InvariantCulture),
                    Convert.ToUInt32(reader.GetValue(15), CultureInfo.InvariantCulture),
                    Convert.ToUInt32(reader.GetValue(5), CultureInfo.InvariantCulture),
                    Convert.ToUInt32(reader.GetValue(6), CultureInfo.InvariantCulture),
                    reader.IsDBNull(16) ? string.Empty : reader.GetString(16)));
            }
        }

        Dictionary<uint, IReadOnlyList<CharacterEquipmentDisplay>> equippedInventory =
            await LoadEquippedInventoryAsync(connection, rows.Select(row => row.Guid), cancellationToken);

        List<CharacterListEntry> result = [];
        foreach (CharacterListRow row in rows)
        {
            IReadOnlyList<CharacterEquipmentDisplay> cachedEquipment = ParseEquipmentCache(row.EquipmentCache, _itemTemplateAccessor);
            IReadOnlyList<CharacterEquipmentDisplay> equipment = equippedInventory.TryGetValue(row.Guid, out IReadOnlyList<CharacterEquipmentDisplay>? inventoryEquipment)
                ? MergeEquipment(cachedEquipment, inventoryEquipment)
                : cachedEquipment;

            result.Add(new CharacterListEntry(
                row.Guid,
                row.Name,
                row.Race,
                row.Class,
                row.Gender,
                row.Level,
                row.Zone,
                row.Map,
                row.PositionX,
                row.PositionY,
                row.PositionZ,
                0,
                row.PlayerFlags,
                row.AtLogin,
                row.PlayerBytes,
                row.PlayerBytes2,
                equipment));
        }

        return result;
    }

    public async Task<bool> CharacterNameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT 1
            FROM `characters`
            WHERE LOWER(`name`) = LOWER(@name)
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@name", name);

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    public async Task<IReadOnlyDictionary<uint, byte>> GetCharacterCountsByAccountAsync(CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT `account`, COUNT(*) AS `character_count`
            FROM `characters`
            GROUP BY `account`;
            """;

        Dictionary<uint, byte> characterCounts = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            uint accountId = Convert.ToUInt32(reader.GetValue(0), CultureInfo.InvariantCulture);
            int count = Convert.ToInt32(reader.GetValue(1), CultureInfo.InvariantCulture);
            characterCounts[accountId] = (byte)Math.Clamp(count, 0, byte.MaxValue);
        }

        return characterCounts;
    }

    public async Task<int> CountCharactersForAccountAsync(uint accountId, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

        command.CommandText = "SELECT COUNT(*) FROM `characters` WHERE `account` = @account;";
        command.Parameters.AddWithValue("@account", accountId);

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    public async Task<uint> CreateCharacterAsync(
        uint accountId,
        CharacterCreateRequest request,
        PlayerCreateInfoRecord createInfo,
        IReadOnlyList<StarterItemCreateData> starterItems,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(createInfo);
        ArgumentNullException.ThrowIfNull(starterItems);

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            uint characterGuid = await GetNextIdAsync(connection, transaction, "characters", "guid", cancellationToken);
            uint nextItemGuid = await GetNextIdAsync(connection, transaction, "item_instance", "guid", cancellationToken);
            string equipmentCache = BuildEquipmentCache(starterItems);
            uint playerBytes = PackPlayerBytes(request.Skin, request.Face, request.HairStyle, request.HairColor);
            uint playerBytes2 = PackPlayerBytes2(request.FacialHair);
            PlayerStats initialStats = CreateInitialStats(request.Class, 1);

            await InsertCharacterAsync(connection, transaction, characterGuid, accountId, request, createInfo, playerBytes, playerBytes2, equipmentCache, initialStats, cancellationToken);
            await InsertHomebindAsync(connection, transaction, characterGuid, createInfo, cancellationToken);

            foreach (StarterItemCreateData item in starterItems)
            {
                if (item.Template.Entry == 0)
                {
                    continue;
                }

                uint itemGuid = nextItemGuid++;
                await InsertItemInstanceAsync(connection, transaction, itemGuid, characterGuid, item.Template, cancellationToken);
                await InsertCharacterInventoryAsync(connection, transaction, characterGuid, itemGuid, item.Template.Entry, item.StorageSlot, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return characterGuid;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<CharacterDeleteRepositoryResult> DeleteCharacterAsync(
        uint accountId,
        uint characterGuid,
        CancellationToken cancellationToken = default)
    {
        if (characterGuid == 0)
        {
            return CharacterDeleteRepositoryResult.NotFound;
        }

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            CharacterOwnershipRecord? character = await LoadCharacterOwnershipForUpdateAsync(connection, transaction, characterGuid, cancellationToken);
            if (character is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return CharacterDeleteRepositoryResult.NotFound;
            }

            if (character.AccountId != accountId)
            {
                await transaction.RollbackAsync(cancellationToken);
                return CharacterDeleteRepositoryResult.AccountMismatch;
            }

            if (character.Online)
            {
                await transaction.RollbackAsync(cancellationToken);
                return CharacterDeleteRepositoryResult.Online;
            }

            if (await IsGuildLeaderAsync(connection, transaction, characterGuid, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return CharacterDeleteRepositoryResult.GuildLeader;
            }

            await DeleteCharacterRowsAsync(connection, transaction, characterGuid, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return CharacterDeleteRepositoryResult.Success;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }


    public async Task<PlayerLoginRecord?> GetPlayerForLoginAsync(
        uint accountId,
        uint characterGuid,
        Func<byte, PlayerFaction> factionResolver,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factionResolver);

        if (characterGuid == 0)
        {
            return null;
        }

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT `guid`, `account`, `name`, `race`, `class`, `gender`, `level`, `xp`, `zone`, `map`,
                   `position_x`, `position_y`, `position_z`, `orientation`, `money`, `playerBytes`,
                   `playerBytes2`, `playerFlags`, `at_login`, `cinematic`, `totaltime`, `leveltime`,
                   `health`, `power1`, `power2`, `power3`, `power4`, `power5`
            FROM `characters`
            WHERE `guid` = @guid
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@guid", characterGuid);

        CharacterLoginRow? row = null;
        await using (MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                row = new CharacterLoginRow(
                    Convert.ToUInt32(reader.GetValue(0), CultureInfo.InvariantCulture),
                    Convert.ToUInt32(reader.GetValue(1), CultureInfo.InvariantCulture),
                    reader.GetString(2),
                    Convert.ToByte(reader.GetValue(3), CultureInfo.InvariantCulture),
                    Convert.ToByte(reader.GetValue(4), CultureInfo.InvariantCulture),
                    Convert.ToByte(reader.GetValue(5), CultureInfo.InvariantCulture),
                    Convert.ToByte(reader.GetValue(6), CultureInfo.InvariantCulture),
                    Convert.ToUInt32(reader.GetValue(7), CultureInfo.InvariantCulture),
                    Convert.ToUInt32(reader.GetValue(8), CultureInfo.InvariantCulture),
                    Convert.ToUInt32(reader.GetValue(9), CultureInfo.InvariantCulture),
                    Convert.ToSingle(reader.GetValue(10), CultureInfo.InvariantCulture),
                    Convert.ToSingle(reader.GetValue(11), CultureInfo.InvariantCulture),
                    Convert.ToSingle(reader.GetValue(12), CultureInfo.InvariantCulture),
                    Convert.ToSingle(reader.GetValue(13), CultureInfo.InvariantCulture),
                    Convert.ToUInt32(reader.GetValue(14), CultureInfo.InvariantCulture),
                    Convert.ToUInt32(reader.GetValue(15), CultureInfo.InvariantCulture),
                    Convert.ToUInt32(reader.GetValue(16), CultureInfo.InvariantCulture),
                    Convert.ToUInt32(reader.GetValue(17), CultureInfo.InvariantCulture),
                    Convert.ToUInt32(reader.GetValue(18), CultureInfo.InvariantCulture),
                    Convert.ToByte(reader.GetValue(19), CultureInfo.InvariantCulture),
                    Convert.ToUInt32(reader.GetValue(20), CultureInfo.InvariantCulture),
                    Convert.ToUInt32(reader.GetValue(21), CultureInfo.InvariantCulture),
                    new PlayerStats(
                        Convert.ToUInt32(reader.GetValue(22), CultureInfo.InvariantCulture),
                        Convert.ToUInt32(reader.GetValue(23), CultureInfo.InvariantCulture),
                        Convert.ToUInt32(reader.GetValue(24), CultureInfo.InvariantCulture),
                        Convert.ToUInt32(reader.GetValue(25), CultureInfo.InvariantCulture),
                        Convert.ToUInt32(reader.GetValue(26), CultureInfo.InvariantCulture),
                        Convert.ToUInt32(reader.GetValue(27), CultureInfo.InvariantCulture),
                        0,
                        0,
                        0,
                        0,
                        0,
                        0));
            }
        }

        if (row is null || row.AccountId != accountId)
        {
            return null;
        }

        IReadOnlyList<PlayerInventoryItem> inventory = await LoadPlayerInventoryAsync(connection, row.Guid, cancellationToken);

        return new PlayerLoginRecord(
            row.Guid,
            row.AccountId,
            row.Name,
            row.Race,
            row.Class,
            row.Gender,
            NormalizeLevel(row.Level),
            row.Xp,
            row.Zone,
            row.Map,
            row.PositionX,
            row.PositionY,
            row.PositionZ,
            row.Orientation,
            row.Money,
            row.PlayerBytes,
            row.PlayerBytes2,
            row.PlayerFlags,
            row.AtLogin,
            row.Cinematic,
            row.TotalTime,
            row.LevelTime,
            NormalizePlayerStats(row.Class, NormalizeLevel(row.Level), row.Stats),
            inventory,
            factionResolver(row.Race));
    }

    public async Task<CharacterNameQueryResult?> GetCharacterNameQueryAsync(uint characterGuid, CancellationToken cancellationToken = default)
    {
        if (characterGuid == 0)
        {
            return null;
        }

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT `guid`, `name`, `race`, `gender`, `class`
            FROM `characters`
            WHERE `guid` = @guid
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@guid", characterGuid);

        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CharacterNameQueryResult(
            Convert.ToUInt32(reader.GetValue(0), CultureInfo.InvariantCulture),
            reader.GetString(1),
            Convert.ToByte(reader.GetValue(2), CultureInfo.InvariantCulture),
            Convert.ToByte(reader.GetValue(3), CultureInfo.InvariantCulture),
            Convert.ToByte(reader.GetValue(4), CultureInfo.InvariantCulture));
    }

    public async Task SetCharacterOnlineAsync(uint characterGuid, bool online, CancellationToken cancellationToken = default)
    {
        if (characterGuid == 0)
        {
            return;
        }

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            UPDATE `characters`
            SET `online` = @online,
                `logout_time` = CASE WHEN @online = 0 THEN @logoutTime ELSE `logout_time` END
            WHERE `guid` = @guid;
            """;
        command.Parameters.AddWithValue("@online", online ? 1 : 0);
        command.Parameters.AddWithValue("@logoutTime", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        command.Parameters.AddWithValue("@guid", characterGuid);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<IReadOnlyList<PlayerInventoryItem>> LoadPlayerInventoryAsync(
        MySqlConnection connection,
        uint characterGuid,
        CancellationToken cancellationToken)
    {
        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT `ci`.`item`, `ci`.`guid`, `ci`.`item_template`, `ci`.`bag`, `ci`.`slot`, COALESCE(`ii`.`data`, '')
            FROM `character_inventory` `ci`
            LEFT JOIN `item_instance` `ii` ON `ii`.`guid` = `ci`.`item`
            WHERE `ci`.`guid` = @guid
            ORDER BY `ci`.`bag`, `ci`.`slot`;
            """;
        command.Parameters.AddWithValue("@guid", characterGuid);

        List<PlayerInventoryItem> items = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new PlayerInventoryItem(
                Convert.ToUInt32(reader.GetValue(0), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(1), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(2), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(3), CultureInfo.InvariantCulture),
                Convert.ToByte(reader.GetValue(4), CultureInfo.InvariantCulture),
                reader.GetString(5)));
        }

        return items;
    }

    private static async Task<uint> GetNextIdAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        using MySqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT COALESCE(MAX(`{columnName}`), 0) + 1 FROM `{tableName}`;";
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToUInt32(result, CultureInfo.InvariantCulture);
    }

    private static async Task InsertCharacterAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        uint characterGuid,
        uint accountId,
        CharacterCreateRequest request,
        PlayerCreateInfoRecord createInfo,
        uint playerBytes,
        uint playerBytes2,
        string equipmentCache,
        PlayerStats initialStats,
        CancellationToken cancellationToken)
    {
        using MySqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO `characters`
                (`guid`, `account`, `name`, `race`, `class`, `gender`, `level`, `xp`, `money`,
                 `playerBytes`, `playerBytes2`, `playerFlags`, `position_x`, `position_y`,
                 `position_z`, `map`, `orientation`, `taximask`, `online`, `cinematic`,
                 `at_login`, `zone`, `equipmentCache`, `health`, `power1`, `power2`, `power3`,
                 `power4`, `power5`, `createdDate`)
            VALUES
                (@guid, @account, @name, @race, @class, @gender, 1, 0, 0,
                 @playerBytes, @playerBytes2, 0, @x, @y,
                 @z, @map, @o, '', 0, 0,
                 @atLogin, @zone, @equipmentCache, @health, @power1, @power2, @power3,
                 @power4, @power5, @createdDate);
            """;
        command.Parameters.AddWithValue("@guid", characterGuid);
        command.Parameters.AddWithValue("@account", accountId);
        command.Parameters.AddWithValue("@name", request.Name);
        command.Parameters.AddWithValue("@race", request.Race);
        command.Parameters.AddWithValue("@class", request.Class);
        command.Parameters.AddWithValue("@gender", request.Gender);
        command.Parameters.AddWithValue("@playerBytes", playerBytes);
        command.Parameters.AddWithValue("@playerBytes2", playerBytes2);
        command.Parameters.AddWithValue("@map", createInfo.Map);
        command.Parameters.AddWithValue("@zone", createInfo.Zone);
        command.Parameters.AddWithValue("@x", createInfo.PositionX);
        command.Parameters.AddWithValue("@y", createInfo.PositionY);
        command.Parameters.AddWithValue("@z", createInfo.PositionZ);
        command.Parameters.AddWithValue("@o", createInfo.Orientation);
        command.Parameters.AddWithValue("@equipmentCache", equipmentCache);
        command.Parameters.AddWithValue("@health", initialStats.Health);
        command.Parameters.AddWithValue("@power1", initialStats.Power1);
        command.Parameters.AddWithValue("@power2", initialStats.Power2);
        command.Parameters.AddWithValue("@power3", initialStats.Power3);
        command.Parameters.AddWithValue("@power4", initialStats.Power4);
        command.Parameters.AddWithValue("@power5", initialStats.Power5);
        command.Parameters.AddWithValue("@atLogin", AtLoginFirst);
        command.Parameters.AddWithValue("@createdDate", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertHomebindAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        uint characterGuid,
        PlayerCreateInfoRecord createInfo,
        CancellationToken cancellationToken)
    {
        using MySqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO `character_homebind`
                (`guid`, `map`, `zone`, `position_x`, `position_y`, `position_z`)
            VALUES
                (@guid, @map, @zone, @x, @y, @z);
            """;
        command.Parameters.AddWithValue("@guid", characterGuid);
        command.Parameters.AddWithValue("@map", createInfo.Map);
        command.Parameters.AddWithValue("@zone", createInfo.Zone);
        command.Parameters.AddWithValue("@x", createInfo.PositionX);
        command.Parameters.AddWithValue("@y", createInfo.PositionY);
        command.Parameters.AddWithValue("@z", createInfo.PositionZ);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertItemInstanceAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        uint itemGuid,
        uint ownerGuid,
        ItemTemplateRecord itemTemplate,
        CancellationToken cancellationToken)
    {
        using MySqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO `item_instance`
                (`guid`, `owner_guid`, `data`, `text`)
            VALUES
                (@guid, @ownerGuid, @data, NULL);
            """;
        command.Parameters.AddWithValue("@guid", itemGuid);
        command.Parameters.AddWithValue("@ownerGuid", ownerGuid);
        command.Parameters.AddWithValue("@data", BuildItemInstanceData(itemGuid, ownerGuid, itemTemplate));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertCharacterInventoryAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        uint characterGuid,
        uint itemGuid,
        uint itemTemplate,
        byte storageSlot,
        CancellationToken cancellationToken)
    {
        using MySqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO `character_inventory`
                (`guid`, `bag`, `slot`, `item`, `item_template`)
            VALUES
                (@guid, 0, @slot, @item, @itemTemplate);
            """;
        command.Parameters.AddWithValue("@guid", characterGuid);
        command.Parameters.AddWithValue("@slot", storageSlot);
        command.Parameters.AddWithValue("@item", itemGuid);
        command.Parameters.AddWithValue("@itemTemplate", itemTemplate);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<CharacterOwnershipRecord?> LoadCharacterOwnershipForUpdateAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        uint characterGuid,
        CancellationToken cancellationToken)
    {
        using MySqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT `account`, `name`, `online`
            FROM `characters`
            WHERE `guid` = @guid
            LIMIT 1
            FOR UPDATE;
            """;
        command.Parameters.AddWithValue("@guid", characterGuid);

        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CharacterOwnershipRecord(
            Convert.ToUInt32(reader.GetValue(0), CultureInfo.InvariantCulture),
            reader.GetString(1),
            Convert.ToByte(reader.GetValue(2), CultureInfo.InvariantCulture) != 0);
    }

    private static async Task<bool> IsGuildLeaderAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        uint characterGuid,
        CancellationToken cancellationToken)
    {
        if (!await TableColumnExistsAsync(connection, transaction, "guild", "leaderguid", cancellationToken))
        {
            return false;
        }

        using MySqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT 1
            FROM `guild`
            WHERE `leaderguid` = @guid
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@guid", characterGuid);

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    private static async Task DeleteCharacterRowsAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        uint characterGuid,
        CancellationToken cancellationToken)
    {
        // Delete optional MaNGOS character-side tables first when the full
        // character schema is installed. The four current milestone tables are
        // always included below, but optional tables are guarded so the minimal
        // schema can still be used while the project is being built out.
        await DeleteWhereColumnEqualsAsync(connection, transaction, "character_action", "guid", characterGuid, cancellationToken);
        await DeleteWhereColumnEqualsAsync(connection, transaction, "character_aura", "guid", characterGuid, cancellationToken);
        await DeleteWhereColumnEqualsAsync(connection, transaction, "character_battleground_data", "guid", characterGuid, cancellationToken);
        await DeleteWhereColumnEqualsAsync(connection, transaction, "character_gifts", "guid", characterGuid, cancellationToken);
        await DeleteWhereColumnEqualsAsync(connection, transaction, "character_honor_cp", "guid", characterGuid, cancellationToken);
        await DeleteWhereColumnEqualsAsync(connection, transaction, "character_instance", "guid", characterGuid, cancellationToken);
        await DeleteWhereColumnEqualsAsync(connection, transaction, "character_pet", "owner", characterGuid, cancellationToken);
        await DeleteWhereColumnEqualsAsync(connection, transaction, "character_queststatus", "guid", characterGuid, cancellationToken);
        await DeleteWhereColumnEqualsAsync(connection, transaction, "character_reputation", "guid", characterGuid, cancellationToken);
        await DeleteWhereColumnEqualsAsync(connection, transaction, "character_skills", "guid", characterGuid, cancellationToken);
        await DeleteWhereColumnEqualsAsync(connection, transaction, "character_social", "guid", characterGuid, cancellationToken);
        await DeleteWhereColumnEqualsAsync(connection, transaction, "character_social", "friend", characterGuid, cancellationToken);
        await DeleteWhereColumnEqualsAsync(connection, transaction, "character_spell", "guid", characterGuid, cancellationToken);
        await DeleteWhereColumnEqualsAsync(connection, transaction, "character_spell_cooldown", "guid", characterGuid, cancellationToken);
        await DeleteWhereColumnEqualsAsync(connection, transaction, "corpse", "player", characterGuid, cancellationToken);
        await DeleteWhereColumnEqualsAsync(connection, transaction, "group_member", "memberGuid", characterGuid, cancellationToken);
        await DeleteWhereColumnEqualsAsync(connection, transaction, "guild_member", "guid", characterGuid, cancellationToken);
        await DeleteWhereColumnEqualsAsync(connection, transaction, "item_loot", "owner_guid", characterGuid, cancellationToken);
        await DeleteWhereColumnEqualsAsync(connection, transaction, "mail_items", "receiver", characterGuid, cancellationToken);
        await DeleteWhereColumnEqualsAsync(connection, transaction, "mail", "receiver", characterGuid, cancellationToken);

        await DeleteWhereColumnEqualsAsync(connection, transaction, "character_inventory", "guid", characterGuid, cancellationToken);
        await DeleteWhereColumnEqualsAsync(connection, transaction, "item_instance", "owner_guid", characterGuid, cancellationToken);
        await DeleteWhereColumnEqualsAsync(connection, transaction, "character_homebind", "guid", characterGuid, cancellationToken);
        await DeleteWhereColumnEqualsAsync(connection, transaction, "characters", "guid", characterGuid, cancellationToken);
    }

    private static async Task DeleteWhereColumnEqualsAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string tableName,
        string columnName,
        uint value,
        CancellationToken cancellationToken)
    {
        if (!await TableColumnExistsAsync(connection, transaction, tableName, columnName, cancellationToken))
        {
            return;
        }

        using MySqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"DELETE FROM `{tableName}` WHERE `{columnName}` = @value;";
        command.Parameters.AddWithValue("@value", value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> TableColumnExistsAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        using MySqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
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

    private async Task<Dictionary<uint, IReadOnlyList<CharacterEquipmentDisplay>>> LoadEquippedInventoryAsync(
        MySqlConnection connection,
        IEnumerable<uint> characterGuids,
        CancellationToken cancellationToken)
    {
        uint[] guids = characterGuids.Distinct().ToArray();
        Dictionary<uint, IReadOnlyList<CharacterEquipmentDisplay>> result = [];
        if (guids.Length == 0)
        {
            return result;
        }

        using MySqlCommand command = connection.CreateCommand();
        List<string> parameterNames = [];
        for (int index = 0; index < guids.Length; index++)
        {
            string parameterName = $"@guid{index}";
            parameterNames.Add(parameterName);
            command.Parameters.AddWithValue(parameterName, guids[index]);
        }

        command.CommandText = $"""
            SELECT `guid`, `slot`, `item_template`
            FROM `character_inventory`
            WHERE `guid` IN ({string.Join(',', parameterNames)})
              AND `bag` = 0
            ORDER BY `guid`, `slot`;
            """;

        Dictionary<uint, CharacterEquipmentDisplay[]> equipmentByCharacter = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            uint characterGuid = Convert.ToUInt32(reader.GetValue(0), CultureInfo.InvariantCulture);
            byte storedSlot = Convert.ToByte(reader.GetValue(1), CultureInfo.InvariantCulture);
            uint itemEntry = Convert.ToUInt32(reader.GetValue(2), CultureInfo.InvariantCulture);

            ItemTemplateRecord? itemTemplate = _itemTemplateAccessor(itemEntry);
            if (itemTemplate is null)
            {
                continue;
            }

            int equipmentSlot = MapInventoryTypeToEquipmentSlot(itemTemplate.InventoryType);
            if (equipmentSlot == NoEquipmentSlot && storedSlot < CharacterEquipmentSlotCount)
            {
                equipmentSlot = storedSlot;
            }

            if (equipmentSlot < 0 || equipmentSlot >= CharacterEquipmentSlotCount)
            {
                continue;
            }

            if (!equipmentByCharacter.TryGetValue(characterGuid, out CharacterEquipmentDisplay[]? equipment))
            {
                equipment = CreateEmptyEquipmentArray();
                equipmentByCharacter[characterGuid] = equipment;
            }

            equipment[equipmentSlot] = new CharacterEquipmentDisplay(itemTemplate.DisplayId, itemTemplate.InventoryType, 0);
        }

        foreach ((uint characterGuid, CharacterEquipmentDisplay[] equipment) in equipmentByCharacter)
        {
            result[characterGuid] = equipment;
        }

        return result;
    }

    private static IReadOnlyList<CharacterEquipmentDisplay> MergeEquipment(
        IReadOnlyList<CharacterEquipmentDisplay> cachedEquipment,
        IReadOnlyList<CharacterEquipmentDisplay> inventoryEquipment)
    {
        CharacterEquipmentDisplay[] merged = CreateEmptyEquipmentArray();

        for (int slot = 0; slot < CharacterEquipmentSlotCount; slot++)
        {
            CharacterEquipmentDisplay cached = slot < cachedEquipment.Count
                ? cachedEquipment[slot]
                : new CharacterEquipmentDisplay(0, 0, 0);

            CharacterEquipmentDisplay inventory = slot < inventoryEquipment.Count
                ? inventoryEquipment[slot]
                : new CharacterEquipmentDisplay(0, 0, 0);

            merged[slot] = inventory.DisplayId != 0 || inventory.InventoryType != 0
                ? inventory
                : cached;
        }

        return merged;
    }

    private static CharacterEquipmentDisplay[] CreateEmptyEquipmentArray()
    {
        return Enumerable
            .Range(0, CharacterEquipmentSlotCount)
            .Select(_ => new CharacterEquipmentDisplay(0, 0, 0))
            .ToArray();
    }

    private static int MapInventoryTypeToEquipmentSlot(byte inventoryType)
    {
        return inventoryType switch
        {
            1 => 0,
            2 => 1,
            3 => 2,
            4 => 3,
            5 => 4,
            6 => 5,
            7 => 6,
            8 => 7,
            9 => 8,
            10 => 9,
            11 => 10,
            12 => 12,
            13 => 15,
            14 => 16,
            15 => 17,
            16 => 14,
            17 => 15,
            19 => 18,
            20 => 4,
            21 => 15,
            22 => 16,
            23 => 16,
            25 => 17,
            26 => 17,
            28 => 17,
            _ => NoEquipmentSlot,
        };
    }

    private static string BuildItemInstanceData(uint itemGuid, uint ownerGuid, ItemTemplateRecord itemTemplate)
    {
        uint[] fields = new uint[48];
        fields[0] = itemGuid;
        fields[2] = 3;
        fields[3] = itemTemplate.Entry;
        fields[4] = 1;
        fields[6] = ownerGuid;
        fields[8] = ownerGuid;
        fields[14] = 1;
        fields[21] = itemTemplate.Flags;
        fields[46] = itemTemplate.MaxDurability;
        fields[47] = itemTemplate.MaxDurability;

        return string.Join(' ', fields.Select(value => value.ToString(CultureInfo.InvariantCulture)));
    }

    private static uint PackPlayerBytes(byte skin, byte face, byte hairStyle, byte hairColor)
    {
        return (uint)(skin | (face << 8) | (hairStyle << 16) | (hairColor << 24));
    }

    private static uint PackPlayerBytes2(byte facialHair)
    {
        return facialHair;
    }

    private static string BuildEquipmentCache(IReadOnlyList<StarterItemCreateData> starterItems)
    {
        uint[] itemEntries = new uint[CharacterEquipmentSlotCount];
        uint[] enchantments = new uint[CharacterEquipmentSlotCount];

        foreach (StarterItemCreateData starterItem in starterItems)
        {
            if (starterItem.EquipmentSlot < 0 || starterItem.EquipmentSlot >= CharacterEquipmentSlotCount)
            {
                continue;
            }

            itemEntries[starterItem.EquipmentSlot] = starterItem.Template.Entry;
            enchantments[starterItem.EquipmentSlot] = 0;
        }

        // MaNGOS stores equipmentCache as two uint values per equipment slot:
        // item entry and permanent enchantment id. The character-list packet then
        // resolves the item entry through item_template to send display/inventory type.
        return string.Join(' ', Enumerable.Range(0, CharacterEquipmentSlotCount).SelectMany(slot => new[]
        {
            itemEntries[slot].ToString(CultureInfo.InvariantCulture),
            enchantments[slot].ToString(CultureInfo.InvariantCulture),
        }));
    }

    private static IReadOnlyList<CharacterEquipmentDisplay> ParseEquipmentCache(
        string equipmentCache,
        Func<uint, ItemTemplateRecord?> itemTemplateAccessor)
    {
        string[] parts = equipmentCache.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Legacy layout from the earlier prototype: display id + inventory type + enchantment per slot.
        if (parts.Length >= CharacterEquipmentSlotCount * 3)
        {
            List<CharacterEquipmentDisplay> equipment = [];
            for (int slot = 0; slot < CharacterEquipmentSlotCount; slot++)
            {
                int baseIndex = slot * 3;
                uint displayId = ReadUInt(parts, baseIndex);
                byte inventoryType = (byte)Math.Min(byte.MaxValue, ReadUInt(parts, baseIndex + 1));
                uint enchantment = ReadUInt(parts, baseIndex + 2);
                equipment.Add(new CharacterEquipmentDisplay(displayId, inventoryType, enchantment));
            }

            return equipment;
        }

        // Current MaNGOS-compatible layout: item entry + enchantment per slot.
        if (parts.Length >= CharacterEquipmentSlotCount * 2)
        {
            List<CharacterEquipmentDisplay> equipment = [];
            for (int slot = 0; slot < CharacterEquipmentSlotCount; slot++)
            {
                int baseIndex = slot * 2;
                uint itemEntry = ReadUInt(parts, baseIndex);
                uint enchantment = ReadUInt(parts, baseIndex + 1);

                ItemTemplateRecord? itemTemplate = itemEntry == 0 ? null : itemTemplateAccessor(itemEntry);
                equipment.Add(itemTemplate is null
                    ? new CharacterEquipmentDisplay(0, 0, enchantment)
                    : new CharacterEquipmentDisplay(itemTemplate.DisplayId, itemTemplate.InventoryType, enchantment));
            }

            return equipment;
        }

        return CreateEmptyEquipmentArray();
    }

    private static uint ReadUInt(string[] parts, int index)
    {
        if (index < 0 || index >= parts.Length)
        {
            return 0;
        }

        return uint.TryParse(parts[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint value)
            ? value
            : 0;
    }


    private static PlayerStats NormalizePlayerStats(byte playerClass, byte level, PlayerStats storedStats)
    {
        PlayerStats defaults = CreateInitialStats(playerClass, level);
        return new PlayerStats(
            storedStats.Health == 0 ? defaults.Health : storedStats.Health,
            storedStats.Power1 == 0 ? defaults.Power1 : storedStats.Power1,
            storedStats.Power2 == 0 ? defaults.Power2 : storedStats.Power2,
            storedStats.Power3 == 0 ? defaults.Power3 : storedStats.Power3,
            storedStats.Power4 == 0 ? defaults.Power4 : storedStats.Power4,
            storedStats.Power5 == 0 ? defaults.Power5 : storedStats.Power5,
            storedStats.Strength == 0 ? defaults.Strength : storedStats.Strength,
            storedStats.Agility == 0 ? defaults.Agility : storedStats.Agility,
            storedStats.Stamina == 0 ? defaults.Stamina : storedStats.Stamina,
            storedStats.Intellect == 0 ? defaults.Intellect : storedStats.Intellect,
            storedStats.Spirit == 0 ? defaults.Spirit : storedStats.Spirit,
            storedStats.Armor == 0 ? defaults.Armor : storedStats.Armor);
    }

    private static PlayerStats CreateInitialStats(byte playerClass, byte level)
    {
        uint safeLevel = Math.Max((uint)level, 1u);
        uint health = 80 + (safeLevel * 20);
        uint mana = playerClass is 1 or 4 ? 0u : 100 + (safeLevel * 30);
        uint rage = 0;
        uint energy = playerClass == 4 ? 100u : 0u;
        (uint strength, uint agility, uint stamina, uint intellect, uint spirit) = ResolveBaseAttributes(playerClass);
        uint levelBonus = safeLevel - 1;
        strength += levelBonus;
        agility += levelBonus;
        stamina += levelBonus;
        intellect += playerClass is 1 or 4 ? 0u : levelBonus;
        spirit += levelBonus;
        uint armor = Math.Max(1u, agility * 2u);
        return new PlayerStats(health, mana, rage, 0, energy, 0, strength, agility, stamina, intellect, spirit, armor);
    }

    private static (uint Strength, uint Agility, uint Stamina, uint Intellect, uint Spirit) ResolveBaseAttributes(byte playerClass)
    {
        return playerClass switch
        {
            1 => (23u, 20u, 22u, 20u, 20u), // Warrior
            2 => (22u, 20u, 22u, 20u, 20u), // Paladin
            3 => (20u, 23u, 21u, 20u, 20u), // Hunter
            4 => (21u, 24u, 20u, 20u, 20u), // Rogue
            5 => (19u, 20u, 20u, 22u, 23u), // Priest
            7 => (21u, 20u, 21u, 21u, 21u), // Shaman
            8 => (19u, 20u, 19u, 24u, 22u), // Mage
            9 => (19u, 20u, 21u, 23u, 22u), // Warlock
            11 => (21u, 22u, 21u, 22u, 22u), // Druid
            _ => (20u, 20u, 20u, 20u, 20u),
        };
    }

    private static byte NormalizeLevel(byte level)
    {
        return level == 0 ? (byte)1 : level;
    }

    private sealed record CharacterLoginRow(
        uint Guid,
        uint AccountId,
        string Name,
        byte Race,
        byte Class,
        byte Gender,
        byte Level,
        uint Xp,
        uint Zone,
        uint Map,
        float PositionX,
        float PositionY,
        float PositionZ,
        float Orientation,
        uint Money,
        uint PlayerBytes,
        uint PlayerBytes2,
        uint PlayerFlags,
        uint AtLogin,
        byte Cinematic,
        uint TotalTime,
        uint LevelTime,
        PlayerStats Stats);

    private sealed record CharacterOwnershipRecord(uint AccountId, string Name, bool Online);

    private sealed record CharacterListRow(
        uint Guid,
        string Name,
        byte Race,
        byte Class,
        byte Gender,
        byte Level,
        uint Xp,
        uint Zone,
        uint Map,
        float PositionX,
        float PositionY,
        float PositionZ,
        uint PlayerFlags,
        uint AtLogin,
        uint PlayerBytes,
        uint PlayerBytes2,
        string EquipmentCache);
}
