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
// File: src/WorldServer/Networking/Packets/TransferAbortReason.cs
// Purpose: Contains transfer abort reason code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.WorldServer.Networking.Packets;

// Type: TransferAbortReason
// Purpose: Defines the allowed transfer abort reason values used by the world server gameplay, session, and character runtime layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public enum TransferAbortReason : byte
{

    // Enum Value: Defines the none enum value.
    // Value: explicit expression 0.
    None = 0,

    // Enum Value: Defines the map not allowed enum value.
    // Value: explicit expression 1.
    MapNotAllowed = 1,

    // Enum Value: Defines the instance not found enum value.
    // Value: explicit expression 2.
    InstanceNotFound = 2,

    // Enum Value: Defines the instance full enum value.
    // Value: explicit expression 3.
    InstanceFull = 3,

    // Enum Value: Defines the zone in combat enum value.
    // Value: explicit expression 6.
    ZoneInCombat = 6,
}
