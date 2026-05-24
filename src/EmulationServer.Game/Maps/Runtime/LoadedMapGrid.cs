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
  * Documents the LoadedMapGrid source file in the runtime map-player state tracking area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Maps.Runtime;

/**
  * Owns the loaded map grid behavior for the runtime map-player state tracking layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class LoadedMapGrid
{
    /**
      * Initializes a new LoadedMapGrid instance with the dependencies required by the runtime map-player state tracking workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: tile.
      */
    public LoadedMapGrid(MapTileDataStore tile)
    {
        Tile = tile ?? throw new ArgumentNullException();
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
      * Performs the touch operation for the runtime map-player state tracking workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      */
    public void Touch()
    {
        LastUsedUtc = DateTimeOffset.UtcNow;
    }
}
