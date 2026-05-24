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
  * File overview: src/RealmServer/Auth/RealmAuthResult.cs
  * Documents the RealmAuthResult source file in the realm authentication, realm-list handling, and external client login services area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.RealmServer.Auth;

/**
  * Lists the supported realm auth result values used by the realm authentication, realm-list handling, and external client login services layer.
  * Numeric values are part of the project contract and should only be changed when the related client packet, DBC value, or database schema is updated as well.
  */
public enum RealmAuthResult : byte
{
    /**
      * Represents the success value for realm auth result handling.
      */
    Success = 0x00,
    /**
      * Represents the failed value for realm auth result handling.
      */
    Failed = 0x01,
    /**
      * Represents the banned value for realm auth result handling.
      */
    Banned = 0x03,
    /**
      * Represents the unknown account value for realm auth result handling.
      */
    UnknownAccount = 0x04,
    /**
      * Represents the already online value for realm auth result handling.
      */
    AlreadyOnline = 0x06,
    /**
      * Represents the no time value for realm auth result handling.
      */
    NoTime = 0x07,
    /**
      * Represents the database busy value for realm auth result handling.
      */
    DatabaseBusy = 0x08,
    /**
      * Represents the version invalid value for realm auth result handling.
      */
    VersionInvalid = 0x09,
    /**
      * Represents the version update value for realm auth result handling.
      */
    VersionUpdate = 0x0A,
    /**
      * Represents the invalid server value for realm auth result handling.
      */
    InvalidServer = 0x0B,
    /**
      * Represents the suspended value for realm auth result handling.
      */
    Suspended = 0x0C,
    /**
      * Represents the no access value for realm auth result handling.
      */
    NoAccess = 0x0D,
    /**
      * Represents the locked enforced value for realm auth result handling.
      */
    LockedEnforced = 0x10,
}
