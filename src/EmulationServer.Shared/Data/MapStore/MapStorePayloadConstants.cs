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

namespace EmulationServer.Shared.Data.MapStore;

/**
  * Owns the runtime payload constants used after mapstore files are validated by the shared outer header.
  */
public static class MapStorePayloadConstants
{
    public const string TerrainMagic = "TRN1";
    public const string LiquidPayloadMagic = "LIQ1";
    public const string CollisionPayloadMagic = "ESVTIL1";
    public const string NavmeshPayloadMagic = "NAV1";
    public const string AreaMagic = "AREA";
    public const string HeightMagic = "MHGT";
    public const string LiquidSectionMagic = "MLIQ";
    public const int CellsPerGrid = 16;
    public const int GridSize = 128;
    public const int V8VertexCount = GridSize * GridSize;
    public const int V9VertexCount = (GridSize + 1) * (GridSize + 1);
    public const int AreaCellCount = CellsPerGrid * CellsPerGrid;
    public const ushort MapAreaNoArea = 0x0001;
    public const uint MapHeightNoHeight = 0x0001;
    public const uint MapHeightAsInt16 = 0x0002;
    public const uint MapHeightAsInt8 = 0x0004;
    public const byte MapLiquidTypeNoWater = 0x00;
    public const ushort MapLiquidNoType = 0x0001;
    public const ushort MapLiquidNoHeight = 0x0002;
}
