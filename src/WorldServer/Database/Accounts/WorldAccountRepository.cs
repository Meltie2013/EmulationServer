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
// File: src/WorldServer/Database/Accounts/WorldAccountRepository.cs
// Purpose: Contains world account repository code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Database.Accounts;
using EmulationServer.Database.Interfaces;

using MySqlConnector;

namespace EmulationServer.WorldServer.Database.Accounts;

// Type: WorldAccountRepository
// Purpose: Provides world account repository behavior for the world server gameplay, session, and character runtime layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class WorldAccountRepository
{

    // Field: Stores the database service state used by the world server gameplay, session, and character runtime layer.
    // Value: current database service backing value maintained by the owning type.
    private readonly IDatabaseService _databaseService;
    // Field: Stores the account repository state used by the world server gameplay, session, and character runtime layer.
    // Value: current account repository backing value maintained by the owning type.
    private readonly AccountRepository _accountRepository;

    // Constructor: WorldAccountRepository
    // Purpose: Initializes a new WorldAccountRepository instance with dependencies and values required by the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - databaseService: Database service value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldAccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    public WorldAccountRepository(IDatabaseService databaseService)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException();
        _accountRepository = new AccountRepository(_databaseService);
    }

    // Method: IsIpBannedAsync
    // Purpose: Validates or evaluates is IP banned rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - ipAddress: Ip address value used when binding, connecting, or routing network traffic.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous Boolean result that is true when is IP banned async succeeds or the requested condition is met.
    // Notes: This keeps the operation scoped to WorldAccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public Task<bool> IsIpBannedAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        return _accountRepository.IsIpBannedAsync(ipAddress, cancellationToken);
    }

    // Method: GetAccountSessionAsync
    // Purpose: Retrieves get account session data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - username: Username value supplied by the caller for this operation.
    // - realmId: Realm ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to WorldAccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task<WorldAccountSessionRecord?> GetAccountSessionAsync(string username, uint realmId, CancellationToken cancellationToken = default)
    {
        username = NormalizeUsername(username);

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT `id`, `username`, `locked`, `sessionkey`
            FROM `account`
            WHERE `username` = @username
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@username", username);

        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        uint accountId = reader.GetUInt32(0);
        string accountUsername = reader.GetString(1);
        bool locked = reader.GetByte(2) != 0;
        string sessionKey = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
        await reader.DisposeAsync();

        RbacPermissionSet permissions = await RbacPermissionResolver.LoadForAccountAsync(connection, accountId, unchecked((int)realmId), cancellationToken);
        return new WorldAccountSessionRecord(
            accountId,
            accountUsername,
            permissions.SecurityLevel,
            permissions,
            locked,
            sessionKey);
    }

    // Method: GetAccountBanStatusAsync
    // Purpose: Retrieves get account ban status data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - accountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to WorldAccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public Task<AccountBanStatus> GetAccountBanStatusAsync(uint accountId, CancellationToken cancellationToken = default)
    {
        return _accountRepository.GetAccountBanStatusAsync(accountId, cancellationToken);
    }

    // Method: SetActiveRealmAsync
    // Purpose: Applies set active realm changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - accountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // - realmId: Realm ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldAccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task SetActiveRealmAsync(uint accountId, uint realmId, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            UPDATE `account`
            SET `active_realm_id` = @realmId
            WHERE `id` = @accountId;
            """;
        command.Parameters.AddWithValue("@realmId", realmId);
        command.Parameters.AddWithValue("@accountId", accountId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    // Method: NormalizeUsername
    // Purpose: Converts incoming data into normalize username form for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - username: Username value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to WorldAccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    public static string NormalizeUsername(string username)
    {
        return AccountRepository.NormalizeUsername(username);
    }
}
