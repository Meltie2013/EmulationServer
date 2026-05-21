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

/**
  * File overview: src/WorldServer/Configuration/GameDataSettings.cs
  * This file belongs to the server configuration loading and strongly typed settings portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

using EmulationServer.Game.Data.Dbc.Maps;

namespace EmulationServer.WorldServer.Configuration;

/**
  * Represents the game data settings component in the server configuration loading and strongly typed settings area.
  * It keeps configuration values grouped by responsibility and prevents unrelated server code from reading raw INI keys.
  */
public sealed class GameDataSettings
{
    /**
      * Gets or stores the enabled value used by GameDataSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public bool Enabled { get; init; }

    /**
      * Gets or stores the data directory value used by GameDataSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string DataDirectory { get; init; } = "Data";

    /**
      * Gets or stores the dbc directory value used by GameDataSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string DbcDirectory { get; init; } = "dbc";

    /**
      * Gets or stores the required dbc files value used by GameDataSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public IReadOnlyList<string> RequiredDbcFiles { get; init; } = DefaultRequiredDbcFiles;

    /**
      * Gets or stores the default required dbc files value used by GameDataSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public static IReadOnlyList<string> DefaultRequiredDbcFiles { get; } =
    [
        MapDbcFileNames.AreaTable,
        MapDbcFileNames.AreaTrigger,
        "AuctionHouse.dbc",
        "BankBagSlotPrices.dbc",
        "CharStartOutfit.dbc",
        "ChatChannels.dbc",
        "ChrClasses.dbc",
        "ChrRaces.dbc",
        "CinematicSequences.dbc",
        "DurabilityCosts.dbc",
        "DurabilityQuality.dbc",
        "Emotes.dbc",
        "EmotesText.dbc",
        "Faction.dbc",
        "FactionTemplate.dbc",
        "ItemBagFamily.dbc",
        "ItemClass.dbc",
        "ItemRandomProperties.dbc",
        "ItemSet.dbc",
        "Lock.dbc",
        "MailTemplate.dbc",
        MapDbcFileNames.Map,
        "QuestSort.dbc",
        "SkillLine.dbc",
        "SkillLineAbility.dbc",
        "SkillRaceClassInfo.dbc",
        "SoundEntries.dbc",
        "Spell.dbc",
        "SpellCastTimes.dbc",
        "SpellDuration.dbc",
        "SpellFocusObject.dbc",
        "SpellItemEnchantment.dbc",
        "SpellRadius.dbc",
        "SpellRange.dbc",
        "SpellShapeshiftForm.dbc",
        "StableSlotPrices.dbc",
        "Talent.dbc",
        "TalentTab.dbc",
        "TaxiNodes.dbc",
        "TaxiPath.dbc",
        "TaxiPathNode.dbc",
        "WMOAreaTable.dbc",
        MapDbcFileNames.WorldMapArea,
        MapDbcFileNames.WorldMapContinent,
        MapDbcFileNames.WorldMapOverlay,
        "WorldSafeLocs.dbc",
    ];

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of GameDataSettings and keeps this workflow isolated from the caller.
      */
    public void Validate()
    {
        if (!Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(DataDirectory))
        {
            throw new InvalidOperationException("WorldServer game data directory is required when game data loading is enabled.");
        }

        if (string.IsNullOrWhiteSpace(DbcDirectory))
        {
            throw new InvalidOperationException("WorldServer DBC directory is required when game data loading is enabled.");
        }

        if (RequiredDbcFiles.Count == 0)
        {
            throw new InvalidOperationException("At least one required DBC file must be configured when game data loading is enabled.");
        }

        foreach (string requiredDbcFile in RequiredDbcFiles)
        {
            if (string.IsNullOrWhiteSpace(requiredDbcFile))
            {
                throw new InvalidOperationException("Required DBC file list cannot contain empty entries.");
            }
        }
    }
}
