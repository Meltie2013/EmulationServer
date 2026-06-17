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
// File: src/WorldServer/Characters/CharacterCreationService.cs
// Purpose: Contains character creation service code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Text.RegularExpressions;

using EmulationServer.Game.Data.Dbc.Characters;
using EmulationServer.Game.Data.Stores;
using EmulationServer.Game.Items;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.WorldServer.Database.Characters;
using EmulationServer.Game.Characters;
using EmulationServer.WorldServer.Networking.Packets;
using EmulationServer.Game.WorldData;

namespace EmulationServer.WorldServer.Characters;

// Type: CharacterCreationService
// Purpose: Provides character creation service behavior for the world server gameplay, session, and character runtime layer.
// Constructor values:
// - characterRepository: Character repository value supplied by the caller for this operation.
// - gameDataAccessor: Game data accessor value supplied by the caller for this operation.
// - worldTemplateAccessor: World template accessor value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed partial class CharacterCreationService(
    CharacterRepository characterRepository,
    Func<WorldGameDataStore> gameDataAccessor,
    Func<WorldTemplateDataStore> worldTemplateAccessor)
{

    // Constant: Defines the maximum characters per account constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed maximum characters per account value used anywhere this rule or protocol value is needed.
    private const int MaximumCharactersPerAccount = 10;

    // Constant: Defines the first backpack slot constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed first backpack slot value used anywhere this rule or protocol value is needed.
    private const int FirstBackpackSlot = 23;

    // Constant: Defines the last backpack slot constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed last backpack slot value used anywhere this rule or protocol value is needed.
    private const int LastBackpackSlot = 38;

    // Constant: Defines the first bag slot constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed first bag slot value used anywhere this rule or protocol value is needed.
    private const int FirstBagSlot = 19;

    // Constant: Defines the last bag slot constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed last bag slot value used anywhere this rule or protocol value is needed.
    private const int LastBagSlot = 22;

    // Method: ArgumentNullException
    // Purpose: Executes the argument null exception operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the character repository character repository = character repository ?? throw new value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterCreationService so callers do not duplicate validation, protocol, or persistence rules.
    private readonly CharacterRepository _characterRepository = characterRepository ?? throw new ArgumentNullException();

    // Method: ArgumentNullException
    // Purpose: Executes the argument null exception operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the func game data accessor = game data accessor ?? throw new value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterCreationService so callers do not duplicate validation, protocol, or persistence rules.
    private readonly Func<WorldGameDataStore> _gameDataAccessor = gameDataAccessor ?? throw new ArgumentNullException();

    // Method: ArgumentNullException
    // Purpose: Executes the argument null exception operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the func world template accessor = world template accessor ?? throw new value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterCreationService so callers do not duplicate validation, protocol, or persistence rules.
    private readonly Func<WorldTemplateDataStore> _worldTemplateAccessor = worldTemplateAccessor ?? throw new ArgumentNullException();

    // Method: GetCharacterListAsync
    // Purpose: Retrieves get character list data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - accountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to CharacterCreationService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public Task<IReadOnlyList<CharacterListEntry>> GetCharacterListAsync(uint accountId, CancellationToken cancellationToken)
    {
        return _characterRepository.GetCharactersForAccountAsync(accountId, cancellationToken);
    }

    // Method: CreateCharacterAsync
    // Purpose: Applies create character changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - accountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // - request: Request value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to CharacterCreationService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task<CharacterCreateResult> CreateCharacterAsync(
        uint accountId,
        CharacterCreateRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        CharacterCreateResult validationResult = ValidateRequest(request);
        if (validationResult != CharacterCreateResult.Success)
        {
            Logger.Write(LogType.WARNING, $"Character create rejected for account {accountId}: request validation returned {validationResult}.", "CharacterCreationService");
            return validationResult;
        }

        int characterCount = await _characterRepository.CountCharactersForAccountAsync(accountId, cancellationToken);
        if (characterCount >= MaximumCharactersPerAccount)
        {
            Logger.Write(LogType.WARNING, $"Character create rejected for account {accountId}: account character limit reached.", "CharacterCreationService");
            return CharacterCreateResult.AccountLimit;
        }

        if (await _characterRepository.CharacterNameExistsAsync(request.Name, cancellationToken))
        {
            Logger.Write(LogType.WARNING, $"Character create rejected for account {accountId}: name '{request.Name}' is already in use.", "CharacterCreationService");
            return CharacterCreateResult.NameInUse;
        }

        CharacterDbcDataStore characterData = _gameDataAccessor().CharacterData;
        if (!characterData.TryGetRace(request.Race, out _) || !characterData.TryGetClass(request.Class, out _))
        {
            Logger.Write(LogType.FAILED, $"Character create failed for account {accountId}: missing DBC race={request.Race} or class={request.Class}.", "CharacterCreationService");
            return CharacterCreateResult.Failed;
        }

        if (!characterData.IsRaceClassAllowed(request.Race, request.Class))
        {
            Logger.Write(LogType.FAILED, $"Character create failed for account {accountId}: race={request.Race}, class={request.Class} is not allowed by CharBaseInfo.dbc.", "CharacterCreationService");
            return CharacterCreateResult.Failed;
        }

        CharacterCustomizationValidationResult customizationResult = ValidateCustomization(characterData, request);
        if (!customizationResult.IsValid)
        {
            Logger.Write(LogType.FAILED, $"Character create failed for account {accountId}: invalid customization race={request.Race}, gender={request.Gender}, skin={request.Skin}, face={request.Face}, hairStyle={request.HairStyle}, hairColor={request.HairColor}, facialHair={request.FacialHair}. {customizationResult}", "CharacterCreationService");
            return CharacterCreateResult.Failed;
        }

        if (!characterData.TryGetStartOutfit(request.Race, request.Class, request.Gender, request.OutfitId, out CharStartOutfitDbcRecord outfit))
        {
            Logger.Write(LogType.FAILED, $"Character create failed for account {accountId}: missing CharStartOutfit row race={request.Race}, class={request.Class}, gender={request.Gender}, outfit={request.OutfitId}.", "CharacterCreationService");
            return CharacterCreateResult.Failed;
        }

        WorldTemplateDataStore worldTemplates = _worldTemplateAccessor();
        if (!worldTemplates.TryGetPlayerCreateInfo(request.Race, request.Class, out PlayerCreateInfoRecord createInfo))
        {
            Logger.Write(LogType.FAILED, $"Missing playercreateinfo row in memory for race={request.Race}, class={request.Class}.", "CharacterCreationService");
            return CharacterCreateResult.Failed;
        }

        List<StarterItemCreateData> starterItems = ResolveStarterItems(request.Race, request.Class, outfit, worldTemplates);
        Logger.Write(LogType.DATABASE, $"Resolved {starterItems.Count} starter item(s) for new character '{request.Name}' race={request.Race}, class={request.Class}, outfit={request.OutfitId}.", "CharacterCreationService");

        try
        {
            await _characterRepository.CreateCharacterAsync(accountId, request, createInfo, starterItems, cancellationToken);
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.FAILED, $"Character create database save failed for account {accountId}, name '{request.Name}': {exception.Message}", "CharacterCreationService");
            return CharacterCreateResult.Failed;
        }

        return CharacterCreateResult.Success;
    }

    // Method: DeleteCharacterAsync
    // Purpose: Applies delete character changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - accountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // - clientGuid: Client GUID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to CharacterCreationService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task<CharacterDeleteServiceResult> DeleteCharacterAsync(
        uint accountId,
        ulong clientGuid,
        CancellationToken cancellationToken)
    {
        uint characterGuid = ExtractCharacterGuid(clientGuid);
        if (characterGuid == 0)
        {
            Logger.Write(LogType.WARNING, $"Character delete rejected for account {accountId}: invalid guid 0x{clientGuid:X16}.", "CharacterCreationService");
            return CharacterDeleteServiceResult.Failed;
        }

        try
        {
            CharacterDeleteRepositoryResult result = await _characterRepository.DeleteCharacterAsync(accountId, characterGuid, cancellationToken);
            switch (result)
            {
                case CharacterDeleteRepositoryResult.Success:
                    return CharacterDeleteServiceResult.Success;

                case CharacterDeleteRepositoryResult.AccountMismatch:
                    Logger.Write(LogType.WARNING, $"Character delete security rejection for account {accountId}: attempted to delete guid {characterGuid} that is not owned by the authenticated account.", "CharacterCreationService");
                    return CharacterDeleteServiceResult.SecurityMismatch;

                case CharacterDeleteRepositoryResult.GuildLeader:
                    Logger.Write(LogType.WARNING, $"Character delete rejected for account {accountId}, guid {characterGuid}: character is a guild leader.", "CharacterCreationService");
                    return CharacterDeleteServiceResult.Failed;

                case CharacterDeleteRepositoryResult.Online:
                    Logger.Write(LogType.WARNING, $"Character delete rejected for account {accountId}, guid {characterGuid}: character is marked online.", "CharacterCreationService");
                    return CharacterDeleteServiceResult.Failed;

                case CharacterDeleteRepositoryResult.NotFound:
                    Logger.Write(LogType.WARNING, $"Character delete rejected for account {accountId}: guid {characterGuid} was not found.", "CharacterCreationService");
                    return CharacterDeleteServiceResult.Failed;

                default:
                    Logger.Write(LogType.WARNING, $"Character delete rejected for account {accountId}, guid {characterGuid}: repository returned {result}.", "CharacterCreationService");
                    return CharacterDeleteServiceResult.Failed;
            }
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.FAILED, $"Character delete database operation failed for account {accountId}, guid {characterGuid}: {exception.Message}", "CharacterCreationService");
            return CharacterDeleteServiceResult.Failed;
        }
    }

    // Method: ExtractCharacterGuid
    // Purpose: Executes the extract character GUID operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - clientGuid: Client GUID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterCreationService so callers do not duplicate validation, protocol, or persistence rules.
    private static uint ExtractCharacterGuid(ulong clientGuid)
    {

        return (uint)(clientGuid & uint.MaxValue);
    }

    // Method: ValidateRequest
    // Purpose: Validates or evaluates validate request rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - request: Request value supplied by the caller for this operation.
    // Returns: Returns the character create result value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterCreationService so callers do not duplicate validation, protocol, or persistence rules.
    private static CharacterCreateResult ValidateRequest(CharacterCreateRequest request)
    {
        if (!IsValidCharacterName(request.Name))
        {
            return CharacterCreateResult.NameInvalid;
        }

        if (request.Gender > 1)
        {
            return CharacterCreateResult.Failed;
        }

        return CharacterCreateResult.Success;
    }

    // Method: ValidateCustomization
    // Purpose: Validates or evaluates validate customization rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - characterData: Character data value supplied by the caller for this operation.
    // - request: Request value supplied by the caller for this operation.
    // Returns: Returns the character customization validation result value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterCreationService so callers do not duplicate validation, protocol, or persistence rules.
    private static CharacterCustomizationValidationResult ValidateCustomization(CharacterDbcDataStore characterData, CharacterCreateRequest request)
    {

        bool skinValid = characterData.IsSectionCustomizationValid(request.Race, request.Gender, 0, 0, request.Skin);
        bool faceValid = characterData.IsSectionCustomizationValid(request.Race, request.Gender, 1, request.Face, request.Skin);
        bool hairColorValid = characterData.IsSectionCustomizationValid(request.Race, request.Gender, 3, request.HairStyle, request.HairColor);
        bool hairStyleValid = characterData.IsHairStyleValid(request.Race, request.Gender, request.HairStyle);
        bool facialHairValid = request.FacialHair == 0 || characterData.IsFacialHairValid(request.Race, request.Gender, request.FacialHair);

        return new CharacterCustomizationValidationResult(
            skinValid,
            faceValid,
            hairColorValid,
            hairStyleValid,
            facialHairValid);
    }

    // Type: CharacterCustomizationValidationResult
    // Purpose: Represents character customization validation result data passed through the world server gameplay, session, and character runtime layer.
    // Constructor values:
    // - SkinValid: Skin valid value supplied by the caller for this operation.
    // - FaceValid: Face valid value supplied by the caller for this operation.
    // - HairColorValid: Hair color valid value supplied by the caller for this operation.
    // - HairStyleValid: Hair style valid value supplied by the caller for this operation.
    // - FacialHairValid: Facial hair valid value supplied by the caller for this operation.
    // Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
    private sealed record CharacterCustomizationValidationResult(
        bool SkinValid,
        bool FaceValid,
        bool HairColorValid,
        bool HairStyleValid,
        bool FacialHairValid)
    {

        // Property: Gets or sets the is valid value used by the world server gameplay, session, and character runtime layer.
        // Value: is valid value exposed by the owning type.
        public bool IsValid => SkinValid && FaceValid && HairColorValid && HairStyleValid && FacialHairValid;

        // Method: ToString
        // Purpose: Executes the to string operation for the world server gameplay, session, and character runtime layer.
        // Parameters: none.
        // Returns: Returns the string value produced by this operation.
        // Notes: This keeps the operation scoped to CharacterCustomizationValidationResult so callers do not duplicate validation, protocol, or persistence rules.
        public override string ToString()
        {
            return $"validation detail: skin={SkinValid}, face={FaceValid}, hairColor={HairColorValid}, hairStyle={HairStyleValid}, facialHair={FacialHairValid}.";
        }
    }

    // Method: ResolveStarterItems
    // Purpose: Retrieves resolve starter items data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - race: Race value supplied by the caller for this operation.
    // - characterClass: Character class value supplied by the caller for this operation.
    // - outfit: Outfit value supplied by the caller for this operation.
    // - worldTemplates: World templates value supplied by the caller for this operation.
    // Returns: Returns the list value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterCreationService so callers do not duplicate validation, protocol, or persistence rules.
    private static List<StarterItemCreateData> ResolveStarterItems(
        byte race,
        byte characterClass,
        CharStartOutfitDbcRecord outfit,
        WorldTemplateDataStore worldTemplates)
    {
        IReadOnlyList<PlayerCreateItemRecord> databaseStarterItems = worldTemplates.GetPlayerCreateItems(race, characterClass);

        uint[] itemEntries = [.. outfit.Items
            .Where(item => item.ItemId > 0)
            .Select(item => (uint)item.ItemId)];

        IReadOnlyDictionary<uint, ItemTemplateRecord> templates = worldTemplates.GetItemTemplates(itemEntries);
        List<StarterItemCreateData> result = [];
        int nextBackpackSlot = FirstBackpackSlot;
        int nextBagSlot = FirstBagSlot;

        foreach (CharStartOutfitItemDbcRecord item in outfit.Items)
        {
            if (item.ItemId <= 0)
            {
                continue;
            }

            uint entry = (uint)item.ItemId;
            if (!templates.TryGetValue(entry, out ItemTemplateRecord? template))
            {
                Logger.Write(LogType.WARNING, $"Starter outfit item {entry} is missing from item_template and will be skipped.", "CharacterCreationService");
                continue;
            }

            byte inventoryType = ResolveInventoryType(item, template);
            if (!TryAddStarterItem(result, template, inventoryType, ref nextBackpackSlot, ref nextBagSlot))
            {
                Logger.Write(LogType.WARNING, $"Starter outfit item {entry} could not be placed because the backpack starter slots are full.", "CharacterCreationService");
            }
        }

        AddPlayerCreateInfoStarterItems(databaseStarterItems, worldTemplates, result, ref nextBackpackSlot, ref nextBagSlot);
        return result;
    }

    // Method: AddPlayerCreateInfoStarterItems
    // Purpose: Applies add player create info starter items changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - starterItems: Starter items value supplied by the caller for this operation.
    // - worldTemplates: World templates value supplied by the caller for this operation.
    // - result: Result value supplied by the caller for this operation.
    // - nextBackpackSlot: Next backpack slot value supplied by the caller for this operation.
    // - nextBagSlot: Next bag slot value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to CharacterCreationService so callers do not duplicate validation, protocol, or persistence rules.
    private static void AddPlayerCreateInfoStarterItems(
        IReadOnlyList<PlayerCreateItemRecord> starterItems,
        WorldTemplateDataStore worldTemplates,
        List<StarterItemCreateData> result,
        ref int nextBackpackSlot,
        ref int nextBagSlot)
    {
        if (starterItems.Count == 0)
        {
            return;
        }

        uint[] itemEntries = [.. starterItems
            .Where(item => item.ItemId != 0)
            .Select(item => item.ItemId)
            .Distinct()];

        IReadOnlyDictionary<uint, ItemTemplateRecord> templates = worldTemplates.GetItemTemplates(itemEntries);

        foreach (PlayerCreateItemRecord item in starterItems)
        {
            if (item.ItemId == 0)
            {
                continue;
            }

            if (!templates.TryGetValue(item.ItemId, out ItemTemplateRecord? template))
            {
                Logger.Write(LogType.WARNING, $"playercreateinfo_item entry {item.ItemId} is missing from item_template and will be skipped.", "CharacterCreationService");
                continue;
            }

            byte amount = item.Amount == 0 ? (byte)1 : item.Amount;
            for (byte index = 0; index < amount; index++)
            {
                if (index == 0 && result.Any(existing => existing.Template.Entry == item.ItemId))
                {
                    continue;
                }

                if (!TryAddStarterItem(result, template, template.InventoryType, ref nextBackpackSlot, ref nextBagSlot))
                {
                    Logger.Write(LogType.WARNING, $"playercreateinfo_item entry {item.ItemId} could not be placed because the backpack starter slots are full.", "CharacterCreationService");
                    break;
                }
            }
        }
    }

    // Method: TryAddStarterItem
    // Purpose: Executes the try add starter item operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - result: Result value supplied by the caller for this operation.
    // - template: Template value supplied by the caller for this operation.
    // - inventoryType: Inventory type value supplied by the caller for this operation.
    // - nextBackpackSlot: Next backpack slot value supplied by the caller for this operation.
    // - nextBagSlot: Next bag slot value supplied by the caller for this operation.
    // Returns: Returns true when try add starter item succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CharacterCreationService so callers do not duplicate validation, protocol, or persistence rules.
    private static bool TryAddStarterItem(
        List<StarterItemCreateData> result,
        ItemTemplateRecord template,
        byte inventoryType,
        ref int nextBackpackSlot,
        ref int nextBagSlot)
    {
        int equipmentSlot = EquipmentSlotMapper.FromInventoryType(inventoryType);
        int storageSlot;

        if (equipmentSlot != EquipmentSlotMapper.NoEquipmentSlot && result.All(item => item.EquipmentSlot != equipmentSlot))
        {
            storageSlot = equipmentSlot;
        }
        else if (inventoryType == 18 && nextBagSlot <= LastBagSlot)
        {
            storageSlot = nextBagSlot++;
            equipmentSlot = EquipmentSlotMapper.NoEquipmentSlot;
        }
        else if (nextBackpackSlot <= LastBackpackSlot)
        {
            storageSlot = nextBackpackSlot++;
            equipmentSlot = EquipmentSlotMapper.NoEquipmentSlot;
        }
        else
        {
            return false;
        }

        result.Add(new StarterItemCreateData(template, (byte)storageSlot, equipmentSlot));
        return true;
    }

    // Method: ResolveInventoryType
    // Purpose: Retrieves resolve inventory type data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - item: Item value supplied by the caller for this operation.
    // - template: Template value supplied by the caller for this operation.
    // Returns: Returns the byte value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterCreationService so callers do not duplicate validation, protocol, or persistence rules.
    private static byte ResolveInventoryType(CharStartOutfitItemDbcRecord item, ItemTemplateRecord template)
    {
        return item.InventorySlotId is > 0 and <= byte.MaxValue
            ? (byte)item.InventorySlotId
            : template.InventoryType;
    }

    // Method: IsValidCharacterName
    // Purpose: Validates or evaluates is valid character name rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - name: Name value supplied by the caller for this operation.
    // Returns: Returns true when is valid character name succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CharacterCreationService so callers do not duplicate validation, protocol, or persistence rules.
    private static bool IsValidCharacterName(string name)
    {
        return name.Length is >= 2 and <= 12 && CharacterNameRegex().IsMatch(name);
    }

    [GeneratedRegex("^[A-Za-z]+$", RegexOptions.CultureInvariant)]
    // Method: CharacterNameRegex
    // Purpose: Executes the character name regex operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the regex value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterCreationService so callers do not duplicate validation, protocol, or persistence rules.
    private static partial Regex CharacterNameRegex();
}
