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
// File: tests/EmulationServer.Tests/Game/Chat/LanguageKnowledgeSystemTests.cs
// Purpose: Contains language knowledge system tests code for the automated test and verification layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Game.Chat;
using EmulationServer.Game.Players;

namespace EmulationServer.Tests.Game.Chat;

// Type: LanguageKnowledgeSystemTests
// Purpose: Provides language knowledge system tests behavior for the automated test and verification layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class LanguageKnowledgeSystemTests
{
    [Theory]
    [InlineData(1, PlayerFaction.Alliance, new int[] { 98 }, new int[] { 668 })]
    [InlineData(3, PlayerFaction.Alliance, new int[] { 98, 111 }, new int[] { 668, 672 })]
    [InlineData(4, PlayerFaction.Alliance, new int[] { 98, 113 }, new int[] { 668, 671 })]
    [InlineData(7, PlayerFaction.Alliance, new int[] { 98, 313 }, new int[] { 668, 7340 })]
    [InlineData(2, PlayerFaction.Horde, new int[] { 109 }, new int[] { 669 })]
    [InlineData(5, PlayerFaction.Horde, new int[] { 109, 673 }, new int[] { 669, 17737 })]
    [InlineData(6, PlayerFaction.Horde, new int[] { 109, 115 }, new int[] { 669, 670 })]
    [InlineData(8, PlayerFaction.Horde, new int[] { 109, 315 }, new int[] { 669, 7341 })]
    // Method: BuildInitialLanguageKnowledge_MatchesVanillaPlayableRaces
    // Purpose: Builds or writes build initial language knowledge matches vanilla playable races output for the automated test and verification layer.
    // Parameters:
    // - race: Race value supplied by the caller for this operation.
    // - faction: Faction value supplied by the caller for this operation.
    // - intexpectedSkillIds: Intexpected skill ids value supplied by the caller for this operation.
    // - intexpectedSpellIds: Intexpected spell ids value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to LanguageKnowledgeSystemTests so callers do not duplicate validation, protocol, or persistence rules.
    public void BuildInitialLanguageKnowledge_MatchesVanillaPlayableRaces(
        byte race,
        PlayerFaction faction,
        int[] expectedSkillIds,
        int[] expectedSpellIds)
    {
        Assert.Equal(expectedSkillIds, LanguageKnowledgeSystem.BuildInitialLanguageSkills(race, faction).Select(skill => checked((int)skill.Skill)).ToArray());
        Assert.All(LanguageKnowledgeSystem.BuildInitialLanguageSkills(race, faction), skill =>
        {
            Assert.Equal(300u, skill.Value);
            Assert.Equal(300u, skill.MaxValue);
        });
        Assert.Equal(expectedSpellIds, LanguageKnowledgeSystem.BuildInitialLanguageSpellIds(race, faction).Select(spellId => checked((int)spellId)).ToArray());
    }

    [Fact]
    // Method: EnsureInitialLanguageSkills_BackfillsMissingFactionAndRaceLanguages
    // Purpose: Validates or evaluates ensure initial language skills backfills missing faction and race languages rules for the automated test and verification layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to LanguageKnowledgeSystemTests so callers do not duplicate validation, protocol, or persistence rules.
    public void EnsureInitialLanguageSkills_BackfillsMissingFactionAndRaceLanguages()
    {
        PlayerSkill existingDefense = new(95, 42, 75);
        IReadOnlyList<PlayerSkill> skills = LanguageKnowledgeSystem.EnsureInitialLanguageSkills(
            race: 5,
            faction: PlayerFaction.Horde,
            savedSkills: [existingDefense]);

        Assert.Contains(skills, skill => skill.Skill == 95 && skill.Value == 42 && skill.MaxValue == 75);
        Assert.Contains(skills, skill => skill.Skill == 109 && skill.Value == 300 && skill.MaxValue == 300);
        Assert.Contains(skills, skill => skill.Skill == 673 && skill.Value == 300 && skill.MaxValue == 300);
    }

    [Fact]
    // Method: GetDefaultLanguage_UsesCommonUnlessFactionIsHorde
    // Purpose: Retrieves get default language uses common unless faction is horde data for the automated test and verification layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to LanguageKnowledgeSystemTests so callers do not duplicate validation, protocol, or persistence rules.
    public void GetDefaultLanguage_UsesCommonUnlessFactionIsHorde()
    {
        Assert.Equal(ChatLanguage.Common, LanguageKnowledgeSystem.GetDefaultLanguage(PlayerFaction.Alliance));
        Assert.Equal(ChatLanguage.Orcish, LanguageKnowledgeSystem.GetDefaultLanguage(PlayerFaction.Horde));
        Assert.Equal(ChatLanguage.Common, LanguageKnowledgeSystem.GetDefaultLanguage(PlayerFaction.Neutral));
    }
}
