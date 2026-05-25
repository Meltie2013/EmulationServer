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

using MySqlConnector;

namespace EmulationServer.Database.Accounts;

/**
  * Resolves TrinityCore-style RBAC rows into the final permission set used by auth and chat commands.
  * The resolver derives account security from RBAC role permissions in this project.
  */
public static class RbacPermissionResolver
{
    /**
      * The default player security level is applied to every account before account-specific grants and denies are evaluated.
      */
    private const int DefaultPlayerSecurityLevel = 0;

    /**
      * Loads default player permissions, account-specific permissions, linked inherited permissions, and explicit deny rows.
      */
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

    /**
      * Reads default permissions for the supplied security level.
      */
    private static async Task<HashSet<uint>> LoadDefaultPermissionsAsync(
        MySqlConnection connection,
        int securityLevel,
        CancellationToken cancellationToken)
    {
        using MySqlCommand command = connection.CreateCommand();
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

    /**
      * Reads direct account grants and denies for global rows plus the active realm row when one exists.
      */
    private static async Task LoadAccountPermissionsAsync(
        MySqlConnection connection,
        uint accountId,
        int realmId,
        HashSet<uint> directGranted,
        HashSet<uint> directDenied,
        CancellationToken cancellationToken)
    {
        using MySqlCommand command = connection.CreateCommand();
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

    /**
      * Reads the permission graph used to expand roles into child roles and command permissions.
      */
    private static async Task<Dictionary<uint, List<uint>>> LoadLinkedPermissionsAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        using MySqlCommand command = connection.CreateCommand();
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

    /**
      * Walks the RBAC permission graph and returns the original permissions plus every linked child permission.
      */
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
