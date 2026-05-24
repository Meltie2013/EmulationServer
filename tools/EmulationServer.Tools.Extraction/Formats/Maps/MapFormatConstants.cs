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

/**
  * File overview: tools/EmulationServer.Tools.Extraction/Formats/Maps/MapFormatConstants.cs
  * Documents the MapFormatConstants source file in the client data extraction and conversion tooling area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Tools.Extraction.Formats.Maps;

/**
  * Owns the map format constants behavior for the client data extraction and conversion tooling layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public static class MapFormatConstants
{
    /**
      * Defines the constant value for map magic.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const string MapMagic = "MAPS";
    /**
      * Defines the constant value for version magic.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const string VersionMagic = "0000";
    /**
      * Defines the constant value for area magic.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const string AreaMagic = "AREA";
    /**
      * Defines the constant value for height magic.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const string HeightMagic = "MHGT";
    /**
      * Defines the constant value for liquid magic.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const string LiquidMagic = "MLIQ";

    /**
      * Defines the constant value for map file header size.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const int MapFileHeaderSize = 44;

    /**
      * Defines the constant value for adt cells per grid.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const int AdtCellsPerGrid = 16;
    /**
      * Defines the constant value for adt grid size.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const int AdtGridSize = 128;
    /**
      * Defines the constant value for v 8 vertex count.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const int V8VertexCount = AdtGridSize * AdtGridSize;
    /**
      * Defines the constant value for v 9 vertex count.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const int V9VertexCount = (AdtGridSize + 1) * (AdtGridSize + 1);
    /**
      * Defines the constant value for area cell count.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const int AreaCellCount = AdtCellsPerGrid * AdtCellsPerGrid;

    /**
      * Defines the constant value for map area no area.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const ushort MapAreaNoArea = 0x0001;

    /**
      * Defines the constant value for map height no height.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const uint MapHeightNoHeight = 0x0001;
    /**
      * Defines the constant value for map height as int 16.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const uint MapHeightAsInt16 = 0x0002;
    /**
      * Defines the constant value for map height as int 8.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const uint MapHeightAsInt8 = 0x0004;

    /**
      * Defines the constant value for map liquid type no water.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const byte MapLiquidTypeNoWater = 0x00;
    /**
      * Defines the constant value for map liquid type magma.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const byte MapLiquidTypeMagma = 0x01;
    /**
      * Defines the constant value for map liquid type ocean.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const byte MapLiquidTypeOcean = 0x02;
    /**
      * Defines the constant value for map liquid type slime.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const byte MapLiquidTypeSlime = 0x04;
    /**
      * Defines the constant value for map liquid type water.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const byte MapLiquidTypeWater = 0x08;
    /**
      * Defines the constant value for map liquid type dark water.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const byte MapLiquidTypeDarkWater = 0x10;
    /**
      * Defines the constant value for map liquid type wmo water.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const byte MapLiquidTypeWmoWater = 0x20;

    /**
      * Defines the constant value for map liquid no type.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const ushort MapLiquidNoType = 0x0001;
    /**
      * Defines the constant value for map liquid no height.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const ushort MapLiquidNoHeight = 0x0002;
}
