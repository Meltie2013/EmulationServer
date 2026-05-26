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

namespace EmulationServer.Game.Commands;

/**
  * Defines one in-game chat command.
  * Each command should live in its own file and expose only the metadata the command registry needs to route and authorize it.
  */
public interface IChatCommand
{
    /**
      * Primary command token without the dot prefix.
      */
    string Name { get; }

    /**
      * Alternate command tokens without the dot prefix.
      */
    IReadOnlyList<string> Aliases { get; }

    /**
      * RBAC permission id required to execute the command.
      */
    uint RequiredPermission { get; }

    /**
      * Short text shown by the help command.
      */
    string Description { get; }

    /**
      * Syntax text shown by the help command.
      */
    string Syntax { get; }

    /**
      * Executes the command after the registry has parsed the command name and RBAC has approved access.
      */
    Task<string> ExecuteAsync(ChatCommandContext context, CancellationToken cancellationToken);
}
