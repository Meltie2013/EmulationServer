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
// File: src/EmulationServer.Game/Reputation/ReputationRank.cs
// Purpose: Contains reputation rank code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Reputation;

// Type: ReputationRank
// Purpose: Defines the allowed reputation rank values used by the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public enum ReputationRank
{
    // Enum Value: Defines the hated enum value.
    // Value: explicit expression 0.
    Hated = 0,
    // Enum Value: Defines the hostile enum value.
    // Value: explicit expression 1.
    Hostile = 1,
    // Enum Value: Defines the unfriendly enum value.
    // Value: explicit expression 2.
    Unfriendly = 2,
    // Enum Value: Defines the neutral enum value.
    // Value: explicit expression 3.
    Neutral = 3,
    // Enum Value: Defines the friendly enum value.
    // Value: explicit expression 4.
    Friendly = 4,
    // Enum Value: Defines the honored enum value.
    // Value: explicit expression 5.
    Honored = 5,
    // Enum Value: Defines the revered enum value.
    // Value: explicit expression 6.
    Revered = 6,
    // Enum Value: Defines the exalted enum value.
    // Value: explicit expression 7.
    Exalted = 7,
}
