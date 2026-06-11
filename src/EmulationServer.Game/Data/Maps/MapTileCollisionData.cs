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
  * Stores parsed collision placement data for one mapstore tile.
  */
public sealed class MapTileCollisionData
{
    public MapTileCollisionData(MapTileKey key, ushort build, uint version, IReadOnlyList<MapTileCollisionPlacement> placements)
    {
        Key = key;
        Build = build;
        Version = version;
        Placements = placements ?? throw new ArgumentNullException();
    }


    /**
      * Creates a disabled collision payload used only when the runtime was recompiled without vmap/collision support.
      */
    public static MapTileCollisionData CreateDisabled(MapTileKey key)
    {
        return new MapTileCollisionData(key, 0, 0, []);
    }

    public MapTileKey Key { get; }
    public ushort Build { get; }
    public uint Version { get; }
    public IReadOnlyList<MapTileCollisionPlacement> Placements { get; }
    public bool HasPlacements => Placements.Count > 0;
}
