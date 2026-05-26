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
  * Bridges in-game account commands to the shared account repository.
  */
public sealed class DatabaseInGameAccountCommandExecutor : IInGameAccountCommandExecutor
{
    private readonly AccountRepository _accountRepository;

    public DatabaseInGameAccountCommandExecutor(AccountRepository accountRepository)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
    }

    public Task<AccountCommandResult> CreateAccountAsync(string username, string password, CancellationToken cancellationToken)
    {
        return _accountRepository.CreateAccountAsync(username, password, cancellationToken: cancellationToken);
    }

    public Task<AccountCommandResult> DeleteAccountAsync(string username, CancellationToken cancellationToken)
    {
        return _accountRepository.RemoveAccountAsync(username, cancellationToken);
    }

    public Task<AccountCommandResult> SetPermissionAsync(string username, uint permissionId, CancellationToken cancellationToken)
    {
        return _accountRepository.SetAccountPermissionAsync(username, permissionId, cancellationToken: cancellationToken);
    }

    public Task<AccountCommandResult> RemovePermissionAsync(string username, uint permissionId, CancellationToken cancellationToken)
    {
        return _accountRepository.RemoveAccountPermissionAsync(username, permissionId, cancellationToken: cancellationToken);
    }

    public Task<AccountCommandResult> BanAccountAsync(string username, string bannedBy, CancellationToken cancellationToken)
    {
        return _accountRepository.BanAccountAsync(username, 0, bannedBy, "Banned by in-game command.", cancellationToken);
    }
}
