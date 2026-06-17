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
// File: src/EmulationServer.Game/Data/Dbc/Maps/MapDbcFileNames.cs
// Purpose: Contains map DBC file names code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Data.Dbc.Maps;

// Type: MapDbcFileNames
// Purpose: Provides map DBC file names behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class MapDbcFileNames
{

    // Constant: Defines the map constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed map value used anywhere this rule or protocol value is needed.
    public const string Map = "Map.dbc";

    // Constant: Defines the area table constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed area table value used anywhere this rule or protocol value is needed.
    public const string AreaTable = "AreaTable.dbc";

    // Constant: Defines the area trigger constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed area trigger value used anywhere this rule or protocol value is needed.
    public const string AreaTrigger = "AreaTrigger.dbc";

    // Constant: Defines the world map area constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed world map area value used anywhere this rule or protocol value is needed.
    public const string WorldMapArea = "WorldMapArea.dbc";

    // Constant: Defines the world map continent constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed world map continent value used anywhere this rule or protocol value is needed.
    public const string WorldMapContinent = "WorldMapContinent.dbc";

    // Constant: Defines the world map overlay constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed world map overlay value used anywhere this rule or protocol value is needed.
    public const string WorldMapOverlay = "WorldMapOverlay.dbc";

    // Property: Gets or sets the core map DBC files value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: core map DBC files value exposed by the owning type.
    public static IReadOnlyList<string> CoreMapDbcFiles { get; } =
    [
        Map,
        AreaTable,
        AreaTrigger,
        WorldMapArea,
        WorldMapContinent,
        WorldMapOverlay,
    ];
}
