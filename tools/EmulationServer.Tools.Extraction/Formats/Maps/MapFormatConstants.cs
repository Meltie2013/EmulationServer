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

namespace EmulationServer.Tools.Extraction.Formats.Maps;

public static class MapFormatConstants
{
    public const string MapMagic = "MAPS";
    public const string VersionMagic = "0000";
    public const string AreaMagic = "AREA";
    public const string HeightMagic = "MHGT";
    public const string LiquidMagic = "MLIQ";

    public const int MapFileHeaderSize = 44;

    public const int AdtCellsPerGrid = 16;
    public const int AdtGridSize = 128;
    public const int V8VertexCount = AdtGridSize * AdtGridSize;
    public const int V9VertexCount = (AdtGridSize + 1) * (AdtGridSize + 1);
    public const int AreaCellCount = AdtCellsPerGrid * AdtCellsPerGrid;

    public const ushort MapAreaNoArea = 0x0001;

    public const uint MapHeightNoHeight = 0x0001;
    public const uint MapHeightAsInt16 = 0x0002;
    public const uint MapHeightAsInt8 = 0x0004;

    public const byte MapLiquidTypeNoWater = 0x00;
    public const byte MapLiquidTypeMagma = 0x01;
    public const byte MapLiquidTypeOcean = 0x02;
    public const byte MapLiquidTypeSlime = 0x04;
    public const byte MapLiquidTypeWater = 0x08;
    public const byte MapLiquidTypeDarkWater = 0x10;
    public const byte MapLiquidTypeWmoWater = 0x20;

    public const ushort MapLiquidNoType = 0x0001;
    public const ushort MapLiquidNoHeight = 0x0002;
}
