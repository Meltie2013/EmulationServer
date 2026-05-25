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

namespace EmulationServer.Database.Accounts;

/**
  * Represents the account security level resolved from RBAC permissions.
  * The numeric values follow the same visible security-level meaning used by TrinityCore-style auth data.
  */
public enum AccountSecurityLevel : byte
{
    /**
      * Normal player account.
      */
    Player = 0,

    /**
      * Game Master account.
      */
    GameMaster = 1,

    /**
      * Administrator account.
      */
    Administrator = 2,
}
