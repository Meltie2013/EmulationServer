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
// File: src/RealmServer/Realms/ConfiguredRealm.cs
// Purpose: Contains configured realm code for the realm server authentication, realm-list, and account connection layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.RealmServer.Configuration;

namespace EmulationServer.RealmServer.Realms;

// Type: ConfiguredRealm
// Purpose: Provides configured realm behavior for the realm server authentication, realm-list, and account connection layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class ConfiguredRealm
{

    private readonly object _syncRoot = new();

    // Field: Stores the online state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current online backing value maintained by the owning type.
    private bool _online;

    // Field: Stores the active connections state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current active connections backing value maintained by the owning type.
    private int _activeConnections;

    // Field: Stores the capacity limit state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current capacity limit backing value maintained by the owning type.
    private int _capacityLimit;

    // Field: Stores the has received world server status state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current has received world server status backing value maintained by the owning type.
    private bool _hasReceivedWorldServerStatus;

    // Field: Stores the last status update utc state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current last status update utc backing value maintained by the owning type.
    private DateTimeOffset? _lastStatusUpdateUtc;

    // Field: Stores the hidden because stale state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current hidden because stale backing value maintained by the owning type.
    private bool _hiddenBecauseStale;
    // Field: Stores the uint state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current uint backing value maintained by the owning type.
    private Dictionary<uint, byte> _characterCountsByAccount = [];

    // Constructor: ConfiguredRealm
    // Purpose: Initializes a new ConfiguredRealm instance with dependencies and values required by the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - settings: Settings values that control how this operation should run.
    // Returns: none.
    // Notes: This keeps the operation scoped to ConfiguredRealm so callers do not duplicate validation, protocol, or persistence rules.
    public ConfiguredRealm(ConfiguredRealmSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        Id = settings.Id;
        Name = settings.Name;
        Address = settings.Address;
        Port = settings.Port;
        Icon = settings.Icon;
        BaseRealmFlags = settings.RealmFlags;
        Timezone = settings.Timezone;
        AllowedSecurityLevel = settings.AllowedSecurityLevel;
        Builds = settings.Builds;

        _online = settings.Online;
        _activeConnections = settings.ActiveConnections;
        _capacityLimit = 1;
    }

    // Property: Gets or sets the ID value used by the realm server authentication, realm-list, and account connection layer.
    // Value: ID value exposed by the owning type.
    public uint Id { get; }

    // Property: Gets or sets the name value used by the realm server authentication, realm-list, and account connection layer.
    // Value: name value exposed by the owning type.
    public string Name { get; }

    // Property: Gets or sets the address value used by the realm server authentication, realm-list, and account connection layer.
    // Value: address value exposed by the owning type.
    public string Address { get; }

    // Property: Gets or sets the port value used by the realm server authentication, realm-list, and account connection layer.
    // Value: port value exposed by the owning type.
    public ushort Port { get; }

    // Property: Gets or sets the icon value used by the realm server authentication, realm-list, and account connection layer.
    // Value: icon value exposed by the owning type.
    public byte Icon { get; }

    // Property: Gets or sets the base realm flags value used by the realm server authentication, realm-list, and account connection layer.
    // Value: base realm flags value exposed by the owning type.
    public RealmFlags BaseRealmFlags { get; }

    // Property: Gets or sets the timezone value used by the realm server authentication, realm-list, and account connection layer.
    // Value: timezone value exposed by the owning type.
    public byte Timezone { get; }

    // Property: Gets or sets the allowed security level value used by the realm server authentication, realm-list, and account connection layer.
    // Value: allowed security level value exposed by the owning type.
    public byte AllowedSecurityLevel { get; }

    // Property: Gets or sets the builds value used by the realm server authentication, realm-list, and account connection layer.
    // Value: builds value exposed by the owning type.
    public IReadOnlySet<ushort> Builds { get; }

    public bool IsOnline
    {
        get
        {
            lock (_syncRoot)
            {
                return _online;
            }
        }
    }

    public int ActiveConnections
    {
        get
        {
            lock (_syncRoot)
            {
                return _activeConnections;
            }
        }
    }

    public int CapacityLimit
    {
        get
        {
            lock (_syncRoot)
            {
                return _capacityLimit;
            }
        }
    }

    public float Population
    {
        get
        {
            lock (_syncRoot)
            {
                return RealmPopulationCalculator.Calculate(_activeConnections, _capacityLimit);
            }
        }
    }

    public bool HasReceivedWorldServerStatus
    {
        get
        {
            lock (_syncRoot)
            {
                return _hasReceivedWorldServerStatus;
            }
        }
    }

    public bool IsHiddenBecauseStale
    {
        get
        {
            lock (_syncRoot)
            {
                return _hiddenBecauseStale;
            }
        }
    }

    public DateTimeOffset? LastStatusUpdateUtc
    {
        get
        {
            lock (_syncRoot)
            {
                return _lastStatusUpdateUtc;
            }
        }
    }

    // Method: GetCharacterCount
    // Purpose: Retrieves get character count data for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - accountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the byte value produced by this operation.
    // Notes: This keeps the operation scoped to ConfiguredRealm so callers do not duplicate validation, protocol, or persistence rules.
    public byte GetCharacterCount(uint accountId)
    {
        lock (_syncRoot)
        {
            return _characterCountsByAccount.TryGetValue(accountId, out byte count)
                ? count
                : (byte)0;
        }
    }

    // Method: ReplaceCharacterCounts
    // Purpose: Executes the replace character counts operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - characterCountsByAccount: Character counts by account value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to ConfiguredRealm so callers do not duplicate validation, protocol, or persistence rules.
    public void ReplaceCharacterCounts(IReadOnlyDictionary<uint, byte> characterCountsByAccount)
    {
        ArgumentNullException.ThrowIfNull(characterCountsByAccount);

        lock (_syncRoot)
        {
            _characterCountsByAccount = characterCountsByAccount
                .ToDictionary(pair => pair.Key, pair => pair.Value);
        }
    }

    // Method: ClearCharacterCounts
    // Purpose: Applies clear character counts changes for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to ConfiguredRealm so callers do not duplicate validation, protocol, or persistence rules.
    public void ClearCharacterCounts()
    {
        lock (_syncRoot)
        {
            _characterCountsByAccount = [];
        }
    }

    // Property: Gets or sets the client address value used by the realm server authentication, realm-list, and account connection layer.
    // Value: client address value exposed by the owning type.
    public string ClientAddress => $"{Address}:{Port}";

    // Method: SetStatus
    // Purpose: Applies set status changes for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - online: Online value supplied by the caller for this operation.
    // - activeConnections: Active connections value supplied by the caller for this operation.
    // - capacityLimit: Capacity limit value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to ConfiguredRealm so callers do not duplicate validation, protocol, or persistence rules.
    public void SetStatus(bool online, int activeConnections, int capacityLimit)
    {
        SetStatus(online, activeConnections, capacityLimit, DateTimeOffset.UtcNow);
    }

    // Method: SetStatus
    // Purpose: Applies set status changes for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - online: Online value supplied by the caller for this operation.
    // - activeConnections: Active connections value supplied by the caller for this operation.
    // - capacityLimit: Capacity limit value supplied by the caller for this operation.
    // - updatedUtc: Updated utc value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to ConfiguredRealm so callers do not duplicate validation, protocol, or persistence rules.
    public void SetStatus(bool online, int activeConnections, int capacityLimit, DateTimeOffset updatedUtc)
    {
        lock (_syncRoot)
        {
            _online = online;
            _activeConnections = Math.Max(0, activeConnections);
            _capacityLimit = Math.Max(1, capacityLimit);
            _hasReceivedWorldServerStatus = true;
            _lastStatusUpdateUtc = updatedUtc;
            _hiddenBecauseStale = false;

            if (!online)
            {
                _characterCountsByAccount = [];
            }
        }
    }

    // Method: IsStatusStale
    // Purpose: Validates or evaluates is status stale rules for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - nowUtc: Now utc value supplied by the caller for this operation.
    // - staleTimeout: Stale timeout value supplied by the caller for this operation.
    // Returns: Returns true when is status stale succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to ConfiguredRealm so callers do not duplicate validation, protocol, or persistence rules.
    public bool IsStatusStale(DateTimeOffset nowUtc, TimeSpan staleTimeout)
    {
        lock (_syncRoot)
        {
            if (!_hasReceivedWorldServerStatus || _lastStatusUpdateUtc is null || _hiddenBecauseStale)
            {
                return false;
            }

            return nowUtc - _lastStatusUpdateUtc.Value >= staleTimeout;
        }
    }

    // Method: TryHideAsStale
    // Purpose: Executes the try hide as stale operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: Returns true when try hide as stale succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to ConfiguredRealm so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryHideAsStale()
    {
        lock (_syncRoot)
        {
            if (!_hasReceivedWorldServerStatus || _hiddenBecauseStale)
            {
                return false;
            }

            _online = false;
            _activeConnections = 0;
            _capacityLimit = Math.Max(1, _capacityLimit);
            _characterCountsByAccount = [];
            _hiddenBecauseStale = true;

            return true;
        }
    }
}
