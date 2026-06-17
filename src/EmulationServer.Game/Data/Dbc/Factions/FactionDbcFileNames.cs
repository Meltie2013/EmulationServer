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
// File: src/EmulationServer.Game/Data/Dbc/Factions/FactionDbcFileNames.cs
// Purpose: Contains faction DBC file names code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Data.Dbc.Factions;

// Type: FactionDbcFileNames
// Purpose: Provides faction DBC file names behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class FactionDbcFileNames
{

    // Constant: Defines the faction constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed faction value used anywhere this rule or protocol value is needed.
    public const string Faction = "Faction.dbc";

    // Constant: Defines the faction template constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed faction template value used anywhere this rule or protocol value is needed.
    public const string FactionTemplate = "FactionTemplate.dbc";

    // Property: Gets or sets the core faction DBC files value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: core faction DBC files value exposed by the owning type.
    public static IReadOnlyList<string> CoreFactionDbcFiles { get; } =
    [
        Faction,
        FactionTemplate,
    ];
}
