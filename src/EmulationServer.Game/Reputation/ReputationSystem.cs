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

using EmulationServer.Game.Data.Dbc.Factions;
using EmulationServer.Game.Players;
using EmulationServer.Game.Formulas;

namespace EmulationServer.Game.Reputation;

/**
  * Builds and normalizes per-character reputation state from Faction.dbc and character_reputation rows.
  */
public static class ReputationSystem
{
    public const int MaxReputationSlots = 64;
    public const int ReputationCap = ReputationFormula.ReputationCap;
    public const int ReputationBottom = ReputationFormula.ReputationBottom;

    /**
      * Builds the default reputation rows that a new or unsaved character should have.
      */
    public static IReadOnlyList<PlayerReputation> BuildInitialReputations(
        FactionDbcDataStore factionData,
        byte race,
        byte playerClass)
    {
        ArgumentNullException.ThrowIfNull(factionData);

        Dictionary<int, PlayerReputation> reputationsByListId = [];
        foreach (FactionDbcRecord faction in factionData.Factions.Values.OrderBy(record => record.Id))
        {
            if (!IsClientReputationFaction(faction))
            {
                continue;
            }

            PlayerReputation candidate = new(
                (uint)faction.Id,
                faction.ReputationIndex,
                0,
                GetDefaultStateFlags(faction, race, playerClass));

            if (!reputationsByListId.TryGetValue(candidate.ReputationListId, out PlayerReputation? existing) ||
                ShouldReplaceDuplicateIndex(existing, candidate, factionData))
            {
                reputationsByListId[candidate.ReputationListId] = candidate;
            }
        }

        return reputationsByListId.Values
            .OrderBy(reputation => reputation.ReputationListId)
            .ToArray();
    }

    /**
      * Builds the runtime reputation state by overlaying saved DB rows onto Faction.dbc defaults.
      */
    public static IReadOnlyList<PlayerReputation> BuildCharacterReputations(
        FactionDbcDataStore factionData,
        byte race,
        byte playerClass,
        IEnumerable<PlayerReputation> savedReputations)
    {
        ArgumentNullException.ThrowIfNull(factionData);
        ArgumentNullException.ThrowIfNull(savedReputations);

        Dictionary<int, PlayerReputation> reputationsByListId = BuildInitialReputations(factionData, race, playerClass)
            .ToDictionary(reputation => reputation.ReputationListId);

        foreach (PlayerReputation saved in savedReputations)
        {
            if (!factionData.TryGetFaction((int)saved.Faction, out FactionDbcRecord faction) || !IsClientReputationFaction(faction))
            {
                continue;
            }

            int standing = ClampStanding(saved.Standing);
            uint flags = reputationsByListId.TryGetValue(faction.ReputationIndex, out PlayerReputation? current)
                ? current.Flags
                : GetDefaultStateFlags(faction, race, playerClass);

            flags = ApplySavedFlags(flags, saved.Flags, GetEffectiveStanding(faction, race, playerClass, standing));

            reputationsByListId[faction.ReputationIndex] = new PlayerReputation(
                (uint)faction.Id,
                faction.ReputationIndex,
                standing,
                flags);
        }

        return reputationsByListId.Values
            .OrderBy(reputation => reputation.ReputationListId)
            .ToArray();
    }

    /**
      * Resolves the absolute standing shown by rank calculations by adding DBC base reputation to saved standing.
      */
    public static int GetEffectiveStanding(FactionDbcRecord faction, byte race, byte playerClass, int standing)
    {
        ArgumentNullException.ThrowIfNull(faction);
        return ClampStanding(GetBaseReputation(faction, race, playerClass) + standing);
    }

    /**
      * Converts an absolute reputation value into a reputation rank.
      */
    public static ReputationRank ReputationToRank(int standing)
    {
        int limit = ReputationCap + 1;
        ReadOnlySpan<int> pointsInRank = ReputationFormula.PointsInRank;
        for (int rank = pointsInRank.Length - 1; rank >= 0; rank--)
        {
            limit -= pointsInRank[rank];
            if (standing >= limit)
            {
                return (ReputationRank)rank;
            }
        }

        return ReputationRank.Hated;
    }

    /**
      * Clamps reputation standing to the Vanilla reputation floor and cap.
      */
    public static int ClampStanding(int standing)
    {
        return ReputationFormula.ClampStanding(standing);
    }

    private static bool IsClientReputationFaction(FactionDbcRecord faction)
    {
        return faction.ReputationIndex is >= 0 and < MaxReputationSlots &&
            (faction.ReputationIndex != 0 || HasReputationDefaults(faction));
    }

    private static bool ShouldReplaceDuplicateIndex(
        PlayerReputation existing,
        PlayerReputation candidate,
        FactionDbcDataStore factionData)
    {
        bool existingHasDefaults = factionData.TryGetFaction((int)existing.Faction, out FactionDbcRecord existingFaction) &&
            HasReputationDefaults(existingFaction);
        bool candidateHasDefaults = factionData.TryGetFaction((int)candidate.Faction, out FactionDbcRecord candidateFaction) &&
            HasReputationDefaults(candidateFaction);

        return !existingHasDefaults && candidateHasDefaults;
    }

    private static bool HasReputationDefaults(FactionDbcRecord faction)
    {
        return faction.ReputationRaceMasks.Any(mask => mask != 0) ||
            faction.ReputationClassMasks.Any(mask => mask != 0) ||
            faction.ReputationBases.Any(value => value != 0) ||
            faction.ReputationFlags.Any(value => value != 0);
    }

    private static int GetBaseReputation(FactionDbcRecord faction, byte race, byte playerClass)
    {
        int index = GetIndexFitTo(faction, race, playerClass);
        return index >= 0 && index < faction.ReputationBases.Count ? faction.ReputationBases[index] : 0;
    }

    private static uint GetDefaultStateFlags(FactionDbcRecord faction, byte race, byte playerClass)
    {
        int index = GetIndexFitTo(faction, race, playerClass);
        return index >= 0 && index < faction.ReputationFlags.Count ? unchecked((uint)faction.ReputationFlags[index]) : 0u;
    }

    private static int GetIndexFitTo(FactionDbcRecord faction, byte race, byte playerClass)
    {
        int raceMask = ToRaceMask(race);
        int classMask = ToClassMask(playerClass);
        int count = Math.Min(
            Math.Min(faction.ReputationRaceMasks.Count, faction.ReputationClassMasks.Count),
            Math.Min(faction.ReputationBases.Count, faction.ReputationFlags.Count));

        for (int index = 0; index < count; index++)
        {
            int factionRaceMask = faction.ReputationRaceMasks[index];
            int factionClassMask = faction.ReputationClassMasks[index];
            if (factionRaceMask == 0 && factionClassMask == 0)
            {
                continue;
            }

            bool raceMatches = factionRaceMask == 0 || (factionRaceMask & raceMask) != 0;
            bool classMatches = factionClassMask == 0 || (factionClassMask & classMask) != 0;
            if (raceMatches && classMatches)
            {
                return index;
            }
        }

        return -1;
    }

    private static uint ApplySavedFlags(uint defaultFlags, uint savedFlags, int effectiveStanding)
    {
        ReputationFlags flags = (ReputationFlags)defaultFlags;
        ReputationFlags saved = (ReputationFlags)savedFlags;

        if (saved.HasFlag(ReputationFlags.Visible))
        {
            flags = SetVisible(flags);
        }

        if (saved.HasFlag(ReputationFlags.Inactive))
        {
            flags = SetInactive(flags, true);
        }

        if (saved.HasFlag(ReputationFlags.AtWar))
        {
            flags = SetAtWar(flags, true, effectiveStanding);
        }
        else if (flags.HasFlag(ReputationFlags.Visible))
        {
            flags = SetAtWar(flags, false, effectiveStanding);
        }

        if (ReputationToRank(effectiveStanding) <= ReputationRank.Hostile)
        {
            flags = SetAtWar(flags, true, effectiveStanding);
        }

        return (uint)flags;
    }

    private static ReputationFlags SetVisible(ReputationFlags flags)
    {
        if ((flags & (ReputationFlags.InvisibleForced | ReputationFlags.Hidden)) != 0)
        {
            return flags;
        }

        return flags | ReputationFlags.Visible;
    }

    private static ReputationFlags SetInactive(ReputationFlags flags, bool inactive)
    {
        if (inactive && ((flags & (ReputationFlags.InvisibleForced | ReputationFlags.Hidden)) != 0 || (flags & ReputationFlags.Visible) == 0))
        {
            return flags;
        }

        return inactive ? flags | ReputationFlags.Inactive : flags & ~ReputationFlags.Inactive;
    }

    private static ReputationFlags SetAtWar(ReputationFlags flags, bool atWar, int effectiveStanding)
    {
        if ((flags & (ReputationFlags.InvisibleForced | ReputationFlags.Hidden)) != 0)
        {
            return flags;
        }

        if (atWar && flags.HasFlag(ReputationFlags.PeaceForced) && ReputationToRank(effectiveStanding) > ReputationRank.Hated)
        {
            return flags;
        }

        return atWar ? flags | ReputationFlags.AtWar : flags & ~ReputationFlags.AtWar;
    }

    private static int ToRaceMask(byte race)
    {
        return race is > 0 and <= 31 ? 1 << (race - 1) : 0;
    }

    private static int ToClassMask(byte playerClass)
    {
        return playerClass is > 0 and <= 31 ? 1 << (playerClass - 1) : 0;
    }
}
