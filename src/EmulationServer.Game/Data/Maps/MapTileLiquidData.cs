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
  * Stores parsed liquid runtime data for one mapstore tile.
  */
public sealed class MapTileLiquidData
{
    public MapTileLiquidData(
        MapTileKey key,
        ushort build,
        bool hasLiquid,
        ushort flags,
        ushort liquidTypeId,
        byte offsetX,
        byte offsetY,
        byte width,
        byte height,
        float liquidLevel,
        ushort[]? liquidTypeIds,
        byte[]? liquidFlags,
        float[]? liquidHeights)
    {
        Key = key;
        Build = build;
        HasLiquid = hasLiquid;
        Flags = flags;
        LiquidTypeId = liquidTypeId;
        OffsetX = offsetX;
        OffsetY = offsetY;
        Width = width;
        Height = height;
        LiquidLevel = liquidLevel;
        LiquidTypeIds = liquidTypeIds;
        LiquidFlags = liquidFlags;
        LiquidHeights = liquidHeights;
    }


    /**
      * Creates a disabled liquid payload used only when the runtime was recompiled without liquid support.
      */
    public static MapTileLiquidData CreateDisabled(MapTileKey key)
    {
        return new MapTileLiquidData(
            key,
            0,
            false,
            MapStorePayloadConstants.MapLiquidNoType | MapStorePayloadConstants.MapLiquidNoHeight,
            0,
            0,
            0,
            0,
            0,
            0.0f,
            null,
            null,
            null);
    }

    public MapTileKey Key { get; }
    public ushort Build { get; }
    public bool HasLiquid { get; }
    public ushort Flags { get; }
    public ushort LiquidTypeId { get; }
    public byte OffsetX { get; }
    public byte OffsetY { get; }
    public byte Width { get; }
    public byte Height { get; }
    public float LiquidLevel { get; }
    public ushort[]? LiquidTypeIds { get; }
    public byte[]? LiquidFlags { get; }
    public float[]? LiquidHeights { get; }
    public bool HasLiquidTypeGrid => LiquidTypeIds is not null;
    public bool HasLiquidHeightGrid => LiquidHeights is not null;
}
