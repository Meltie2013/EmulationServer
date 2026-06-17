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
// File: src/RealmServer/Auth/RealmAuthResult.cs
// Purpose: Contains realm auth result code for the realm server authentication, realm-list, and account connection layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.RealmServer.Auth;

// Type: RealmAuthResult
// Purpose: Defines the allowed realm auth result values used by the realm server authentication, realm-list, and account connection layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public enum RealmAuthResult : byte
{

    // Enum Value: Defines the success enum value.
    // Value: explicit expression 0x00.
    Success = 0x00,

    // Enum Value: Defines the failed enum value.
    // Value: explicit expression 0x01.
    Failed = 0x01,

    // Enum Value: Defines the banned enum value.
    // Value: explicit expression 0x03.
    Banned = 0x03,

    // Enum Value: Defines the unknown account enum value.
    // Value: explicit expression 0x04.
    UnknownAccount = 0x04,

    // Enum Value: Defines the already online enum value.
    // Value: explicit expression 0x06.
    AlreadyOnline = 0x06,

    // Enum Value: Defines the no time enum value.
    // Value: explicit expression 0x07.
    NoTime = 0x07,

    // Enum Value: Defines the database busy enum value.
    // Value: explicit expression 0x08.
    DatabaseBusy = 0x08,

    // Enum Value: Defines the version invalid enum value.
    // Value: explicit expression 0x09.
    VersionInvalid = 0x09,

    // Enum Value: Defines the version update enum value.
    // Value: explicit expression 0x0A.
    VersionUpdate = 0x0A,

    // Enum Value: Defines the invalid server enum value.
    // Value: explicit expression 0x0B.
    InvalidServer = 0x0B,

    // Enum Value: Defines the suspended enum value.
    // Value: explicit expression 0x0C.
    Suspended = 0x0C,

    // Enum Value: Defines the no access enum value.
    // Value: explicit expression 0x0D.
    NoAccess = 0x0D,

    // Enum Value: Defines the locked enforced enum value.
    // Value: explicit expression 0x10.
    LockedEnforced = 0x10,
}
