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

namespace EmulationServer.Database.Accounts;

/**
  * Carries the resolved RBAC permissions for an authenticated account.
  * Granted and denied permissions are kept separately so diagnostics and later account tools can explain why a command is available or blocked.
  */
public sealed class RbacPermissionSet
{
    public RbacPermissionSet(
        IReadOnlySet<uint> grantedPermissions,
        IReadOnlySet<uint> deniedPermissions,
        IReadOnlySet<uint> effectivePermissions)
    {
        GrantedPermissions = grantedPermissions ?? throw new ArgumentNullException(nameof(grantedPermissions));
        DeniedPermissions = deniedPermissions ?? throw new ArgumentNullException(nameof(deniedPermissions));
        EffectivePermissions = effectivePermissions ?? throw new ArgumentNullException(nameof(effectivePermissions));
        SecurityLevel = ResolveSecurityLevel(EffectivePermissions);
    }

    /**
      * Empty permission set used before account authentication has completed.
      */
    public static RbacPermissionSet Empty { get; } = new(
        new HashSet<uint>(),
        new HashSet<uint>(),
        new HashSet<uint>());

    /**
      * All permissions granted before deny rows are applied.
      */
    public IReadOnlySet<uint> GrantedPermissions { get; }

    /**
      * All permissions explicitly denied to the account.
      */
    public IReadOnlySet<uint> DeniedPermissions { get; }

    /**
      * Final usable permissions after granted permissions are reduced by denied permissions.
      */
    public IReadOnlySet<uint> EffectivePermissions { get; }

    /**
      * Security level inferred from the final RBAC permission set.
      */
    public AccountSecurityLevel SecurityLevel { get; }

    /**
      * Returns true when the final permission set contains the requested permission id.
      */
    public bool HasPermission(uint permissionId)
    {
        return EffectivePermissions.Contains(permissionId);
    }

    /**
      * Resolves the visible security level from role permissions.
      */
    private static AccountSecurityLevel ResolveSecurityLevel(IReadOnlySet<uint> effectivePermissions)
    {
        if (effectivePermissions.Contains(RbacPermissionIds.AdministratorPermission))
        {
            return AccountSecurityLevel.Administrator;
        }

        if (effectivePermissions.Contains(RbacPermissionIds.GameMasterPermission))
        {
            return AccountSecurityLevel.GameMaster;
        }

        return AccountSecurityLevel.Player;
    }
}
