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
using EmulationServer.WorldServer.Networking.Packets;
using EmulationServer.WorldServer.WorldData;

namespace EmulationServer.WorldServer.Characters;

public sealed partial class CharacterCreationService
{
    private const int MaximumCharactersPerAccount = 10;
    private const int FirstBackpackSlot = 23;
    private const int LastBackpackSlot = 38;
    private const int FirstBagSlot = 19;
    private const int LastBagSlot = 22;
    private const int NoEquipmentSlot = -1;

    private readonly CharacterRepository _characterRepository;
    private readonly Func<WorldGameDataStore> _gameDataAccessor;
    private readonly Func<WorldTemplateDataStore> _worldTemplateAccessor;

    public CharacterCreationService(
        CharacterRepository characterRepository,
        Func<WorldGameDataStore> gameDataAccessor,
        Func<WorldTemplateDataStore> worldTemplateAccessor)
    {
        _characterRepository = characterRepository ?? throw new ArgumentNullException(nameof(characterRepository));
        _gameDataAccessor = gameDataAccessor ?? throw new ArgumentNullException(nameof(gameDataAccessor));
        _worldTemplateAccessor = worldTemplateAccessor ?? throw new ArgumentNullException(nameof(worldTemplateAccessor));
    }

    public Task<IReadOnlyList<CharacterListEntry>> GetCharacterListAsync(uint accountId, CancellationToken cancellationToken)
    {
        return _characterRepository.GetCharactersForAccountAsync(accountId, cancellationToken);
    }

    public async Task<CharacterCreateResult> CreateCharacterAsync(
        uint accountId,
        CharacterCreateRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        CharacterCreateResult validationResult = ValidateRequest(request);
        if (validationResult != CharacterCreateResult.Success)
        {
            Logger.Write(LogType.WARNING, $"Character create rejected for account {accountId}: request validation returned {validationResult}.", nameof(CharacterCreationService));
            return validationResult;
        }

        int characterCount = await _characterRepository.CountCharactersForAccountAsync(accountId, cancellationToken);
        if (characterCount >= MaximumCharactersPerAccount)
        {
            Logger.Write(LogType.WARNING, $"Character create rejected for account {accountId}: account character limit reached.", nameof(CharacterCreationService));
            return CharacterCreateResult.AccountLimit;
        }

        if (await _characterRepository.CharacterNameExistsAsync(request.Name, cancellationToken))
        {
            Logger.Write(LogType.WARNING, $"Character create rejected for account {accountId}: name '{request.Name}' is already in use.", nameof(CharacterCreationService));
            return CharacterCreateResult.NameInUse;
        }

        CharacterDbcDataStore characterData = _gameDataAccessor().CharacterData;
        if (!characterData.TryGetRace(request.Race, out _) || !characterData.TryGetClass(request.Class, out _))
        {
            Logger.Write(LogType.FAILED, $"Character create failed for account {accountId}: missing DBC race={request.Race} or class={request.Class}.", nameof(CharacterCreationService));
            return CharacterCreateResult.Failed;
        }

        if (!characterData.IsRaceClassAllowed(request.Race, request.Class))
        {
            Logger.Write(LogType.FAILED, $"Character create failed for account {accountId}: race={request.Race}, class={request.Class} is not allowed by CharBaseInfo.dbc.", nameof(CharacterCreationService));
            return CharacterCreateResult.Failed;
        }

        CharacterCustomizationValidationResult customizationResult = ValidateCustomization(characterData, request);
        if (!customizationResult.IsValid)
        {
            Logger.Write(LogType.FAILED, $"Character create failed for account {accountId}: invalid customization race={request.Race}, gender={request.Gender}, skin={request.Skin}, face={request.Face}, hairStyle={request.HairStyle}, hairColor={request.HairColor}, facialHair={request.FacialHair}. {customizationResult}", nameof(CharacterCreationService));
            return CharacterCreateResult.Failed;
        }

        if (!characterData.TryGetStartOutfit(request.Race, request.Class, request.Gender, request.OutfitId, out CharStartOutfitDbcRecord outfit))
        {
            Logger.Write(LogType.FAILED, $"Character create failed for account {accountId}: missing CharStartOutfit row race={request.Race}, class={request.Class}, gender={request.Gender}, outfit={request.OutfitId}.", nameof(CharacterCreationService));
            return CharacterCreateResult.Failed;
        }

        WorldTemplateDataStore worldTemplates = _worldTemplateAccessor();
        if (!worldTemplates.TryGetPlayerCreateInfo(request.Race, request.Class, out PlayerCreateInfoRecord createInfo))
        {
            Logger.Write(LogType.FAILED, $"Missing playercreateinfo row in memory for race={request.Race}, class={request.Class}.", nameof(CharacterCreationService));
            return CharacterCreateResult.Failed;
        }

        IReadOnlyList<StarterItemCreateData> starterItems = ResolveStarterItems(outfit, worldTemplates);

        try
        {
            await _characterRepository.CreateCharacterAsync(accountId, request, createInfo, starterItems, cancellationToken);
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.FAILED, $"Character create database save failed for account {accountId}, name '{request.Name}': {exception.Message}", nameof(CharacterCreationService));
            return CharacterCreateResult.Failed;
        }

        return CharacterCreateResult.Success;
    }

    public async Task<CharacterDeleteServiceResult> DeleteCharacterAsync(
        uint accountId,
        ulong clientGuid,
        CancellationToken cancellationToken)
    {
        uint characterGuid = ExtractCharacterGuid(clientGuid);
        if (characterGuid == 0)
        {
            Logger.Write(LogType.WARNING, $"Character delete rejected for account {accountId}: invalid guid 0x{clientGuid:X16}.", nameof(CharacterCreationService));
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
                    Logger.Write(LogType.WARNING, $"Character delete security rejection for account {accountId}: attempted to delete guid {characterGuid} that is not owned by the authenticated account.", nameof(CharacterCreationService));
                    return CharacterDeleteServiceResult.SecurityMismatch;

                case CharacterDeleteRepositoryResult.GuildLeader:
                    Logger.Write(LogType.WARNING, $"Character delete rejected for account {accountId}, guid {characterGuid}: character is a guild leader.", nameof(CharacterCreationService));
                    return CharacterDeleteServiceResult.Failed;

                case CharacterDeleteRepositoryResult.Online:
                    Logger.Write(LogType.WARNING, $"Character delete rejected for account {accountId}, guid {characterGuid}: character is marked online.", nameof(CharacterCreationService));
                    return CharacterDeleteServiceResult.Failed;

                case CharacterDeleteRepositoryResult.NotFound:
                    Logger.Write(LogType.WARNING, $"Character delete rejected for account {accountId}: guid {characterGuid} was not found.", nameof(CharacterCreationService));
                    return CharacterDeleteServiceResult.Failed;

                default:
                    Logger.Write(LogType.WARNING, $"Character delete rejected for account {accountId}, guid {characterGuid}: repository returned {result}.", nameof(CharacterCreationService));
                    return CharacterDeleteServiceResult.Failed;
            }
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.FAILED, $"Character delete database operation failed for account {accountId}, guid {characterGuid}: {exception.Message}", nameof(CharacterCreationService));
            return CharacterDeleteServiceResult.Failed;
        }
    }

    private static uint ExtractCharacterGuid(ulong clientGuid)
    {
        // Vanilla clients send the full ObjectGuid back to CMSG_CHAR_DELETE.
        // The MaNGOS character table stores the low counter in characters.guid.
        // This also supports the current milestone enum packet, which sends the
        // low guid directly until the full object-guid builder is implemented.
        return (uint)(clientGuid & uint.MaxValue);
    }

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

    private sealed record CharacterCustomizationValidationResult(
        bool SkinValid,
        bool FaceValid,
        bool HairColorValid,
        bool HairStyleValid,
        bool FacialHairValid)
    {
        public bool IsValid => SkinValid && FaceValid && HairColorValid && HairStyleValid && FacialHairValid;

        public override string ToString()
        {
            return $"validation detail: skin={SkinValid}, face={FaceValid}, hairColor={HairColorValid}, hairStyle={HairStyleValid}, facialHair={FacialHairValid}.";
        }
    }

    private static IReadOnlyList<StarterItemCreateData> ResolveStarterItems(
        CharStartOutfitDbcRecord outfit,
        WorldTemplateDataStore worldTemplates)
    {
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
                Logger.Write(LogType.WARNING, $"Starter outfit item {entry} is missing from item_template and will be skipped.", nameof(CharacterCreationService));
                continue;
            }

            byte inventoryType = ResolveInventoryType(item, template);
            int equipmentSlot = MapInventoryTypeToEquipmentSlot(inventoryType);
            int storageSlot;

            if (equipmentSlot != NoEquipmentSlot)
            {
                storageSlot = equipmentSlot;
            }
            else if (inventoryType == 18 && nextBagSlot <= LastBagSlot)
            {
                storageSlot = nextBagSlot++;
            }
            else if (nextBackpackSlot <= LastBackpackSlot)
            {
                storageSlot = nextBackpackSlot++;
            }
            else
            {
                Logger.Write(LogType.WARNING, $"Starter outfit item {entry} could not be placed because the backpack starter slots are full.", nameof(CharacterCreationService));
                continue;
            }

            result.Add(new StarterItemCreateData(template, (byte)storageSlot, equipmentSlot));
        }

        return result;
    }

    private static byte ResolveInventoryType(CharStartOutfitItemDbcRecord item, ItemTemplateRecord template)
    {
        return item.InventorySlotId is > 0 and <= byte.MaxValue
            ? (byte)item.InventorySlotId
            : template.InventoryType;
    }

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

    private static bool IsValidCharacterName(string name)
    {
        return name.Length is >= 2 and <= 12 && CharacterNameRegex().IsMatch(name);
    }

    [GeneratedRegex("^[A-Za-z]+$", RegexOptions.CultureInvariant)]
    private static partial Regex CharacterNameRegex();
}
