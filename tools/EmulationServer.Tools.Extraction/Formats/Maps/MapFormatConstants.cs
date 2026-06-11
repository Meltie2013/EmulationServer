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

namespace EmulationServer.Tools.Extraction.Formats.Maps;

/**
  * Keeps the legacy extracted terrain section constants together while reusing the shared mapstore payload values.
  */
public static class MapFormatConstants
{
    public const string MapMagic = "MAPS";
    public const string VersionMagic = "0000";
    public const string AreaMagic = MapStorePayloadConstants.AreaMagic;
    public const string HeightMagic = MapStorePayloadConstants.HeightMagic;
    public const string LiquidMagic = MapStorePayloadConstants.LiquidSectionMagic;

    public const int MapFileHeaderSize = 44;
    public const int AdtCellsPerGrid = MapStorePayloadConstants.CellsPerGrid;
    public const int AdtGridSize = MapStorePayloadConstants.GridSize;
    public const int V8VertexCount = MapStorePayloadConstants.V8VertexCount;
    public const int V9VertexCount = MapStorePayloadConstants.V9VertexCount;
    public const int AreaCellCount = MapStorePayloadConstants.AreaCellCount;

    public const ushort MapAreaNoArea = MapStorePayloadConstants.MapAreaNoArea;

    public const uint MapHeightNoHeight = MapStorePayloadConstants.MapHeightNoHeight;
    public const uint MapHeightAsInt16 = MapStorePayloadConstants.MapHeightAsInt16;
    public const uint MapHeightAsInt8 = MapStorePayloadConstants.MapHeightAsInt8;

    public const byte MapLiquidTypeNoWater = MapStorePayloadConstants.MapLiquidTypeNoWater;
    public const byte MapLiquidTypeMagma = 0x01;
    public const byte MapLiquidTypeOcean = 0x02;
    public const byte MapLiquidTypeSlime = 0x04;
    public const byte MapLiquidTypeWater = 0x08;
    public const byte MapLiquidTypeDarkWater = 0x10;
    public const byte MapLiquidTypeWmoWater = 0x20;

    public const ushort MapLiquidNoType = MapStorePayloadConstants.MapLiquidNoType;
    public const ushort MapLiquidNoHeight = MapStorePayloadConstants.MapLiquidNoHeight;
}
