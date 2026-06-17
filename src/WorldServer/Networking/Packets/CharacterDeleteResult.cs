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
// File: src/WorldServer/Networking/Packets/CharacterDeleteResult.cs
// Purpose: Contains character delete result code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.WorldServer.Networking.Packets;

// Type: CharacterDeleteResult
// Purpose: Defines the allowed character delete result values used by the world server gameplay, session, and character runtime layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public enum CharacterDeleteResult : byte
{

    // Enum Value: Defines the in progress enum value.
    // Value: explicit expression 0x38.
    InProgress = 0x38,

    // Enum Value: Defines the success enum value.
    // Value: explicit expression 0x39.
    Success = 0x39,

    // Enum Value: Defines the failed enum value.
    // Value: explicit expression 0x3A.
    Failed = 0x3A,
}
