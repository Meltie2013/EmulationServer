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
// File: src/WorldServer/Configuration/GameDataSettings.cs
// Purpose: Contains game data settings code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Game.Data.Dbc.Chat;
using EmulationServer.Game.Data.Dbc.Characters;
using EmulationServer.Game.Data.Dbc.Creatures;
using EmulationServer.Game.Data.Dbc.Factions;
using EmulationServer.Game.Data.Dbc.Items;
using EmulationServer.Game.Data.Dbc.Maps;
using EmulationServer.Game.Data.Dbc.Spells;

namespace EmulationServer.WorldServer.Configuration;

// Type: GameDataSettings
// Purpose: Provides game data settings behavior for the world server gameplay, session, and character runtime layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class GameDataSettings
{

    // Property: Gets or sets the enabled value used by the world server gameplay, session, and character runtime layer.
    // Value: enabled value exposed by the owning type.
    public bool Enabled { get; init; }

    // Property: Gets or sets the data directory value used by the world server gameplay, session, and character runtime layer.
    // Value: data directory value exposed by the owning type.
    public string DataDirectory { get; init; } = "Data";

    // Property: Gets or sets the DBC directory value used by the world server gameplay, session, and character runtime layer.
    // Value: DBC directory value exposed by the owning type.
    public string DbcDirectory { get; init; } = "dbc";

    // Property: Gets or sets the map store directory value used by the world server gameplay, session, and character runtime layer.
    // Value: map store directory value exposed by the owning type.
    public string MapStoreDirectory { get; init; } = "mapstore";

    // Property: Gets or sets the required DBC files value used by the world server gameplay, session, and character runtime layer.
    // Value: required DBC files value exposed by the owning type.
    public IReadOnlyList<string> RequiredDbcFiles { get; init; } = DefaultRequiredDbcFiles;

    // Property: Gets or sets the default required DBC files value used by the world server gameplay, session, and character runtime layer.
    // Value: default required DBC files value exposed by the owning type.
    public static IReadOnlyList<string> DefaultRequiredDbcFiles { get; } =
    [

        ..MapDbcFileNames.CoreMapDbcFiles,

        ..CharacterDbcFileNames.CoreCharacterDbcFiles,

        ..ItemDbcFileNames.CoreItemDbcFiles,

        ..CreatureDbcFileNames.CoreCreatureDbcFiles,

        ..SpellDbcFileNames.CoreSpellDbcFiles,

        ..FactionDbcFileNames.CoreFactionDbcFiles,

        ..ChatDbcFileNames.CoreChatDbcFiles,

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

    // Method: Validate
    // Purpose: Validates or evaluates validate rules for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to GameDataSettings so callers do not duplicate validation, protocol, or persistence rules.
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

        if (string.IsNullOrWhiteSpace(MapStoreDirectory))
        {
            throw new InvalidOperationException("WorldServer mapstore directory is required when game data loading is enabled.");
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
