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
  * Provides a typed navmesh query boundary while real Recast/Detour data remains deferred.
  */
public sealed class MapTileNavmeshQueryService
{
    private readonly MapTileNavmeshData _navmesh;

    public MapTileNavmeshQueryService(MapTileNavmeshData navmesh)
    {
        _navmesh = navmesh ?? throw new ArgumentNullException();
    }

    public bool HasNavigationData => _navmesh.HasNavigationData;

    public bool TryFindPath(MapTileVector3 start, MapTileVector3 end, out IReadOnlyList<MapTileVector3> path)
    {
        path = [];
        return false;
    }
}
