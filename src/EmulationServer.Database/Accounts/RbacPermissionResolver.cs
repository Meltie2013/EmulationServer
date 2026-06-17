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
// File: src/EmulationServer.Database/Accounts/RbacPermissionResolver.cs
// Purpose: Contains RBAC permission resolver code for the database persistence, repository, and MySQL connectivity layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using MySqlConnector;

namespace EmulationServer.Database.Accounts;

// Type: RbacPermissionResolver
// Purpose: Provides RBAC permission resolver behavior for the database persistence, repository, and MySQL connectivity layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class RbacPermissionResolver
{

    // Constant: Defines the default player security level constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed default player security level value used anywhere this rule or protocol value is needed.
    private const int DefaultPlayerSecurityLevel = 0;

    // Method: LoadForAccountAsync
    // Purpose: Retrieves load for account data for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - accountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // - realmId: Realm ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to RbacPermissionResolver so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public static async Task<RbacPermissionSet> LoadForAccountAsync(
        MySqlConnection connection,
        uint accountId,
        int realmId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        Dictionary<uint, List<uint>> linkedPermissions = await LoadLinkedPermissionsAsync(connection, cancellationToken);
        HashSet<uint> directGranted = await LoadDefaultPermissionsAsync(connection, DefaultPlayerSecurityLevel, cancellationToken);
        HashSet<uint> directDenied = [];

        await LoadAccountPermissionsAsync(connection, accountId, realmId, directGranted, directDenied, cancellationToken);

        HashSet<uint> granted = ExpandLinkedPermissions(directGranted, linkedPermissions);
        HashSet<uint> denied = ExpandLinkedPermissions(directDenied, linkedPermissions);
        HashSet<uint> effective = [.. granted.Where(permissionId => !denied.Contains(permissionId))];

        return new RbacPermissionSet(granted, denied, effective);
    }

    // Method: LoadDefaultPermissionsAsync
    // Purpose: Retrieves load default permissions data for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - securityLevel: Security level value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to RbacPermissionResolver so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task<HashSet<uint>> LoadDefaultPermissionsAsync(
        MySqlConnection connection,
        int securityLevel,
        CancellationToken cancellationToken)
    {
        await using MySqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT `permissionId`
            FROM `rbac_default_permissions`
            WHERE `secId` = @securityLevel;
            """;
        command.Parameters.AddWithValue("@securityLevel", securityLevel);

        HashSet<uint> permissionIds = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            permissionIds.Add(reader.GetUInt32(0));
        }

        return permissionIds;
    }

    // Method: LoadAccountPermissionsAsync
    // Purpose: Retrieves load account permissions data for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - accountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // - realmId: Realm ID identifier used to select the exact record, object, or runtime owner.
    // - directGranted: Direct granted value supplied by the caller for this operation.
    // - directDenied: Direct denied value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RbacPermissionResolver so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task LoadAccountPermissionsAsync(
        MySqlConnection connection,
        uint accountId,
        int realmId,
        HashSet<uint> directGranted,
        HashSet<uint> directDenied,
        CancellationToken cancellationToken)
    {
        await using MySqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT `permissionId`, `granted`
            FROM `rbac_account_permissions`
            WHERE `accountId` = @accountId
              AND (`realmId` = -1 OR `realmId` = @realmId);
            """;
        command.Parameters.AddWithValue("@accountId", accountId);
        command.Parameters.AddWithValue("@realmId", realmId);

        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            uint permissionId = reader.GetUInt32(0);
            bool granted = reader.GetBoolean(1);

            if (granted)
            {
                directGranted.Add(permissionId);
            }
            else
            {
                directDenied.Add(permissionId);
            }
        }
    }

    // Method: LoadLinkedPermissionsAsync
    // Purpose: Retrieves load linked permissions data for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to RbacPermissionResolver so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task<Dictionary<uint, List<uint>>> LoadLinkedPermissionsAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using MySqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT `id`, `linkedId`
            FROM `rbac_linked_permissions`;
            """;

        Dictionary<uint, List<uint>> linkedPermissions = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            uint permissionId = reader.GetUInt32(0);
            uint linkedId = reader.GetUInt32(1);

            if (!linkedPermissions.TryGetValue(permissionId, out List<uint>? links))
            {
                links = [];
                linkedPermissions[permissionId] = links;
            }

            links.Add(linkedId);
        }

        return linkedPermissions;
    }

    // Method: ExpandLinkedPermissions
    // Purpose: Executes the expand linked permissions operation for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - rootPermissions: Root permissions value supplied by the caller for this operation.
    // - linkedPermissions: Linked permissions value supplied by the caller for this operation.
    // Returns: Returns the hash set value produced by this operation.
    // Notes: This keeps the operation scoped to RbacPermissionResolver so callers do not duplicate validation, protocol, or persistence rules.
    private static HashSet<uint> ExpandLinkedPermissions(
        IEnumerable<uint> rootPermissions,
        IReadOnlyDictionary<uint, List<uint>> linkedPermissions)
    {
        HashSet<uint> resolved = [];
        Stack<uint> pending = new(rootPermissions);

        while (pending.Count > 0)
        {
            uint permissionId = pending.Pop();
            if (!resolved.Add(permissionId))
            {
                continue;
            }

            if (!linkedPermissions.TryGetValue(permissionId, out List<uint>? links))
            {
                continue;
            }

            foreach (uint linkedId in links)
            {
                pending.Push(linkedId);
            }
        }

        return resolved;
    }
}
