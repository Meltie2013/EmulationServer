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

using EmulationServer.Game.Formulas;

namespace EmulationServer.Tests.Game.Formulas;

public sealed class ExperienceFormulaTests
{
    [Theory]
    [InlineData(1, 0)]
    [InlineData(5, 0)]
    [InlineData(10, 4)]
    [InlineData(39, 31)]
    [InlineData(40, 31)]
    [InlineData(60, 51)]
    public void GetGrayLevel_MatchesVanillaThresholds(uint playerLevel, uint expectedGrayLevel)
    {
        Assert.Equal(expectedGrayLevel, ExperienceFormula.GetGrayLevel(playerLevel));
    }

    [Theory]
    [InlineData(10, 15, ExperienceColor.Red)]
    [InlineData(10, 13, ExperienceColor.Orange)]
    [InlineData(10, 8, ExperienceColor.Yellow)]
    [InlineData(10, 5, ExperienceColor.Green)]
    [InlineData(10, 4, ExperienceColor.Gray)]
    public void GetColorCode_UsesVanillaConBands(uint playerLevel, uint targetLevel, ExperienceColor expectedColor)
    {
        Assert.Equal(expectedColor, ExperienceFormula.GetColorCode(playerLevel, targetLevel));
    }

    [Theory]
    [InlineData(1, 1, 50)]
    [InlineData(10, 10, 95)]
    [InlineData(10, 14, 114)]
    [InlineData(10, 5, 47)]
    [InlineData(10, 4, 0)]
    public void CalculateBaseKillExperience_MatchesMangosFormula(uint playerLevel, uint targetLevel, uint expectedExperience)
    {
        Assert.Equal(expectedExperience, ExperienceFormula.CalculateBaseKillExperience(playerLevel, targetLevel));
    }

    [Fact]
    public void CalculateKillExperience_AppliesEliteAndServerRateAfterBaseGain()
    {
        Assert.Equal(190u, ExperienceFormula.CalculateKillExperience(10, 10, isElite: true));
        Assert.Equal(47u, ExperienceFormula.CalculateKillExperience(10, 10, killRate: 0.5f));
        Assert.Equal(0u, ExperienceFormula.CalculateKillExperience(10, 10, targetGrantsExperience: false));
    }

    [Theory]
    [InlineData(1, false, 1.0f)]
    [InlineData(2, false, 1.0f)]
    [InlineData(3, false, 1.166f)]
    [InlineData(4, false, 1.3f)]
    [InlineData(5, false, 1.4f)]
    [InlineData(40, true, 1.0f)]
    public void GetGroupRate_MatchesMangosGroupRates(uint memberCount, bool isRaid, float expectedRate)
    {
        Assert.Equal(expectedRate, ExperienceFormula.GetGroupRate(memberCount, isRaid), precision: 3);
    }

    [Fact]
    public void CalculateGroupMemberKillExperience_DistributesByLevelWeight()
    {
        Assert.Equal(55u, ExperienceFormula.CalculateGroupMemberKillExperience(95, 10, 20, 2, false));
        Assert.Equal(28u, ExperienceFormula.CalculateGroupMemberKillExperience(95, 10, 20, 2, false, hasHigherGrayParticipant: true));
    }

    [Theory]
    [InlineData(1, 400)]
    [InlineData(10, 7600)]
    [InlineData(11, 8700)]
    public void GetFallbackNextLevelExperience_ProvidesStableFallback(uint level, uint expectedExperience)
    {
        Assert.Equal(expectedExperience, ExperienceFormula.GetFallbackNextLevelExperience(level));
    }
}
