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
// File: src/EmulationServer.Game/Formulas/ExperienceFormula.cs
// Purpose: Contains experience formula code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Formulas;

// Type: ExperienceFormula
// Purpose: Provides experience formula behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class ExperienceFormula
{
    // Constant: Defines the base kill experience constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed base kill experience value used anywhere this rule or protocol value is needed.
    private const uint BaseKillExperience = 45;

    // Method: GetGrayLevel
    // Purpose: Retrieves get gray level data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - playerLevel: Player level value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to ExperienceFormula so callers do not duplicate validation, protocol, or persistence rules.
    public static uint GetGrayLevel(uint playerLevel)
    {
        uint safeLevel = Math.Max(playerLevel, 1u);
        if (safeLevel <= 5)
        {
            return 0;
        }

        if (safeLevel <= 39)
        {
            return safeLevel - 5u - (safeLevel / 10u);
        }

        if (safeLevel == 60)
        {
            return 51;
        }

        return safeLevel - 1u - (safeLevel / 5u);
    }

    // Method: GetColorCode
    // Purpose: Retrieves get color code data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - playerLevel: Player level value supplied by the caller for this operation.
    // - targetLevel: Target level value supplied by the caller for this operation.
    // Returns: Returns the experience color value produced by this operation.
    // Notes: This keeps the operation scoped to ExperienceFormula so callers do not duplicate validation, protocol, or persistence rules.
    public static ExperienceColor GetColorCode(uint playerLevel, uint targetLevel)
    {
        uint safePlayerLevel = Math.Max(playerLevel, 1u);
        uint safeTargetLevel = Math.Max(targetLevel, 1u);

        if (safeTargetLevel >= safePlayerLevel + 5u)
        {
            return ExperienceColor.Red;
        }

        if (safeTargetLevel >= safePlayerLevel + 3u)
        {
            return ExperienceColor.Orange;
        }

        if (safeTargetLevel + 2u >= safePlayerLevel)
        {
            return ExperienceColor.Yellow;
        }

        return safeTargetLevel > GetGrayLevel(safePlayerLevel)
            ? ExperienceColor.Green
            : ExperienceColor.Gray;
    }

    // Method: GetZeroDifference
    // Purpose: Retrieves get zero difference data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - playerLevel: Player level value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to ExperienceFormula so callers do not duplicate validation, protocol, or persistence rules.
    public static uint GetZeroDifference(uint playerLevel)
    {
        uint safeLevel = Math.Max(playerLevel, 1u);
        if (safeLevel < 8)
        {
            return 5;
        }

        if (safeLevel < 10)
        {
            return 6;
        }

        if (safeLevel < 12)
        {
            return 7;
        }

        if (safeLevel < 16)
        {
            return 8;
        }

        if (safeLevel < 20)
        {
            return 9;
        }

        if (safeLevel < 30)
        {
            return 11;
        }

        if (safeLevel < 40)
        {
            return 12;
        }

        if (safeLevel < 45)
        {
            return 13;
        }

        if (safeLevel < 50)
        {
            return 14;
        }

        if (safeLevel < 55)
        {
            return 15;
        }

        if (safeLevel < 60)
        {
            return 16;
        }

        return 17;
    }

    // Method: CalculateBaseKillExperience
    // Purpose: Calculates calculate base kill experience values for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - playerLevel: Player level value supplied by the caller for this operation.
    // - targetLevel: Target level value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to ExperienceFormula so callers do not duplicate validation, protocol, or persistence rules.
    public static uint CalculateBaseKillExperience(uint playerLevel, uint targetLevel)
    {
        uint safePlayerLevel = Math.Max(playerLevel, 1u);
        uint safeTargetLevel = Math.Max(targetLevel, 1u);

        if (safeTargetLevel >= safePlayerLevel)
        {
            uint levelDifference = safeTargetLevel - safePlayerLevel;
            if (levelDifference > 4)
            {
                levelDifference = 4;
            }

            return ((((safePlayerLevel * 5u) + BaseKillExperience) * (20u + levelDifference) / 10u) + 1u) / 2u;
        }

        uint grayLevel = GetGrayLevel(safePlayerLevel);
        if (safeTargetLevel <= grayLevel)
        {
            return 0;
        }

        uint zeroDifference = GetZeroDifference(safePlayerLevel);
        return ((safePlayerLevel * 5u) + BaseKillExperience) * (zeroDifference + safeTargetLevel - safePlayerLevel) / zeroDifference;
    }

    // Method: CalculateKillExperience
    // Purpose: Calculates calculate kill experience values for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - playerLevel: Player level value supplied by the caller for this operation.
    // - targetLevel: Target level value supplied by the caller for this operation.
    // - isElite: Is elite value supplied by the caller for this operation.
    // - targetGrantsExperience: Target grants experience value supplied by the caller for this operation.
    // - killRate: Kill rate value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to ExperienceFormula so callers do not duplicate validation, protocol, or persistence rules.
    public static uint CalculateKillExperience(
        uint playerLevel,
        uint targetLevel,
        bool isElite = false,
        bool targetGrantsExperience = true,
        float killRate = 1.0f)
    {
        if (!targetGrantsExperience || killRate <= 0.0f)
        {
            return 0;
        }

        uint experience = CalculateBaseKillExperience(playerLevel, targetLevel);
        if (experience == 0)
        {
            return 0;
        }

        if (isElite)
        {
            experience *= 2u;
        }

        return (uint)MathF.Floor(experience * killRate);
    }

    // Method: GetGroupRate
    // Purpose: Retrieves get group rate data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - memberCount: Member count value supplied by the caller for this operation.
    // - isRaid: Is raid value supplied by the caller for this operation.
    // Returns: Returns the float value produced by this operation.
    // Notes: This keeps the operation scoped to ExperienceFormula so callers do not duplicate validation, protocol, or persistence rules.
    public static float GetGroupRate(uint memberCount, bool isRaid)
    {
        if (isRaid)
        {

            return 1.0f;
        }

        return memberCount switch
        {
            0 or 1 or 2 => 1.0f,
            3 => 1.166f,
            4 => 1.3f,
            _ => 1.4f,
        };
    }

    // Method: CalculateGroupMemberKillExperience
    // Purpose: Calculates calculate group member kill experience values for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - baseKillExperience: Base kill experience value supplied by the caller for this operation.
    // - memberLevel: Member level value supplied by the caller for this operation.
    // - groupLevelSum: Group level sum value supplied by the caller for this operation.
    // - memberCount: Member count value supplied by the caller for this operation.
    // - isRaid: Is raid value supplied by the caller for this operation.
    // - hasHigherGrayParticipant: Has higher gray participant value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to ExperienceFormula so callers do not duplicate validation, protocol, or persistence rules.
    public static uint CalculateGroupMemberKillExperience(
        uint baseKillExperience,
        uint memberLevel,
        uint groupLevelSum,
        uint memberCount,
        bool isRaid,
        bool hasHigherGrayParticipant = false)
    {
        if (baseKillExperience == 0 || memberLevel == 0 || groupLevelSum == 0)
        {
            return 0;
        }

        float memberRate = GetGroupRate(memberCount, isRaid) * memberLevel / groupLevelSum;
        return hasHigherGrayParticipant
            ? (uint)((baseKillExperience * memberRate / 2.0f) + 1.0f)
            : (uint)(baseKillExperience * memberRate);
    }

    // Method: GetFallbackNextLevelExperience
    // Purpose: Retrieves get fallback next level experience data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - level: Level value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to ExperienceFormula so callers do not duplicate validation, protocol, or persistence rules.
    public static uint GetFallbackNextLevelExperience(uint level)
    {
        uint safeLevel = Math.Max(level, 1u);
        return safeLevel switch
        {
            1 => 400,
            2 => 900,
            3 => 1400,
            4 => 2100,
            5 => 2800,
            6 => 3600,
            7 => 4500,
            8 => 5400,
            9 => 6500,
            10 => 7600,
            _ => 7600 + ((safeLevel - 10u) * 1100u),
        };
    }
}
