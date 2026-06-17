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
// File: src/EmulationServer.Game/Data/Dbc/Maps/MapInstanceType.cs
// Purpose: Contains map instance type code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Data.Dbc.Maps;

// Type: MapInstanceType
// Purpose: Defines the allowed map instance type values used by the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public enum MapInstanceType
{

    // Enum Value: Defines the unknown enum value.
    // Value: explicit expression -1.
    Unknown = -1,

    // Enum Value: Defines the world enum value.
    // Value: explicit expression 0.
    World = 0,

    // Enum Value: Defines the dungeon enum value.
    // Value: explicit expression 1.
    Dungeon = 1,

    // Enum Value: Defines the raid enum value.
    // Value: explicit expression 2.
    Raid = 2,

    // Enum Value: Defines the battleground enum value.
    // Value: explicit expression 3.
    Battleground = 3,

    // Enum Value: Defines the arena enum value.
    // Value: explicit expression 4.
    Arena = 4,
}
