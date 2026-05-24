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

using EmulationServer.Database.Configuration;
using EmulationServer.Network.Configuration;

using EmulationServer.Shared.Logging.Configuration;

/**
  * File overview: src/RealmServer/Configuration/RealmServerSettings.cs
  * Documents the RealmServerSettings source file in the realm authentication, realm-list handling, and external client login services area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.RealmServer.Configuration;

/**
  * Owns the realm server settings behavior for the realm authentication, realm-list handling, and external client login services layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class RealmServerSettings
{
    /**
      * Gets or stores the logging value used by RealmServerSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public LoggingSettings Logging { get; init; } = new();

    /**
      * Gets or stores the socket value used by RealmServerSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public RealmSocketListenerSettings Socket { get; init; } = new();

    /**
      * Gets or stores the database value used by RealmServerSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public DatabaseSettings Database { get; init; } = new();

    /**
      * Gets or stores the internal network value used by RealmServerSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public InternalNetworkSettings InternalNetwork { get; init; } = new();

    /**
      * Gets or stores the realms value used by RealmServerSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public IReadOnlyList<ConfiguredRealmSettings> Realms { get; init; } = [];

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of RealmServerSettings and keeps this workflow isolated from the caller.
      */
    public void Validate()
    {
        Logging.Validate();
        Socket.Validate();
        Database.Validate();
        InternalNetwork.Validate();

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
