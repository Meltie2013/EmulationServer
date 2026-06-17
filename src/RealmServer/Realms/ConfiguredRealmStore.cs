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
// File: src/RealmServer/Realms/ConfiguredRealmStore.cs
// Purpose: Contains configured realm store code for the realm server authentication, realm-list, and account connection layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.RealmServer.Configuration;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.RealmServer.Realms;

// Type: ConfiguredRealmStore
// Purpose: Provides configured realm store behavior for the realm server authentication, realm-list, and account connection layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class ConfiguredRealmStore
{
    // Field: Stores the uint state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current uint backing value maintained by the owning type.
    private readonly Dictionary<uint, ConfiguredRealm> _realms;
    // Field: Stores the realm list settings state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current realm list settings backing value maintained by the owning type.
    private readonly RealmListSettings _realmListSettings;

    // Constructor: ConfiguredRealmStore
    // Purpose: Initializes a new ConfiguredRealmStore instance with dependencies and values required by the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - realmSettings: Realm settings value supplied by the caller for this operation.
    // - realmListSettings: Realm list settings value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to ConfiguredRealmStore so callers do not duplicate validation, protocol, or persistence rules.
    public ConfiguredRealmStore(IEnumerable<ConfiguredRealmSettings> realmSettings, RealmListSettings? realmListSettings = null)
    {
        ArgumentNullException.ThrowIfNull(realmSettings);

        _realmListSettings = realmListSettings ?? new RealmListSettings();
        _realmListSettings.Validate();

        _realms = realmSettings
            .Select(settings => new ConfiguredRealm(settings))
            .ToDictionary(realm => realm.Id);

        if (_realms.Count == 0)
        {
            throw new InvalidOperationException("At least one configured realm is required.");
        }
    }

    // Method: GetRealmsForBuild
    // Purpose: Retrieves get realms for build data for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - build: Build value supplied by the caller for this operation.
    // Returns: Returns the I read only collection value produced by this operation.
    // Notes: This keeps the operation scoped to ConfiguredRealmStore so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyCollection<ConfiguredRealm> GetRealmsForBuild(ushort build)
    {
        return GetRealmsForBuild(build, DateTimeOffset.UtcNow);
    }

    // Method: GetRealmsForBuild
    // Purpose: Retrieves get realms for build data for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - build: Build value supplied by the caller for this operation.
    // - nowUtc: Now utc value supplied by the caller for this operation.
    // Returns: Returns the I read only collection value produced by this operation.
    // Notes: This keeps the operation scoped to ConfiguredRealmStore so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyCollection<ConfiguredRealm> GetRealmsForBuild(ushort build, DateTimeOffset nowUtc)
    {
        HideStaleRealms(nowUtc);

        return _realms.Values
            .Where(realm => realm.Builds.Contains(build))
            .Where(ShouldShowRealm)
            .OrderBy(realm => realm.Id)
            .ToArray();
    }

    // Method: HideStaleRealms
    // Purpose: Executes the hide stale realms operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - nowUtc: Now utc value supplied by the caller for this operation.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to ConfiguredRealmStore so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyList<uint> HideStaleRealms(DateTimeOffset nowUtc)
    {
        if (!_realmListSettings.HideStaleRealms)
        {
            return [];
        }

        List<uint> hiddenRealmIds = [];
        foreach (ConfiguredRealm realm in _realms.Values)
        {
            if (!realm.IsStatusStale(nowUtc, _realmListSettings.StaleRealmTimeout))
            {
                continue;
            }

            if (realm.TryHideAsStale())
            {
                hiddenRealmIds.Add(realm.Id);
                Logger.Write(LogType.WARNING, $"Realm {realm.Id} was hidden from the realm list because WorldServer status has been stale for {_realmListSettings.StaleRealmTimeout}.", "ConfiguredRealmStore");
            }
        }

        return hiddenRealmIds;
    }

    // Method: TryReplaceRealmCharacterCounts
    // Purpose: Executes the try replace realm character counts operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - realmId: Realm ID identifier used to select the exact record, object, or runtime owner.
    // - characterCountsByAccount: Character counts by account value supplied by the caller for this operation.
    // Returns: Returns true when try replace realm character counts succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to ConfiguredRealmStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryReplaceRealmCharacterCounts(uint realmId, IReadOnlyDictionary<uint, byte> characterCountsByAccount)
    {
        if (!_realms.TryGetValue(realmId, out ConfiguredRealm? realm))
        {
            return false;
        }

        realm.ReplaceCharacterCounts(characterCountsByAccount);
        return true;
    }

    // Method: TrySetRealmStatus
    // Purpose: Executes the try set realm status operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - realmId: Realm ID identifier used to select the exact record, object, or runtime owner.
    // - online: Online value supplied by the caller for this operation.
    // - activeConnections: Active connections value supplied by the caller for this operation.
    // - capacityLimit: Capacity limit value supplied by the caller for this operation.
    // Returns: Returns true when try set realm status succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to ConfiguredRealmStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool TrySetRealmStatus(uint realmId, bool online, int activeConnections, int capacityLimit)
    {
        return TrySetRealmStatus(realmId, online, activeConnections, capacityLimit, DateTimeOffset.UtcNow);
    }

    // Method: TrySetRealmStatus
    // Purpose: Executes the try set realm status operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - realmId: Realm ID identifier used to select the exact record, object, or runtime owner.
    // - online: Online value supplied by the caller for this operation.
    // - activeConnections: Active connections value supplied by the caller for this operation.
    // - capacityLimit: Capacity limit value supplied by the caller for this operation.
    // - updatedUtc: Updated utc value supplied by the caller for this operation.
    // Returns: Returns true when try set realm status succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to ConfiguredRealmStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool TrySetRealmStatus(uint realmId, bool online, int activeConnections, int capacityLimit, DateTimeOffset updatedUtc)
    {
        if (!_realms.TryGetValue(realmId, out ConfiguredRealm? realm))
        {
            return false;
        }

        realm.SetStatus(online, activeConnections, capacityLimit, updatedUtc);
        return true;
    }

    // Method: ShouldShowRealm
    // Purpose: Validates or evaluates should show realm rules for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - realm: Realm value supplied by the caller for this operation.
    // Returns: Returns true when should show realm succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to ConfiguredRealmStore so callers do not duplicate validation, protocol, or persistence rules.
    private bool ShouldShowRealm(ConfiguredRealm realm)
    {
        if (realm.BaseRealmFlags.HasFlag(RealmFlags.Invalid))
        {
            return false;
        }

        if (_realmListSettings.RequireWorldServerStatus && !realm.HasReceivedWorldServerStatus)
        {
            return false;
        }

        return !realm.IsHiddenBecauseStale;
    }
}
