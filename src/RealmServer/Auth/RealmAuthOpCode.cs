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
 * File overview: src/RealmServer/Auth/RealmAuthOpCode.cs
 * Documents the RealmAuthOpCode source file in the realm authentication, realm-list handling, and external client login services area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.RealmServer.Auth;

/**
 * Lists the supported realm auth op code values used by the realm authentication, realm-list handling, and external client login services layer.
 * Numeric values are part of the project contract and should only be changed when the related client packet, DBC value, or database schema is updated as well.
 */
public enum RealmAuthOpCode : byte
{
    /**
     * Represents the auth logon challenge value for realm auth op code handling.
     */
    AuthLogonChallenge = 0x00,
    /**
     * Represents the auth logon proof value for realm auth op code handling.
     */
    AuthLogonProof = 0x01,
    /**
     * Represents the auth reconnect challenge value for realm auth op code handling.
     */
    AuthReconnectChallenge = 0x02,
    /**
     * Represents the auth reconnect proof value for realm auth op code handling.
     */
    AuthReconnectProof = 0x03,
    /**
     * Represents the realm list value for realm auth op code handling.
     */
    RealmList = 0x10,
}
