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

using EmulationServer.Database.Accounts;
using EmulationServer.Game.Players;

/**
  * File overview: src/EmulationServer.Game/Commands/IInGameCommandSession.cs
  * Documents the IInGameCommandSession source file in the in-game command parsing and command session access area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Commands;

/**
  * Defines the data and permission checks chat commands can use without depending on a concrete WorldClientSession.
  * Command handlers should use this interface for account, player, and RBAC information so each command stays isolated in its own file.
  */
public interface IInGameCommandSession
{
    /**
      * Account id used by permission checks and later command audit logging.
      */
    uint AccountId { get; }

    /**
      * Account name used in diagnostics and command responses.
      */
    string AccountName { get; }

    /**
      * RBAC-derived account security level.
      */
    AccountSecurityLevel AccountSecurityLevel { get; }

    /**
      * Active world player count exposed for command handlers.
      */
    int ActivePlayerCount { get; }

    /**
      * Configured message of the day exposed for command handlers.
      */
    string MessageOfTheDay { get; }

    /**
      * Checks the final RBAC permission set for a command or role permission id.
      */
    bool HasPermission(uint permissionId);

    /**
      * Reloads the current account RBAC data from the account database.
      */
    Task ReloadPermissionsAsync(CancellationToken cancellationToken);

    /**
      * Requires the current in-world player and throws when the command was executed before entering the world.
      */
    PlayerLoginRecord RequireCurrentPlayer();
}
