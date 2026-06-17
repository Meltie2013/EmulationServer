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
// File: src/EmulationServer.Game/Reputation/ReputationFlags.cs
// Purpose: Contains reputation flags code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Reputation;

[Flags]
// Type: ReputationFlags
// Purpose: Defines the allowed reputation flags values used by the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public enum ReputationFlags : uint
{
    // Enum Value: Defines the none enum value.
    // Value: explicit expression 0x00.
    None = 0x00,
    // Enum Value: Defines the visible enum value.
    // Value: explicit expression 0x01.
    Visible = 0x01,
    // Enum Value: Defines the at war enum value.
    // Value: explicit expression 0x02.
    AtWar = 0x02,
    // Enum Value: Defines the hidden enum value.
    // Value: explicit expression 0x04.
    Hidden = 0x04,
    // Enum Value: Defines the invisible forced enum value.
    // Value: explicit expression 0x08.
    InvisibleForced = 0x08,
    // Enum Value: Defines the peace forced enum value.
    // Value: explicit expression 0x10.
    PeaceForced = 0x10,
    // Enum Value: Defines the inactive enum value.
    // Value: explicit expression 0x20.
    Inactive = 0x20,
    // Enum Value: Defines the rival enum value.
    // Value: explicit expression 0x40.
    Rival = 0x40,
    // Enum Value: Defines the special enum value.
    // Value: explicit expression 0x80.
    Special = 0x80,
}
