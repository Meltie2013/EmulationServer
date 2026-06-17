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
// File: src/RealmServer/Realms/RealmFlags.cs
// Purpose: Contains realm flags code for the realm server authentication, realm-list, and account connection layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.RealmServer.Realms;

[Flags]
// Type: RealmFlags
// Purpose: Defines the allowed realm flags values used by the realm server authentication, realm-list, and account connection layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public enum RealmFlags : byte
{

    // Enum Value: Defines the none enum value.
    // Value: explicit expression 0x00.
    None = 0x00,

    // Enum Value: Defines the invalid enum value.
    // Value: explicit expression 0x01.
    Invalid = 0x01,

    // Enum Value: Defines the offline enum value.
    // Value: explicit expression 0x02.
    Offline = 0x02,

    // Enum Value: Defines the specify build enum value.
    // Value: explicit expression 0x04.
    SpecifyBuild = 0x04,

    // Enum Value: Defines the new players enum value.
    // Value: explicit expression 0x20.
    NewPlayers = 0x20,

    // Enum Value: Defines the recommended enum value.
    // Value: explicit expression 0x40.
    Recommended = 0x40,

    // Enum Value: Defines the full enum value.
    // Value: explicit expression 0x80.
    Full = 0x80,
}
