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
  * This file belongs to the project runtime logic and supporting data models portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.RealmServer.Realms;

/**
  * Represents the configured realm component in the project runtime logic and supporting data models area.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public sealed class ConfiguredRealm
{
    /**
      * Stores the sync root dependency or runtime value for ConfiguredRealm.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly object _syncRoot = new();

    /**
      * Stores the online dependency or runtime value for ConfiguredRealm.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private bool _online;
    /**
      * Stores the active connections dependency or runtime value for ConfiguredRealm.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private int _activeConnections;
    /**
      * Stores the capacity limit dependency or runtime value for ConfiguredRealm.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private int _capacityLimit;

    /**
      * Creates a new ConfiguredRealm instance and stores the dependencies required by the component.
      * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
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
        }
    }
}
