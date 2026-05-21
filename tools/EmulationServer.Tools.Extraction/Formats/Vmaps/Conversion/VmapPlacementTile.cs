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
  * File overview: tools/EmulationServer.Tools.Extraction/Formats/Vmaps/Conversion/VmapPlacementTile.cs
  * This file stores all WMO placements for one map tile.
  */

namespace EmulationServer.Tools.Extraction.Formats.Vmaps.Conversion;

/**
  * Represents the compact placement payload for one map tile.
  */
public sealed class VmapPlacementTile
{
    /**
      * Creates a new placement tile.
      */
    public VmapPlacementTile(uint mapId, int tileX, int tileY, IReadOnlyList<VmapPlacement> placements)
    {
        MapId = mapId;
        TileX = tileX;
        TileY = tileY;
        Placements = placements;
    }

    /**
      * Gets the Map.dbc identifier for this tile.
      */
    public uint MapId { get; }

    /**
      * Gets the ADT tile X coordinate.
      */
    public int TileX { get; }

    /**
      * Gets the ADT tile Y coordinate.
      */
    public int TileY { get; }

    /**
      * Gets the WMO placements found in the ADT tile.
      */
    public IReadOnlyList<VmapPlacement> Placements { get; }
}
