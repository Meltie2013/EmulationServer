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
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

/**
  * File overview: src/RealmServer/Realms/ConfiguredRealmStore.cs
  * Documents the ConfiguredRealmStore source file in the realm authentication, realm-list handling, and external client login services area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.RealmServer.Realms;

/**
  * Owns the configured realm store behavior for the realm authentication, realm-list handling, and external client login services layer.
  * The class keeps configured realm definitions loaded at startup while applying runtime WorldServer visibility rules when realm-list packets are built.
  */
public sealed class ConfiguredRealmStore
{
    private readonly Dictionary<uint, ConfiguredRealm> _realms;
    private readonly RealmListSettings _realmListSettings;

    /**
      * Initializes a new ConfiguredRealmStore instance with the dependencies required by the realm authentication, realm-list handling, and external client login services workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: realmSettings, realmListSettings.
      */
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

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of ConfiguredRealmStore and keeps this workflow isolated from the caller.
      */
    public IReadOnlyCollection<ConfiguredRealm> GetRealmsForBuild(ushort build)
    {
        return GetRealmsForBuild(build, DateTimeOffset.UtcNow);
    }

    /**
      * Returns visible realms for the client build after applying WorldServer registration and stale-status rules.
      */
    public IReadOnlyCollection<ConfiguredRealm> GetRealmsForBuild(ushort build, DateTimeOffset nowUtc)
    {
        HideStaleRealms(nowUtc);

        return _realms.Values
            .Where(realm => realm.Builds.Contains(build))
            .Where(ShouldShowRealm)
            .OrderBy(realm => realm.Id)
            .ToArray();
    }

    /**
      * Hides stale realms and returns the realm ids hidden during this call.
      */
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
        return TrySetRealmStatus(realmId, online, activeConnections, capacityLimit, DateTimeOffset.UtcNow);
    }

    /**
      * Attempts to set status while using an explicit timestamp for tests and controlled status handling.
      */
    public bool TrySetRealmStatus(uint realmId, bool online, int activeConnections, int capacityLimit, DateTimeOffset updatedUtc)
    {
        if (!_realms.TryGetValue(realmId, out ConfiguredRealm? realm))
        {
            return false;
        }

        realm.SetStatus(online, activeConnections, capacityLimit, updatedUtc);
        return true;
    }

    /**
      * Returns whether this realm should be advertised to clients in a realm-list packet.
      */
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
