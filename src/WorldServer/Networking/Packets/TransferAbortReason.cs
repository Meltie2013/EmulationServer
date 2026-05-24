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

/**
 * File overview: src/WorldServer/Networking/Packets/TransferAbortReason.cs
 * Documents the TransferAbortReason source file in the World of Warcraft packet opcode, reader, writer, and builder support area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.WorldServer.Networking.Packets;

/**
 * Lists the supported transfer abort reason values used by the World of Warcraft packet opcode, reader, writer, and builder support layer.
 * Numeric values are part of the project contract and should only be changed when the related client packet, DBC value, or database schema is updated as well.
 */
public enum TransferAbortReason : byte
{
    /**
     * Represents the none value for transfer abort reason handling.
     */
    None = 0,
    /**
     * Represents the map not allowed value for transfer abort reason handling.
     */
    MapNotAllowed = 1,
    /**
     * Represents the instance not found value for transfer abort reason handling.
     */
    InstanceNotFound = 2,
    /**
     * Represents the instance full value for transfer abort reason handling.
     */
    InstanceFull = 3,
    /**
     * Represents the zone in combat value for transfer abort reason handling.
     */
    ZoneInCombat = 6,
}
