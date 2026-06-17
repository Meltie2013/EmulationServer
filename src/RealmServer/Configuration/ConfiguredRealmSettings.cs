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
// File: src/RealmServer/Configuration/ConfiguredRealmSettings.cs
// Purpose: Contains configured realm settings code for the realm server authentication, realm-list, and account connection layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.RealmServer.Realms;

namespace EmulationServer.RealmServer.Configuration;

// Type: ConfiguredRealmSettings
// Purpose: Provides configured realm settings behavior for the realm server authentication, realm-list, and account connection layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class ConfiguredRealmSettings
{

    // Property: Gets or sets the ID value used by the realm server authentication, realm-list, and account connection layer.
    // Value: ID value exposed by the owning type.
    public uint Id { get; init; }

    // Property: Gets or sets the name value used by the realm server authentication, realm-list, and account connection layer.
    // Value: name value exposed by the owning type.
    public string Name { get; init; } = "Emulation Server";

    // Property: Gets or sets the address value used by the realm server authentication, realm-list, and account connection layer.
    // Value: address value exposed by the owning type.
    public string Address { get; init; } = "127.0.0.1";

    // Property: Gets or sets the port value used by the realm server authentication, realm-list, and account connection layer.
    // Value: port value exposed by the owning type.
    public ushort Port { get; init; } = 8085;

    // Property: Gets or sets the icon value used by the realm server authentication, realm-list, and account connection layer.
    // Value: icon value exposed by the owning type.
    public byte Icon { get; init; }

    // Property: Gets or sets the realm flags value used by the realm server authentication, realm-list, and account connection layer.
    // Value: realm flags value exposed by the owning type.
    public RealmFlags RealmFlags { get; init; }

    // Property: Gets or sets the timezone value used by the realm server authentication, realm-list, and account connection layer.
    // Value: timezone value exposed by the owning type.
    public byte Timezone { get; init; } = 1;

    // Property: Gets or sets the allowed security level value used by the realm server authentication, realm-list, and account connection layer.
    // Value: allowed security level value exposed by the owning type.
    public byte AllowedSecurityLevel { get; init; }

    // Property: Gets or sets the online value used by the realm server authentication, realm-list, and account connection layer.
    // Value: online value exposed by the owning type.
    public bool Online { get; init; }

    // Property: Gets or sets the active connections value used by the realm server authentication, realm-list, and account connection layer.
    // Value: active connections value exposed by the owning type.
    public int ActiveConnections { get; init; }

    // Property: Gets or sets the builds value used by the realm server authentication, realm-list, and account connection layer.
    // Value: builds value exposed by the owning type.
    public IReadOnlySet<ushort> Builds { get; init; } = new HashSet<ushort>
    {
        5875,
        6005,
        6141,
        8606,
        12340,
        15595
    };

    // Method: Validate
    // Purpose: Validates or evaluates validate rules for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to ConfiguredRealmSettings so callers do not duplicate validation, protocol, or persistence rules.
    public void Validate()
    {
        if (Id == 0)
        {
            throw new InvalidOperationException("Realm id must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException($"Realm {Id} name is required.");
        }

        if (string.IsNullOrWhiteSpace(Address))
        {
            throw new InvalidOperationException($"Realm {Id} address is required.");
        }

        if (Port == 0)
        {
            throw new InvalidOperationException($"Realm {Id} port is required.");
        }

        if (ActiveConnections < 0)
        {
            throw new InvalidOperationException($"Realm {Id} active connections cannot be negative.");
        }

        try
        {
            RealmFlagUtilities.EnsureConfigurationFlagsAreSupported(RealmFlags);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException($"Realm {Id} has invalid realm flags. {ex.Message}", ex);
        }

        if (Builds.Count == 0)
        {
            throw new InvalidOperationException($"Realm {Id} must allow at least one client build.");
        }
    }
}
