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
  * File overview: src/WorldServer/Networking/Packets/AuthResponseCode.cs
  * Documents the AuthResponseCode source file in the World of Warcraft packet opcode, reader, writer, and builder support area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.WorldServer.Networking.Packets;

/**
  * Lists the supported auth response code values used by the World of Warcraft packet opcode, reader, writer, and builder support layer.
  * Numeric values are part of the project contract and should only be changed when the related client packet, DBC value, or database schema is updated as well.
  */
public enum AuthResponseCode : byte
{
    /**
      * Represents the ok value for auth response code handling.
      */
    Ok = 0x0C,
    /**
      * Represents the failed value for auth response code handling.
      */
    Failed = 0x0D,
    /**
      * Represents the rejected value for auth response code handling.
      */
    Rejected = 0x0F,
    /**
      * Represents the version mismatch value for auth response code handling.
      */
    VersionMismatch = 0x14,
    /**
      * Represents the banned value for auth response code handling.
      */
    Banned = 0x1C,
    /**
      * Represents the suspended value for auth response code handling.
      */
    Suspended = 0x20,
}
