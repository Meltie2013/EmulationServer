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

using EmulationServer.Shared.Data.MapStore;

namespace EmulationServer.Game.Data.Maps;

/**
  * Stores parsed terrain runtime data for one mapstore tile.
  */
public sealed class MapTileTerrainData
{
    public MapTileTerrainData(
        MapTileKey key,
        ushort build,
        ushort areaFlags,
        ushort gridAreaFlag,
        ushort[] areaGrid,
        uint heightFlags,
        float gridHeight,
        float gridMaxHeight,
        float[]? v9Heights,
        float[]? v8Heights,
        ushort[] holes)
    {
        Key = key;
        Build = build;
        AreaFlags = areaFlags;
        GridAreaFlag = gridAreaFlag;
        AreaGrid = areaGrid ?? throw new ArgumentNullException();
        HeightFlags = heightFlags;
        GridHeight = gridHeight;
        GridMaxHeight = gridMaxHeight;
        V9Heights = v9Heights;
        V8Heights = v8Heights;
        Holes = holes ?? throw new ArgumentNullException();
    }


    /**
      * Creates a disabled terrain payload used only when the runtime was recompiled without terrain support.
      */
    public static MapTileTerrainData CreateDisabled(MapTileKey key)
    {
        return new MapTileTerrainData(
            key,
            0,
            MapStorePayloadConstants.MapAreaNoArea,
            0,
            new ushort[MapStorePayloadConstants.AreaCellCount],
            MapStorePayloadConstants.MapHeightNoHeight,
            0.0f,
            0.0f,
            null,
            null,
            new ushort[MapStorePayloadConstants.AreaCellCount]);
    }

    public MapTileKey Key { get; }
    public ushort Build { get; }
    public ushort AreaFlags { get; }
    public ushort GridAreaFlag { get; }
    public ushort[] AreaGrid { get; }
    public uint HeightFlags { get; }
    public float GridHeight { get; }
    public float GridMaxHeight { get; }
    public float[]? V9Heights { get; }
    public float[]? V8Heights { get; }
    public ushort[] Holes { get; }
    public bool HasAreaGrid => (AreaFlags & MapStorePayloadConstants.MapAreaNoArea) == 0;
    public bool HasHeightGrid => (HeightFlags & MapStorePayloadConstants.MapHeightNoHeight) == 0 && V9Heights is not null;
    public bool HasHoles => Holes.Any(value => value != 0);
}
