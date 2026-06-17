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
// File: src/EmulationServer.Game/Data/Dbc/Creatures/CreatureDbcFileNames.cs
// Purpose: Contains creature DBC file names code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Data.Dbc.Creatures;

// Type: CreatureDbcFileNames
// Purpose: Provides creature DBC file names behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class CreatureDbcFileNames
{
    // Constant: Defines the creature display info constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed creature display info value used anywhere this rule or protocol value is needed.
    public const string CreatureDisplayInfo = "CreatureDisplayInfo.dbc";
    // Constant: Defines the creature display info extra constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed creature display info extra value used anywhere this rule or protocol value is needed.
    public const string CreatureDisplayInfoExtra = "CreatureDisplayInfoExtra.dbc";
    // Constant: Defines the creature family constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed creature family value used anywhere this rule or protocol value is needed.
    public const string CreatureFamily = "CreatureFamily.dbc";
    // Constant: Defines the creature model data constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed creature model data value used anywhere this rule or protocol value is needed.
    public const string CreatureModelData = "CreatureModelData.dbc";
    // Constant: Defines the creature sound data constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed creature sound data value used anywhere this rule or protocol value is needed.
    public const string CreatureSoundData = "CreatureSoundData.dbc";
    // Constant: Defines the creature spell data constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed creature spell data value used anywhere this rule or protocol value is needed.
    public const string CreatureSpellData = "CreatureSpellData.dbc";
    // Constant: Defines the creature type constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed creature type value used anywhere this rule or protocol value is needed.
    public const string CreatureType = "CreatureType.dbc";

    // Property: Gets or sets the core creature DBC files value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: core creature DBC files value exposed by the owning type.
    public static IReadOnlyList<string> CoreCreatureDbcFiles { get; } =
    [
        CreatureDisplayInfo,
        CreatureDisplayInfoExtra,
        CreatureFamily,
        CreatureModelData,
        CreatureSoundData,
        CreatureSpellData,
        CreatureType,
    ];
}
