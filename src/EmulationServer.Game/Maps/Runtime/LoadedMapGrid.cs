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

using EmulationServer.Game.Data.Maps;

/**
  * File overview: src/EmulationServer.Game/Maps/Runtime/LoadedMapGrid.cs
  * This file belongs to the map service runtime, grid ownership, service state transitions, and health reporting portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Game.Maps.Runtime;

/**
  * Represents the loaded map grid component in the map service runtime, grid ownership, service state transitions, and health reporting area.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public sealed class LoadedMapGrid
{
    /**
      * Creates a new LoadedMapGrid instance and stores the dependencies required by the component.
      * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
      */
    public LoadedMapGrid(MapTileDataStore tile)
    {
        Tile = tile ?? throw new ArgumentNullException(nameof(tile));
        LoadedUtc = DateTimeOffset.UtcNow;
        LastUsedUtc = LoadedUtc;
    }

    /**
      * Gets or stores the tile value used by LoadedMapGrid.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public MapTileDataStore Tile { get; }

    /**
      * Gets or stores the loaded utc value used by LoadedMapGrid.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public DateTimeOffset LoadedUtc { get; }

    /**
      * Gets or stores the last used utc value used by LoadedMapGrid.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public DateTimeOffset LastUsedUtc { get; private set; }

    /**
      * Performs the touch operation for LoadedMapGrid.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
    public void Touch()
    {
        LastUsedUtc = DateTimeOffset.UtcNow;
    }
}
