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
// File: src/RealmServer/Auth/RealmAuthStatus.cs
// Purpose: Contains realm auth status code for the realm server authentication, realm-list, and account connection layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.RealmServer.Auth;

// Type: RealmAuthStatus
// Purpose: Defines the allowed realm auth status values used by the realm server authentication, realm-list, and account connection layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public enum RealmAuthStatus
{

    // Enum Value: Defines the challenge enum value.
    // Value: next sequential value assigned by C#.
    Challenge,

    // Enum Value: Defines the logon proof enum value.
    // Value: next sequential value assigned by C#.
    LogonProof,

    // Enum Value: Defines the reconnect proof enum value.
    // Value: next sequential value assigned by C#.
    ReconnectProof,

    // Enum Value: Defines the authenticated enum value.
    // Value: next sequential value assigned by C#.
    Authenticated,

    // Enum Value: Defines the closed enum value.
    // Value: next sequential value assigned by C#.
    Closed,
}
