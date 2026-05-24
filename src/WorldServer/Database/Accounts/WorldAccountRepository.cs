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
  * File overview: src/WorldServer/Database/Accounts/WorldAccountRepository.cs
  * Documents the WorldAccountRepository source file in the world database repositories and persisted player/account records area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.WorldServer.Database.Accounts;

/**
  * Owns the world account repository behavior for the world database repositories and persisted player/account records layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class WorldAccountRepository
{
    /**
      * Holds the private database service state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly IDatabaseService _databaseService;

    /**
      * Initializes a new WorldAccountRepository instance with the dependencies required by the world database repositories and persisted player/account records workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: databaseService.
      */
    public WorldAccountRepository(IDatabaseService databaseService)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException();
    }

    /**
      * Resolves the account session value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: username, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Performs the set active realm operation for the world database repositories and persisted player/account records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: accountId, realmId, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Normalizes the username for the world database repositories and persisted player/account records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: username.
      */
    public static string NormalizeUsername(string username)
    {
        return string.IsNullOrWhiteSpace(username)
            ? string.Empty
            : username.Trim().ToUpperInvariant();
    }
}
