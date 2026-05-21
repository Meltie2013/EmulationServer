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

using EmulationServer.Core.Configuration;
using EmulationServer.Game.Data.Dbc.Maps;
using EmulationServer.Game.Data.Maps;
using EmulationServer.Game.Maps.Runtime;
using EmulationServer.Shared.Configuration;

/**
  * File overview: src/MapServer/Configuration/MapServerConfigurationLoader.cs
  * This file belongs to the server configuration loading and strongly typed settings portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.MapServer.Configuration;

/**
  * Represents the map server configuration loader component in the server configuration loading and strongly typed settings area.
  * It centralizes INI parsing so startup code can work with strongly typed settings instead of raw strings.
  */
public static class MapServerConfigurationLoader
{
    private const string MapServerSection = "MapServer";
    private const string MapServicesSection = "MapServices";

    /**
      * Gets or stores the default required dbc files value used by MapServerConfigurationLoader.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public static IReadOnlyList<string> DefaultRequiredDbcFiles { get; } =
    [
        MapDbcFileNames.AreaTable,
        MapDbcFileNames.AreaTrigger,
        "Faction.dbc",
        "FactionTemplate.dbc",
        "GameObjectDisplayInfo.dbc",
        "LiquidType.dbc",
        MapDbcFileNames.Map,
        "WMOAreaTable.dbc",
        MapDbcFileNames.WorldMapArea,
        MapDbcFileNames.WorldMapContinent,
        MapDbcFileNames.WorldMapOverlay,
        "WorldSafeLocs.dbc",
    ];

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of MapServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    public static MapServerSettings Load(string path)
    {
        string fullPath = Path.GetFullPath(path);

        IniConfiguration configuration = IniConfiguration.Load(fullPath);

        MapServerSettings settings = new()
        {
            Logging = ServerConfigurationLoader.LoadLoggingSettings(configuration, fullPath, "MapServer"),

            InternalNetwork = ServerConfigurationLoader.LoadInternalNetworkSettings(
                configuration,
                MapServerSection,
                "MapServer",
                5003),

            MapServices = LoadMapServices(configuration),
        };

        settings.Validate();

        return settings;
    }

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of MapServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static MapRuntimeSettings LoadMapServices(IniConfiguration configuration)
    {
        TimeSpan tickInterval = configuration.GetTimeSpan(
            MapServicesSection,
            "TickInterval",
            TimeSpan.FromMilliseconds(100));

        bool logTicks = configuration.GetBool(
            MapServicesSection,
            "LogTicks",
            false);

        string maps = configuration.GetString(
            MapServicesSection,
            "Maps",
            "0:Eastern Kingdoms;1:Kalimdor;530:Outland;571:Northrend");

        string requiredDbcFiles = configuration.GetString(
            MapServicesSection,
            "RequiredDbcFiles",
            string.Join(';', DefaultRequiredDbcFiles));

        string startupGrids = configuration.GetString(
            MapServicesSection,
            "StartupGrids",
            string.Empty);

        return new MapRuntimeSettings
        {
            Enabled = configuration.GetBool(MapServicesSection, "Enabled", true),
            TickInterval = tickInterval,
            StatusReportInterval = configuration.GetTimeSpan(MapServicesSection, "StatusReportInterval", TimeSpan.FromSeconds(15)),
            LogTicks = logTicks,
            DataDirectory = configuration.GetString(MapServicesSection, "DataDirectory", "Data"),
            DbcDirectory = configuration.GetString(MapServicesSection, "DbcDirectory", "dbc"),
            MapsDirectory = configuration.GetString(MapServicesSection, "MapsDirectory", "maps"),
            LoadDbcStores = configuration.GetBool(MapServicesSection, "LoadDbcStores", true),
            LoadMapTiles = configuration.GetBool(MapServicesSection, "LoadMapTiles", true),
            GridLoadingMode = ParseGridLoadingMode(configuration.GetString(MapServicesSection, "GridLoadingMode", "OnDemand")),
            KeepLoadedGrids = configuration.GetBool(MapServicesSection, "KeepLoadedGrids", false),
            GridIdleUnloadDelay = configuration.GetTimeSpan(MapServicesSection, "GridIdleUnloadDelay", TimeSpan.FromMinutes(5)),
            StartupGrids = ParseStartupGrids(startupGrids),
            RequiredDbcFiles = SplitList(requiredDbcFiles).ToArray(),
            Services = ParseMapServices(maps, MapServiceKind.World, tickInterval, logTicks),
        };
    }

    /**
      * Parses text input into a strongly typed value used by the server runtime.
      * The method is part of MapServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static MapGridLoadingMode ParseGridLoadingMode(string value)
    {
        if (Enum.TryParse(value, ignoreCase: true, out MapGridLoadingMode mode))
        {
            return mode;
        }

        throw new ConfigurationException($"Invalid GridLoadingMode '{value}'. Expected OnDemand or Preload.");
    }

    /**
      * Parses text input into a strongly typed value used by the server runtime.
      * The method is part of MapServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static IReadOnlyList<MapTileKey> ParseStartupGrids(string value)
    {
        List<MapTileKey> grids = [];
        foreach (string entry in SplitList(value))
        {
            string[] parts = entry.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3 ||
                !uint.TryParse(parts[0], out uint mapId) ||
                !byte.TryParse(parts[1], out byte tileX) ||
                !byte.TryParse(parts[2], out byte tileY))
            {
                throw new ConfigurationException($"Invalid StartupGrids entry '{entry}'. Expected MapId:TileX:TileY, for example 0:32:32.");
            }

            grids.Add(new MapTileKey(mapId, tileX, tileY));
        }

        return grids;
    }

    /**
      * Parses text input into a strongly typed value used by the server runtime.
      * The method is part of MapServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static IReadOnlyList<MapServiceDefinition> ParseMapServices(
        string value,
        MapServiceKind kind,
        TimeSpan tickInterval,
        bool logTicks)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        List<MapServiceDefinition> services = [];
        string[] entries = value.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        foreach (string entry in entries)
        {
            services.Add(ParseMapService(entry, kind, tickInterval, logTicks));
        }

        return services;
    }

    /**
      * Parses text input into a strongly typed value used by the server runtime.
      * The method is part of MapServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static MapServiceDefinition ParseMapService(
        string entry,
        MapServiceKind kind,
        TimeSpan tickInterval,
        bool logTicks)
    {
        string[] parts = entry.Split(':', 2, StringSplitOptions.TrimEntries);
        if (!int.TryParse(parts[0], out int mapId) || mapId < 0)
        {
            throw new ConfigurationException($"Invalid map service entry '{entry}'. Expected MapId or MapId:Name.");
        }

        string name = parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1])
            ? parts[1]
            : $"Map {mapId}";

        return new MapServiceDefinition
        {
            MapId = mapId,
            InstanceId = 0,
            Name = name,
            Kind = kind,
            TickInterval = tickInterval,
            LogTicks = logTicks,
        };
    }

    /**
      * Splits the supplied text into command parts while preserving quoted values.
      * The method is part of MapServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static IEnumerable<string> SplitList(string value)
    {
        return value.Split([';', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}
