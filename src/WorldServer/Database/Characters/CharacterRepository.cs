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

/**
  * File overview: src/WorldServer/Database/Characters/CharacterRepository.cs
  * Documents the CharacterRepository source file in the world database repositories and persisted player/account records area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.WorldServer.Database.Characters;

/**
  * Owns the character repository behavior for the world database repositories and persisted player/account records layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class CharacterRepository
{
    /**
      * Defines the constant value for character equipment slot count.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const int CharacterEquipmentSlotCount = 19;
    /**
      * Defines the constant value for at login first.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const uint AtLoginFirst = 0x20;
    /**
      * Defines the constant value for no equipment slot.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const int NoEquipmentSlot = -1;
    private const int ItemInstanceFieldCount = 48;
    private const int ObjectFieldGuid = 0x0000;
    private const int ObjectFieldType = 0x0002;
    private const int ObjectFieldEntry = 0x0003;
    private const int ObjectFieldScaleX = 0x0004;
    private const int ItemFieldOwner = 0x0006;
    private const int ItemFieldContained = 0x0008;
    private const int ItemFieldStackCount = 0x000E;
    private const int ItemFieldDuration = 0x000F;
    private const int ItemFieldFlags = 0x0015;
    private const int ItemFieldRandomPropertiesId = 0x002C;
    private const int ItemFieldDurability = 0x002E;
    private const int ItemFieldMaxDurability = 0x002F;
    /**
      * Holds the private database service state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly IDatabaseService _databaseService;
    private readonly Func<uint, ItemTemplateRecord?> _itemTemplateAccessor;
    /**
      * Holds the private world template accessor state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly Func<WorldTemplateDataStore> _worldTemplateAccessor;

    /**
      * Initializes a new CharacterRepository instance with the dependencies required by the world database repositories and persisted player/account records workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: databaseService, itemTemplateAccessor, worldTemplateAccessor.
      */
    public CharacterRepository(
        IDatabaseService databaseService,
        Func<uint, ItemTemplateRecord?> itemTemplateAccessor,
        Func<WorldTemplateDataStore> worldTemplateAccessor)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException();
        _itemTemplateAccessor = itemTemplateAccessor ?? throw new ArgumentNullException();
        _worldTemplateAccessor = worldTemplateAccessor ?? throw new ArgumentNullException();
    }

    /**
      * Resolves the characters for account value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: accountId, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    public async Task<IReadOnlyDictionary<string, bool>> GetPlayerStateTableAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        string[] tableNames =
        [
            "character_action",
            "character_aura",
            "character_inventory",
            "character_reputation",
            "character_skills",
            "character_spell",
            "character_stats",
            "character_tutorial",
            "item_instance",
        ];

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);

        Dictionary<string, bool> availability = new(StringComparer.OrdinalIgnoreCase);
        foreach (string tableName in tableNames)
        {
            availability[tableName] = await TableExistsAsync(connection, tableName, cancellationToken);
        }

        return availability;
    }

    /**
      * Performs the character name exists operation for the world database repositories and persisted player/account records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: name, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Performs the count characters for account operation for the world database repositories and persisted player/account records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: accountId, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async Task<int> CountCharactersForAccountAsync(uint accountId, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

        command.CommandText = "SELECT COUNT(*) FROM `characters` WHERE `account` = @account;";
        command.Parameters.AddWithValue("@account", accountId);

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    /**
      * Creates the character result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: accountId, request, createInfo, starterItems, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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
            PlayerStats initialStats = ResolvePlayerStats(request.Race, request.Class, 1, PlayerStats.Empty);

            await InsertCharacterAsync(connection, transaction, characterGuid, accountId, request, createInfo, playerBytes, playerBytes2, equipmentCache, initialStats, cancellationToken);
            await InsertHomebindAsync(connection, transaction, characterGuid, createInfo, cancellationToken);
            await InsertCharacterStatsAsync(connection, transaction, characterGuid, initialStats, cancellationToken);
            await InsertCharacterTutorialAsync(connection, transaction, accountId, cancellationToken);
            await InsertCharacterSpellsAsync(connection, transaction, characterGuid, _worldTemplateAccessor().GetPlayerCreateSpells(request.Race, request.Class), cancellationToken);
            await InsertCharacterActionsAsync(connection, transaction, characterGuid, _worldTemplateAccessor().GetPlayerCreateActions(request.Race, request.Class), cancellationToken);

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

    /**
      * Updates one or more inventory placements for a character and returns the refreshed inventory state.
      */
    public async Task<IReadOnlyList<PlayerInventoryItem>> UpdateInventoryPlacementsAsync(
        uint characterGuid,
        IReadOnlyList<PlayerInventoryPlacementUpdate> placements,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(placements);

        if (characterGuid == 0 || placements.Count == 0)
        {
            return Array.Empty<PlayerInventoryItem>();
        }

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (PlayerInventoryPlacementUpdate placement in placements)
            {
                using MySqlCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = """
                    UPDATE `character_inventory`
                    SET `bag` = @bag,
                        `slot` = @slot
                    WHERE `guid` = @guid
                      AND `item` = @item;
                    """;
                command.Parameters.AddWithValue("@guid", characterGuid);
                command.Parameters.AddWithValue("@item", placement.ItemGuid);
                command.Parameters.AddWithValue("@bag", placement.BagGuid);
                command.Parameters.AddWithValue("@slot", placement.Slot);

                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return await LoadPlayerInventoryAsync(connection, characterGuid, cancellationToken);
    }

    /**
      * Splits a stackable item into an empty destination slot, or merges the requested count into a compatible destination stack.
      */
    public async Task<IReadOnlyList<PlayerInventoryItem>> SplitInventoryStackAsync(
        uint characterGuid,
        uint sourceItemGuid,
        uint destinationBagGuid,
        byte destinationSlot,
        uint splitCount,
        CancellationToken cancellationToken = default)
    {
        if (characterGuid == 0 || sourceItemGuid == 0 || splitCount == 0)
        {
            return Array.Empty<PlayerInventoryItem>();
        }

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        IReadOnlyList<PlayerInventoryItem> inventory = await LoadPlayerInventoryAsync(connection, characterGuid, cancellationToken);
        PlayerInventoryItem? sourceItem = inventory.FirstOrDefault(item => item.ItemGuid == sourceItemGuid);
        if (sourceItem is null || sourceItem.IsContainer)
        {
            return Array.Empty<PlayerInventoryItem>();
        }

        if (!_worldTemplateAccessor().TryGetItemTemplate(sourceItem.TemplateEntry, out ItemTemplateRecord sourceTemplate))
        {
            return Array.Empty<PlayerInventoryItem>();
        }

        uint maximumStack = ResolveMaximumStackCount(sourceTemplate);
        uint sourceCount = Math.Max(sourceItem.StackCount, 1u);
        if (maximumStack <= 1 || splitCount >= sourceCount || splitCount > maximumStack)
        {
            return Array.Empty<PlayerInventoryItem>();
        }

        PlayerInventoryItem? destinationItem = inventory.FirstOrDefault(item => item.BagGuid == destinationBagGuid && item.Slot == destinationSlot);
        if (destinationItem is not null)
        {
            if (destinationItem.ItemGuid == sourceItem.ItemGuid ||
                destinationItem.TemplateEntry != sourceItem.TemplateEntry ||
                destinationItem.IsContainer)
            {
                return Array.Empty<PlayerInventoryItem>();
            }

            uint destinationCount = Math.Max(destinationItem.StackCount, 1u);
            if (destinationCount >= maximumStack || splitCount > maximumStack - destinationCount)
            {
                return Array.Empty<PlayerInventoryItem>();
            }
        }

        await using MySqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            string normalizedSourceData = NormalizeItemInstanceData(sourceItem.InstanceData, sourceItem.ItemGuid, characterGuid, sourceTemplate);
            uint sourceNewCount = sourceCount - splitCount;
            await UpdateItemInstanceDataAsync(
                connection,
                transaction,
                sourceItem.ItemGuid,
                SetItemInstanceStackCount(normalizedSourceData, sourceItem.ItemGuid, characterGuid, sourceNewCount),
                cancellationToken);

            if (destinationItem is not null)
            {
                string normalizedDestinationData = NormalizeItemInstanceData(destinationItem.InstanceData, destinationItem.ItemGuid, characterGuid, sourceTemplate);
                uint destinationNewCount = Math.Max(destinationItem.StackCount, 1u) + splitCount;
                await UpdateItemInstanceDataAsync(
                    connection,
                    transaction,
                    destinationItem.ItemGuid,
                    SetItemInstanceStackCount(normalizedDestinationData, destinationItem.ItemGuid, characterGuid, destinationNewCount),
                    cancellationToken);
            }
            else
            {
                uint newItemGuid = await GetNextIdAsync(connection, transaction, "item_instance", "guid", cancellationToken);
                string newItemData = SetItemInstanceStackCount(normalizedSourceData, newItemGuid, characterGuid, splitCount);
                await InsertItemInstanceDataAsync(connection, transaction, newItemGuid, characterGuid, newItemData, cancellationToken);
                await InsertCharacterInventoryAsync(connection, transaction, characterGuid, newItemGuid, sourceItem.TemplateEntry, destinationBagGuid, destinationSlot, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return await LoadPlayerInventoryAsync(connection, characterGuid, cancellationToken);
    }

    /**
      * Performs the delete character operation for the world database repositories and persisted player/account records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: accountId, characterGuid, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Resolves the player for login value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: accountId, characterGuid, factionResolver, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

        byte level = NormalizeLevel(row.Level);
        PlayerStats? characterStats = await LoadCharacterStatsAsync(connection, row.Guid, cancellationToken);
        PlayerStats stats = ResolvePlayerStats(row.Race, row.Class, level, characterStats ?? row.Stats);
        IReadOnlyList<PlayerInventoryItem> inventory = await LoadPlayerInventoryAsync(connection, row.Guid, cancellationToken);
        IReadOnlyList<PlayerSpell> spells = await LoadCharacterSpellsAsync(connection, row.Guid, row.Race, row.Class, cancellationToken);
        IReadOnlyList<PlayerActionButton> actionButtons = await LoadCharacterActionsAsync(connection, row.Guid, row.Race, row.Class, cancellationToken);
        uint[] tutorialFlags = await LoadCharacterTutorialFlagsAsync(connection, row.AccountId, cancellationToken);
        IReadOnlyList<PlayerReputation> reputations = await LoadCharacterReputationAsync(connection, row.Guid, cancellationToken);
        IReadOnlyList<PlayerSkill> skills = await LoadCharacterSkillsAsync(connection, row.Guid, cancellationToken);

        return new PlayerLoginRecord(
            row.Guid,
            row.AccountId,
            row.Name,
            row.Race,
            row.Class,
            row.Gender,
            level,
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
            stats,
            _worldTemplateAccessor().GetNextLevelExperience(level),
            inventory,
            spells,
            actionButtons,
            tutorialFlags,
            reputations,
            skills,
            factionResolver(row.Race));
    }

    /**
      * Resolves the character name query value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: characterGuid, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Performs the set character online operation for the world database repositories and persisted player/account records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: characterGuid, online, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Saves the lightweight movement/time fields used by routine autosaves.
      * Full player data is still saved during logout and explicit forced saves.
      */
    public async Task SavePlayerPositionAsync(PlayerLoginRecord player, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (player.Guid == 0)
        {
            return;
        }

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            UPDATE `characters`
            SET `zone` = @zone,
                `map` = @map,
                `position_x` = @x,
                `position_y` = @y,
                `position_z` = @z,
                `orientation` = @o,
                `totaltime` = @totalTime,
                `leveltime` = @levelTime
            WHERE `guid` = @guid
              AND `account` = @account;
            """;
        command.Parameters.AddWithValue("@guid", player.Guid);
        command.Parameters.AddWithValue("@account", player.AccountId);
        command.Parameters.AddWithValue("@zone", player.Zone);
        command.Parameters.AddWithValue("@map", player.Map);
        command.Parameters.AddWithValue("@x", player.PositionX);
        command.Parameters.AddWithValue("@y", player.PositionY);
        command.Parameters.AddWithValue("@z", player.PositionZ);
        command.Parameters.AddWithValue("@o", player.Orientation);
        command.Parameters.AddWithValue("@totalTime", player.TotalTime);
        command.Parameters.AddWithValue("@levelTime", player.LevelTime);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /**
      * Updates save player state in memory or persistent storage.
      * The method keeps mutation rules centralized so player/account data changes remain auditable and safe to call from packet handlers.
      * Inputs used by this operation: player, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async Task SavePlayerAsync(PlayerLoginRecord player, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (player.Guid == 0)
        {
            return;
        }

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            using MySqlCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                UPDATE `characters`
                SET `level` = @level,
                    `xp` = @xp,
                    `money` = @money,
                    `zone` = @zone,
                    `map` = @map,
                    `position_x` = @x,
                    `position_y` = @y,
                    `position_z` = @z,
                    `orientation` = @o,
                    `playerBytes` = @playerBytes,
                    `playerBytes2` = @playerBytes2,
                    `playerFlags` = @playerFlags,
                    `totaltime` = @totalTime,
                    `leveltime` = @levelTime,
                    `health` = @health,
                    `power1` = @power1,
                    `power2` = @power2,
                    `power3` = @power3,
                    `power4` = @power4,
                    `power5` = @power5
                WHERE `guid` = @guid
                  AND `account` = @account;
                """;
            command.Parameters.AddWithValue("@guid", player.Guid);
            command.Parameters.AddWithValue("@account", player.AccountId);
            command.Parameters.AddWithValue("@level", player.Level);
            command.Parameters.AddWithValue("@xp", player.Experience);
            command.Parameters.AddWithValue("@money", player.Money);
            command.Parameters.AddWithValue("@zone", player.Zone);
            command.Parameters.AddWithValue("@map", player.Map);
            command.Parameters.AddWithValue("@x", player.PositionX);
            command.Parameters.AddWithValue("@y", player.PositionY);
            command.Parameters.AddWithValue("@z", player.PositionZ);
            command.Parameters.AddWithValue("@o", player.Orientation);
            command.Parameters.AddWithValue("@playerBytes", player.PlayerBytes);
            command.Parameters.AddWithValue("@playerBytes2", player.PlayerBytes2);
            command.Parameters.AddWithValue("@playerFlags", player.PlayerFlags);
            command.Parameters.AddWithValue("@totalTime", player.TotalTime);
            command.Parameters.AddWithValue("@levelTime", player.LevelTime);
            command.Parameters.AddWithValue("@health", player.Stats.Health);
            command.Parameters.AddWithValue("@power1", player.Stats.Power1);
            command.Parameters.AddWithValue("@power2", player.Stats.Power2);
            command.Parameters.AddWithValue("@power3", player.Stats.Power3);
            command.Parameters.AddWithValue("@power4", player.Stats.Power4);
            command.Parameters.AddWithValue("@power5", player.Stats.Power5);

            await command.ExecuteNonQueryAsync(cancellationToken);
            await InsertCharacterStatsAsync(connection, transaction, player.Guid, player.Stats, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    /**
      * Loads load player inventory information from configuration, files, or persistent storage.
      * The method normalizes external input before returning it so the rest of the server can work with validated, strongly typed data.
      * Inputs used by this operation: connection, characterGuid, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task<IReadOnlyList<PlayerInventoryItem>> LoadPlayerInventoryAsync(
        MySqlConnection connection,
        uint characterGuid,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "character_inventory", cancellationToken))
        {
            return Array.Empty<PlayerInventoryItem>();
        }

        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT `ci`.`item`, `ci`.`guid`, `ci`.`item_template`, `ci`.`bag`, `ci`.`slot`, COALESCE(`ii`.`data`, '')
            FROM `character_inventory` `ci`
            LEFT JOIN `item_instance` `ii` ON `ii`.`guid` = `ci`.`item`
            WHERE `ci`.`guid` = @guid
            ORDER BY `ci`.`bag`, `ci`.`slot`;
            """;
        command.Parameters.AddWithValue("@guid", characterGuid);

        WorldTemplateDataStore worldTemplates = _worldTemplateAccessor();
        List<PlayerInventoryItem> items = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            uint itemGuid = Convert.ToUInt32(reader.GetValue(0), CultureInfo.InvariantCulture);
            uint ownerGuid = Convert.ToUInt32(reader.GetValue(1), CultureInfo.InvariantCulture);
            uint templateEntry = Convert.ToUInt32(reader.GetValue(2), CultureInfo.InvariantCulture);
            uint bagGuid = Convert.ToUInt32(reader.GetValue(3), CultureInfo.InvariantCulture);
            byte slot = Convert.ToByte(reader.GetValue(4), CultureInfo.InvariantCulture);
            string instanceData = reader.GetString(5);

            byte inventoryType = 0;
            uint displayId = 0;
            byte containerSlots = 0;
            uint maxDurability = 0;
            if (templateEntry != 0 && worldTemplates.TryGetItemTemplate(templateEntry, out ItemTemplateRecord itemTemplate))
            {
                inventoryType = itemTemplate.InventoryType;
                displayId = itemTemplate.DisplayId;
                containerSlots = itemTemplate.ContainerSlots;
                maxDurability = itemTemplate.MaxDurability;
            }

            items.Add(new PlayerInventoryItem(
                itemGuid,
                ownerGuid,
                templateEntry,
                bagGuid,
                slot,
                instanceData,
                inventoryType,
                displayId,
                ReadItemInstanceField(instanceData, 22),
                containerSlots,
                maxDurability,
                Math.Max(ReadItemInstanceField(instanceData, ItemFieldStackCount), 1u)));
        }

        return items;
    }

    /**
      * Resolves the next id value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: connection, transaction, tableName, columnName, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Performs the insert character operation for the world database repositories and persisted player/account records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: connection, transaction, characterGuid, accountId, request, createInfo....
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Performs the insert homebind operation for the world database repositories and persisted player/account records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: connection, transaction, characterGuid, createInfo, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Performs the insert character stats operation for the world database repositories and persisted player/account records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: connection, transaction, characterGuid, stats, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private static async Task InsertCharacterStatsAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        uint characterGuid,
        PlayerStats stats,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, transaction, "character_stats", cancellationToken))
        {
            return;
        }

        using MySqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO `character_stats`
                (`guid`, `maxhealth`, `maxpower1`, `maxpower2`, `maxpower3`, `maxpower4`, `maxpower5`, `maxpower6`, `maxpower7`,
                 `strength`, `agility`, `stamina`, `intellect`, `spirit`, `armor`, `resHoly`, `resFire`, `resNature`, `resFrost`, `resShadow`, `resArcane`,
                 `blockPct`, `dodgePct`, `parryPct`, `critPct`, `rangedCritPct`, `attackPower`, `rangedAttackPower`)
            VALUES
                (@guid, @health, @power1, @power2, @power3, @power4, @power5, 0, 0,
                 @strength, @agility, @stamina, @intellect, @spirit, @armor, 0, 0, 0, 0, 0, 0,
                 0, 0, 0, 0, 0, @attackPower, @rangedAttackPower)
            ON DUPLICATE KEY UPDATE
                `maxhealth` = VALUES(`maxhealth`),
                `maxpower1` = VALUES(`maxpower1`),
                `maxpower2` = VALUES(`maxpower2`),
                `maxpower3` = VALUES(`maxpower3`),
                `maxpower4` = VALUES(`maxpower4`),
                `maxpower5` = VALUES(`maxpower5`),
                `strength` = VALUES(`strength`),
                `agility` = VALUES(`agility`),
                `stamina` = VALUES(`stamina`),
                `intellect` = VALUES(`intellect`),
                `spirit` = VALUES(`spirit`),
                `armor` = VALUES(`armor`),
                `attackPower` = VALUES(`attackPower`),
                `rangedAttackPower` = VALUES(`rangedAttackPower`);
            """;
        command.Parameters.AddWithValue("@guid", characterGuid);
        command.Parameters.AddWithValue("@health", stats.Health);
        command.Parameters.AddWithValue("@power1", stats.Power1);
        command.Parameters.AddWithValue("@power2", stats.Power2);
        command.Parameters.AddWithValue("@power3", stats.Power3);
        command.Parameters.AddWithValue("@power4", stats.Power4);
        command.Parameters.AddWithValue("@power5", stats.Power5);
        command.Parameters.AddWithValue("@strength", stats.Strength);
        command.Parameters.AddWithValue("@agility", stats.Agility);
        command.Parameters.AddWithValue("@stamina", stats.Stamina);
        command.Parameters.AddWithValue("@intellect", stats.Intellect);
        command.Parameters.AddWithValue("@spirit", stats.Spirit);
        command.Parameters.AddWithValue("@armor", stats.Armor);
        command.Parameters.AddWithValue("@attackPower", Math.Max(1u, stats.Strength * 2u));
        command.Parameters.AddWithValue("@rangedAttackPower", stats.Agility);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /**
      * Performs the insert character tutorial operation for the world database repositories and persisted player/account records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: connection, transaction, accountId, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private static async Task InsertCharacterTutorialAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        uint accountId,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, transaction, "character_tutorial", cancellationToken))
        {
            return;
        }

        using MySqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT IGNORE INTO `character_tutorial`
                (`account`, `tut0`, `tut1`, `tut2`, `tut3`, `tut4`, `tut5`, `tut6`, `tut7`)
            VALUES
                (@account, @flags, @flags, @flags, @flags, @flags, @flags, @flags, @flags);
            """;
        command.Parameters.AddWithValue("@account", accountId);
        command.Parameters.AddWithValue("@flags", uint.MaxValue);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /**
      * Performs the insert character spells operation for the world database repositories and persisted player/account records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: connection, transaction, characterGuid, starterSpells, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private static async Task InsertCharacterSpellsAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        uint characterGuid,
        IReadOnlyList<PlayerCreateSpellRecord> starterSpells,
        CancellationToken cancellationToken)
    {
        if (starterSpells.Count == 0 || !await TableExistsAsync(connection, transaction, "character_spell", cancellationToken))
        {
            return;
        }

        foreach (PlayerCreateSpellRecord spell in starterSpells)
        {
            if (spell.SpellId == 0)
            {
                continue;
            }

            using MySqlCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT IGNORE INTO `character_spell`
                    (`guid`, `spell`, `active`, `disabled`)
                VALUES
                    (@guid, @spell, 1, 0);
                """;
            command.Parameters.AddWithValue("@guid", characterGuid);
            command.Parameters.AddWithValue("@spell", spell.SpellId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /**
      * Performs the insert character actions operation for the world database repositories and persisted player/account records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: connection, transaction, characterGuid, starterActions, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private static async Task InsertCharacterActionsAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        uint characterGuid,
        IReadOnlyList<PlayerCreateActionRecord> starterActions,
        CancellationToken cancellationToken)
    {
        if (starterActions.Count == 0 || !await TableExistsAsync(connection, transaction, "character_action", cancellationToken))
        {
            return;
        }

        foreach (PlayerCreateActionRecord action in starterActions)
        {
            if (action.Button >= 120)
            {
                continue;
            }

            using MySqlCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO `character_action`
                    (`guid`, `button`, `action`, `type`)
                VALUES
                    (@guid, @button, @action, @type)
                ON DUPLICATE KEY UPDATE
                    `action` = VALUES(`action`),
                    `type` = VALUES(`type`);
                """;
            command.Parameters.AddWithValue("@guid", characterGuid);
            command.Parameters.AddWithValue("@button", action.Button);
            command.Parameters.AddWithValue("@action", action.Action);
            command.Parameters.AddWithValue("@type", action.Type);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /**
      * Performs the insert item instance operation for the world database repositories and persisted player/account records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: connection, transaction, itemGuid, ownerGuid, itemTemplate, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Performs the insert character inventory operation for the world database repositories and persisted player/account records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: connection, transaction, characterGuid, itemGuid, itemTemplate, storageSlot....
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Loads load character ownership for update information from configuration, files, or persistent storage.
      * The method normalizes external input before returning it so the rest of the server can work with validated, strongly typed data.
      * Inputs used by this operation: connection, transaction, characterGuid, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Determines whether guild leader for the world database repositories and persisted player/account records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: connection, transaction, characterGuid, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Performs the delete character rows operation for the world database repositories and persisted player/account records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: connection, transaction, characterGuid, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private static async Task DeleteCharacterRowsAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        uint characterGuid,
        CancellationToken cancellationToken)
    {
        // Delete optional character-side tables first when the full
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
        await DeleteWhereColumnEqualsAsync(connection, transaction, "character_stats", "guid", characterGuid, cancellationToken);
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

    /**
      * Performs the delete where column equals operation for the world database repositories and persisted player/account records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: connection, transaction, tableName, columnName, value, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Performs the table column exists operation for the world database repositories and persisted player/account records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: connection, transaction, tableName, columnName, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Performs the table exists operation for the world database repositories and persisted player/account records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: connection, transaction, tableName, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private static async Task<bool> TableExistsAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string tableName,
        CancellationToken cancellationToken)
    {
        using MySqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
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

    /**
      * Performs the table exists operation for the world database repositories and persisted player/account records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: connection, tableName, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private static async Task<bool> TableExistsAsync(
        MySqlConnection connection,
        string tableName,
        CancellationToken cancellationToken)
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

    /**
      * Creates the default tutorial flags result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      */
    private static uint[] CreateDefaultTutorialFlags()
    {
        return Enumerable.Repeat(uint.MaxValue, 8).ToArray();
    }

    /**
      * Loads load character stats information from configuration, files, or persistent storage.
      * The method normalizes external input before returning it so the rest of the server can work with validated, strongly typed data.
      * Inputs used by this operation: connection, characterGuid, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task<PlayerStats?> LoadCharacterStatsAsync(MySqlConnection connection, uint characterGuid, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "character_stats", cancellationToken))
        {
            return null;
        }

        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT `maxhealth`, `maxpower1`, `maxpower2`, `maxpower3`, `maxpower4`, `maxpower5`,
                   `strength`, `agility`, `stamina`, `intellect`, `spirit`, `armor`
            FROM `character_stats`
            WHERE `guid` = @guid
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@guid", characterGuid);

        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new PlayerStats(
            Convert.ToUInt32(reader.GetValue(0), CultureInfo.InvariantCulture),
            Convert.ToUInt32(reader.GetValue(1), CultureInfo.InvariantCulture),
            Convert.ToUInt32(reader.GetValue(2), CultureInfo.InvariantCulture),
            Convert.ToUInt32(reader.GetValue(3), CultureInfo.InvariantCulture),
            Convert.ToUInt32(reader.GetValue(4), CultureInfo.InvariantCulture),
            Convert.ToUInt32(reader.GetValue(5), CultureInfo.InvariantCulture),
            Convert.ToUInt32(reader.GetValue(6), CultureInfo.InvariantCulture),
            Convert.ToUInt32(reader.GetValue(7), CultureInfo.InvariantCulture),
            Convert.ToUInt32(reader.GetValue(8), CultureInfo.InvariantCulture),
            Convert.ToUInt32(reader.GetValue(9), CultureInfo.InvariantCulture),
            Convert.ToUInt32(reader.GetValue(10), CultureInfo.InvariantCulture),
            Convert.ToUInt32(reader.GetValue(11), CultureInfo.InvariantCulture));
    }

    /**
      * Loads load character spells information from configuration, files, or persistent storage.
      * The method normalizes external input before returning it so the rest of the server can work with validated, strongly typed data.
      * Inputs used by this operation: connection, characterGuid, race, characterClass, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task<IReadOnlyList<PlayerSpell>> LoadCharacterSpellsAsync(
        MySqlConnection connection,
        uint characterGuid,
        byte race,
        byte characterClass,
        CancellationToken cancellationToken)
    {
        List<PlayerSpell> spells = [];
        if (await TableExistsAsync(connection, "character_spell", cancellationToken))
        {
            using MySqlCommand command = connection.CreateCommand();
            command.CommandText = """
                SELECT `spell`, `active`, `disabled`
                FROM `character_spell`
                WHERE `guid` = @guid
                ORDER BY `spell`;
                """;
            command.Parameters.AddWithValue("@guid", characterGuid);

            await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                spells.Add(new PlayerSpell(
                    Convert.ToUInt32(reader.GetValue(0), CultureInfo.InvariantCulture),
                    Convert.ToByte(reader.GetValue(1), CultureInfo.InvariantCulture) != 0,
                    Convert.ToByte(reader.GetValue(2), CultureInfo.InvariantCulture) != 0));
            }
        }

        if (spells.Count != 0)
        {
            return spells;
        }

        return _worldTemplateAccessor()
            .GetPlayerCreateSpells(race, characterClass)
            .Where(spell => spell.SpellId != 0)
            .Select(spell => new PlayerSpell(spell.SpellId, true, false))
            .ToArray();
    }

    /**
      * Loads load character actions information from configuration, files, or persistent storage.
      * The method normalizes external input before returning it so the rest of the server can work with validated, strongly typed data.
      * Inputs used by this operation: connection, characterGuid, race, characterClass, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task<IReadOnlyList<PlayerActionButton>> LoadCharacterActionsAsync(
        MySqlConnection connection,
        uint characterGuid,
        byte race,
        byte characterClass,
        CancellationToken cancellationToken)
    {
        List<PlayerActionButton> actions = [];
        if (await TableExistsAsync(connection, "character_action", cancellationToken))
        {
            using MySqlCommand command = connection.CreateCommand();
            command.CommandText = """
                SELECT `button`, `action`, `type`
                FROM `character_action`
                WHERE `guid` = @guid
                ORDER BY `button`;
                """;
            command.Parameters.AddWithValue("@guid", characterGuid);

            await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                actions.Add(new PlayerActionButton(
                    Convert.ToByte(reader.GetValue(0), CultureInfo.InvariantCulture),
                    Convert.ToUInt32(reader.GetValue(1), CultureInfo.InvariantCulture),
                    Convert.ToByte(reader.GetValue(2), CultureInfo.InvariantCulture)));
            }
        }

        if (actions.Count != 0)
        {
            return actions;
        }

        return _worldTemplateAccessor()
            .GetPlayerCreateActions(race, characterClass)
            .Where(action => action.Button < 120)
            .Select(action => new PlayerActionButton(action.Button, action.Action, action.Type))
            .ToArray();
    }

    /**
      * Loads load character tutorial flags information from configuration, files, or persistent storage.
      * The method normalizes external input before returning it so the rest of the server can work with validated, strongly typed data.
      * Inputs used by this operation: connection, accountId, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private static async Task<uint[]> LoadCharacterTutorialFlagsAsync(MySqlConnection connection, uint accountId, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "character_tutorial", cancellationToken))
        {
            return CreateDefaultTutorialFlags();
        }

        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT `tut0`, `tut1`, `tut2`, `tut3`, `tut4`, `tut5`, `tut6`, `tut7`
            FROM `character_tutorial`
            WHERE `account` = @account
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@account", accountId);

        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return CreateDefaultTutorialFlags();
        }

        uint[] flags = new uint[8];
        for (int index = 0; index < flags.Length; index++)
        {
            flags[index] = Convert.ToUInt32(reader.GetValue(index), CultureInfo.InvariantCulture);
        }

        return flags;
    }

    /**
      * Loads load character reputation information from configuration, files, or persistent storage.
      * The method normalizes external input before returning it so the rest of the server can work with validated, strongly typed data.
      * Inputs used by this operation: connection, characterGuid, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private static async Task<IReadOnlyList<PlayerReputation>> LoadCharacterReputationAsync(MySqlConnection connection, uint characterGuid, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "character_reputation", cancellationToken))
        {
            return Array.Empty<PlayerReputation>();
        }

        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT `faction`, `standing`, `flags`
            FROM `character_reputation`
            WHERE `guid` = @guid
            ORDER BY `faction`;
            """;
        command.Parameters.AddWithValue("@guid", characterGuid);

        List<PlayerReputation> reputations = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            reputations.Add(new PlayerReputation(
                Convert.ToUInt32(reader.GetValue(0), CultureInfo.InvariantCulture),
                Convert.ToInt32(reader.GetValue(1), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(2), CultureInfo.InvariantCulture)));
        }

        return reputations;
    }

    /**
      * Loads load character skills information from configuration, files, or persistent storage.
      * The method normalizes external input before returning it so the rest of the server can work with validated, strongly typed data.
      * Inputs used by this operation: connection, characterGuid, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private static async Task<IReadOnlyList<PlayerSkill>> LoadCharacterSkillsAsync(MySqlConnection connection, uint characterGuid, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "character_skills", cancellationToken))
        {
            return Array.Empty<PlayerSkill>();
        }

        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT `skill`, `value`, `max`
            FROM `character_skills`
            WHERE `guid` = @guid
            ORDER BY `skill`;
            """;
        command.Parameters.AddWithValue("@guid", characterGuid);

        List<PlayerSkill> skills = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            skills.Add(new PlayerSkill(
                Convert.ToUInt32(reader.GetValue(0), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(1), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(2), CultureInfo.InvariantCulture)));
        }

        return skills;
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

    /**
      * Performs the merge equipment operation for the world database repositories and persisted player/account records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: cachedEquipment, inventoryEquipment.
      */
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

    /**
      * Creates the empty equipment array result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      */
    private static CharacterEquipmentDisplay[] CreateEmptyEquipmentArray()
    {
        return Enumerable
            .Range(0, CharacterEquipmentSlotCount)
            .Select(_ => new CharacterEquipmentDisplay(0, 0, 0))
            .ToArray();
    }

    /**
      * Performs the map inventory type to equipment slot operation for the world database repositories and persisted player/account records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: inventoryType.
      */
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

    /**
      * Parses read item instance field input into the strongly typed server representation.
      * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
      * Inputs used by this operation: instanceData, fieldIndex.
      */
    private static uint ReadItemInstanceField(string instanceData, int fieldIndex)
    {
        if (string.IsNullOrWhiteSpace(instanceData) || fieldIndex < 0)
        {
            return 0;
        }

        string[] parts = instanceData.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (fieldIndex >= parts.Length)
        {
            return 0;
        }

        return uint.TryParse(parts[fieldIndex], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint value)
            ? value
            : 0;
    }

    /**
      * Builds the build item instance data result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: itemGuid, ownerGuid, itemTemplate.
      */
    private static string BuildItemInstanceData(uint itemGuid, uint ownerGuid, ItemTemplateRecord itemTemplate)
    {
        // Vanilla item update fields end at ITEM_END (0x30). Container slot fields are
        // generated from character_inventory at packet-build time so bag contents stay authoritative.
        uint[] fields = new uint[ItemInstanceFieldCount];
        ulong itemClientGuid = CharacterGuid.ToItemGuid(itemGuid);
        fields[ObjectFieldGuid] = (uint)(itemClientGuid & uint.MaxValue);
        fields[ObjectFieldGuid + 1] = (uint)(itemClientGuid >> 32);
        fields[ObjectFieldType] = itemTemplate.ContainerSlots > 0 ? 0x07u : 0x03u;
        fields[ObjectFieldEntry] = itemTemplate.Entry;
        fields[ObjectFieldScaleX] = BitConverter.SingleToUInt32Bits(1.0f);
        fields[ItemFieldOwner] = ownerGuid;
        fields[ItemFieldContained] = ownerGuid;
        fields[ItemFieldStackCount] = 1;
        fields[ItemFieldDuration] = itemTemplate.Duration;
        fields[ItemFieldFlags] = itemTemplate.Flags;
        fields[ItemFieldRandomPropertiesId] = itemTemplate.RandomProperty;
        fields[ItemFieldDurability] = itemTemplate.MaxDurability;
        fields[ItemFieldMaxDurability] = itemTemplate.MaxDurability;

        return string.Join(' ', fields.Select(value => value.ToString(CultureInfo.InvariantCulture)));
    }

    /**
      * Resolves the largest legal count for one item stack. Vanilla templates use 0/1 for non-stackable items.
      */
    private static uint ResolveMaximumStackCount(ItemTemplateRecord itemTemplate)
    {
        return itemTemplate.Stackable > 1 ? itemTemplate.Stackable : 1u;
    }

    /**
      * Builds a valid item instance data string when a legacy or empty row is missing stack/owner/object fields.
      */
    private static string NormalizeItemInstanceData(string instanceData, uint itemGuid, uint ownerGuid, ItemTemplateRecord itemTemplate)
    {
        if (string.IsNullOrWhiteSpace(instanceData))
        {
            return BuildItemInstanceData(itemGuid, ownerGuid, itemTemplate);
        }

        uint[] fields = ReadItemInstanceFields(instanceData);
        ulong itemClientGuid = CharacterGuid.ToItemGuid(itemGuid);
        fields[ObjectFieldGuid] = (uint)(itemClientGuid & uint.MaxValue);
        fields[ObjectFieldGuid + 1] = (uint)(itemClientGuid >> 32);
        fields[ObjectFieldType] = itemTemplate.ContainerSlots > 0 ? 0x07u : 0x03u;
        fields[ObjectFieldEntry] = itemTemplate.Entry;
        fields[ObjectFieldScaleX] = fields[ObjectFieldScaleX] == 0 ? BitConverter.SingleToUInt32Bits(1.0f) : fields[ObjectFieldScaleX];
        fields[ItemFieldOwner] = ownerGuid;
        fields[ItemFieldContained] = ownerGuid;
        fields[ItemFieldStackCount] = fields[ItemFieldStackCount] == 0 ? 1u : fields[ItemFieldStackCount];
        fields[ItemFieldDuration] = fields[ItemFieldDuration] == 0 ? itemTemplate.Duration : fields[ItemFieldDuration];
        fields[ItemFieldFlags] = fields[ItemFieldFlags] == 0 ? itemTemplate.Flags : fields[ItemFieldFlags];
        fields[ItemFieldRandomPropertiesId] = fields[ItemFieldRandomPropertiesId] == 0 ? itemTemplate.RandomProperty : fields[ItemFieldRandomPropertiesId];
        fields[ItemFieldDurability] = fields[ItemFieldDurability] == 0 ? itemTemplate.MaxDurability : fields[ItemFieldDurability];
        fields[ItemFieldMaxDurability] = fields[ItemFieldMaxDurability] == 0 ? itemTemplate.MaxDurability : fields[ItemFieldMaxDurability];
        return string.Join(' ', fields.Select(value => value.ToString(CultureInfo.InvariantCulture)));
    }

    /**
      * Returns a new item instance data string with GUID, owner, contained, and stack count fields updated.
      */
    private static string SetItemInstanceStackCount(string instanceData, uint itemGuid, uint ownerGuid, uint stackCount)
    {
        uint[] fields = ReadItemInstanceFields(instanceData);
        ulong itemClientGuid = CharacterGuid.ToItemGuid(itemGuid);
        fields[ObjectFieldGuid] = (uint)(itemClientGuid & uint.MaxValue);
        fields[ObjectFieldGuid + 1] = (uint)(itemClientGuid >> 32);
        fields[ItemFieldOwner] = ownerGuid;
        fields[ItemFieldContained] = ownerGuid;
        fields[ItemFieldStackCount] = Math.Max(stackCount, 1u);
        return string.Join(' ', fields.Select(value => value.ToString(CultureInfo.InvariantCulture)));
    }

    /**
      * Parses the space-separated item_instance.data update fields into a dense array.
      */
    private static uint[] ReadItemInstanceFields(string instanceData)
    {
        uint[] fields = new uint[ItemInstanceFieldCount];
        if (string.IsNullOrWhiteSpace(instanceData))
        {
            return fields;
        }

        string[] parts = instanceData.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length > fields.Length)
        {
            Array.Resize(ref fields, parts.Length);
        }

        for (int index = 0; index < parts.Length; index++)
        {
            if (uint.TryParse(parts[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint value))
            {
                fields[index] = value;
            }
        }

        return fields;
    }

    /**
      * Persists the mutable item instance data blob for one item.
      */
    private static async Task UpdateItemInstanceDataAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        uint itemGuid,
        string instanceData,
        CancellationToken cancellationToken)
    {
        using MySqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE `item_instance`
            SET `data` = @data
            WHERE `guid` = @guid;
            """;
        command.Parameters.AddWithValue("@guid", itemGuid);
        command.Parameters.AddWithValue("@data", instanceData);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /**
      * Inserts a cloned item instance row for stack splitting.
      */
    private static async Task InsertItemInstanceDataAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        uint itemGuid,
        uint ownerGuid,
        string instanceData,
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
        command.Parameters.AddWithValue("@data", instanceData);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /**
      * Inserts a cloned item into an explicit bag/slot after stack splitting.
      */
    private static async Task InsertCharacterInventoryAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        uint characterGuid,
        uint itemGuid,
        uint itemTemplate,
        uint bagGuid,
        byte storageSlot,
        CancellationToken cancellationToken)
    {
        using MySqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO `character_inventory`
                (`guid`, `bag`, `slot`, `item`, `item_template`)
            VALUES
                (@guid, @bag, @slot, @item, @itemTemplate);
            """;
        command.Parameters.AddWithValue("@guid", characterGuid);
        command.Parameters.AddWithValue("@bag", bagGuid);
        command.Parameters.AddWithValue("@slot", storageSlot);
        command.Parameters.AddWithValue("@item", itemGuid);
        command.Parameters.AddWithValue("@itemTemplate", itemTemplate);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /**
      * Performs the pack player bytes operation for the world database repositories and persisted player/account records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: skin, face, hairStyle, hairColor.
      */
    private static uint PackPlayerBytes(byte skin, byte face, byte hairStyle, byte hairColor)
    {
        return (uint)(skin | (face << 8) | (hairStyle << 16) | (hairColor << 24));
    }

    /**
      * Performs the pack player bytes 2 operation for the world database repositories and persisted player/account records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: facialHair.
      */
    private static uint PackPlayerBytes2(byte facialHair)
    {
        return facialHair;
    }

    /**
      * Builds the build equipment cache result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: starterItems.
      */
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

        // The equipmentCache layout stores two uint values per equipment slot:
        // item entry and permanent enchantment id. The character-list packet then
        // resolves the item entry through item_template to send display/inventory type.
        return string.Join(' ', Enumerable.Range(0, CharacterEquipmentSlotCount).SelectMany(slot => new[]
        {
            itemEntries[slot].ToString(CultureInfo.InvariantCulture),
            enchantments[slot].ToString(CultureInfo.InvariantCulture),
        }));
    }

    /**
      * Parses parse equipment cache input into the strongly typed server representation.
      * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
      * Inputs used by this operation: equipmentCache, itemTemplateAccessor.
      */
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

        // Current equipment cache layout: item entry + enchantment per slot.
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

    /**
      * Parses read u int input into the strongly typed server representation.
      * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
      * Inputs used by this operation: parts, index.
      */
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

    /**
      * Resolves the player stats value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: race, playerClass, level, storedStats.
      */
    private PlayerStats ResolvePlayerStats(byte race, byte playerClass, byte level, PlayerStats storedStats)
    {
        PlayerStats defaults = _worldTemplateAccessor().BuildBasePlayerStats(race, playerClass, level);
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

    /**
      * Normalizes the level for the world database repositories and persisted player/account records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: level.
      */
    private static byte NormalizeLevel(byte level)
    {
        return level == 0 ? (byte)1 : level;
    }

    /**
      * Carries immutable character login row data for the world database repositories and persisted player/account records layer.
      * Records in this project are used as explicit transfer models so packet parsing, database repositories, and runtime systems can pass strongly typed values without mutating shared state.
      * Positional fields carried by this record: Guid, AccountId, Name, Race, Class, Gender, Level, Xp, Zone, Map, PositionX, PositionY, PositionZ, Orientation, Money, PlayerBytes, PlayerBytes2, PlayerFlags, AtLogin, Cinematic, TotalTime, LevelTime, Stats.
      */
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

    /**
      * Carries immutable character ownership record data for the world database repositories and persisted player/account records layer.
      * Records in this project are used as explicit transfer models so packet parsing, database repositories, and runtime systems can pass strongly typed values without mutating shared state.
      * Positional fields carried by this record: AccountId, Name, Online.
      */
    private sealed record CharacterOwnershipRecord(uint AccountId, string Name, bool Online);

    /**
      * Carries immutable character list row data for the world database repositories and persisted player/account records layer.
      * Records in this project are used as explicit transfer models so packet parsing, database repositories, and runtime systems can pass strongly typed values without mutating shared state.
      * Positional fields carried by this record: Guid, Name, Race, Class, Gender, Level, Xp, Zone, Map, PositionX, PositionY, PositionZ, PlayerFlags, AtLogin, PlayerBytes, PlayerBytes2, EquipmentCache.
      */
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
