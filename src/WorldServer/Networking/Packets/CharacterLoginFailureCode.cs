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
 * File overview: src/WorldServer/Networking/Packets/CharacterLoginFailureCode.cs
 * Documents the CharacterLoginFailureCode source file in the World of Warcraft packet opcode, reader, writer, and builder support area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.WorldServer.Networking.Packets;

/**
 * Lists the supported character login failure code values used by the World of Warcraft packet opcode, reader, writer, and builder support layer.
 * Numeric values are part of the project contract and should only be changed when the related client packet, DBC value, or database schema is updated as well.
 */
public enum CharacterLoginFailureCode : byte
{
    /**
     * Represents the no world value for character login failure code handling.
     */
    NoWorld = 0x3E,
    /**
     * Represents the duplicate login value for character login failure code handling.
     */
    DuplicateLogin = 0x3F,
    /**
     * Represents the no instances value for character login failure code handling.
     */
    NoInstances = 0x40,
    /**
     * Represents the failed value for character login failure code handling.
     */
    Failed = 0x41,
    /**
     * Represents the disabled value for character login failure code handling.
     */
    Disabled = 0x42,
    /**
     * Represents the not found value for character login failure code handling.
     */
    NotFound = 0x43,
}
