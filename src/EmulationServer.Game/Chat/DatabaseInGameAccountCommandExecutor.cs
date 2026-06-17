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
// File: src/EmulationServer.Game/Chat/DatabaseInGameAccountCommandExecutor.cs
// Purpose: Contains database in game account command executor code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Database.Accounts;

namespace EmulationServer.Game.Commands;

// Type: DatabaseInGameAccountCommandExecutor
// Purpose: Provides database in game account command executor behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class DatabaseInGameAccountCommandExecutor : IInGameAccountCommandExecutor
{
    // Field: Stores the account repository state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current account repository backing value maintained by the owning type.
    private readonly AccountRepository _accountRepository;

    // Constructor: DatabaseInGameAccountCommandExecutor
    // Purpose: Initializes a new DatabaseInGameAccountCommandExecutor instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - accountRepository: Account repository value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to DatabaseInGameAccountCommandExecutor so callers do not duplicate validation, protocol, or persistence rules.
    public DatabaseInGameAccountCommandExecutor(AccountRepository accountRepository)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
    }

    // Method: CreateAccountAsync
    // Purpose: Applies create account changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - username: Username value supplied by the caller for this operation.
    // - password: Password value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to DatabaseInGameAccountCommandExecutor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public Task<AccountCommandResult> CreateAccountAsync(string username, string password, CancellationToken cancellationToken)
    {
        return _accountRepository.CreateAccountAsync(username, password, cancellationToken: cancellationToken);
    }

    // Method: DeleteAccountAsync
    // Purpose: Applies delete account changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - username: Username value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to DatabaseInGameAccountCommandExecutor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public Task<AccountCommandResult> DeleteAccountAsync(string username, CancellationToken cancellationToken)
    {
        return _accountRepository.RemoveAccountAsync(username, cancellationToken);
    }

    // Method: SetPermissionAsync
    // Purpose: Applies set permission changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - username: Username value supplied by the caller for this operation.
    // - permissionId: Permission ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to DatabaseInGameAccountCommandExecutor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public Task<AccountCommandResult> SetPermissionAsync(string username, uint permissionId, CancellationToken cancellationToken)
    {
        return _accountRepository.SetAccountPermissionAsync(username, permissionId, cancellationToken: cancellationToken);
    }

    // Method: RemovePermissionAsync
    // Purpose: Applies remove permission changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - username: Username value supplied by the caller for this operation.
    // - permissionId: Permission ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to DatabaseInGameAccountCommandExecutor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public Task<AccountCommandResult> RemovePermissionAsync(string username, uint permissionId, CancellationToken cancellationToken)
    {
        return _accountRepository.RemoveAccountPermissionAsync(username, permissionId, cancellationToken: cancellationToken);
    }

    // Method: BanAccountAsync
    // Purpose: Executes the ban account operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - username: Username value supplied by the caller for this operation.
    // - bannedBy: Banned by value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to DatabaseInGameAccountCommandExecutor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public Task<AccountCommandResult> BanAccountAsync(string username, string bannedBy, CancellationToken cancellationToken)
    {
        return _accountRepository.BanAccountAsync(username, 0, bannedBy, "Banned by in-game command.", cancellationToken);
    }
}
