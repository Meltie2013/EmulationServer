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
  * Provides typed collision placement queries for one loaded mapstore tile.
  */
public sealed class MapTileCollisionQueryService
{
    private readonly MapTileCollisionData _collision;

    public MapTileCollisionQueryService(MapTileCollisionData collision)
    {
        _collision = collision ?? throw new ArgumentNullException();
    }

    public IReadOnlyList<MapTileCollisionPlacement> Placements => _collision.Placements;

    public IReadOnlyList<MapTileCollisionPlacement> FindPlacementsContaining(MapTileVector3 point)
    {
        if (_collision.Placements.Count == 0)
        {
            return [];
        }

        List<MapTileCollisionPlacement> matches = [];
        foreach (MapTileCollisionPlacement placement in _collision.Placements)
        {
            if (placement.Bounds.Contains(point))
            {
                matches.Add(placement);
            }
        }

        return matches;
    }
}
