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
  * File overview: src/EmulationServer.Game/Maps/Runtime/MapServiceDefinition.cs
  * Documents the MapServiceDefinition source file in the runtime map-player state tracking area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Maps.Runtime;

/**
  * Owns the map service definition behavior for the runtime map-player state tracking layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class MapServiceDefinition
{
    /**
      * Gets or stores the map id value used by MapServiceDefinition.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int MapId { get; init; }

    /**
      * Gets or stores the instance id value used by MapServiceDefinition.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public long InstanceId { get; init; }

    /**
      * Gets or stores the name value used by MapServiceDefinition.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string Name { get; init; } = string.Empty;

    /**
      * Gets or stores the kind value used by MapServiceDefinition.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public MapServiceKind Kind { get; init; }

    /**
      * Gets or stores the tick interval value used by MapServiceDefinition.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public TimeSpan TickInterval { get; init; } = TimeSpan.FromMilliseconds(100);

    /**
      * Gets or stores the log ticks value used by MapServiceDefinition.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public bool LogTicks { get; init; }

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of MapServiceDefinition and keeps this workflow isolated from the caller.
      */
    public void Validate()
    {
        if (MapId < 0)
        {
            throw new InvalidOperationException("Map service map id must be greater than or equal to zero.");
        }

        if (InstanceId < 0)
        {
            throw new InvalidOperationException("Map service instance id must be greater than or equal to zero.");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException($"Map service {MapId} requires a display name.");
        }

        if (TickInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"Map service '{Name}' tick interval must be greater than zero.");
        }
    }
}
