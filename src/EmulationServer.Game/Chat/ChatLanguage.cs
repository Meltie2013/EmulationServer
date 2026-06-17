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
// File: src/EmulationServer.Game/Chat/ChatLanguage.cs
// Purpose: Contains chat language code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Chat;

// Type: ChatLanguage
// Purpose: Defines the allowed chat language values used by the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public enum ChatLanguage : uint
{

    // Enum Value: Defines the universal enum value.
    // Value: explicit expression 0.
    Universal = 0,

    // Enum Value: Defines the orcish enum value.
    // Value: explicit expression 1.
    Orcish = 1,

    // Enum Value: Defines the darnassian enum value.
    // Value: explicit expression 2.
    Darnassian = 2,

    // Enum Value: Defines the taurahe enum value.
    // Value: explicit expression 3.
    Taurahe = 3,

    // Enum Value: Defines the dwarvish enum value.
    // Value: explicit expression 6.
    Dwarvish = 6,

    // Enum Value: Defines the common enum value.
    // Value: explicit expression 7.
    Common = 7,

    // Enum Value: Defines the demonic enum value.
    // Value: explicit expression 8.
    Demonic = 8,

    // Enum Value: Defines the titan enum value.
    // Value: explicit expression 9.
    Titan = 9,

    // Enum Value: Defines the thalassian enum value.
    // Value: explicit expression 10.
    Thalassian = 10,

    // Enum Value: Defines the draconic enum value.
    // Value: explicit expression 11.
    Draconic = 11,

    // Enum Value: Defines the kalimag enum value.
    // Value: explicit expression 12.
    Kalimag = 12,

    // Enum Value: Defines the gnomish enum value.
    // Value: explicit expression 13.
    Gnomish = 13,

    // Enum Value: Defines the troll enum value.
    // Value: explicit expression 14.
    Troll = 14,

    // Enum Value: Defines the gutterspeak enum value.
    // Value: explicit expression 33.
    Gutterspeak = 33,
}
