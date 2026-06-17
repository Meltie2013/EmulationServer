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
// File: src/EmulationServer.Database/Accounts/AccountRepository.cs
// Purpose: Contains account repository code for the database persistence, repository, and MySQL connectivity layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Database.Interfaces;

using MySqlConnector;

namespace EmulationServer.Database.Accounts;

// Type: AccountRepository
// Purpose: Provides account repository behavior for the database persistence, repository, and MySQL connectivity layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class AccountRepository(IDatabaseService databaseService)
{

    // Field: Stores the database service state used by the database persistence, repository, and MySQL connectivity layer.
    // Value: current database service backing value maintained by the owning type.
    private readonly IDatabaseService _databaseService = databaseService ?? throw new ArgumentNullException();

    // Method: GetForLogonAsync
    // Purpose: Retrieves get for logon data for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - username: Username value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to AccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task<AccountLogonRecord?> GetForLogonAsync(string username, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT `id`, `username`, `sha_pass_hash`, `locked`, `last_ip`, `v`, `s`, `sessionkey`
            FROM `account`
            WHERE `username` = @username
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@username", NormalizeUsername(username));

        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        uint accountId = reader.GetUInt32(0);
        string accountUsername = reader.GetString(1);
        string shaPassHash = reader.GetString(2);
        bool locked = reader.GetByte(3) != 0;
        string lastIp = reader.GetString(4);
        string? verifier = reader.IsDBNull(5) ? null : reader.GetString(5);
        string? salt = reader.IsDBNull(6) ? null : reader.GetString(6);
        string? sessionKey = reader.IsDBNull(7) ? null : reader.GetString(7);
        await reader.DisposeAsync();

        RbacPermissionSet permissions = await RbacPermissionResolver.LoadForAccountAsync(connection, accountId, -1, cancellationToken);
        return new AccountLogonRecord(
            accountId,
            accountUsername,
            shaPassHash,
            permissions.SecurityLevel,
            permissions,
            locked,
            lastIp,
            verifier,
            salt,
            sessionKey);
    }

    // Method: IsIpBannedAsync
    // Purpose: Validates or evaluates is IP banned rules for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - ipAddress: Ip address value used when binding, connecting, or routing network traffic.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous Boolean result that is true when is IP banned async succeeds or the requested condition is met.
    // Notes: This keeps the operation scoped to AccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task<bool> IsIpBannedAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT 1
            FROM `ip_banned`
            WHERE (`unbandate` = `bandate` OR `unbandate` > UNIX_TIMESTAMP())
              AND `ip` = @ip
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@ip", ipAddress);

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    // Method: GetAccountBanStatusAsync
    // Purpose: Retrieves get account ban status data for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - accountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to AccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task<AccountBanStatus> GetAccountBanStatusAsync(uint accountId, CancellationToken cancellationToken = default)
    {
        await DeactivateExpiredAccountBansAsync(cancellationToken);

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT `bandate`, `unbandate`
            FROM `account_banned`
            WHERE `id` = @id
              AND `active` = 1
              AND (`unbandate` > UNIX_TIMESTAMP() OR `unbandate` = `bandate`)
            ORDER BY `bandate` DESC
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@id", accountId);

        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return AccountBanStatus.NotBanned;
        }

        ulong banDate = reader.GetUInt64(0);
        ulong unbanDate = reader.GetUInt64(1);

        return new AccountBanStatus(true, banDate == unbanDate);
    }

    // Method: UpdateVerifierAsync
    // Purpose: Applies update verifier changes for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - username: Username value supplied by the caller for this operation.
    // - verifier: Verifier value supplied by the caller for this operation.
    // - salt: Salt value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to AccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task UpdateVerifierAsync(string username, string verifier, string salt, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            UPDATE `account`
            SET `v` = @verifier,
                `s` = @salt
            WHERE `username` = @username;
            """;
        command.Parameters.AddWithValue("@verifier", verifier);
        command.Parameters.AddWithValue("@salt", salt);
        command.Parameters.AddWithValue("@username", NormalizeUsername(username));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    // Method: UpdateSuccessfulLoginAsync
    // Purpose: Applies update successful login changes for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - username: Username value supplied by the caller for this operation.
    // - sessionKey: Session key value supplied by the caller for this operation.
    // - lastIp: Last IP value supplied by the caller for this operation.
    // - locale: Locale value supplied by the caller for this operation.
    // - os: Os value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to AccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task UpdateSuccessfulLoginAsync(
        string username,
        string sessionKey,
        string lastIp,
        byte locale,
        string os,
        CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            UPDATE `account`
            SET `sessionkey` = @sessionKey,
                `last_ip` = @lastIp,
                `last_login` = NOW(),
                `locale` = @locale,
                `os` = @os,
                `failed_logins` = 0
            WHERE `username` = @username;
            """;
        command.Parameters.AddWithValue("@sessionKey", sessionKey);
        command.Parameters.AddWithValue("@lastIp", lastIp);
        command.Parameters.AddWithValue("@locale", locale);
        command.Parameters.AddWithValue("@os", os.Length > 3 ? os[..3] : os);
        command.Parameters.AddWithValue("@username", NormalizeUsername(username));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    // Method: IncrementFailedLoginsAsync
    // Purpose: Executes the increment failed logins operation for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - username: Username value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to AccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task IncrementFailedLoginsAsync(string username, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            UPDATE `account`
            SET `failed_logins` = `failed_logins` + 1
            WHERE `username` = @username;
            """;
        command.Parameters.AddWithValue("@username", NormalizeUsername(username));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    // Method: CreateAccountAsync
    // Purpose: Applies create account changes for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - username: Username value supplied by the caller for this operation.
    // - password: Password value supplied by the caller for this operation.
    // - email: Email value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to AccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task<AccountCommandResult> CreateAccountAsync(
        string username,
        string password,
        string email = "",
        CancellationToken cancellationToken = default)
    {
        username = NormalizeUsername(username);

        if (username.Length is < 3 or > 32)
        {
            return new AccountCommandResult(false, "Username must be between 3 and 32 characters.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return new AccountCommandResult(false, "Password is required.");
        }

        string hash = AccountPasswordHasher.ComputeShaPassHash(username, password);

        try
        {
            await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
            await using MySqlCommand command = connection.CreateCommand();

            command.CommandText = """
                INSERT INTO `account`
                    (`username`, `sha_pass_hash`, `sessionkey`, `v`, `s`, `email`, `joindate`, `last_ip`, `failed_logins`, `locked`, `last_login`, `active_realm_id`, `expansion`, `mutetime`, `locale`, `os`, `playerBot`)
                VALUES
                    (@username, @hash, '', '0', '0', @email, NOW(), '0.0.0.0', 0, 0, NOW(), 0, 0, 0, 0, '', b'0');
                """;
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@hash", hash);
            command.Parameters.AddWithValue("@email", email ?? string.Empty);

            await command.ExecuteNonQueryAsync(cancellationToken);
            return new AccountCommandResult(true, $"Account '{username}' was created.");
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            return new AccountCommandResult(false, $"Account '{username}' already exists.");
        }
    }

    // Method: RemoveAccountAsync
    // Purpose: Applies remove account changes for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - username: Username value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to AccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task<AccountCommandResult> RemoveAccountAsync(string username, CancellationToken cancellationToken = default)
    {
        username = NormalizeUsername(username);

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlCommand deleteBans = connection.CreateCommand();
        await using MySqlCommand deleteAccount = connection.CreateCommand();

        deleteBans.CommandText = """
            DELETE `account_banned`
            FROM `account_banned`
            INNER JOIN `account` ON `account`.`id` = `account_banned`.`id`
            WHERE `account`.`username` = @username;
            """;
        deleteBans.Parameters.AddWithValue("@username", username);
        await deleteBans.ExecuteNonQueryAsync(cancellationToken);

        deleteAccount.CommandText = "DELETE FROM `account` WHERE `username` = @username;";
        deleteAccount.Parameters.AddWithValue("@username", username);

        int deleted = await deleteAccount.ExecuteNonQueryAsync(cancellationToken);
        if (deleted == 0)
        {
            return new AccountCommandResult(false, $"Account '{username}' was not found.");
        }

        return new AccountCommandResult(true, $"Account '{username}' was removed.");
    }

    // Method: SetAccountPermissionAsync
    // Purpose: Applies set account permission changes for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - username: Username value supplied by the caller for this operation.
    // - permissionId: Permission ID identifier used to select the exact record, object, or runtime owner.
    // - realmId: Realm ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to AccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task<AccountCommandResult> SetAccountPermissionAsync(
        string username,
        uint permissionId,
        int realmId = -1,
        CancellationToken cancellationToken = default)
    {
        username = NormalizeUsername(username);

        if (string.IsNullOrWhiteSpace(username))
        {
            return new AccountCommandResult(false, "Username is required.");
        }

        if (permissionId == 0)
        {
            return new AccountCommandResult(false, "Permission id must be greater than zero.");
        }

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        uint? accountId = await GetAccountIdAsync(connection, transaction, username, cancellationToken);
        if (accountId is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new AccountCommandResult(false, $"Account '{username}' was not found.");
        }

        if (!await PermissionExistsAsync(connection, transaction, permissionId, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return new AccountCommandResult(false, $"RBAC permission {permissionId} does not exist.");
        }

        await using MySqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO `rbac_account_permissions`
                (`accountId`, `permissionId`, `granted`, `realmId`)
            VALUES
                (@accountId, @permissionId, 1, @realmId)
            ON DUPLICATE KEY UPDATE
                `granted` = VALUES(`granted`);
            """;
        command.Parameters.AddWithValue("@accountId", accountId.Value);
        command.Parameters.AddWithValue("@permissionId", permissionId);
        command.Parameters.AddWithValue("@realmId", realmId);

        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        string scope = realmId < 0 ? "globally" : $"for realm {realmId}";
        return new AccountCommandResult(true, $"Permission {permissionId} was granted to account '{username}' {scope}.");
    }

    // Method: RemoveAccountPermissionAsync
    // Purpose: Applies remove account permission changes for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - username: Username value supplied by the caller for this operation.
    // - permissionId: Permission ID identifier used to select the exact record, object, or runtime owner.
    // - realmId: Realm ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to AccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task<AccountCommandResult> RemoveAccountPermissionAsync(
        string username,
        uint permissionId,
        int realmId = -1,
        CancellationToken cancellationToken = default)
    {
        username = NormalizeUsername(username);

        if (string.IsNullOrWhiteSpace(username))
        {
            return new AccountCommandResult(false, "Username is required.");
        }

        if (permissionId == 0)
        {
            return new AccountCommandResult(false, "Permission id must be greater than zero.");
        }

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        uint? accountId = await GetAccountIdAsync(connection, transaction, username, cancellationToken);
        if (accountId is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new AccountCommandResult(false, $"Account '{username}' was not found.");
        }

        await using MySqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DELETE FROM `rbac_account_permissions`
            WHERE `accountId` = @accountId
              AND `permissionId` = @permissionId
              AND `realmId` = @realmId;
            """;
        command.Parameters.AddWithValue("@accountId", accountId.Value);
        command.Parameters.AddWithValue("@permissionId", permissionId);
        command.Parameters.AddWithValue("@realmId", realmId);

        int removed = await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        if (removed == 0)
        {
            return new AccountCommandResult(false, $"Permission {permissionId} was not directly assigned to account '{username}'.");
        }

        string scope = realmId < 0 ? "globally" : $"for realm {realmId}";
        return new AccountCommandResult(true, $"Permission {permissionId} was removed from account '{username}' {scope}.");
    }

    // Method: BanAccountAsync
    // Purpose: Executes the ban account operation for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - username: Username value supplied by the caller for this operation.
    // - durationSeconds: Duration seconds value supplied by the caller for this operation.
    // - bannedBy: Banned by value supplied by the caller for this operation.
    // - reason: Reason value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to AccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task<AccountCommandResult> BanAccountAsync(
        string username,
        ulong durationSeconds,
        string bannedBy,
        string reason,
        CancellationToken cancellationToken = default)
    {
        username = NormalizeUsername(username);
        bannedBy = NormalizeBanText(bannedBy, 50, "RealmConsole");
        reason = NormalizeBanText(reason, 255, "No reason provided.");

        if (string.IsNullOrWhiteSpace(username))
        {
            return new AccountCommandResult(false, "Username is required.");
        }

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        uint? accountId = await GetAccountIdAsync(connection, transaction, username, cancellationToken);
        if (accountId is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new AccountCommandResult(false, $"Account '{username}' was not found.");
        }

        await using MySqlCommand deactivateExisting = connection.CreateCommand();
        deactivateExisting.Transaction = transaction;
        deactivateExisting.CommandText = """
            UPDATE `account_banned`
            SET `active` = 0
            WHERE `id` = @id
              AND `active` = 1;
            """;
        deactivateExisting.Parameters.AddWithValue("@id", accountId.Value);
        await deactivateExisting.ExecuteNonQueryAsync(cancellationToken);

        await using MySqlCommand insertBan = connection.CreateCommand();
        insertBan.Transaction = transaction;
        insertBan.CommandText = """
            INSERT INTO `account_banned`
                (`id`, `bandate`, `unbandate`, `bannedby`, `banreason`, `active`)
            SELECT
                @id,
                UNIX_TIMESTAMP(),
                CASE WHEN @durationSeconds = 0 THEN UNIX_TIMESTAMP() ELSE UNIX_TIMESTAMP() + @durationSeconds END,
                @bannedBy,
                @reason,
                1
            ON DUPLICATE KEY UPDATE
                `unbandate` = VALUES(`unbandate`),
                `bannedby` = VALUES(`bannedby`),
                `banreason` = VALUES(`banreason`),
                `active` = 1;
            """;
        insertBan.Parameters.AddWithValue("@id", accountId.Value);
        insertBan.Parameters.AddWithValue("@durationSeconds", durationSeconds);
        insertBan.Parameters.AddWithValue("@bannedBy", bannedBy);
        insertBan.Parameters.AddWithValue("@reason", reason);
        await insertBan.ExecuteNonQueryAsync(cancellationToken);

        await using MySqlCommand clearRealmState = connection.CreateCommand();
        clearRealmState.Transaction = transaction;
        clearRealmState.CommandText = """
            UPDATE `account`
            SET `sessionkey` = '',
                `active_realm_id` = 0
            WHERE `id` = @id;
            """;
        clearRealmState.Parameters.AddWithValue("@id", accountId.Value);
        await clearRealmState.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        string durationMessage = durationSeconds == 0 ? "permanently" : $"for {FormatDuration(durationSeconds)}";
        return new AccountCommandResult(true, $"Account '{username}' was banned {durationMessage}. Reason: {reason}");
    }

    // Method: UnbanAccountAsync
    // Purpose: Executes the unban account operation for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - username: Username value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to AccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task<AccountCommandResult> UnbanAccountAsync(string username, CancellationToken cancellationToken = default)
    {
        username = NormalizeUsername(username);

        if (string.IsNullOrWhiteSpace(username))
        {
            return new AccountCommandResult(false, "Username is required.");
        }

        await DeactivateExpiredAccountBansAsync(cancellationToken);

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        uint? accountId = await GetAccountIdAsync(connection, transaction, username, cancellationToken);
        if (accountId is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new AccountCommandResult(false, $"Account '{username}' was not found.");
        }

        await using MySqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE `account_banned`
            SET `active` = 0
            WHERE `id` = @id
              AND `active` = 1
              AND (`unbandate` = `bandate` OR `unbandate` > UNIX_TIMESTAMP());
            """;
        command.Parameters.AddWithValue("@id", accountId.Value);

        int changed = await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        if (changed == 0)
        {
            return new AccountCommandResult(false, $"Account '{username}' does not have an active ban.");
        }

        return new AccountCommandResult(true, $"Account '{username}' was unbanned.");
    }

    // Method: GetActiveAccountBansAsync
    // Purpose: Retrieves get active account bans data for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - usernameFilter: Username filter value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to AccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task<IReadOnlyList<AccountBanRecord>> GetActiveAccountBansAsync(string usernameFilter = "", CancellationToken cancellationToken = default)
    {
        await DeactivateExpiredAccountBansAsync(cancellationToken);

        usernameFilter = NormalizeUsername(usernameFilter);
        List<AccountBanRecord> records = [];

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT `account`.`id`, `account`.`username`, `account_banned`.`bandate`, `account_banned`.`unbandate`,
                   `account_banned`.`bannedby`, `account_banned`.`banreason`, `account_banned`.`active`
            FROM `account_banned`
            INNER JOIN `account` ON `account`.`id` = `account_banned`.`id`
            WHERE `account_banned`.`active` = 1
              AND (`account_banned`.`unbandate` = `account_banned`.`bandate` OR `account_banned`.`unbandate` > UNIX_TIMESTAMP())
              AND (@usernameFilter = '' OR `account`.`username` LIKE CONCAT('%', @usernameFilter, '%'))
            ORDER BY `account_banned`.`bandate` DESC;
            """;
        command.Parameters.AddWithValue("@usernameFilter", usernameFilter);

        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(ReadBanRecord(reader));
        }

        return records;
    }

    // Method: GetAccountBanHistoryAsync
    // Purpose: Retrieves get account ban history data for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - username: Username value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to AccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task<AccountBanHistoryResult> GetAccountBanHistoryAsync(string username, CancellationToken cancellationToken = default)
    {
        await DeactivateExpiredAccountBansAsync(cancellationToken);

        username = NormalizeUsername(username);
        List<AccountBanRecord> records = [];

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlCommand accountCommand = connection.CreateCommand();
        accountCommand.CommandText = """
            SELECT `id`, `username`
            FROM `account`
            WHERE `username` = @username
            LIMIT 1;
            """;
        accountCommand.Parameters.AddWithValue("@username", username);

        uint accountId;
        string accountName;
        await using (MySqlDataReader accountReader = await accountCommand.ExecuteReaderAsync(cancellationToken))
        {
            if (!await accountReader.ReadAsync(cancellationToken))
            {
                return new AccountBanHistoryResult(false, username, records);
            }

            accountId = accountReader.GetUInt32(0);
            accountName = accountReader.GetString(1);
        }

        await using MySqlCommand historyCommand = connection.CreateCommand();
        historyCommand.CommandText = """
            SELECT `account`.`id`, `account`.`username`, `account_banned`.`bandate`, `account_banned`.`unbandate`,
                   `account_banned`.`bannedby`, `account_banned`.`banreason`, `account_banned`.`active`
            FROM `account_banned`
            INNER JOIN `account` ON `account`.`id` = `account_banned`.`id`
            WHERE `account_banned`.`id` = @id
            ORDER BY `account_banned`.`bandate` DESC;
            """;
        historyCommand.Parameters.AddWithValue("@id", accountId);

        await using MySqlDataReader historyReader = await historyCommand.ExecuteReaderAsync(cancellationToken);
        while (await historyReader.ReadAsync(cancellationToken))
        {
            records.Add(ReadBanRecord(historyReader));
        }

        return new AccountBanHistoryResult(true, accountName, records);
    }

    // Method: DeactivateExpiredAccountBansAsync
    // Purpose: Executes the deactivate expired account bans operation for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to AccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task<int> DeactivateExpiredAccountBansAsync(CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        await using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            UPDATE `account_banned`
            SET `active` = 0
            WHERE `active` = 1
              AND `unbandate` <> `bandate`
              AND `unbandate` <= UNIX_TIMESTAMP();
            """;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    // Method: PermissionExistsAsync
    // Purpose: Executes the permission exists operation for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - transaction: Database transaction used to execute this operation without opening unnecessary additional state.
    // - permissionId: Permission ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous Boolean result that is true when permission exists async succeeds or the requested condition is met.
    // Notes: This keeps the operation scoped to AccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task<bool> PermissionExistsAsync(
        MySqlConnection connection,
        MySqlTransaction? transaction,
        uint permissionId,
        CancellationToken cancellationToken)
    {
        await using MySqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT 1 FROM `rbac_permissions` WHERE `id` = @permissionId LIMIT 1;";
        command.Parameters.AddWithValue("@permissionId", permissionId);

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    // Method: GetAccountIdAsync
    // Purpose: Retrieves get account ID data for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - transaction: Database transaction used to execute this operation without opening unnecessary additional state.
    // - username: Username value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to AccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task<uint?> GetAccountIdAsync(
        MySqlConnection connection,
        MySqlTransaction? transaction,
        string username,
        CancellationToken cancellationToken)
    {
        await using MySqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT `id` FROM `account` WHERE `username` = @username LIMIT 1;";
        command.Parameters.AddWithValue("@username", username);

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null ? null : Convert.ToUInt32(result);
    }

    // Method: ReadBanRecord
    // Purpose: Retrieves read ban record data for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - reader: Database reader used to execute this operation without opening unnecessary additional state.
    // Returns: Returns the account ban record value produced by this operation.
    // Notes: This keeps the operation scoped to AccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    private static AccountBanRecord ReadBanRecord(MySqlDataReader reader)
    {
        return new AccountBanRecord(
            reader.GetUInt32(0),
            reader.GetString(1),
            reader.GetUInt64(2),
            reader.GetUInt64(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetByte(6) != 0);
    }

    // Method: NormalizeBanText
    // Purpose: Converts incoming data into normalize ban text form for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // - maximumLength: Maximum length value supplied by the caller for this operation.
    // - defaultValue: Default value value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to AccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    private static string NormalizeBanText(string value, int maximumLength, string defaultValue)
    {
        string normalized = string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
        return normalized.Length <= maximumLength ? normalized : normalized[..maximumLength];
    }

    // Method: FormatDuration
    // Purpose: Executes the format duration operation for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - durationSeconds: Duration seconds value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to AccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    private static string FormatDuration(ulong durationSeconds)
    {
        TimeSpan duration = TimeSpan.FromSeconds(durationSeconds > int.MaxValue ? int.MaxValue : (int)durationSeconds);
        if (durationSeconds > int.MaxValue)
        {
            return $"{durationSeconds} seconds";
        }

        List<string> parts = [];
        if (duration.Days > 0)
        {
            parts.Add($"{duration.Days} day{(duration.Days == 1 ? string.Empty : "s")}");
        }

        if (duration.Hours > 0)
        {
            parts.Add($"{duration.Hours} hour{(duration.Hours == 1 ? string.Empty : "s")}");
        }

        if (duration.Minutes > 0)
        {
            parts.Add($"{duration.Minutes} minute{(duration.Minutes == 1 ? string.Empty : "s")}");
        }

        if (duration.Seconds > 0 || parts.Count == 0)
        {
            parts.Add($"{duration.Seconds} second{(duration.Seconds == 1 ? string.Empty : "s")}");
        }

        return string.Join(' ', parts);
    }

    // Method: NormalizeUsername
    // Purpose: Converts incoming data into normalize username form for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - username: Username value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to AccountRepository so callers do not duplicate validation, protocol, or persistence rules.
    public static string NormalizeUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return string.Empty;
        }

        return username.Trim().ToUpperInvariant();
    }
}
