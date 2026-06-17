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
// File: src/EmulationServer.Game/Reputation/ReputationSystem.cs
// Purpose: Contains reputation system code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Game.Data.Dbc.Factions;
using EmulationServer.Game.Players;
using EmulationServer.Game.Formulas;

namespace EmulationServer.Game.Reputation;

// Type: ReputationSystem
// Purpose: Provides reputation system behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class ReputationSystem
{
    // Constant: Defines the max reputation slots constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed max reputation slots value used anywhere this rule or protocol value is needed.
    public const int MaxReputationSlots = 64;
    // Constant: Defines the reputation cap constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed reputation cap value used anywhere this rule or protocol value is needed.
    public const int ReputationCap = ReputationFormula.ReputationCap;
    // Constant: Defines the reputation bottom constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed reputation bottom value used anywhere this rule or protocol value is needed.
    public const int ReputationBottom = ReputationFormula.ReputationBottom;

    // Method: BuildInitialReputations
    // Purpose: Builds or writes build initial reputations output for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - factionData: Faction data value supplied by the caller for this operation.
    // - race: Race value supplied by the caller for this operation.
    // - playerClass: Player class value supplied by the caller for this operation.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to ReputationSystem so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: BuildCharacterReputations
    // Purpose: Builds or writes build character reputations output for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - factionData: Faction data value supplied by the caller for this operation.
    // - race: Race value supplied by the caller for this operation.
    // - playerClass: Player class value supplied by the caller for this operation.
    // - savedReputations: Saved reputations value supplied by the caller for this operation.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to ReputationSystem so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: GetEffectiveStanding
    // Purpose: Retrieves get effective standing data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - faction: Faction value supplied by the caller for this operation.
    // - race: Race value supplied by the caller for this operation.
    // - playerClass: Player class value supplied by the caller for this operation.
    // - standing: Standing value supplied by the caller for this operation.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to ReputationSystem so callers do not duplicate validation, protocol, or persistence rules.
    public static int GetEffectiveStanding(FactionDbcRecord faction, byte race, byte playerClass, int standing)
    {
        ArgumentNullException.ThrowIfNull(faction);
        return ClampStanding(GetBaseReputation(faction, race, playerClass) + standing);
    }

    // Method: ReputationToRank
    // Purpose: Executes the reputation to rank operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - standing: Standing value supplied by the caller for this operation.
    // Returns: Returns the reputation rank value produced by this operation.
    // Notes: This keeps the operation scoped to ReputationSystem so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ClampStanding
    // Purpose: Executes the clamp standing operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - standing: Standing value supplied by the caller for this operation.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to ReputationSystem so callers do not duplicate validation, protocol, or persistence rules.
    public static int ClampStanding(int standing)
    {
        return ReputationFormula.ClampStanding(standing);
    }

    // Method: IsClientReputationFaction
    // Purpose: Validates or evaluates is client reputation faction rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - faction: Faction value supplied by the caller for this operation.
    // Returns: Returns true when is client reputation faction succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to ReputationSystem so callers do not duplicate validation, protocol, or persistence rules.
    private static bool IsClientReputationFaction(FactionDbcRecord faction)
    {
        return faction.ReputationIndex is >= 0 and < MaxReputationSlots &&
            (faction.ReputationIndex != 0 || HasReputationDefaults(faction));
    }

    // Method: ShouldReplaceDuplicateIndex
    // Purpose: Validates or evaluates should replace duplicate index rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - existing: Existing value supplied by the caller for this operation.
    // - candidate: Candidate value supplied by the caller for this operation.
    // - factionData: Faction data value supplied by the caller for this operation.
    // Returns: Returns true when should replace duplicate index succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to ReputationSystem so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: HasReputationDefaults
    // Purpose: Validates or evaluates has reputation defaults rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - faction: Faction value supplied by the caller for this operation.
    // Returns: Returns true when has reputation defaults succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to ReputationSystem so callers do not duplicate validation, protocol, or persistence rules.
    private static bool HasReputationDefaults(FactionDbcRecord faction)
    {
        return faction.ReputationRaceMasks.Any(mask => mask != 0) ||
            faction.ReputationClassMasks.Any(mask => mask != 0) ||
            faction.ReputationBases.Any(value => value != 0) ||
            faction.ReputationFlags.Any(value => value != 0);
    }

    // Method: GetBaseReputation
    // Purpose: Retrieves get base reputation data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - faction: Faction value supplied by the caller for this operation.
    // - race: Race value supplied by the caller for this operation.
    // - playerClass: Player class value supplied by the caller for this operation.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to ReputationSystem so callers do not duplicate validation, protocol, or persistence rules.
    private static int GetBaseReputation(FactionDbcRecord faction, byte race, byte playerClass)
    {
        int index = GetIndexFitTo(faction, race, playerClass);
        return index >= 0 && index < faction.ReputationBases.Count ? faction.ReputationBases[index] : 0;
    }

    // Method: GetDefaultStateFlags
    // Purpose: Retrieves get default state flags data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - faction: Faction value supplied by the caller for this operation.
    // - race: Race value supplied by the caller for this operation.
    // - playerClass: Player class value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to ReputationSystem so callers do not duplicate validation, protocol, or persistence rules.
    private static uint GetDefaultStateFlags(FactionDbcRecord faction, byte race, byte playerClass)
    {
        int index = GetIndexFitTo(faction, race, playerClass);
        return index >= 0 && index < faction.ReputationFlags.Count ? unchecked((uint)faction.ReputationFlags[index]) : 0u;
    }

    // Method: GetIndexFitTo
    // Purpose: Retrieves get index fit to data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - faction: Faction value supplied by the caller for this operation.
    // - race: Race value supplied by the caller for this operation.
    // - playerClass: Player class value supplied by the caller for this operation.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to ReputationSystem so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ApplySavedFlags
    // Purpose: Applies apply saved flags changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - defaultFlags: Default flags value supplied by the caller for this operation.
    // - savedFlags: Saved flags value supplied by the caller for this operation.
    // - effectiveStanding: Effective standing value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to ReputationSystem so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: SetVisible
    // Purpose: Applies set visible changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - flags: Flags value supplied by the caller for this operation.
    // Returns: Returns the reputation flags value produced by this operation.
    // Notes: This keeps the operation scoped to ReputationSystem so callers do not duplicate validation, protocol, or persistence rules.
    private static ReputationFlags SetVisible(ReputationFlags flags)
    {
        if ((flags & (ReputationFlags.InvisibleForced | ReputationFlags.Hidden)) != 0)
        {
            return flags;
        }

        return flags | ReputationFlags.Visible;
    }

    // Method: SetInactive
    // Purpose: Applies set inactive changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - flags: Flags value supplied by the caller for this operation.
    // - inactive: Inactive value supplied by the caller for this operation.
    // Returns: Returns the reputation flags value produced by this operation.
    // Notes: This keeps the operation scoped to ReputationSystem so callers do not duplicate validation, protocol, or persistence rules.
    private static ReputationFlags SetInactive(ReputationFlags flags, bool inactive)
    {
        if (inactive && ((flags & (ReputationFlags.InvisibleForced | ReputationFlags.Hidden)) != 0 || (flags & ReputationFlags.Visible) == 0))
        {
            return flags;
        }

        return inactive ? flags | ReputationFlags.Inactive : flags & ~ReputationFlags.Inactive;
    }

    // Method: SetAtWar
    // Purpose: Applies set at war changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - flags: Flags value supplied by the caller for this operation.
    // - atWar: At war value supplied by the caller for this operation.
    // - effectiveStanding: Effective standing value supplied by the caller for this operation.
    // Returns: Returns the reputation flags value produced by this operation.
    // Notes: This keeps the operation scoped to ReputationSystem so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ToRaceMask
    // Purpose: Executes the to race mask operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - race: Race value supplied by the caller for this operation.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to ReputationSystem so callers do not duplicate validation, protocol, or persistence rules.
    private static int ToRaceMask(byte race)
    {
        return race is > 0 and <= 31 ? 1 << (race - 1) : 0;
    }

    // Method: ToClassMask
    // Purpose: Executes the to class mask operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - playerClass: Player class value supplied by the caller for this operation.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to ReputationSystem so callers do not duplicate validation, protocol, or persistence rules.
    private static int ToClassMask(byte playerClass)
    {
        return playerClass is > 0 and <= 31 ? 1 << (playerClass - 1) : 0;
    }
}
