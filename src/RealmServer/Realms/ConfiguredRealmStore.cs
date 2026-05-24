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

using EmulationServer.RealmServer.Configuration;


/**
 * File overview: src/RealmServer/Realms/ConfiguredRealmStore.cs
 * Documents the ConfiguredRealmStore source file in the realm authentication, realm-list handling, and external client login services area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.RealmServer.Realms;

/**
 * Owns the configured realm store behavior for the realm authentication, realm-list handling, and external client login services layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public sealed class ConfiguredRealmStore
{
    private readonly Dictionary<uint, ConfiguredRealm> _realms;

    /**
     * Initializes a new ConfiguredRealmStore instance with the dependencies required by the realm authentication, realm-list handling, and external client login services workflow.
     * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
     * Inputs used by this operation: realmSettings.
     */
    public ConfiguredRealmStore(IEnumerable<ConfiguredRealmSettings> realmSettings)
    {
        ArgumentNullException.ThrowIfNull(realmSettings);

        _realms = realmSettings
            .Select(settings => new ConfiguredRealm(settings))
            .ToDictionary(realm => realm.Id);

        if (_realms.Count == 0)
        {
            throw new InvalidOperationException("At least one configured realm is required.");
        }
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of ConfiguredRealmStore and keeps this workflow isolated from the caller.
      */
    public IReadOnlyCollection<ConfiguredRealm> GetRealmsForBuild(ushort build)
    {
        return _realms.Values
            .Where(realm => realm.Builds.Contains(build))
            .OrderBy(realm => realm.Id)
            .ToArray();
    }

    /**
      * Updates the per-account character-count snapshot for a realm.
      */
    public bool TryReplaceRealmCharacterCounts(uint realmId, IReadOnlyDictionary<uint, byte> characterCountsByAccount)
    {
        if (!_realms.TryGetValue(realmId, out ConfiguredRealm? realm))
        {
            return false;
        }

        realm.ReplaceCharacterCounts(characterCountsByAccount);
        return true;
    }

    /**
      * Attempts the operation without treating a normal failure as an exceptional condition.
      * The method is part of ConfiguredRealmStore and keeps this workflow isolated from the caller.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    public bool TrySetRealmStatus(uint realmId, bool online, int activeConnections, int capacityLimit)
    {
        if (!_realms.TryGetValue(realmId, out ConfiguredRealm? realm))
        {
            return false;
        }

        realm.SetStatus(online, activeConnections, capacityLimit);
        return true;
    }
}
