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
// File: src/WorldServer/Networking/Packets/CharacterCreateResult.cs
// Purpose: Contains character create result code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.WorldServer.Networking.Packets;

// Type: CharacterCreateResult
// Purpose: Defines the allowed character create result values used by the world server gameplay, session, and character runtime layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public enum CharacterCreateResult : byte
{

    // Enum Value: Defines the success enum value.
    // Value: explicit expression 0x2E.
    Success = 0x2E,

    // Enum Value: Defines the error enum value.
    // Value: explicit expression 0x2F.
    Error = 0x2F,

    // Enum Value: Defines the failed enum value.
    // Value: explicit expression 0x30.
    Failed = 0x30,

    // Enum Value: Defines the name in use enum value.
    // Value: explicit expression 0x31.
    NameInUse = 0x31,

    // Enum Value: Defines the disabled enum value.
    // Value: explicit expression 0x32.
    Disabled = 0x32,

    // Enum Value: Defines the pv P teams violation enum value.
    // Value: explicit expression 0x33.
    PvPTeamsViolation = 0x33,

    // Enum Value: Defines the server limit enum value.
    // Value: explicit expression 0x34.
    ServerLimit = 0x34,

    // Enum Value: Defines the account limit enum value.
    // Value: explicit expression 0x35.
    AccountLimit = 0x35,

    // Enum Value: Defines the server queue enum value.
    // Value: explicit expression 0x36.
    ServerQueue = 0x36,

    // Enum Value: Defines the only existing enum value.
    // Value: explicit expression 0x37.
    OnlyExisting = 0x37,

    // Enum Value: Defines the expansion enum value.
    // Value: explicit expression 0x38.
    Expansion = 0x38,

    // Enum Value: Defines the name invalid enum value.
    // Value: explicit expression 0x39.
    NameInvalid = 0x39,

    // Enum Value: Defines the name profane enum value.
    // Value: explicit expression 0x3A.
    NameProfane = 0x3A,

    // Enum Value: Defines the name reserved enum value.
    // Value: explicit expression 0x3B.
    NameReserved = 0x3B,
}
