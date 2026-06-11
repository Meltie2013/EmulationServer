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

namespace EmulationServer.Game.Data.Maps;

/**
  * Represents one axis-aligned bounding box from a runtime collision tile.
  */
public readonly record struct MapTileBounds(MapTileVector3 Minimum, MapTileVector3 Maximum)
{
    public static MapTileBounds Empty { get; } = new(MapTileVector3.Zero, MapTileVector3.Zero);

    /**
      * Returns true when the point is inside the bounds copied from the converted placement record.
      */
    public bool Contains(MapTileVector3 point)
    {
        return point.X >= Minimum.X && point.X <= Maximum.X &&
               point.Y >= Minimum.Y && point.Y <= Maximum.Y &&
               point.Z >= Minimum.Z && point.Z <= Maximum.Z;
    }
}
