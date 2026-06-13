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

namespace MapStoreViewer.Scene;

/**
  * Stores the map-level tile coverage summary shown by the overview page.
  */
public sealed class MapOverviewScene
{
    public MapOverviewScene(uint mapId, ushort build, int previewResolution, IReadOnlyList<MapOverviewTileScene> tiles, IReadOnlyList<string> messages)
    {
        MapId = mapId;
        Build = build;
        PreviewResolution = previewResolution;
        Tiles = tiles;
        Messages = messages;
        MapKey = MapStoreFileNames.FormatMapId(mapId);
    }

    public uint MapId { get; }
    public ushort Build { get; }
    public string MapKey { get; }
    public int PreviewResolution { get; }
    public IReadOnlyList<MapOverviewTileScene> Tiles { get; }
    public IReadOnlyList<string> Messages { get; }
}

public sealed record MapOverviewTileScene(
    byte TileX,
    byte TileY,
    MapStoreTileDataFlags Flags,
    long TerrainBytes,
    long LiquidBytes,
    long CollisionBytes,
    long NavmeshBytes,
    MapOverviewTilePreviewScene? Preview);

public sealed record MapOverviewTilePreviewScene(
    float[]? TerrainHeights,
    int[]? LiquidMask,
    int[]? HoleMask,
    float MinimumHeight,
    float MaximumHeight,
    bool HasTerrain,
    bool HasLiquid,
    bool HasHoles);
