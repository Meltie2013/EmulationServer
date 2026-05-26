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

namespace EmulationServer.Game.Commands;

/**
  * Carries runtime services that command files can use without depending on a concrete WorldServer implementation.
  */
public sealed class InGameCommandDependencies
{
    public static InGameCommandDependencies Empty { get; } = new();

    public IInGameAccountCommandExecutor? AccountCommands { get; init; }

    public IInGameMapCommandExecutor? MapCommands { get; init; }

    public IInGameRbacCommandExecutor? RbacCommands { get; init; }

    public IInGameServerCommandExecutor? ServerCommands { get; init; }
}

/**
  * Executes account database mutations requested by administrator chat commands.
  */
public interface IInGameAccountCommandExecutor
{
    Task<AccountCommandResult> CreateAccountAsync(string username, string password, CancellationToken cancellationToken);

    Task<AccountCommandResult> DeleteAccountAsync(string username, CancellationToken cancellationToken);

    Task<AccountCommandResult> SetPermissionAsync(string username, uint permissionId, CancellationToken cancellationToken);

    Task<AccountCommandResult> RemovePermissionAsync(string username, uint permissionId, CancellationToken cancellationToken);

    Task<AccountCommandResult> BanAccountAsync(string username, string bannedBy, CancellationToken cancellationToken);
}

/**
  * Sends map control commands to MapServer or InstanceServer and formats feedback for the in-game caller.
  */
public interface IInGameMapCommandExecutor
{
    Task<string> ExecuteMapCommandAsync(string action, int mapId, TimeSpan delay, string requestedBy, CancellationToken cancellationToken);
}


/**
  * Reloads RBAC data for active in-game sessions.
  */
public interface IInGameRbacCommandExecutor
{
    Task<string> ReloadRbacAsync(CancellationToken cancellationToken);
}

/**
  * Schedules whole-server shutdown and restart requests issued from in-game administrator commands.
  */
public interface IInGameServerCommandExecutor
{
    Task<string> ScheduleShutdownAsync(TimeSpan delay, string requestedBy, CancellationToken cancellationToken);

    Task<string> ScheduleRestartAsync(TimeSpan delay, string requestedBy, CancellationToken cancellationToken);
}
