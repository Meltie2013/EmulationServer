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

namespace EmulationServer.Database.Accounts;

public sealed class AccountRepository
{
    private readonly IDatabaseService _databaseService;

    public AccountRepository(IDatabaseService databaseService)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
    }

    public async Task<AccountLogonRecord?> GetForLogonAsync(string username, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT `id`, `username`, `sha_pass_hash`, `gmlevel`, `locked`, `last_ip`, `v`, `s`
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

        return new AccountLogonRecord(
            reader.GetUInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetByte(3),
            reader.GetByte(4) != 0,
            reader.GetString(5),
            reader.IsDBNull(6) ? null : reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetString(7));
    }

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

    public async Task<AccountBanStatus> GetAccountBanStatusAsync(uint accountId, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT `bandate`, `unbandate`
            FROM `account_banned`
            WHERE `id` = @id
              AND `active` = 1
              AND (`unbandate` > UNIX_TIMESTAMP() OR `unbandate` = `bandate`)
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

    public async Task<AccountCommandResult> CreateAccountAsync(
        string username,
        string password,
        string email = "",
        byte gmLevel = 0,
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
                    (`username`, `sha_pass_hash`, `gmlevel`, `sessionkey`, `v`, `s`, `email`, `joindate`, `last_ip`, `failed_logins`, `locked`, `last_login`, `active_realm_id`, `expansion`, `mutetime`, `locale`, `os`, `playerBot`)
                VALUES
                    (@username, @hash, @gmLevel, '', '0', '0', @email, NOW(), '0.0.0.0', 0, 0, NOW(), 0, 0, 0, 0, '', b'0');
                """;
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@hash", hash);
            command.Parameters.AddWithValue("@gmLevel", gmLevel);
            command.Parameters.AddWithValue("@email", email ?? string.Empty);

            await command.ExecuteNonQueryAsync(cancellationToken);
            return new AccountCommandResult(true, $"Account '{username}' was created.");
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            return new AccountCommandResult(false, $"Account '{username}' already exists.");
        }
    }

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

    public static string NormalizeUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return string.Empty;
        }

        return username.Trim().ToUpperInvariant();
    }
}
