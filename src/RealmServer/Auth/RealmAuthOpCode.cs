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
  * This file belongs to the realm authentication, build validation, and realm list packet creation portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.RealmServer.Auth;

/**
  * Defines the allowed realm auth op code values used to keep state and protocol decisions explicit.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public enum RealmAuthOpCode : byte
{
    /**
      * Represents the auth logon challenge value for RealmAuthOpCode.
      */
    AuthLogonChallenge = 0x00,
    /**
      * Represents the auth logon proof value for RealmAuthOpCode.
      */
    AuthLogonProof = 0x01,
    /**
      * Represents the auth reconnect challenge value for RealmAuthOpCode.
      */
    AuthReconnectChallenge = 0x02,
    /**
      * Represents the auth reconnect proof value for RealmAuthOpCode.
      */
    AuthReconnectProof = 0x03,
    /**
      * Represents the realm list value for RealmAuthOpCode.
      */
    RealmList = 0x10,
}
