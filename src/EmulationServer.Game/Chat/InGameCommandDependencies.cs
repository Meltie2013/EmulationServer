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
// File: src/EmulationServer.Game/Chat/InGameCommandDependencies.cs
// Purpose: Contains in game command dependencies code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Database.Accounts;

namespace EmulationServer.Game.Commands;

// Type: InGameCommandDependencies
// Purpose: Provides in game command dependencies behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class InGameCommandDependencies
{
    public static InGameCommandDependencies Empty { get; } = new();

    // Property: Gets or sets the account commands value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: account commands value exposed by the owning type.
    public IInGameAccountCommandExecutor? AccountCommands { get; init; }

    // Property: Gets or sets the map commands value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: map commands value exposed by the owning type.
    public IInGameMapCommandExecutor? MapCommands { get; init; }

    // Property: Gets or sets the RBAC commands value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: RBAC commands value exposed by the owning type.
    public IInGameRbacCommandExecutor? RbacCommands { get; init; }

    // Property: Gets or sets the server commands value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: server commands value exposed by the owning type.
    public IInGameServerCommandExecutor? ServerCommands { get; init; }
}

// Type: IInGameAccountCommandExecutor
// Purpose: Defines the I in game account command executor contract used by the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public interface IInGameAccountCommandExecutor
{
    Task<AccountCommandResult> CreateAccountAsync(string username, string password, CancellationToken cancellationToken);

    Task<AccountCommandResult> DeleteAccountAsync(string username, CancellationToken cancellationToken);

    Task<AccountCommandResult> SetPermissionAsync(string username, uint permissionId, CancellationToken cancellationToken);

    Task<AccountCommandResult> RemovePermissionAsync(string username, uint permissionId, CancellationToken cancellationToken);

    Task<AccountCommandResult> BanAccountAsync(string username, string bannedBy, CancellationToken cancellationToken);
}

// Type: IInGameMapCommandExecutor
// Purpose: Defines the I in game map command executor contract used by the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public interface IInGameMapCommandExecutor
{
    Task<string> ExecuteMapCommandAsync(string action, int mapId, TimeSpan delay, string requestedBy, CancellationToken cancellationToken);
}

// Type: IInGameRbacCommandExecutor
// Purpose: Defines the I in game RBAC command executor contract used by the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public interface IInGameRbacCommandExecutor
{
    Task<string> ReloadRbacAsync(CancellationToken cancellationToken);
}

// Type: IInGameServerCommandExecutor
// Purpose: Defines the I in game server command executor contract used by the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public interface IInGameServerCommandExecutor
{
    Task<string> ScheduleShutdownAsync(TimeSpan delay, string requestedBy, CancellationToken cancellationToken);

    Task<string> ScheduleRestartAsync(TimeSpan delay, string requestedBy, CancellationToken cancellationToken);
}
