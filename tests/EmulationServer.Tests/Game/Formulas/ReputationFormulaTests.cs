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

public sealed class ReputationFormulaTests
{
    [Theory]
    [InlineData(-50000, -42000)]
    [InlineData(-42000, -42000)]
    [InlineData(0, 0)]
    [InlineData(42999, 42999)]
    [InlineData(50000, 42999)]
    public void ClampStanding_UsesVanillaReputationBounds(int standing, int expectedStanding)
    {
        Assert.Equal(expectedStanding, ReputationFormula.ClampStanding(standing));
    }

    [Fact]
    public void CalculateStoredStanding_HandlesIncrementalStandingAgainstBaseReputation()
    {
        Assert.Equal(150, ReputationFormula.CalculateStoredStanding(100, 25, 125, incremental: true));
        Assert.Equal(200, ReputationFormula.CalculateStoredStanding(100, 0, 300, incremental: false));
    }

    [Fact]
    public void CalculateReward_AppliesGlobalSourceAndLowLevelRates()
    {
        Assert.Equal(50, ReputationFormula.CalculateReward(100, sourceRate: 1.0f, globalRate: 1.0f, lowLevelRate: 0.5f, isLowLevel: true));
        Assert.Equal(200, ReputationFormula.CalculateReward(100, sourceRate: 2.0f));
        Assert.Equal(0, ReputationFormula.CalculateReward(100, sourceRate: 0.0f));
    }
}
