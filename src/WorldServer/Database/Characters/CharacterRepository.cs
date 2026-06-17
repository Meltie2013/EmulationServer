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
// File: src/WorldServer/Database/Characters/CharacterRepository.cs
// Purpose: Contains character repository code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Globalization;

using EmulationServer.Database.Interfaces;
using EmulationServer.Game.Characters;
using EmulationServer.Game.Chat;
using EmulationServer.Game.Data.Dbc.Factions;
using EmulationServer.Game.Data.Stores;
using EmulationServer.Game.Items;
using EmulationServer.Game.Players;
using EmulationServer.Game.Reputation;
using EmulationServer.Game.WorldData;

using MySqlConnector;

namespace EmulationServer.WorldServer.Database.Characters;

// Type: CharacterRepository
// Purpose: Provides character repository behavior for the world server gameplay, session, and character runtime layer.
// Constructor values:
// - databaseService: Database service value supplied by the caller for this operation.
// - itemTemplateAccessor: Item template accessor value supplied by the caller for this operation.
// - worldTemplateAccessor: World template accessor value supplied by the caller for this operation.
// - worldGameDataAccessor: World game data accessor value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class CharacterRepository(
    IDatabaseService databaseService,
    Func<uint, ItemTemplateRecord?> itemTemplateAccessor,
    Func<WorldTemplateDataStore> worldTemplateAccessor,
    Func<WorldGameDataStore> worldGameDataAccessor)
{

    // Constant: Defines the character equipment slot count constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed character equipment slot count value used anywhere this rule or protocol value is needed.
    private const int CharacterEquipmentSlotCount = 19;

    // Constant: Defines the at login first constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed at login first value used anywhere this rule or protocol value is needed.
    private const uint AtLoginFirst = 0x20;
    // Constant: Defines the item instance field count constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed item instance field count value used anywhere this rule or protocol value is needed.
    private const int ItemInstanceFieldCount = 48;
    // Constant: Defines the object field GUID constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed object field GUID value used anywhere this rule or protocol value is needed.
    private const int ObjectFieldGuid = 0x0000;
    // Constant: Defines the object field type constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed object field type value used anywhere this rule or protocol value is needed.
    private const int ObjectFieldType = 0x0002;
    // Constant: Defines the object field entry constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed object field entry value used anywhere this rule or protocol value is needed.
    private const int ObjectFieldEntry = 0x0003;
    // Constant: Defines the object field scale X constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed object field scale X value used anywhere this rule or protocol value is needed.
    private const int ObjectFieldScaleX = 0x0004;
    // Constant: Defines the item field owner constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed item field owner value used anywhere this rule or protocol value is needed.
    private const int ItemFieldOwner = 0x0006;
    // Constant: Defines the item field contained constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed item field contained value used anywhere this rule or protocol value is needed.
    private const int ItemFieldContained = 0x0008;
    // Constant: Defines the item field stack count constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed item field stack count value used anywhere this rule or protocol value is needed.
    private const int ItemFieldStackCount = 0x000E;
    // Constant: Defines the item field duration constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed item field duration value used anywhere this rule or protocol value is needed.
    private const int ItemFieldDuration = 0x000F;
    // Constant: Defines the item field flags constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed item field flags value used anywhere this rule or protocol value is needed.
    private const int ItemFieldFlags = 0x0015;
    // Constant: Defines the item field random properties ID constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed item field random properties ID value used anywhere this rule or protocol value is needed.
    private const int ItemFieldRandomPropertiesId = 0x002C;
    // Constant: Defines the item field durability constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed item field durability value used anywhere this rule or protocol value is needed.
    private const int ItemFieldDurability = 0x002E;
    // Constant: Defines the item field max durability constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed item field max durability value used anywhere this rule or protocol value is needed.
    private const int ItemFieldMaxDurability = 0x002F;

    // Method: ArgumentNullException
    // Purpose: Executes the argument null exception operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the I database service database service = database service ?? throw new value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    private readonly IDatabaseService _databaseService = databaseService ?? throw new ArgumentNullException();
    // Method: ArgumentNullException
    // Purpose: Executes the argument null exception operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the func item template accessor = item template accessor ?? throw new value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    private readonly Func<uint, ItemTemplateRecord?> _itemTemplateAccessor = itemTemplateAccessor ?? throw new ArgumentNullException();

    // Method: ArgumentNullException
    // Purpose: Executes the argument null exception operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the func world template accessor = world template accessor ?? throw new value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    private readonly Func<WorldTemplateDataStore> _worldTemplateAccessor = worldTemplateAccessor ?? throw new ArgumentNullException();

    // Method: ArgumentNullException
    // Purpose: Executes the argument null exception operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the func world game data accessor = world game data accessor ?? throw new value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    private readonly Func<WorldGameDataStore> _worldGameDataAccessor = worldGameDataAccessor ?? throw new ArgumentNullException();

    // Method: GetCharactersForAccountAsync
    // Purpose: Retrieves get characters for account data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - accountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task<IReadOnlyList<CharacterListEntry>> GetCharactersForAccountAsync(uint accountId, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlCommand command = connection.CreateCommand();

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

    // Method: GetPlayerStateTableAvailabilityAsync
    // Purpose: Retrieves get player state table availability data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: CharacterNameExistsAsync
    // Purpose: Executes the character name exists operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - name: Name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous Boolean result that is true when character name exists async succeeds or the requested condition is met.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task<bool> CharacterNameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlCommand command = connection.CreateCommand();

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

    // Method: GetCharacterCountsByAccountAsync
    // Purpose: Retrieves get character counts by account data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task<IReadOnlyDictionary<uint, byte>> GetCharacterCountsByAccountAsync(CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlCommand command = connection.CreateCommand();

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

    // Method: CountCharactersForAccountAsync
    // Purpose: Calculates count characters for account values for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - accountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task<int> CountCharactersForAccountAsync(uint accountId, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlCommand command = connection.CreateCommand();

        command.CommandText = "SELECT COUNT(*) FROM `characters` WHERE `account` = @account;";
        command.Parameters.AddWithValue("@account", accountId);

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    // Method: CreateCharacterAsync
    // Purpose: Applies create character changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - accountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // - request: Request value supplied by the caller for this operation.
    // - createInfo: Create info value supplied by the caller for this operation.
    // - starterItems: Starter items value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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
            PlayerFaction faction = ResolveFactionForRace(request.Race);

            await InsertCharacterAsync(connection, transaction, characterGuid, accountId, request, createInfo, playerBytes, playerBytes2, equipmentCache, initialStats, cancellationToken);
            await InsertHomebindAsync(connection, transaction, characterGuid, createInfo, cancellationToken);
            await InsertCharacterStatsAsync(connection, transaction, characterGuid, initialStats, cancellationToken);
            await InsertCharacterTutorialAsync(connection, transaction, accountId, cancellationToken);
            await InsertCharacterSpellsAsync(connection, transaction, characterGuid, _worldTemplateAccessor().GetPlayerCreateSpells(request.Race, request.Class), request.Race, faction, cancellationToken);
            await InsertCharacterActionsAsync(connection, transaction, characterGuid, _worldTemplateAccessor().GetPlayerCreateActions(request.Race, request.Class), cancellationToken);
            await InsertCharacterReputationsAsync(connection, transaction, characterGuid, request.Race, request.Class, _worldGameDataAccessor().FactionData, cancellationToken);
            await InsertCharacterSkillsAsync(connection, transaction, characterGuid, request.Race, faction, cancellationToken);

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

    // Method: UpdateInventoryPlacementsAsync
    // Purpose: Applies update inventory placements changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - characterGuid: Character GUID identifier used to select the exact record, object, or runtime owner.
    // - placements: Placements value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task<IReadOnlyList<PlayerInventoryItem>> UpdateInventoryPlacementsAsync(
        uint characterGuid,
        IReadOnlyList<PlayerInventoryPlacementUpdate> placements,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(placements);

        if (characterGuid == 0 || placements.Count == 0)
        {
            return [];
        }

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (PlayerInventoryPlacementUpdate placement in placements)
            {
                await using MySqlCommand command = connection.CreateCommand();
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

    // Method: SplitInventoryStackAsync
    // Purpose: Executes the split inventory stack operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - characterGuid: Character GUID identifier used to select the exact record, object, or runtime owner.
    // - sourceItemGuid: Source item GUID identifier used to select the exact record, object, or runtime owner.
    // - destinationBagGuid: Destination bag GUID identifier used to select the exact record, object, or runtime owner.
    // - destinationSlot: Destination slot value supplied by the caller for this operation.
    // - splitCount: Split count value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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
            return [];
        }

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        IReadOnlyList<PlayerInventoryItem> inventory = await LoadPlayerInventoryAsync(connection, characterGuid, cancellationToken);
        PlayerInventoryItem? sourceItem = inventory.FirstOrDefault(item => item.ItemGuid == sourceItemGuid);
        if (sourceItem?.IsContainer is not false)
        {
            return [];
        }

        if (!_worldTemplateAccessor().TryGetItemTemplate(sourceItem.TemplateEntry, out ItemTemplateRecord sourceTemplate))
        {
            return [];
        }

        uint maximumStack = ResolveMaximumStackCount(sourceTemplate);
        uint sourceCount = Math.Max(sourceItem.StackCount, 1u);
        if (maximumStack <= 1 || splitCount >= sourceCount || splitCount > maximumStack)
        {
            return [];
        }

        PlayerInventoryItem? destinationItem = inventory.FirstOrDefault(item => item.BagGuid == destinationBagGuid && item.Slot == destinationSlot);
        if (destinationItem is not null)
        {
            if (destinationItem.ItemGuid == sourceItem.ItemGuid ||
                destinationItem.TemplateEntry != sourceItem.TemplateEntry ||
                destinationItem.IsContainer)
            {
                return [];
            }

            uint destinationCount = Math.Max(destinationItem.StackCount, 1u);
            if (destinationCount >= maximumStack || splitCount > maximumStack - destinationCount)
            {
                return [];
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

    // Method: DeleteCharacterAsync
    // Purpose: Applies delete character changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - accountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // - characterGuid: Character GUID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: GetPlayerForLoginAsync
    // Purpose: Retrieves get player for login data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - accountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // - characterGuid: Character GUID identifier used to select the exact record, object, or runtime owner.
    // - factionResolver: Faction resolver value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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
        await using MySqlCommand command = connection.CreateCommand();

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
        IReadOnlyList<PlayerReputation> reputations = await LoadCharacterReputationAsync(connection, row.Guid, row.Race, row.Class, _worldGameDataAccessor().FactionData, cancellationToken);
        PlayerFaction faction = factionResolver(row.Race);
        IReadOnlyList<PlayerSkill> skills = await LoadCharacterSkillsAsync(connection, row.Guid, row.Race, faction, cancellationToken);

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
            faction);
    }

    // Method: GetCharacterNameQueryAsync
    // Purpose: Retrieves get character name query data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - characterGuid: Character GUID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task<CharacterNameQueryResult?> GetCharacterNameQueryAsync(uint characterGuid, CancellationToken cancellationToken = default)
    {
        if (characterGuid == 0)
        {
            return null;
        }

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlCommand command = connection.CreateCommand();

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

    // Method: SetCharacterOnlineAsync
    // Purpose: Applies set character online changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - characterGuid: Character GUID identifier used to select the exact record, object, or runtime owner.
    // - online: Online value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task SetCharacterOnlineAsync(uint characterGuid, bool online, CancellationToken cancellationToken = default)
    {
        if (characterGuid == 0)
        {
            return;
        }

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlCommand command = connection.CreateCommand();

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

    // Method: SavePlayerPositionAsync
    // Purpose: Applies save player position changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task SavePlayerPositionAsync(PlayerLoginRecord player, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (player.Guid == 0)
        {
            return;
        }

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlCommand command = connection.CreateCommand();

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

    // Method: SavePlayerAsync
    // Purpose: Applies save player changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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
            await using MySqlCommand command = connection.CreateCommand();
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

    // Method: LoadPlayerInventoryAsync
    // Purpose: Retrieves load player inventory data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - characterGuid: Character GUID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task<IReadOnlyList<PlayerInventoryItem>> LoadPlayerInventoryAsync(
        MySqlConnection connection,
        uint characterGuid,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "character_inventory", cancellationToken))
        {
            return [];
        }

        await using MySqlCommand command = connection.CreateCommand();
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

    // Method: GetNextIdAsync
    // Purpose: Retrieves get next ID data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - transaction: Database transaction used to execute this operation without opening unnecessary additional state.
    // - tableName: Table name value supplied by the caller for this operation.
    // - columnName: Column name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task<uint> GetNextIdAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        await using MySqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT COALESCE(MAX(`{columnName}`), 0) + 1 FROM `{tableName}`;";
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToUInt32(result, CultureInfo.InvariantCulture);
    }

    // Method: InsertCharacterAsync
    // Purpose: Applies insert character changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - transaction: Database transaction used to execute this operation without opening unnecessary additional state.
    // - characterGuid: Character GUID identifier used to select the exact record, object, or runtime owner.
    // - accountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // - request: Request value supplied by the caller for this operation.
    // - createInfo: Create info value supplied by the caller for this operation.
    // - playerBytes: Player bytes value supplied by the caller for this operation.
    // - playerBytes2: Player bytes2 value supplied by the caller for this operation.
    // - equipmentCache: Equipment cache value supplied by the caller for this operation.
    // - initialStats: Initial stats value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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
        await using MySqlCommand command = connection.CreateCommand();
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

    // Method: InsertHomebindAsync
    // Purpose: Applies insert homebind changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - transaction: Database transaction used to execute this operation without opening unnecessary additional state.
    // - characterGuid: Character GUID identifier used to select the exact record, object, or runtime owner.
    // - createInfo: Create info value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task InsertHomebindAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        uint characterGuid,
        PlayerCreateInfoRecord createInfo,
        CancellationToken cancellationToken)
    {
        await using MySqlCommand command = connection.CreateCommand();
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

    // Method: InsertCharacterStatsAsync
    // Purpose: Applies insert character stats changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - transaction: Database transaction used to execute this operation without opening unnecessary additional state.
    // - characterGuid: Character GUID identifier used to select the exact record, object, or runtime owner.
    // - stats: Stats value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

        await using MySqlCommand command = connection.CreateCommand();
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

    // Method: InsertCharacterTutorialAsync
    // Purpose: Applies insert character tutorial changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - transaction: Database transaction used to execute this operation without opening unnecessary additional state.
    // - accountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

        await using MySqlCommand command = connection.CreateCommand();
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

    // Method: InsertCharacterSpellsAsync
    // Purpose: Applies insert character spells changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - transaction: Database transaction used to execute this operation without opening unnecessary additional state.
    // - characterGuid: Character GUID identifier used to select the exact record, object, or runtime owner.
    // - starterSpells: Starter spells value supplied by the caller for this operation.
    // - race: Race value supplied by the caller for this operation.
    // - faction: Faction value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task InsertCharacterSpellsAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        uint characterGuid,
        IReadOnlyList<PlayerCreateSpellRecord> starterSpells,
        byte race,
        PlayerFaction faction,
        CancellationToken cancellationToken)
    {
        SortedSet<uint> spellIds = [];
        foreach (PlayerCreateSpellRecord spell in starterSpells)
        {
            if (spell.SpellId != 0)
            {
                spellIds.Add(spell.SpellId);
            }
        }

        foreach (uint languageSpellId in LanguageKnowledgeSystem.BuildInitialLanguageSpellIds(race, faction))
        {
            if (languageSpellId != 0)
            {
                spellIds.Add(languageSpellId);
            }
        }

        if (spellIds.Count == 0 || !await TableExistsAsync(connection, transaction, "character_spell", cancellationToken))
        {
            return;
        }

        foreach (uint spellId in spellIds)
        {
            await using MySqlCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT IGNORE INTO `character_spell`
                    (`guid`, `spell`, `active`, `disabled`)
                VALUES
                    (@guid, @spell, 1, 0);
                """;
            command.Parameters.AddWithValue("@guid", characterGuid);
            command.Parameters.AddWithValue("@spell", spellId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    // Method: InsertCharacterActionsAsync
    // Purpose: Applies insert character actions changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - transaction: Database transaction used to execute this operation without opening unnecessary additional state.
    // - characterGuid: Character GUID identifier used to select the exact record, object, or runtime owner.
    // - starterActions: Starter actions value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

            await using MySqlCommand command = connection.CreateCommand();
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

    // Method: InsertCharacterReputationsAsync
    // Purpose: Applies insert character reputations changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - transaction: Database transaction used to execute this operation without opening unnecessary additional state.
    // - characterGuid: Character GUID identifier used to select the exact record, object, or runtime owner.
    // - race: Race value supplied by the caller for this operation.
    // - playerClass: Player class value supplied by the caller for this operation.
    // - factionData: Faction data value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task InsertCharacterReputationsAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        uint characterGuid,
        byte race,
        byte playerClass,
        FactionDbcDataStore factionData,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PlayerReputation> reputations = ReputationSystem.BuildInitialReputations(factionData, race, playerClass);
        if (reputations.Count == 0 || !await TableExistsAsync(connection, transaction, "character_reputation", cancellationToken))
        {
            return;
        }

        foreach (PlayerReputation reputation in reputations)
        {
            await using MySqlCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO `character_reputation`
                    (`guid`, `faction`, `standing`, `flags`)
                VALUES
                    (@guid, @faction, @standing, @flags)
                ON DUPLICATE KEY UPDATE
                    `standing` = VALUES(`standing`),
                    `flags` = VALUES(`flags`);
                """;
            command.Parameters.AddWithValue("@guid", characterGuid);
            command.Parameters.AddWithValue("@faction", reputation.Faction);
            command.Parameters.AddWithValue("@standing", reputation.Standing);
            command.Parameters.AddWithValue("@flags", reputation.Flags);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    // Method: InsertCharacterSkillsAsync
    // Purpose: Applies insert character skills changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - transaction: Database transaction used to execute this operation without opening unnecessary additional state.
    // - characterGuid: Character GUID identifier used to select the exact record, object, or runtime owner.
    // - race: Race value supplied by the caller for this operation.
    // - faction: Faction value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task InsertCharacterSkillsAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        uint characterGuid,
        byte race,
        PlayerFaction faction,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PlayerSkill> skills = LanguageKnowledgeSystem.BuildInitialLanguageSkills(race, faction);
        if (skills.Count == 0 || !await TableExistsAsync(connection, transaction, "character_skills", cancellationToken))
        {
            return;
        }

        foreach (PlayerSkill skill in skills)
        {
            await using MySqlCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO `character_skills`
                    (`guid`, `skill`, `value`, `max`)
                VALUES
                    (@guid, @skill, @value, @max)
                ON DUPLICATE KEY UPDATE
                    `value` = GREATEST(`value`, VALUES(`value`)),
                    `max` = GREATEST(`max`, VALUES(`max`));
                """;
            command.Parameters.AddWithValue("@guid", characterGuid);
            command.Parameters.AddWithValue("@skill", skill.Skill);
            command.Parameters.AddWithValue("@value", skill.Value);
            command.Parameters.AddWithValue("@max", skill.MaxValue);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    // Method: InsertItemInstanceAsync
    // Purpose: Applies insert item instance changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - transaction: Database transaction used to execute this operation without opening unnecessary additional state.
    // - itemGuid: Item GUID identifier used to select the exact record, object, or runtime owner.
    // - ownerGuid: Owner GUID identifier used to select the exact record, object, or runtime owner.
    // - itemTemplate: Item template value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task InsertItemInstanceAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        uint itemGuid,
        uint ownerGuid,
        ItemTemplateRecord itemTemplate,
        CancellationToken cancellationToken)
    {
        await using MySqlCommand command = connection.CreateCommand();
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

    // Method: InsertCharacterInventoryAsync
    // Purpose: Applies insert character inventory changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - transaction: Database transaction used to execute this operation without opening unnecessary additional state.
    // - characterGuid: Character GUID identifier used to select the exact record, object, or runtime owner.
    // - itemGuid: Item GUID identifier used to select the exact record, object, or runtime owner.
    // - itemTemplate: Item template value supplied by the caller for this operation.
    // - storageSlot: Storage slot value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task InsertCharacterInventoryAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        uint characterGuid,
        uint itemGuid,
        uint itemTemplate,
        byte storageSlot,
        CancellationToken cancellationToken)
    {
        await using MySqlCommand command = connection.CreateCommand();
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

    // Method: LoadCharacterOwnershipForUpdateAsync
    // Purpose: Retrieves load character ownership for update data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - transaction: Database transaction used to execute this operation without opening unnecessary additional state.
    // - characterGuid: Character GUID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task<CharacterOwnershipRecord?> LoadCharacterOwnershipForUpdateAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        uint characterGuid,
        CancellationToken cancellationToken)
    {
        await using MySqlCommand command = connection.CreateCommand();
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

    // Method: IsGuildLeaderAsync
    // Purpose: Validates or evaluates is guild leader rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - transaction: Database transaction used to execute this operation without opening unnecessary additional state.
    // - characterGuid: Character GUID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous Boolean result that is true when is guild leader async succeeds or the requested condition is met.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

        await using MySqlCommand command = connection.CreateCommand();
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

    // Method: DeleteCharacterRowsAsync
    // Purpose: Applies delete character rows changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - transaction: Database transaction used to execute this operation without opening unnecessary additional state.
    // - characterGuid: Character GUID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task DeleteCharacterRowsAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        uint characterGuid,
        CancellationToken cancellationToken)
    {

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

    // Method: DeleteWhereColumnEqualsAsync
    // Purpose: Applies delete where column equals changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - transaction: Database transaction used to execute this operation without opening unnecessary additional state.
    // - tableName: Table name value supplied by the caller for this operation.
    // - columnName: Column name value supplied by the caller for this operation.
    // - value: Value value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

        await using MySqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"DELETE FROM `{tableName}` WHERE `{columnName}` = @value;";
        command.Parameters.AddWithValue("@value", value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    // Method: TableColumnExistsAsync
    // Purpose: Executes the table column exists operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - transaction: Database transaction used to execute this operation without opening unnecessary additional state.
    // - tableName: Table name value supplied by the caller for this operation.
    // - columnName: Column name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous Boolean result that is true when table column exists async succeeds or the requested condition is met.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task<bool> TableColumnExistsAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        await using MySqlCommand command = connection.CreateCommand();
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

    // Method: TableExistsAsync
    // Purpose: Executes the table exists operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - transaction: Database transaction used to execute this operation without opening unnecessary additional state.
    // - tableName: Table name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous Boolean result that is true when table exists async succeeds or the requested condition is met.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task<bool> TableExistsAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using MySqlCommand command = connection.CreateCommand();
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

    // Method: TableExistsAsync
    // Purpose: Executes the table exists operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - tableName: Table name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous Boolean result that is true when table exists async succeeds or the requested condition is met.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task<bool> TableExistsAsync(
        MySqlConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using MySqlCommand command = connection.CreateCommand();
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

    // Method: CreateDefaultTutorialFlags
    // Purpose: Applies create default tutorial flags changes for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the uint[] value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    private static uint[] CreateDefaultTutorialFlags()
    {
        return [.. Enumerable.Repeat(uint.MaxValue, 8)];
    }

    // Method: LoadCharacterStatsAsync
    // Purpose: Retrieves load character stats data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - characterGuid: Character GUID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task<PlayerStats?> LoadCharacterStatsAsync(MySqlConnection connection, uint characterGuid, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "character_stats", cancellationToken))
        {
            return null;
        }

        await using MySqlCommand command = connection.CreateCommand();
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

    // Method: LoadCharacterSpellsAsync
    // Purpose: Retrieves load character spells data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - characterGuid: Character GUID identifier used to select the exact record, object, or runtime owner.
    // - race: Race value supplied by the caller for this operation.
    // - characterClass: Character class value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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
            await using MySqlCommand command = connection.CreateCommand();
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

        return [.. _worldTemplateAccessor()
            .GetPlayerCreateSpells(race, characterClass)
            .Where(spell => spell.SpellId != 0)
            .Select(spell => new PlayerSpell(spell.SpellId, true, false))];
    }

    // Method: LoadCharacterActionsAsync
    // Purpose: Retrieves load character actions data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - characterGuid: Character GUID identifier used to select the exact record, object, or runtime owner.
    // - race: Race value supplied by the caller for this operation.
    // - characterClass: Character class value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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
            await using MySqlCommand command = connection.CreateCommand();
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

        return [.. _worldTemplateAccessor()
            .GetPlayerCreateActions(race, characterClass)
            .Where(action => action.Button < 120)
            .Select(action => new PlayerActionButton(action.Button, action.Action, action.Type))];
    }

    // Method: LoadCharacterTutorialFlagsAsync
    // Purpose: Retrieves load character tutorial flags data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - accountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task<uint[]> LoadCharacterTutorialFlagsAsync(MySqlConnection connection, uint accountId, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "character_tutorial", cancellationToken))
        {
            return CreateDefaultTutorialFlags();
        }

        await using MySqlCommand command = connection.CreateCommand();
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

    // Method: LoadCharacterReputationAsync
    // Purpose: Retrieves load character reputation data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - characterGuid: Character GUID identifier used to select the exact record, object, or runtime owner.
    // - race: Race value supplied by the caller for this operation.
    // - playerClass: Player class value supplied by the caller for this operation.
    // - factionData: Faction data value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task<IReadOnlyList<PlayerReputation>> LoadCharacterReputationAsync(
        MySqlConnection connection,
        uint characterGuid,
        byte race,
        byte playerClass,
        FactionDbcDataStore factionData,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "character_reputation", cancellationToken))
        {
            return ReputationSystem.BuildInitialReputations(factionData, race, playerClass);
        }

        await using MySqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT `faction`, `standing`, `flags`
            FROM `character_reputation`
            WHERE `guid` = @guid
            ORDER BY `faction`;
            """;
        command.Parameters.AddWithValue("@guid", characterGuid);

        List<PlayerReputation> savedReputations = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            uint factionId = Convert.ToUInt32(reader.GetValue(0), CultureInfo.InvariantCulture);
            int reputationListId = factionData.TryGetFaction((int)factionId, out FactionDbcRecord faction)
                ? faction.ReputationIndex
                : -1;

            savedReputations.Add(new PlayerReputation(
                factionId,
                reputationListId,
                Convert.ToInt32(reader.GetValue(1), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(2), CultureInfo.InvariantCulture)));
        }

        return ReputationSystem.BuildCharacterReputations(factionData, race, playerClass, savedReputations);
    }

    // Method: LoadCharacterSkillsAsync
    // Purpose: Retrieves load character skills data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - characterGuid: Character GUID identifier used to select the exact record, object, or runtime owner.
    // - race: Race value supplied by the caller for this operation.
    // - faction: Faction value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task<IReadOnlyList<PlayerSkill>> LoadCharacterSkillsAsync(
        MySqlConnection connection,
        uint characterGuid,
        byte race,
        PlayerFaction faction,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "character_skills", cancellationToken))
        {
            return LanguageKnowledgeSystem.BuildInitialLanguageSkills(race, faction);
        }

        await using MySqlCommand command = connection.CreateCommand();
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

        return LanguageKnowledgeSystem.EnsureInitialLanguageSkills(race, faction, skills);
    }

    // Method: ResolveFactionForRace
    // Purpose: Retrieves resolve faction for race data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - race: Race value supplied by the caller for this operation.
    // Returns: Returns the player faction value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    private static PlayerFaction ResolveFactionForRace(byte race)
    {
        return race switch
        {
            1 or 3 or 4 or 7 => PlayerFaction.Alliance,
            2 or 5 or 6 or 8 => PlayerFaction.Horde,
            _ => PlayerFaction.Neutral,
        };
    }

    // Method: LoadEquippedInventoryAsync
    // Purpose: Retrieves load equipped inventory data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - characterGuids: Character guids value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task<Dictionary<uint, IReadOnlyList<CharacterEquipmentDisplay>>> LoadEquippedInventoryAsync(
        MySqlConnection connection,
        IEnumerable<uint> characterGuids,
        CancellationToken cancellationToken)
    {
        uint[] guids = [.. characterGuids.Distinct()];
        Dictionary<uint, IReadOnlyList<CharacterEquipmentDisplay>> result = [];
        if (guids.Length == 0)
        {
            return result;
        }

        await using MySqlCommand command = connection.CreateCommand();
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

            int equipmentSlot = EquipmentSlotMapper.FromInventoryType(itemTemplate.InventoryType);
            if (equipmentSlot == EquipmentSlotMapper.NoEquipmentSlot && storedSlot < CharacterEquipmentSlotCount)
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

    // Method: MergeEquipment
    // Purpose: Executes the merge equipment operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cachedEquipment: Cached equipment value supplied by the caller for this operation.
    // - inventoryEquipment: Inventory equipment value supplied by the caller for this operation.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: CreateEmptyEquipmentArray
    // Purpose: Applies create empty equipment array changes for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the character equipment display[] value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    private static CharacterEquipmentDisplay[] CreateEmptyEquipmentArray()
    {
        return [.. Enumerable
            .Range(0, CharacterEquipmentSlotCount)
            .Select(_ => new CharacterEquipmentDisplay(0, 0, 0))];
    }

    // Method: ReadItemInstanceField
    // Purpose: Retrieves read item instance field data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - instanceData: Instance data value supplied by the caller for this operation.
    // - fieldIndex: Field index value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: BuildItemInstanceData
    // Purpose: Builds or writes build item instance data output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - itemGuid: Item GUID identifier used to select the exact record, object, or runtime owner.
    // - ownerGuid: Owner GUID identifier used to select the exact record, object, or runtime owner.
    // - itemTemplate: Item template value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    private static string BuildItemInstanceData(uint itemGuid, uint ownerGuid, ItemTemplateRecord itemTemplate)
    {

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

    // Method: ResolveMaximumStackCount
    // Purpose: Retrieves resolve maximum stack count data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - itemTemplate: Item template value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    private static uint ResolveMaximumStackCount(ItemTemplateRecord itemTemplate)
    {
        return itemTemplate.Stackable > 1 ? itemTemplate.Stackable : 1u;
    }

    // Method: NormalizeItemInstanceData
    // Purpose: Converts incoming data into normalize item instance data form for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - instanceData: Instance data value supplied by the caller for this operation.
    // - itemGuid: Item GUID identifier used to select the exact record, object, or runtime owner.
    // - ownerGuid: Owner GUID identifier used to select the exact record, object, or runtime owner.
    // - itemTemplate: Item template value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: SetItemInstanceStackCount
    // Purpose: Applies set item instance stack count changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - instanceData: Instance data value supplied by the caller for this operation.
    // - itemGuid: Item GUID identifier used to select the exact record, object, or runtime owner.
    // - ownerGuid: Owner GUID identifier used to select the exact record, object, or runtime owner.
    // - stackCount: Stack count value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadItemInstanceFields
    // Purpose: Retrieves read item instance fields data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - instanceData: Instance data value supplied by the caller for this operation.
    // Returns: Returns the uint[] value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: UpdateItemInstanceDataAsync
    // Purpose: Applies update item instance data changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - transaction: Database transaction used to execute this operation without opening unnecessary additional state.
    // - itemGuid: Item GUID identifier used to select the exact record, object, or runtime owner.
    // - instanceData: Instance data value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task UpdateItemInstanceDataAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        uint itemGuid,
        string instanceData,
        CancellationToken cancellationToken)
    {
        await using MySqlCommand command = connection.CreateCommand();
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

    // Method: InsertItemInstanceDataAsync
    // Purpose: Applies insert item instance data changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - transaction: Database transaction used to execute this operation without opening unnecessary additional state.
    // - itemGuid: Item GUID identifier used to select the exact record, object, or runtime owner.
    // - ownerGuid: Owner GUID identifier used to select the exact record, object, or runtime owner.
    // - instanceData: Instance data value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task InsertItemInstanceDataAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        uint itemGuid,
        uint ownerGuid,
        string instanceData,
        CancellationToken cancellationToken)
    {
        await using MySqlCommand command = connection.CreateCommand();
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

    // Method: InsertCharacterInventoryAsync
    // Purpose: Applies insert character inventory changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - transaction: Database transaction used to execute this operation without opening unnecessary additional state.
    // - characterGuid: Character GUID identifier used to select the exact record, object, or runtime owner.
    // - itemGuid: Item GUID identifier used to select the exact record, object, or runtime owner.
    // - itemTemplate: Item template value supplied by the caller for this operation.
    // - bagGuid: Bag GUID identifier used to select the exact record, object, or runtime owner.
    // - storageSlot: Storage slot value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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
        await using MySqlCommand command = connection.CreateCommand();
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

    // Method: PackPlayerBytes
    // Purpose: Builds or writes pack player bytes output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - skin: Skin value supplied by the caller for this operation.
    // - face: Face value supplied by the caller for this operation.
    // - hairStyle: Hair style value supplied by the caller for this operation.
    // - hairColor: Hair color value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    private static uint PackPlayerBytes(byte skin, byte face, byte hairStyle, byte hairColor)
    {
        return (uint)(skin | (face << 8) | (hairStyle << 16) | (hairColor << 24));
    }

    // Method: PackPlayerBytes2
    // Purpose: Builds or writes pack player bytes2 output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - facialHair: Facial hair value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    private static uint PackPlayerBytes2(byte facialHair)
    {
        return facialHair;
    }

    // Method: BuildEquipmentCache
    // Purpose: Builds or writes build equipment cache output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - starterItems: Starter items value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
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

        return string.Join(' ', Enumerable.Range(0, CharacterEquipmentSlotCount).SelectMany(slot => new[]
        {
            itemEntries[slot].ToString(CultureInfo.InvariantCulture),
            enchantments[slot].ToString(CultureInfo.InvariantCulture),
        }));
    }

    // Method: ParseEquipmentCache
    // Purpose: Converts incoming data into parse equipment cache form for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - equipmentCache: Equipment cache value supplied by the caller for this operation.
    // - itemTemplateAccessor: Item template accessor value supplied by the caller for this operation.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    private static IReadOnlyList<CharacterEquipmentDisplay> ParseEquipmentCache(
        string equipmentCache,
        Func<uint, ItemTemplateRecord?> itemTemplateAccessor)
    {
        string[] parts = equipmentCache.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

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

    // Method: ReadUInt
    // Purpose: Retrieves read U int data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - stringparts: Stringparts value supplied by the caller for this operation.
    // - index: Index value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ResolvePlayerStats
    // Purpose: Retrieves resolve player stats data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - race: Race value supplied by the caller for this operation.
    // - playerClass: Player class value supplied by the caller for this operation.
    // - level: Level value supplied by the caller for this operation.
    // - storedStats: Stored stats value supplied by the caller for this operation.
    // Returns: Returns the player stats value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: NormalizeLevel
    // Purpose: Converts incoming data into normalize level form for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - level: Level value supplied by the caller for this operation.
    // Returns: Returns the byte value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterRepository so callers do not duplicate validation, protocol, or persistence rules.
    private static byte NormalizeLevel(byte level)
    {
        return level == 0 ? (byte)1 : level;
    }

    // Type: CharacterLoginRow
    // Purpose: Represents character login row data passed through the world server gameplay, session, and character runtime layer.
    // Constructor values:
    // - Guid: GUID identifier used to select the exact record, object, or runtime owner.
    // - AccountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // - Name: Name value supplied by the caller for this operation.
    // - Race: Race value supplied by the caller for this operation.
    // - Class: Class value supplied by the caller for this operation.
    // - Gender: Gender value supplied by the caller for this operation.
    // - Level: Level value supplied by the caller for this operation.
    // - Xp: XP value supplied by the caller for this operation.
    // - Zone: Zone value supplied by the caller for this operation.
    // - Map: Map value supplied by the caller for this operation.
    // - PositionX: Position X value supplied by the caller for this operation.
    // - PositionY: Position Y value supplied by the caller for this operation.
    // - PositionZ: Position Z value supplied by the caller for this operation.
    // - Orientation: Orientation value supplied by the caller for this operation.
    // - Money: Money value supplied by the caller for this operation.
    // - PlayerBytes: Player bytes value supplied by the caller for this operation.
    // - PlayerBytes2: Player bytes2 value supplied by the caller for this operation.
    // - PlayerFlags: Player flags value supplied by the caller for this operation.
    // - AtLogin: At login value supplied by the caller for this operation.
    // - Cinematic: Cinematic value supplied by the caller for this operation.
    // - TotalTime: Total time value supplied by the caller for this operation.
    // - LevelTime: Level time value supplied by the caller for this operation.
    // - Stats: Stats value supplied by the caller for this operation.
    // Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
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

    // Type: CharacterOwnershipRecord
    // Purpose: Represents character ownership record data passed through the world server gameplay, session, and character runtime layer.
    // Constructor values:
    // - AccountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // - Name: Name value supplied by the caller for this operation.
    // - Online: Online value supplied by the caller for this operation.
    // Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
    private sealed record CharacterOwnershipRecord(uint AccountId, string Name, bool Online);

    // Type: CharacterListRow
    // Purpose: Represents character list row data passed through the world server gameplay, session, and character runtime layer.
    // Constructor values:
    // - Guid: GUID identifier used to select the exact record, object, or runtime owner.
    // - Name: Name value supplied by the caller for this operation.
    // - Race: Race value supplied by the caller for this operation.
    // - Class: Class value supplied by the caller for this operation.
    // - Gender: Gender value supplied by the caller for this operation.
    // - Level: Level value supplied by the caller for this operation.
    // - Xp: XP value supplied by the caller for this operation.
    // - Zone: Zone value supplied by the caller for this operation.
    // - Map: Map value supplied by the caller for this operation.
    // - PositionX: Position X value supplied by the caller for this operation.
    // - PositionY: Position Y value supplied by the caller for this operation.
    // - PositionZ: Position Z value supplied by the caller for this operation.
    // - PlayerFlags: Player flags value supplied by the caller for this operation.
    // - AtLogin: At login value supplied by the caller for this operation.
    // - PlayerBytes: Player bytes value supplied by the caller for this operation.
    // - PlayerBytes2: Player bytes2 value supplied by the caller for this operation.
    // - EquipmentCache: Equipment cache value supplied by the caller for this operation.
    // Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
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
