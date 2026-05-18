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
  * This file belongs to the realm authentication, build validation, and realm list packet creation portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.RealmServer.Auth;

/**
  * Defines the allowed realm auth result values used to keep state and protocol decisions explicit.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public enum RealmAuthResult : byte
{
    /**
      * Represents the success value for RealmAuthResult.
      */
    Success = 0x00,
    /**
      * Represents the failed value for RealmAuthResult.
      */
    Failed = 0x01,
    /**
      * Represents the banned value for RealmAuthResult.
      */
    Banned = 0x03,
    /**
      * Represents the unknown account value for RealmAuthResult.
      */
    UnknownAccount = 0x04,
    /**
      * Represents the already online value for RealmAuthResult.
      */
    AlreadyOnline = 0x06,
    /**
      * Represents the no time value for RealmAuthResult.
      */
    NoTime = 0x07,
    /**
      * Represents the database busy value for RealmAuthResult.
      */
    DatabaseBusy = 0x08,
    /**
      * Represents the version invalid value for RealmAuthResult.
      */
    VersionInvalid = 0x09,
    /**
      * Represents the version update value for RealmAuthResult.
      */
    VersionUpdate = 0x0A,
    /**
      * Represents the invalid server value for RealmAuthResult.
      */
    InvalidServer = 0x0B,
    /**
      * Represents the suspended value for RealmAuthResult.
      */
    Suspended = 0x0C,
    /**
      * Represents the no access value for RealmAuthResult.
      */
    NoAccess = 0x0D,
    /**
      * Represents the locked enforced value for RealmAuthResult.
      */
    LockedEnforced = 0x10,
}
