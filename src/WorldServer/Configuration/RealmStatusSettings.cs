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
 * File overview: src/WorldServer/Configuration/RealmStatusSettings.cs
 * Documents the RealmStatusSettings source file in the world server configuration and startup settings area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.WorldServer.Configuration;

/**
 * Owns the realm status settings behavior for the world server configuration and startup settings layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public sealed class RealmStatusSettings
{
    /**
      * Gets or stores the enabled value used by RealmStatusSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public bool Enabled { get; init; } = true;

    /**
      * Gets or stores the realm id value used by RealmStatusSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public uint RealmId { get; init; } = 1;

    /**
      * Gets or stores the realm server host value used by RealmStatusSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string RealmServerHost { get; init; } = "127.0.0.1";

    /**
      * Gets or stores the realm server port value used by RealmStatusSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public ushort RealmServerPort { get; init; } = 5005;

    /**
      * Gets or stores the update interval value used by RealmStatusSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public TimeSpan UpdateInterval { get; init; } = TimeSpan.FromSeconds(15);


    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of RealmStatusSettings and keeps this workflow isolated from the caller.
      */
    public void Validate()
    {
        if (!Enabled)
        {
            return;
        }

        if (RealmId == 0)
        {
            throw new InvalidOperationException("Realm status realm id must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(RealmServerHost))
        {
            throw new InvalidOperationException("Realm status RealmServer host is required.");
        }

        if (RealmServerPort == 0)
        {
            throw new InvalidOperationException("Realm status RealmServer port must be greater than zero.");
        }

        if (UpdateInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Realm status update interval must be greater than zero.");
        }

    }
}
