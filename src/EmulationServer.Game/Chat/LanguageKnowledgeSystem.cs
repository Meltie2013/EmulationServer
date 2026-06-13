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

namespace EmulationServer.Game.Chat;

/**
  * Centralizes the Vanilla language spell, skill, and race mapping used by the
  * chat system, character creation, and login object update packets.
  */
public static class LanguageKnowledgeSystem
{
    private const uint LanguageSkillValue = 300;

    private static readonly IReadOnlyDictionary<ChatLanguage, LanguageDefinition> Definitions = new Dictionary<ChatLanguage, LanguageDefinition>
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

    public static ChatLanguage GetDefaultLanguage(PlayerFaction faction)
    {
        return faction == PlayerFaction.Horde ? ChatLanguage.Orcish : ChatLanguage.Common;
    }

    public static IReadOnlyList<PlayerSkill> BuildInitialLanguageSkills(byte race, PlayerFaction faction)
    {
        SortedDictionary<uint, PlayerSkill> skills = [];
        foreach (LanguageDefinition language in ResolveKnownLanguageDefinitions(race, faction))
        {
            skills[language.SkillId] = new PlayerSkill(language.SkillId, LanguageSkillValue, LanguageSkillValue);
        }

        return skills.Values.ToArray();
    }

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

        return spellIds.ToArray();
    }

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

        return skills.Values.ToArray();
    }

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

    private sealed record LanguageDefinition(ChatLanguage Language, uint SkillId, uint SpellId);
}
