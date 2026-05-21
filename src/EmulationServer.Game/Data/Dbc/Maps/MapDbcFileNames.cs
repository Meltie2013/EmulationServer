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
  * File overview: src/EmulationServer.Game/Data/Dbc/Maps/MapDbcFileNames.cs
  * This file defines the DBC file names that feed map, area, trigger, continent, and overlay metadata into the map system.
  */

namespace EmulationServer.Game.Data.Dbc.Maps;

/**
  * Centralizes map-related DBC file names so WorldServer, MapServer, and InstanceServer can share the same required data list.
  */
public static class MapDbcFileNames
{
    /**
      * Required for map identity, map type, display names, loading screens, and world-vs-instance classification.
      */
    public const string Map = "Map.dbc";

    /**
      * Required for zones, sub-zones, area flags, faction groups, liquid defaults, and area names.
      */
    public const string AreaTable = "AreaTable.dbc";

    /**
      * Required for map-area triggers such as portals, instance entrances, and positional trigger volumes.
      */
    public const string AreaTrigger = "AreaTrigger.dbc";

    /**
      * Required for connecting rendered world-map areas back to area table records.
      */
    public const string WorldMapArea = "WorldMapArea.dbc";

    /**
      * Required for continent-level world-map coordinate data for maps such as Eastern Kingdoms and Kalimdor.
      */
    public const string WorldMapContinent = "WorldMapContinent.dbc";

    /**
      * Required for overlay texture and area-highlight data used by map and area lookup systems.
      */
    public const string WorldMapOverlay = "WorldMapOverlay.dbc";

    /**
      * Contains the core map DBC files that should be available anywhere map services or map routing are enabled.
      */
    public static IReadOnlyList<string> CoreMapDbcFiles { get; } =
    [
        Map,
        AreaTable,
        AreaTrigger,
        WorldMapArea,
        WorldMapContinent,
        WorldMapOverlay,
    ];
}
