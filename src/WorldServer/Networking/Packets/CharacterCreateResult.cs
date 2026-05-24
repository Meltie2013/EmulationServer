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
 * File overview: src/WorldServer/Networking/Packets/CharacterCreateResult.cs
 * Documents the CharacterCreateResult source file in the World of Warcraft packet opcode, reader, writer, and builder support area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.WorldServer.Networking.Packets;

/**
 * Lists the supported character create result values used by the World of Warcraft packet opcode, reader, writer, and builder support layer.
 * Numeric values are part of the project contract and should only be changed when the related client packet, DBC value, or database schema is updated as well.
 */
public enum CharacterCreateResult : byte
{
    /**
     * Represents the success value for character create result handling.
     */
    Success = 0x2E,
    /**
     * Represents the error value for character create result handling.
     */
    Error = 0x2F,
    /**
     * Represents the failed value for character create result handling.
     */
    Failed = 0x30,
    /**
     * Represents the name in use value for character create result handling.
     */
    NameInUse = 0x31,
    /**
     * Represents the disabled value for character create result handling.
     */
    Disabled = 0x32,
    /**
     * Represents the pv p teams violation value for character create result handling.
     */
    PvPTeamsViolation = 0x33,
    /**
     * Represents the server limit value for character create result handling.
     */
    ServerLimit = 0x34,
    /**
     * Represents the account limit value for character create result handling.
     */
    AccountLimit = 0x35,
    /**
     * Represents the server queue value for character create result handling.
     */
    ServerQueue = 0x36,
    /**
     * Represents the only existing value for character create result handling.
     */
    OnlyExisting = 0x37,
    /**
     * Represents the expansion value for character create result handling.
     */
    Expansion = 0x38,
    /**
     * Represents the name invalid value for character create result handling.
     */
    NameInvalid = 0x39,
    /**
     * Represents the name profane value for character create result handling.
     */
    NameProfane = 0x3A,
    /**
     * Represents the name reserved value for character create result handling.
     */
    NameReserved = 0x3B,
}
