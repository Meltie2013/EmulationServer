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

namespace EmulationServer.WorldServer.Database.Accounts;

public sealed class WorldAccountRepository
{
    private readonly IDatabaseService _databaseService;

    public WorldAccountRepository(IDatabaseService databaseService)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
    }

    public async Task<WorldAccountSessionRecord?> GetAccountSessionAsync(string username, CancellationToken cancellationToken = default)
    {
        username = NormalizeUsername(username);

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT `id`, `username`, `gmlevel`, `locked`, `sessionkey`
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

        return new WorldAccountSessionRecord(
            reader.GetUInt32(0),
            reader.GetString(1),
            reader.GetByte(2),
            reader.GetByte(3) != 0,
            reader.IsDBNull(4) ? string.Empty : reader.GetString(4));
    }

    public async Task SetActiveRealmAsync(uint accountId, uint realmId, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            UPDATE `account`
            SET `active_realm_id` = @realmId
            WHERE `id` = @accountId;
            """;
        command.Parameters.AddWithValue("@realmId", realmId);
        command.Parameters.AddWithValue("@accountId", accountId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public static string NormalizeUsername(string username)
    {
        return string.IsNullOrWhiteSpace(username)
            ? string.Empty
            : username.Trim().ToUpperInvariant();
    }
}
