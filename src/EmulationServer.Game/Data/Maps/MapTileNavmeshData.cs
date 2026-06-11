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
  * Stores parsed navmesh metadata for one mapstore tile. The real polygon payload is intentionally left for the later navmesh implementation.
  */
public sealed class MapTileNavmeshData
{
    public MapTileNavmeshData(MapTileKey key, ushort build, uint polygonCount, uint connectionCount)
    {
        Key = key;
        Build = build;
        PolygonCount = polygonCount;
        ConnectionCount = connectionCount;
    }


    /**
      * Creates a disabled navmesh payload used only when the runtime was recompiled without mmap/navmesh support.
      */
    public static MapTileNavmeshData CreateDisabled(MapTileKey key)
    {
        return new MapTileNavmeshData(key, 0, 0, 0);
    }

    public MapTileKey Key { get; }
    public ushort Build { get; }
    public uint PolygonCount { get; }
    public uint ConnectionCount { get; }
    public bool HasNavigationData => PolygonCount > 0;
}
