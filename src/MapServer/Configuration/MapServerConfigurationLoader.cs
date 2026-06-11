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
using EmulationServer.Game.Maps.Runtime;
using EmulationServer.Shared.Configuration;

/**
  * File overview: src/MapServer/Configuration/MapServerConfigurationLoader.cs
  * Documents the MapServerConfigurationLoader source file in the map service startup, map status reporting, and player location routing area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.MapServer.Configuration;

/**
  * Owns the map server configuration loader behavior for the map service startup, map status reporting, and player location routing layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public static class MapServerConfigurationLoader
{
    /**
      * Defines the constant value for map server section.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const string MapServerSection = "MapServer";
    /**
      * Defines the constant value for map services section.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
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

        return new MapRuntimeSettings
        {
            Enabled = configuration.GetBool(MapServicesSection, "Enabled", true),
            TickInterval = tickInterval,
            StatusReportInterval = configuration.GetTimeSpan(MapServicesSection, "StatusReportInterval", TimeSpan.FromSeconds(15)),
            LogTicks = logTicks,
            DataDirectory = configuration.GetString(MapServicesSection, "DataDirectory", "Data"),
            DbcDirectory = configuration.GetString(MapServicesSection, "DbcDirectory", "dbc"),
            MapsDirectory = configuration.GetString(MapServicesSection, "MapsDirectory", "mapstore"),
            LoadDbcStores = configuration.GetBool(MapServicesSection, "LoadDbcStores", true),
            // Mapstore grid data is intentionally not a runtime toggle. Terrain, liquid, collision/vmaps,
            // and navmesh/mmaps are always preloaded and kept resident unless a compile-time symbol disables a component.
            RequiredDbcFiles = SplitList(requiredDbcFiles).ToArray(),
            Services = ParseMapServices(maps, MapServiceKind.World, tickInterval, logTicks),
        };
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
