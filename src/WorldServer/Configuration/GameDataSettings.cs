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


using EmulationServer.Game.Data.Dbc.Chat;
using EmulationServer.Game.Data.Dbc.Characters;
using EmulationServer.Game.Data.Dbc.Factions;
using EmulationServer.Game.Data.Dbc.Items;
using EmulationServer.Game.Data.Dbc.Maps;
using EmulationServer.Game.Data.Dbc.Spells;

/**
 * File overview: src/WorldServer/Configuration/GameDataSettings.cs
 * Documents the GameDataSettings source file in the world server configuration and startup settings area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.WorldServer.Configuration;

/**
 * Owns the game data settings behavior for the world server configuration and startup settings layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
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
        // Map and area metadata used for routing, map-service summaries, and future world-entry validation.
        ..MapDbcFileNames.CoreMapDbcFiles,

        // Character screen and character creation validation.
        ..CharacterDbcFileNames.CoreCharacterDbcFiles,

        // Starter gear, item display, and future inventory validation.
        ..ItemDbcFileNames.CoreItemDbcFiles,

        // Skills, spells, ranges, durations, and icons used by starter character data.
        ..SpellDbcFileNames.CoreSpellDbcFiles,

        // Race/faction defaults and hostile/friendly faction templates.
        ..FactionDbcFileNames.CoreFactionDbcFiles,

        // Chat channel templates and player language names used by world chat routing.
        ..ChatDbcFileNames.CoreChatDbcFiles,

        // Additional vanilla global DBCs that will be needed by character/account systems soon.
        "AuctionHouse.dbc",
        "BankBagSlotPrices.dbc",
        "CinematicSequences.dbc",
        "DurabilityCosts.dbc",
        "DurabilityQuality.dbc",
        "Emotes.dbc",
        "EmotesText.dbc",
        "Lock.dbc",
        "MailTemplate.dbc",
        "QuestSort.dbc",
        "SoundEntries.dbc",
        "SpellFocusObject.dbc",
        "SpellItemEnchantment.dbc",
        "SpellRadius.dbc",
        "SpellShapeshiftForm.dbc",
        "StableSlotPrices.dbc",
        "Talent.dbc",
        "TalentTab.dbc",
        "TaxiNodes.dbc",
        "TaxiPath.dbc",
        "TaxiPathNode.dbc",
        "WMOAreaTable.dbc",
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
