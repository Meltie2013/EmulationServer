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

using System.Text.RegularExpressions;

using EmulationServer.Game.Data.Dbc.Characters;
using EmulationServer.Game.Data.Stores;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.WorldServer.Database.Characters;
using EmulationServer.Game.Characters;
using EmulationServer.WorldServer.Networking.Packets;
using EmulationServer.Game.WorldData;

/**
  * File overview: src/WorldServer/Characters/CharacterCreationService.cs
  * Documents the CharacterCreationService source file in the world character creation validation and character database access area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.WorldServer.Characters;

/**
  * Owns the character creation service behavior for the world character creation validation and character database access layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed partial class CharacterCreationService
{
    /**
      * Defines the constant value for maximum characters per account.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const int MaximumCharactersPerAccount = 10;
    /**
      * Defines the constant value for first backpack slot.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const int FirstBackpackSlot = 23;
    /**
      * Defines the constant value for last backpack slot.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const int LastBackpackSlot = 38;
    /**
      * Defines the constant value for first bag slot.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const int FirstBagSlot = 19;
    /**
      * Defines the constant value for last bag slot.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const int LastBagSlot = 22;
    /**
      * Defines the constant value for no equipment slot.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const int NoEquipmentSlot = -1;
    /**
      * Holds the private character repository state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly CharacterRepository _characterRepository;
    /**
      * Holds the private game data accessor state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly Func<WorldGameDataStore> _gameDataAccessor;
    /**
      * Holds the private world template accessor state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly Func<WorldTemplateDataStore> _worldTemplateAccessor;

    /**
      * Initializes a new CharacterCreationService instance with the dependencies required by the world character creation validation and character database access workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: characterRepository, gameDataAccessor, worldTemplateAccessor.
      */
    public CharacterCreationService(
        CharacterRepository characterRepository,
        Func<WorldGameDataStore> gameDataAccessor,
        Func<WorldTemplateDataStore> worldTemplateAccessor)
    {
        _characterRepository = characterRepository ?? throw new ArgumentNullException();
        _gameDataAccessor = gameDataAccessor ?? throw new ArgumentNullException();
        _worldTemplateAccessor = worldTemplateAccessor ?? throw new ArgumentNullException();
    }

    /**
      * Resolves the character list value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: accountId, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public Task<IReadOnlyList<CharacterListEntry>> GetCharacterListAsync(uint accountId, CancellationToken cancellationToken)
    {
        return _characterRepository.GetCharactersForAccountAsync(accountId, cancellationToken);
    }

    /**
      * Creates the character result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: accountId, request, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

        IReadOnlyList<StarterItemCreateData> starterItems = ResolveStarterItems(request.Race, request.Class, outfit, worldTemplates);
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

    /**
      * Performs the delete character operation for the world character creation validation and character database access workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: accountId, clientGuid, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Performs the extract character guid operation for the world character creation validation and character database access workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: clientGuid.
      */
    private static uint ExtractCharacterGuid(ulong clientGuid)
    {
        // Vanilla clients send the full ObjectGuid back to CMSG_CHAR_DELETE.
        // The character table stores the low counter in characters.guid.
        // This also supports the current milestone enum packet, which sends the
        // low guid directly until the full object-guid builder is implemented.
        return (uint)(clientGuid & uint.MaxValue);
    }

    /**
      * Validates validate request state before it is used by another server component.
      * Validation failures are raised as close to the source as possible so configuration, packet, and data problems are easier to diagnose.
      * Inputs used by this operation: request.
      */
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

    /**
      * Validates validate customization state before it is used by another server component.
      * Validation failures are raised as close to the source as possible so configuration, packet, and data problems are easier to diagnose.
      * Inputs used by this operation: characterData, request.
      */
    private static CharacterCustomizationValidationResult ValidateCustomization(CharacterDbcDataStore characterData, CharacterCreateRequest request)
    {
        // Vanilla CharSections.dbc stores character creation values as:
        //   section 0: skin color    -> VariationIndex = 0,         ColorIndex = skin
        //   section 1: face texture  -> VariationIndex = face,      ColorIndex = skin
        //   section 3: hair texture  -> VariationIndex = hairStyle, ColorIndex = hairColor
        // Hair style geometry is stored separately in CharHairGeosets.dbc.
        // Facial hair, earrings, and piercings are stored in CharacterFacialHairStyles.dbc.
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

    /**
      * Carries immutable character customization validation result data for the world character creation validation and character database access layer.
      * Records in this project are used as explicit transfer models so packet parsing, database repositories, and runtime systems can pass strongly typed values without mutating shared state.
      * Positional fields carried by this record: SkinValid, FaceValid, HairColorValid, HairStyleValid, FacialHairValid.
      */
    private sealed record CharacterCustomizationValidationResult(
        bool SkinValid,
        bool FaceValid,
        bool HairColorValid,
        bool HairStyleValid,
        bool FacialHairValid)
    {
        /**
          * Stores the default is valid value used when the caller does not supply an override.
          * Centralizing the default keeps configuration and packet behavior consistent across the server process.
          */
        public bool IsValid => SkinValid && FaceValid && HairColorValid && HairStyleValid && FacialHairValid;

        /**
          * Performs the to string operation for the world character creation validation and character database access workflow.
          * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
          */
        public override string ToString()
        {
            return $"validation detail: skin={SkinValid}, face={FaceValid}, hairColor={HairColorValid}, hairStyle={HairStyleValid}, facialHair={FacialHairValid}.";
        }
    }

    /**
      * Resolves the starter items value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: race, characterClass, outfit, worldTemplates.
      */
    private static IReadOnlyList<StarterItemCreateData> ResolveStarterItems(
        byte race,
        byte characterClass,
        CharStartOutfitDbcRecord outfit,
        WorldTemplateDataStore worldTemplates)
    {
        IReadOnlyList<PlayerCreateItemRecord> databaseStarterItems = worldTemplates.GetPlayerCreateItems(race, characterClass);

        uint[] itemEntries = outfit.Items
            .Where(item => item.ItemId > 0)
            .Select(item => (uint)item.ItemId)
            .ToArray();

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

    /**
      * Adds world database starter items after the DBC outfit has been placed.
      * The world table is treated as additive so an incomplete playercreateinfo_item table cannot suppress equipped starter gear.
      */
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

        uint[] itemEntries = starterItems
            .Where(item => item.ItemId != 0)
            .Select(item => item.ItemId)
            .Distinct()
            .ToArray();

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

    /**
      * Resolves the starter items from world table value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: starterItems, worldTemplates.
      */
    private static IReadOnlyList<StarterItemCreateData> ResolveStarterItemsFromWorldTable(
        byte race,
        byte characterClass,
        IReadOnlyList<PlayerCreateItemRecord> starterItems,
        WorldTemplateDataStore worldTemplates)
    {
        uint[] itemEntries = starterItems
            .Where(item => item.ItemId != 0)
            .Select(item => item.ItemId)
            .ToArray();

        IReadOnlyDictionary<uint, ItemTemplateRecord> templates = worldTemplates.GetItemTemplates(itemEntries);
        List<StarterItemCreateData> result = [];
        int nextBackpackSlot = FirstBackpackSlot;
        int nextBagSlot = FirstBagSlot;

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
                if (!TryAddStarterItem(result, template, template.InventoryType, ref nextBackpackSlot, ref nextBagSlot))
                {
                    Logger.Write(LogType.WARNING, $"playercreateinfo_item entry {item.ItemId} could not be placed because the backpack starter slots are full.", "CharacterCreationService");
                    break;
                }
            }
        }

        return result;
    }

    /**
      * Tries to resolve the add starter item value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: result, template, inventoryType, nextBackpackSlot, nextBagSlot.
      */
    private static bool TryAddStarterItem(
        List<StarterItemCreateData> result,
        ItemTemplateRecord template,
        byte inventoryType,
        ref int nextBackpackSlot,
        ref int nextBagSlot)
    {
        int equipmentSlot = MapInventoryTypeToEquipmentSlot(inventoryType);
        int storageSlot;

        if (equipmentSlot != NoEquipmentSlot && result.All(item => item.EquipmentSlot != equipmentSlot))
        {
            storageSlot = equipmentSlot;
        }
        else if (inventoryType == 18 && nextBagSlot <= LastBagSlot)
        {
            storageSlot = nextBagSlot++;
            equipmentSlot = NoEquipmentSlot;
        }
        else if (nextBackpackSlot <= LastBackpackSlot)
        {
            storageSlot = nextBackpackSlot++;
            equipmentSlot = NoEquipmentSlot;
        }
        else
        {
            return false;
        }

        result.Add(new StarterItemCreateData(template, (byte)storageSlot, equipmentSlot));
        return true;
    }

    /**
      * Resolves the inventory type value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: item, template.
      */
    private static byte ResolveInventoryType(CharStartOutfitItemDbcRecord item, ItemTemplateRecord template)
    {
        return item.InventorySlotId is > 0 and <= byte.MaxValue
            ? (byte)item.InventorySlotId
            : template.InventoryType;
    }

    /**
      * Performs the map inventory type to equipment slot operation for the world character creation validation and character database access workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: inventoryType.
      */
    private static int MapInventoryTypeToEquipmentSlot(byte inventoryType)
    {
        // CharStartOutfit.dbc stores item inventory type values, not character
        // equipment slot indexes. These are the Vanilla equipment slots used by
        // character_inventory and SMSG_CHAR_ENUM.
        return inventoryType switch
        {
            1 => 0,   // Head
            2 => 1,   // Neck
            3 => 2,   // Shoulders
            4 => 3,   // Shirt/body
            5 => 4,   // Chest
            6 => 5,   // Waist
            7 => 6,   // Legs
            8 => 7,   // Feet
            9 => 8,   // Wrists
            10 => 9,  // Hands
            11 => 10, // First finger
            12 => 12, // First trinket
            13 => 15, // One-hand weapon
            14 => 16, // Shield
            15 => 17, // Ranged
            16 => 14, // Back
            17 => 15, // Two-hand weapon
            19 => 18, // Tabard
            20 => 4,  // Robe/chest
            21 => 15, // Main hand
            22 => 16, // Off hand
            23 => 16, // Held in off hand
            25 => 17, // Thrown
            26 => 17, // Ranged right
            28 => 17, // Relic
            _ => NoEquipmentSlot,
        };
    }

    /**
      * Determines whether valid character name for the world character creation validation and character database access workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: name.
      */
    private static bool IsValidCharacterName(string name)
    {
        return name.Length is >= 2 and <= 12 && CharacterNameRegex().IsMatch(name);
    }

    /**
      * Performs the character name regex operation for the world character creation validation and character database access workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      */
    [GeneratedRegex("^[A-Za-z]+$", RegexOptions.CultureInvariant)]
    private static partial Regex CharacterNameRegex();
}
