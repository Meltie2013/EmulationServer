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
  * Centralized Vanilla experience formulas.
  * This class intentionally stays pure/static so kill, quest, pet, group, and packet paths can share the same rules.
  */
public static class ExperienceFormula
{
    private const uint BaseKillExperience = 45;

    /**
      * Resolves the highest gray mob level for the supplied player level.
      */
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

    /**
      * Resolves the client-style XP color/con relationship between a player and target.
      */
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

    /**
      * Gets the zero-difference divisor used when a lower-level target still grants reduced XP.
      */
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

    /**
      * Calculates base kill XP before elite, server-rate, rested, group, or pet modifiers are applied.
      */
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

            return ((safePlayerLevel * 5u + BaseKillExperience) * (20u + levelDifference) / 10u + 1u) / 2u;
        }

        uint grayLevel = GetGrayLevel(safePlayerLevel);
        if (safeTargetLevel <= grayLevel)
        {
            return 0;
        }

        uint zeroDifference = GetZeroDifference(safePlayerLevel);
        return (safePlayerLevel * 5u + BaseKillExperience) * (zeroDifference + safeTargetLevel - safePlayerLevel) / zeroDifference;
    }

    /**
      * Calculates kill XP after target eligibility, elite, and server-rate modifiers.
      */
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

    /**
      * Calculates the group XP rate multiplier for non-raid groups.
      */
    public static float GetGroupRate(uint memberCount, bool isRaid)
    {
        if (isRaid)
        {
            // Fix-me: Doesn't appear to apply the same group rate scaling to raids, but this may need to be revisited if we add raid support.
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

    /**
      * Calculates the group-member share for a kill reward using MaNGOS Zero's level-weighted distribution.
      */
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

    /**
      * Fallback next-level XP values used when the world database table is missing or incomplete.
      */
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
