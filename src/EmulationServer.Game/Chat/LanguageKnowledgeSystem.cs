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
// File: src/EmulationServer.Game/Chat/LanguageKnowledgeSystem.cs
// Purpose: Contains language knowledge system code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Game.Players;

namespace EmulationServer.Game.Chat;

// Type: LanguageKnowledgeSystem
// Purpose: Provides language knowledge system behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class LanguageKnowledgeSystem
{
    // Constant: Defines the language skill value constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed language skill value value used anywhere this rule or protocol value is needed.
    private const uint LanguageSkillValue = 300;

    private static readonly Dictionary<ChatLanguage, LanguageDefinition> Definitions = new()
    {
        [ChatLanguage.Common] = new(ChatLanguage.Common, 98, 668),
        [ChatLanguage.Orcish] = new(ChatLanguage.Orcish, 109, 669),
        [ChatLanguage.Dwarvish] = new(ChatLanguage.Dwarvish, 111, 672),
        [ChatLanguage.Darnassian] = new(ChatLanguage.Darnassian, 113, 671),
        [ChatLanguage.Taurahe] = new(ChatLanguage.Taurahe, 115, 670),
        [ChatLanguage.Gnomish] = new(ChatLanguage.Gnomish, 313, 7340),
        [ChatLanguage.Troll] = new(ChatLanguage.Troll, 315, 7341),
        [ChatLanguage.Gutterspeak] = new(ChatLanguage.Gutterspeak, 673, 17737),
    };

    // Method: GetDefaultLanguage
    // Purpose: Retrieves get default language data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - faction: Faction value supplied by the caller for this operation.
    // Returns: Returns the chat language value produced by this operation.
    // Notes: This keeps the operation scoped to LanguageKnowledgeSystem so callers do not duplicate validation, protocol, or persistence rules.
    public static ChatLanguage GetDefaultLanguage(PlayerFaction faction)
    {
        return faction == PlayerFaction.Horde ? ChatLanguage.Orcish : ChatLanguage.Common;
    }

    // Method: BuildInitialLanguageSkills
    // Purpose: Builds or writes build initial language skills output for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - race: Race value supplied by the caller for this operation.
    // - faction: Faction value supplied by the caller for this operation.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to LanguageKnowledgeSystem so callers do not duplicate validation, protocol, or persistence rules.
    public static IReadOnlyList<PlayerSkill> BuildInitialLanguageSkills(byte race, PlayerFaction faction)
    {
        SortedDictionary<uint, PlayerSkill> skills = [];
        foreach (LanguageDefinition language in ResolveKnownLanguageDefinitions(race, faction))
        {
            skills[language.SkillId] = new PlayerSkill(language.SkillId, LanguageSkillValue, LanguageSkillValue);
        }

        return [.. skills.Values];
    }

    // Method: BuildInitialLanguageSpellIds
    // Purpose: Builds or writes build initial language spell ids output for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - race: Race value supplied by the caller for this operation.
    // - faction: Faction value supplied by the caller for this operation.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to LanguageKnowledgeSystem so callers do not duplicate validation, protocol, or persistence rules.
    public static IReadOnlyList<uint> BuildInitialLanguageSpellIds(byte race, PlayerFaction faction)
    {
        SortedSet<uint> spellIds = [];
        foreach (LanguageDefinition language in ResolveKnownLanguageDefinitions(race, faction))
        {
            if (language.SpellId != 0)
            {
                spellIds.Add(language.SpellId);
            }
        }

        return [.. spellIds];
    }

    // Method: EnsureInitialLanguageSkills
    // Purpose: Validates or evaluates ensure initial language skills rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - race: Race value supplied by the caller for this operation.
    // - faction: Faction value supplied by the caller for this operation.
    // - savedSkills: Saved skills value supplied by the caller for this operation.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to LanguageKnowledgeSystem so callers do not duplicate validation, protocol, or persistence rules.
    public static IReadOnlyList<PlayerSkill> EnsureInitialLanguageSkills(
        byte race,
        PlayerFaction faction,
        IEnumerable<PlayerSkill> savedSkills)
    {
        ArgumentNullException.ThrowIfNull(savedSkills);

        SortedDictionary<uint, PlayerSkill> skills = [];
        foreach (PlayerSkill savedSkill in savedSkills)
        {
            if (savedSkill.Skill == 0)
            {
                continue;
            }

            uint maxValue = savedSkill.MaxValue == 0 ? savedSkill.Value : savedSkill.MaxValue;
            skills[savedSkill.Skill] = savedSkill with
            {
                Value = savedSkill.Value,
                MaxValue = maxValue,
            };
        }

        foreach (PlayerSkill languageSkill in BuildInitialLanguageSkills(race, faction))
        {
            if (!skills.TryGetValue(languageSkill.Skill, out PlayerSkill? existingSkill) ||
                existingSkill.Value == 0 ||
                existingSkill.MaxValue < languageSkill.MaxValue)
            {
                skills[languageSkill.Skill] = languageSkill;
            }
        }

        return [.. skills.Values];
    }

    // Method: PlayerKnowsLanguage
    // Purpose: Executes the player knows language operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - language: Language value supplied by the caller for this operation.
    // Returns: Returns true when player knows language succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to LanguageKnowledgeSystem so callers do not duplicate validation, protocol, or persistence rules.
    public static bool PlayerKnowsLanguage(PlayerLoginRecord player, ChatLanguage language)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (language == ChatLanguage.Universal)
        {
            return true;
        }

        if (!Definitions.TryGetValue(language, out LanguageDefinition? definition))
        {
            return false;
        }

        if (player.Skills.Any(skill => skill.Skill == definition.SkillId && skill.Value > 0))
        {
            return true;
        }

        return ResolveKnownLanguageDefinitions(player.Race, player.Faction)
            .Any(knownLanguage => knownLanguage.Language == language);
    }

    // Method: ResolveKnownLanguageDefinitions
    // Purpose: Retrieves resolve known language definitions data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - race: Race value supplied by the caller for this operation.
    // - faction: Faction value supplied by the caller for this operation.
    // Returns: Returns the I enumerable value produced by this operation.
    // Notes: This keeps the operation scoped to LanguageKnowledgeSystem so callers do not duplicate validation, protocol, or persistence rules.
    private static IEnumerable<LanguageDefinition> ResolveKnownLanguageDefinitions(byte race, PlayerFaction faction)
    {
        SortedSet<ChatLanguage> languages = [GetDefaultLanguage(faction)];

        switch (race)
        {
            case 3:
                languages.Add(ChatLanguage.Dwarvish);
                break;
            case 4:
                languages.Add(ChatLanguage.Darnassian);
                break;
            case 5:
                languages.Add(ChatLanguage.Gutterspeak);
                break;
            case 6:
                languages.Add(ChatLanguage.Taurahe);
                break;
            case 7:
                languages.Add(ChatLanguage.Gnomish);
                break;
            case 8:
                languages.Add(ChatLanguage.Troll);
                break;
        }

        foreach (ChatLanguage language in languages)
        {
            if (Definitions.TryGetValue(language, out LanguageDefinition? definition))
            {
                yield return definition;
            }
        }
    }

    // Type: LanguageDefinition
    // Purpose: Represents language definition data passed through the game-domain data, player state, DBC, and world-template layer.
    // Constructor values:
    // - Language: Language value supplied by the caller for this operation.
    // - SkillId: Skill ID identifier used to select the exact record, object, or runtime owner.
    // - SpellId: Spell ID identifier used to select the exact record, object, or runtime owner.
    // Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
    private sealed record LanguageDefinition(ChatLanguage Language, uint SkillId, uint SpellId);
}
