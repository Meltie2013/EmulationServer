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
// File: src/EmulationServer.Database/Accounts/AccountSecurityLevel.cs
// Purpose: Contains account security level code for the database persistence, repository, and MySQL connectivity layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Database.Accounts;

// Type: AccountSecurityLevel
// Purpose: Defines the allowed account security level values used by the database persistence, repository, and MySQL connectivity layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public enum AccountSecurityLevel : byte
{

    // Enum Value: Defines the player enum value.
    // Value: explicit expression 0.
    Player = 0,

    // Enum Value: Defines the game master enum value.
    // Value: explicit expression 1.
    GameMaster = 1,

    // Enum Value: Defines the administrator enum value.
    // Value: explicit expression 2.
    Administrator = 2,
}
