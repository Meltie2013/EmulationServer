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
// File: src/RealmServer/Auth/RealmAuthOpCode.cs
// Purpose: Contains realm auth op code code for the realm server authentication, realm-list, and account connection layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.RealmServer.Auth;

// Type: RealmAuthOpCode
// Purpose: Defines the allowed realm auth op code values used by the realm server authentication, realm-list, and account connection layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public enum RealmAuthOpCode : byte
{

    // Enum Value: Defines the auth logon challenge enum value.
    // Value: explicit expression 0x00.
    AuthLogonChallenge = 0x00,

    // Enum Value: Defines the auth logon proof enum value.
    // Value: explicit expression 0x01.
    AuthLogonProof = 0x01,

    // Enum Value: Defines the auth reconnect challenge enum value.
    // Value: explicit expression 0x02.
    AuthReconnectChallenge = 0x02,

    // Enum Value: Defines the auth reconnect proof enum value.
    // Value: explicit expression 0x03.
    AuthReconnectProof = 0x03,

    // Enum Value: Defines the realm list enum value.
    // Value: explicit expression 0x10.
    RealmList = 0x10,
}
