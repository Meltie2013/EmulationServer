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

using EmulationServer.Game.Chat;
using EmulationServer.Game.Players;

namespace EmulationServer.Tests.Game.Chat;

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
    public void GetDefaultLanguage_UsesCommonUnlessFactionIsHorde()
    {
        Assert.Equal(ChatLanguage.Common, LanguageKnowledgeSystem.GetDefaultLanguage(PlayerFaction.Alliance));
        Assert.Equal(ChatLanguage.Orcish, LanguageKnowledgeSystem.GetDefaultLanguage(PlayerFaction.Horde));
        Assert.Equal(ChatLanguage.Common, LanguageKnowledgeSystem.GetDefaultLanguage(PlayerFaction.Neutral));
    }
}
