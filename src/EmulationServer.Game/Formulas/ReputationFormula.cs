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
// File: src/EmulationServer.Game/Formulas/ReputationFormula.cs
// Purpose: Contains reputation formula code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Formulas;

// Type: ReputationFormula
// Purpose: Provides reputation formula behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class ReputationFormula
{
    // Constant: Defines the reputation cap constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed reputation cap value used anywhere this rule or protocol value is needed.
    public const int ReputationCap = 42999;
    // Constant: Defines the reputation bottom constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed reputation bottom value used anywhere this rule or protocol value is needed.
    public const int ReputationBottom = -42000;

    // Property: Gets or sets the points in rank value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: points in rank value exposed by the owning type.
    public static ReadOnlySpan<int> PointsInRank => [36000, 3000, 3000, 3000, 6000, 12000, 21000, 1000];

    // Method: ClampStanding
    // Purpose: Executes the clamp standing operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - standing: Standing value supplied by the caller for this operation.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to ReputationFormula so callers do not duplicate validation, protocol, or persistence rules.
    public static int ClampStanding(int standing)
    {
        return Math.Clamp(standing, ReputationBottom, ReputationCap);
    }

    // Method: CalculateStoredStanding
    // Purpose: Calculates calculate stored standing values for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - baseReputation: Base reputation value supplied by the caller for this operation.
    // - currentStoredStanding: Current stored standing value supplied by the caller for this operation.
    // - standing: Standing value supplied by the caller for this operation.
    // - incremental: Incremental value supplied by the caller for this operation.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to ReputationFormula so callers do not duplicate validation, protocol, or persistence rules.
    public static int CalculateStoredStanding(int baseReputation, int currentStoredStanding, int standing, bool incremental)
    {
        int absoluteStanding = incremental
            ? baseReputation + currentStoredStanding + standing
            : standing;

        return ClampStanding(absoluteStanding) - baseReputation;
    }

    // Method: CalculateReward
    // Purpose: Calculates calculate reward values for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // - sourceRate: Source rate value supplied by the caller for this operation.
    // - globalRate: Global rate value supplied by the caller for this operation.
    // - lowLevelRate: Low level rate value supplied by the caller for this operation.
    // - isLowLevel: Is low level value supplied by the caller for this operation.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to ReputationFormula so callers do not duplicate validation, protocol, or persistence rules.
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
