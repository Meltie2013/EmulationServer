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
// File: src/EmulationServer.Game/Players/PlayerLoginFailure.cs
// Purpose: Contains player login failure code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Players;

// Type: PlayerLoginFailure
// Purpose: Defines the allowed player login failure values used by the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public enum PlayerLoginFailure : byte
{

    // Enum Value: Defines the no world enum value.
    // Value: explicit expression 0x3E.
    NoWorld = 0x3E,

    // Enum Value: Defines the duplicate login enum value.
    // Value: explicit expression 0x3F.
    DuplicateLogin = 0x3F,

    // Enum Value: Defines the no instances enum value.
    // Value: explicit expression 0x40.
    NoInstances = 0x40,

    // Enum Value: Defines the failed enum value.
    // Value: explicit expression 0x41.
    Failed = 0x41,

    // Enum Value: Defines the disabled enum value.
    // Value: explicit expression 0x42.
    Disabled = 0x42,

    // Enum Value: Defines the not found enum value.
    // Value: explicit expression 0x43.
    NotFound = 0x43,

    // Enum Value: Defines the account mismatch enum value.
    // Value: explicit expression 0x44.
    AccountMismatch = 0x44,
}
