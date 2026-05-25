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

using EmulationServer.Database.Interfaces;

using MySqlConnector;

/**
  * File overview: src/EmulationServer.Database/Accounts/AccountRepository.cs
  * Documents the AccountRepository source file in the database access, account persistence, and MySQL connectivity area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Database.Accounts;

/**
  * Owns the account repository behavior for the database access, account persistence, and MySQL connectivity layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class AccountRepository
{
    /**
      * Holds the private database service state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly IDatabaseService _databaseService;

    /**
      * Initializes a new AccountRepository instance with the dependencies required by the database access, account persistence, and MySQL connectivity workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: databaseService.
      */
    public AccountRepository(IDatabaseService databaseService)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException();
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of AccountRepository and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async Task<AccountLogonRecord?> GetForLogonAsync(string username, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

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

    /**
      * Determines whether ip banned for the database access, account persistence, and MySQL connectivity workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: ipAddress, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async Task<bool> IsIpBannedAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

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

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of AccountRepository and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async Task<AccountBanStatus> GetAccountBanStatusAsync(uint accountId, CancellationToken cancellationToken = default)
    {
        await DeactivateExpiredAccountBansAsync(cancellationToken);

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

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

    /**
      * Updates update verifier state in memory or persistent storage.
      * The method keeps mutation rules centralized so player/account data changes remain auditable and safe to call from packet handlers.
      * Inputs used by this operation: username, verifier, salt, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async Task UpdateVerifierAsync(string username, string verifier, string salt, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

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

    /**
      * Updates update successful login state in memory or persistent storage.
      * The method keeps mutation rules centralized so player/account data changes remain auditable and safe to call from packet handlers.
      * Inputs used by this operation: username, sessionKey, lastIp, locale, os, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async Task UpdateSuccessfulLoginAsync(
        string username,
        string sessionKey,
        string lastIp,
        byte locale,
        string os,
        CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

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

    /**
      * Updates increment failed logins state in memory or persistent storage.
      * The method keeps mutation rules centralized so player/account data changes remain auditable and safe to call from packet handlers.
      * Inputs used by this operation: username, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async Task IncrementFailedLoginsAsync(string username, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            UPDATE `account`
            SET `failed_logins` = `failed_logins` + 1
            WHERE `username` = @username;
            """;
        command.Parameters.AddWithValue("@username", NormalizeUsername(username));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /**
      * Creates the account result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: username, password, email, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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
            using MySqlCommand command = connection.CreateCommand();

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

    /**
      * Removes an item from the managed collection and cleans up related state.
      * The method is part of AccountRepository and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async Task<AccountCommandResult> RemoveAccountAsync(string username, CancellationToken cancellationToken = default)
    {
        username = NormalizeUsername(username);

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand deleteBans = connection.CreateCommand();
        using MySqlCommand deleteAccount = connection.CreateCommand();

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

    /**
      * Grants a direct RBAC permission to an account for all realms unless a specific realm id is supplied.
      * The command updates the account-specific permission override table and leaves role template rows unchanged.
      */
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

        using MySqlCommand command = connection.CreateCommand();
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

    /**
      * Removes a direct RBAC permission override from an account.
      * Linked/default permissions are not modified; the account may still inherit the same permission through a role.
      */
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

        using MySqlCommand command = connection.CreateCommand();
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

    /**
      * Creates an account_banned row for a permanent or temporary account ban.
      * Existing active bans are deactivated first so one account has a single current ban while preserving the previous ban history.
      * Inputs used by this operation: username, durationSeconds, bannedBy, reason, cancellationToken.
      * A duration of zero seconds stores bandate and unbandate as the same value, which marks the ban as permanent.
      */
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

        using MySqlCommand deactivateExisting = connection.CreateCommand();
        deactivateExisting.Transaction = transaction;
        deactivateExisting.CommandText = """
            UPDATE `account_banned`
            SET `active` = 0
            WHERE `id` = @id
              AND `active` = 1;
            """;
        deactivateExisting.Parameters.AddWithValue("@id", accountId.Value);
        await deactivateExisting.ExecuteNonQueryAsync(cancellationToken);

        using MySqlCommand insertBan = connection.CreateCommand();
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

        using MySqlCommand clearRealmState = connection.CreateCommand();
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

    /**
      * Removes the active account ban by marking current rows inactive instead of deleting history.
      * The method keeps ban audit data available for later inspection.
      * Inputs used by this operation: username, cancellationToken.
      */
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

        using MySqlCommand command = connection.CreateCommand();
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

    /**
      * Returns the active account bans used by console ban list output.
      * Expired temporary bans are deactivated before the list is read so the displayed data matches login behavior.
      * Inputs used by this operation: usernameFilter, cancellationToken.
      */
    public async Task<IReadOnlyList<AccountBanRecord>> GetActiveAccountBansAsync(string usernameFilter = "", CancellationToken cancellationToken = default)
    {
        await DeactivateExpiredAccountBansAsync(cancellationToken);

        usernameFilter = NormalizeUsername(usernameFilter);
        List<AccountBanRecord> records = [];

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

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

    /**
      * Returns the full account ban history for one account.
      * The result distinguishes a missing account from an account that exists but has no prior bans.
      * Inputs used by this operation: username, cancellationToken.
      */
    public async Task<AccountBanHistoryResult> GetAccountBanHistoryAsync(string username, CancellationToken cancellationToken = default)
    {
        await DeactivateExpiredAccountBansAsync(cancellationToken);

        username = NormalizeUsername(username);
        List<AccountBanRecord> records = [];

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand accountCommand = connection.CreateCommand();
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

        using MySqlCommand historyCommand = connection.CreateCommand();
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

    /**
      * Deactivates expired temporary account bans while leaving permanent bans and history rows intact.
      * This mirrors the account_banned active flag while keeping the table auditable.
      */
    public async Task<int> DeactivateExpiredAccountBansAsync(CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            UPDATE `account_banned`
            SET `active` = 0
            WHERE `active` = 1
              AND `unbandate` <> `bandate`
              AND `unbandate` <= UNIX_TIMESTAMP();
            """;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /**
      * Returns whether an RBAC permission id exists before account override rows reference it.
      */
    private static async Task<bool> PermissionExistsAsync(
        MySqlConnection connection,
        MySqlTransaction? transaction,
        uint permissionId,
        CancellationToken cancellationToken)
    {
        using MySqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT 1 FROM `rbac_permissions` WHERE `id` = @permissionId LIMIT 1;";
        command.Parameters.AddWithValue("@permissionId", permissionId);

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    /**
      * Resolves an account id inside an existing connection and optional transaction.
      * Keeping this helper local to the repository avoids repeating account lookup SQL across command operations.
      */
    private static async Task<uint?> GetAccountIdAsync(
        MySqlConnection connection,
        MySqlTransaction? transaction,
        string username,
        CancellationToken cancellationToken)
    {
        using MySqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT `id` FROM `account` WHERE `username` = @username LIMIT 1;";
        command.Parameters.AddWithValue("@username", username);

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null ? null : Convert.ToUInt32(result);
    }

    /**
      * Reads an account ban row from a data reader using the repository's shared select shape.
      * Centralizing this mapping keeps list and history commands consistent.
      */
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

    /**
      * Normalizes ban metadata before inserting it into fixed-width schema fields.
      * Empty values are replaced with defaults and long values are truncated to the database column limit.
      */
    private static string NormalizeBanText(string value, int maximumLength, string defaultValue)
    {
        string normalized = string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
        return normalized.Length <= maximumLength ? normalized : normalized[..maximumLength];
    }

    /**
      * Formats a duration for command feedback without depending on external localization resources.
      * The output is intended for console logs and admin command responses.
      */
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

    /**
      * Normalizes the username for the database access, account persistence, and MySQL connectivity workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: username.
      */
    public static string NormalizeUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return string.Empty;
        }

        return username.Trim().ToUpperInvariant();
    }
}
