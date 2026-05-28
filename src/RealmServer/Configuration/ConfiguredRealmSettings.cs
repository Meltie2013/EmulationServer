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

/**
  * File overview: src/RealmServer/Configuration/ConfiguredRealmSettings.cs
  * Documents the ConfiguredRealmSettings source file in the realm authentication, realm-list handling, and external client login services area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

using EmulationServer.RealmServer.Realms;

namespace EmulationServer.RealmServer.Configuration;

/**
  * Owns the configured realm settings behavior for the realm authentication, realm-list handling, and external client login services layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class ConfiguredRealmSettings
{
    /**
      * Gets or stores the id value used by ConfiguredRealmSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public uint Id { get; init; }

    /**
      * Gets or stores the name value used by ConfiguredRealmSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string Name { get; init; } = "Emulation Server";

    /**
      * Gets or stores the address value used by ConfiguredRealmSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string Address { get; init; } = "127.0.0.1";

    /**
      * Gets or stores the port value used by ConfiguredRealmSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public ushort Port { get; init; } = 8085;

    /**
      * Gets or stores the icon value used by ConfiguredRealmSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public byte Icon { get; init; }

    /**
      * Gets or stores the realm flags value used by ConfiguredRealmSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public RealmFlags RealmFlags { get; init; }

    /**
      * Gets or stores the timezone value used by ConfiguredRealmSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public byte Timezone { get; init; } = 1;

    /**
      * Gets or stores the allowed security level value used by ConfiguredRealmSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public byte AllowedSecurityLevel { get; init; }

    /**
      * Gets or stores the online value used by ConfiguredRealmSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public bool Online { get; init; }

    /**
      * Gets or stores the active connections value used by ConfiguredRealmSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int ActiveConnections { get; init; }

    /**
      * Gets or stores the builds value used by ConfiguredRealmSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public IReadOnlySet<ushort> Builds { get; init; } = new HashSet<ushort>
    {
        5875,
        6005,
        6141,
        8606,
        12340,
        15595
    };

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of ConfiguredRealmSettings and keeps this workflow isolated from the caller.
      */
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
