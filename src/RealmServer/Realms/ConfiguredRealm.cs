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
  * File overview: src/RealmServer/Realms/ConfiguredRealm.cs
  * Documents the ConfiguredRealm source file in the realm authentication, realm-list handling, and external client login services area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.RealmServer.Realms;

/**
  * Owns the configured realm behavior for the realm authentication, realm-list handling, and external client login services layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class ConfiguredRealm
{
    /**
      * Holds the private sync root state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly object _syncRoot = new();

    /**
      * Holds the private online state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private bool _online;
    /**
      * Holds the private active connections state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private int _activeConnections;
    /**
      * Holds the private capacity limit state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private int _capacityLimit;
    private Dictionary<uint, byte> _characterCountsByAccount = [];

    /**
      * Initializes a new ConfiguredRealm instance with the dependencies required by the realm authentication, realm-list handling, and external client login services workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: settings.
      */
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

    /**
      * Gets or stores the id value used by ConfiguredRealm.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public uint Id { get; }

    /**
      * Gets or stores the name value used by ConfiguredRealm.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string Name { get; }

    /**
      * Gets or stores the address value used by ConfiguredRealm.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string Address { get; }

    /**
      * Gets or stores the port value used by ConfiguredRealm.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public ushort Port { get; }

    /**
      * Gets or stores the icon value used by ConfiguredRealm.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public byte Icon { get; }

    /**
      * Gets or stores the base realm flags value used by ConfiguredRealm.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public byte BaseRealmFlags { get; }

    /**
      * Gets or stores the timezone value used by ConfiguredRealm.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public byte Timezone { get; }

    /**
      * Gets or stores the allowed security level value used by ConfiguredRealm.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public byte AllowedSecurityLevel { get; }

    /**
      * Gets or stores the builds value used by ConfiguredRealm.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public IReadOnlySet<ushort> Builds { get; }

    /**
      * Gets or stores the is online value used by ConfiguredRealm.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
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

    /**
      * Gets or stores the active connections value used by ConfiguredRealm.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
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

    /**
      * Gets or stores the capacity limit value used by ConfiguredRealm.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
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

    /**
      * Gets or stores the population value used by ConfiguredRealm.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
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

    /**
      * Returns the number of characters this account has on this realm from the latest WorldServer snapshot.
      */
    public byte GetCharacterCount(uint accountId)
    {
        lock (_syncRoot)
        {
            return _characterCountsByAccount.TryGetValue(accountId, out byte count)
                ? count
                : (byte)0;
        }
    }

    /**
      * Replaces the in-memory account character counts for this realm.
      */
    public void ReplaceCharacterCounts(IReadOnlyDictionary<uint, byte> characterCountsByAccount)
    {
        ArgumentNullException.ThrowIfNull(characterCountsByAccount);

        lock (_syncRoot)
        {
            _characterCountsByAccount = characterCountsByAccount
                .ToDictionary(pair => pair.Key, pair => pair.Value);
        }
    }

    /**
      * Clears cached character counts when the owning WorldServer is offline.
      */
    public void ClearCharacterCounts()
    {
        lock (_syncRoot)
        {
            _characterCountsByAccount = [];
        }
    }

    /**
      * Gets or stores the client address value used by ConfiguredRealm.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string ClientAddress => $"{Address}:{Port}";

    /**
      * Updates the stored value after validating that the new value is safe to use.
      * The method is part of ConfiguredRealm and keeps this workflow isolated from the caller.
      */
    public void SetStatus(bool online, int activeConnections, int capacityLimit)
    {
        lock (_syncRoot)
        {
            _online = online;
            _activeConnections = Math.Max(0, activeConnections);
            _capacityLimit = Math.Max(1, capacityLimit);

            if (!online)
            {
                _characterCountsByAccount = [];
            }
        }
    }
}
