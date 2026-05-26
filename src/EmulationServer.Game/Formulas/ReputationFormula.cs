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

namespace EmulationServer.Game.Formulas;

/**
  * Centralized reputation math helpers used by character reputation state and future reward pipelines.
  */
public static class ReputationFormula
{
    public const int ReputationCap = 42999;
    public const int ReputationBottom = -42000;

    /**
      * Vanilla rank widths from Hated through Exalted.
      */
    public static ReadOnlySpan<int> PointsInRank => [36000, 3000, 3000, 3000, 6000, 12000, 21000, 1000];

    /**
      * Clamps a reputation standing to the legal Vanilla client/server range.
      */
    public static int ClampStanding(int standing)
    {
        return Math.Clamp(standing, ReputationBottom, ReputationCap);
    }

    /**
      * Applies a delta or absolute value against a base DBC reputation and returns the saved standing delta.
      */
    public static int CalculateStoredStanding(int baseReputation, int currentStoredStanding, int standing, bool incremental)
    {
        int absoluteStanding = incremental
            ? baseReputation + currentStoredStanding + standing
            : standing;

        return ClampStanding(absoluteStanding) - baseReputation;
    }

    /**
      * Applies global and source-specific reputation rates with low-level scaling.
      */
    public static int CalculateReward(int value, float sourceRate = 1.0f, float globalRate = 1.0f, float lowLevelRate = 1.0f, bool isLowLevel = false)
    {
        if (value == 0 || sourceRate <= 0.0f || globalRate <= 0.0f || lowLevelRate <= 0.0f)
        {
            return 0;
        }

        float rate = sourceRate * globalRate * (isLowLevel ? lowLevelRate : 1.0f);
        return (int)MathF.Round(value * rate, MidpointRounding.AwayFromZero);
    }
}
