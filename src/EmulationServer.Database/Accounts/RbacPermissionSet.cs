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
// File: src/EmulationServer.Database/Accounts/RbacPermissionSet.cs
// Purpose: Contains RBAC permission set code for the database persistence, repository, and MySQL connectivity layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Database.Accounts;

// Type: RbacPermissionSet
// Purpose: Provides RBAC permission set behavior for the database persistence, repository, and MySQL connectivity layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class RbacPermissionSet
{
    // Constructor: RbacPermissionSet
    // Purpose: Initializes a new RbacPermissionSet instance with dependencies and values required by the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - grantedPermissions: Granted permissions value supplied by the caller for this operation.
    // - deniedPermissions: Denied permissions value supplied by the caller for this operation.
    // - effectivePermissions: Effective permissions value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to RbacPermissionSet so callers do not duplicate validation, protocol, or persistence rules.
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

    public static RbacPermissionSet Empty { get; } = new(
        new HashSet<uint>(),
        new HashSet<uint>(),
        new HashSet<uint>());

    // Property: Gets or sets the granted permissions value used by the database persistence, repository, and MySQL connectivity layer.
    // Value: granted permissions value exposed by the owning type.
    public IReadOnlySet<uint> GrantedPermissions { get; }

    // Property: Gets or sets the denied permissions value used by the database persistence, repository, and MySQL connectivity layer.
    // Value: denied permissions value exposed by the owning type.
    public IReadOnlySet<uint> DeniedPermissions { get; }

    // Property: Gets or sets the effective permissions value used by the database persistence, repository, and MySQL connectivity layer.
    // Value: effective permissions value exposed by the owning type.
    public IReadOnlySet<uint> EffectivePermissions { get; }

    // Property: Gets or sets the security level value used by the database persistence, repository, and MySQL connectivity layer.
    // Value: security level value exposed by the owning type.
    public AccountSecurityLevel SecurityLevel { get; }

    // Method: HasPermission
    // Purpose: Validates or evaluates has permission rules for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - permissionId: Permission ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns true when has permission succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to RbacPermissionSet so callers do not duplicate validation, protocol, or persistence rules.
    public bool HasPermission(uint permissionId)
    {
        return EffectivePermissions.Contains(permissionId);
    }

    // Method: ResolveSecurityLevel
    // Purpose: Retrieves resolve security level data for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - effectivePermissions: Effective permissions value supplied by the caller for this operation.
    // Returns: Returns the account security level value produced by this operation.
    // Notes: This keeps the operation scoped to RbacPermissionSet so callers do not duplicate validation, protocol, or persistence rules.
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
