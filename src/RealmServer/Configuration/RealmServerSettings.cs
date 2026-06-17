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
// File: src/RealmServer/Configuration/RealmServerSettings.cs
// Purpose: Contains realm server settings code for the realm server authentication, realm-list, and account connection layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Database.Configuration;
using EmulationServer.Network.Configuration;

using EmulationServer.Shared.Logging.Configuration;

namespace EmulationServer.RealmServer.Configuration;

// Type: RealmServerSettings
// Purpose: Provides realm server settings behavior for the realm server authentication, realm-list, and account connection layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class RealmServerSettings
{

    public LoggingSettings Logging { get; init; } = new();

    public RealmSocketListenerSettings Socket { get; init; } = new();

    public DatabaseSettings Database { get; init; } = new();

    public InternalNetworkSettings InternalNetwork { get; init; } = new();

    public RealmListSettings RealmList { get; init; } = new();

    // Property: Gets or sets the realms value used by the realm server authentication, realm-list, and account connection layer.
    // Value: realms value exposed by the owning type.
    public IReadOnlyList<ConfiguredRealmSettings> Realms { get; init; } = [];

    // Method: Validate
    // Purpose: Validates or evaluates validate rules for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmServerSettings so callers do not duplicate validation, protocol, or persistence rules.
    public void Validate()
    {
        Logging.Validate();
        Socket.Validate();
        Database.Validate();
        InternalNetwork.Validate();
        RealmList.Validate();

        if (Realms.Count == 0)
        {
            throw new InvalidOperationException("At least one realm must be configured.");
        }

        HashSet<uint> realmIds = [];
        foreach (ConfiguredRealmSettings realm in Realms)
        {
            realm.Validate();

            if (!realmIds.Add(realm.Id))
            {
                throw new InvalidOperationException($"Duplicate realm id configured: {realm.Id}.");
            }
        }
    }
}
