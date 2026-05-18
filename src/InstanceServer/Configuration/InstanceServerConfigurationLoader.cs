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
using EmulationServer.Game.Data.Maps;
using EmulationServer.Game.Maps.Runtime;
using EmulationServer.Shared.Configuration;

/**
  * File overview: src/InstanceServer/Configuration/InstanceServerConfigurationLoader.cs
  * This file belongs to the server configuration loading and strongly typed settings portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.InstanceServer.Configuration;

/**
  * Represents the instance server configuration loader component in the server configuration loading and strongly typed settings area.
  * It centralizes INI parsing so startup code can work with strongly typed settings instead of raw strings.
  */
public static class InstanceServerConfigurationLoader
{
    private const string InstanceServerSection = "InstanceServer";
    private const string InstanceServicesSection = "InstanceServices";

    /**
      * Gets or stores the default required dbc files value used by InstanceServerConfigurationLoader.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public static IReadOnlyList<string> DefaultRequiredDbcFiles { get; } =
    [
        "AreaTable.dbc",
        "AreaTrigger.dbc",
        "Faction.dbc",
        "FactionTemplate.dbc",
        "GameObjectDisplayInfo.dbc",
        "LiquidType.dbc",
        "Map.dbc",
        "WMOAreaTable.dbc",
        "WorldMapArea.dbc",
        "WorldSafeLocs.dbc",
    ];

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of InstanceServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    public static InstanceServerSettings Load(string path)
    {
        string fullPath = Path.GetFullPath(path);

        IniConfiguration configuration = IniConfiguration.Load(fullPath);

        InstanceServerSettings settings = new()
        {
            Logging = ServerConfigurationLoader.LoadLoggingSettings(configuration, fullPath, "InstanceServer"),

            InternalNetwork = ServerConfigurationLoader.LoadInternalNetworkSettings(
                configuration,
                InstanceServerSection,
                "InstanceServer",
                5004),

            InstanceServices = LoadInstanceServices(configuration),
        };

        settings.Validate();

        return settings;
    }

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of InstanceServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static MapRuntimeSettings LoadInstanceServices(IniConfiguration configuration)
    {
        TimeSpan tickInterval = configuration.GetTimeSpan(
            InstanceServicesSection,
            "TickInterval",
            TimeSpan.FromMilliseconds(100));

        bool logTicks = configuration.GetBool(
            InstanceServicesSection,
            "LogTicks",
            false);

        string instances = configuration.GetString(
            InstanceServicesSection,
            "Instances",
            "36:Deadmines;33:Shadowfang Keep");

        string requiredDbcFiles = configuration.GetString(
            InstanceServicesSection,
            "RequiredDbcFiles",
            string.Join(';', DefaultRequiredDbcFiles));

        string startupGrids = configuration.GetString(
            InstanceServicesSection,
            "StartupGrids",
            string.Empty);

        return new MapRuntimeSettings
        {
            Enabled = configuration.GetBool(InstanceServicesSection, "Enabled", true),
            TickInterval = tickInterval,
            StatusReportInterval = configuration.GetTimeSpan(InstanceServicesSection, "StatusReportInterval", TimeSpan.FromSeconds(15)),
            LogTicks = logTicks,
            DataDirectory = configuration.GetString(InstanceServicesSection, "DataDirectory", "Data"),
            DbcDirectory = configuration.GetString(InstanceServicesSection, "DbcDirectory", "dbc"),
            MapsDirectory = configuration.GetString(InstanceServicesSection, "MapsDirectory", "maps"),
            LoadDbcStores = configuration.GetBool(InstanceServicesSection, "LoadDbcStores", true),
            LoadMapTiles = configuration.GetBool(InstanceServicesSection, "LoadMapTiles", true),
            GridLoadingMode = ParseGridLoadingMode(configuration.GetString(InstanceServicesSection, "GridLoadingMode", "OnDemand")),
            KeepLoadedGrids = configuration.GetBool(InstanceServicesSection, "KeepLoadedGrids", false),
            GridIdleUnloadDelay = configuration.GetTimeSpan(InstanceServicesSection, "GridIdleUnloadDelay", TimeSpan.FromMinutes(5)),
            StartupGrids = ParseStartupGrids(startupGrids),
            RequiredDbcFiles = SplitList(requiredDbcFiles).ToArray(),
            Services = ParseInstanceServices(instances, tickInterval, logTicks),
        };
    }

    /**
      * Parses text input into a strongly typed value used by the server runtime.
      * The method is part of InstanceServerConfigurationLoader and keeps this workflow isolated from the caller.
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
      * The method is part of InstanceServerConfigurationLoader and keeps this workflow isolated from the caller.
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
                throw new ConfigurationException($"Invalid StartupGrids entry '{entry}'. Expected MapId:TileX:TileY, for example 36:48:48.");
            }

            grids.Add(new MapTileKey(mapId, tileX, tileY));
        }

        return grids;
    }

    /**
      * Parses text input into a strongly typed value used by the server runtime.
      * The method is part of InstanceServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static IReadOnlyList<MapServiceDefinition> ParseInstanceServices(
        string value,
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
            services.Add(ParseInstanceService(entry, tickInterval, logTicks));
        }

        return services;
    }

    /**
      * Parses text input into a strongly typed value used by the server runtime.
      * The method is part of InstanceServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static MapServiceDefinition ParseInstanceService(
        string entry,
        TimeSpan tickInterval,
        bool logTicks)
    {
        string[] parts = entry.Split(':', 2, StringSplitOptions.TrimEntries);

        string idPart = parts[0];
        long instanceId = 0;

        int instanceSeparator = idPart.IndexOf('@');
        if (instanceSeparator >= 0)
        {
            string mapIdPart = idPart[..instanceSeparator];
            string instanceIdPart = idPart[(instanceSeparator + 1)..];

            if (!long.TryParse(instanceIdPart, out instanceId) || instanceId < 0)
            {
                throw new ConfigurationException($"Invalid instance service entry '{entry}'. Expected MapId or MapId@InstanceId optionally followed by :Name.");
            }

            idPart = mapIdPart;
        }

        if (!int.TryParse(idPart, out int mapId) || mapId < 0)
        {
            throw new ConfigurationException($"Invalid instance service entry '{entry}'. Expected MapId or MapId@InstanceId optionally followed by :Name.");
        }

        string name = parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1])
            ? parts[1]
            : $"Instance Map {mapId}";

        return new MapServiceDefinition
        {
            MapId = mapId,
            InstanceId = instanceId,
            Name = name,
            Kind = MapServiceKind.Instance,
            TickInterval = tickInterval,
            LogTicks = logTicks,
        };
    }

    /**
      * Splits the supplied text into command parts while preserving quoted values.
      * The method is part of InstanceServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static IEnumerable<string> SplitList(string value)
    {
        return value.Split([';', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}
