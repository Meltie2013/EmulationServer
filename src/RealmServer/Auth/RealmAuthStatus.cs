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
  * File overview: src/RealmServer/Auth/RealmAuthStatus.cs
  * Documents the RealmAuthStatus source file in the realm authentication, realm-list handling, and external client login services area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.RealmServer.Auth;

/**
  * Lists the supported realm auth status values used by the realm authentication, realm-list handling, and external client login services layer.
  * Numeric values are part of the project contract and should only be changed when the related client packet, DBC value, or database schema is updated as well.
  */
public enum RealmAuthStatus
{
    /**
      * Represents the challenge value for realm auth status handling.
      */
    Challenge,
    /**
      * Represents the logon proof value for realm auth status handling.
      */
    LogonProof,
    /**
      * Represents the authenticated value for realm auth status handling.
      */
    Authenticated,
    /**
      * Represents the closed value for realm auth status handling.
      */
    Closed,
}
