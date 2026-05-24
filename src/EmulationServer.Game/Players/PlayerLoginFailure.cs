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
  * File overview: src/EmulationServer.Game/Players/PlayerLoginFailure.cs
  * Documents the PlayerLoginFailure source file in the logged-in player state, persistence models, and gameplay records area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Players;

/**
  * Lists the supported player login failure values used by the logged-in player state, persistence models, and gameplay records layer.
  * Numeric values are part of the project contract and should only be changed when the related client packet, DBC value, or database schema is updated as well.
  */
public enum PlayerLoginFailure : byte
{
    /**
      * Represents the no world value for player login failure handling.
      */
    NoWorld = 0x3E,
    /**
      * Represents the duplicate login value for player login failure handling.
      */
    DuplicateLogin = 0x3F,
    /**
      * Represents the no instances value for player login failure handling.
      */
    NoInstances = 0x40,
    /**
      * Represents the failed value for player login failure handling.
      */
    Failed = 0x41,
    /**
      * Represents the disabled value for player login failure handling.
      */
    Disabled = 0x42,
    /**
      * Represents the not found value for player login failure handling.
      */
    NotFound = 0x43,
    /**
      * Represents the account mismatch value for player login failure handling.
      */
    AccountMismatch = 0x44,
}
