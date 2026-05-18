using EmulationServer.Core.Configuration;
using EmulationServer.Game.Data.Maps;
using EmulationServer.Game.Maps.Runtime;
using EmulationServer.Shared.Configuration;

namespace EmulationServer.MapServer.Configuration;

public static class MapServerConfigurationLoader
{
    private const string MapServerSection = "MapServer";
    private const string MapServicesSection = "MapServices";

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

    public static MapServerSettings Load(string path)
    {
        string fullPath = Path.GetFullPath(path);

        IniConfiguration configuration = IniConfiguration.Load(fullPath);

        MapServerSettings settings = new()
        {
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

    private static MapGridLoadingMode ParseGridLoadingMode(string value)
    {
        if (Enum.TryParse(value, ignoreCase: true, out MapGridLoadingMode mode))
        {
            return mode;
        }

        throw new ConfigurationException($"Invalid GridLoadingMode '{value}'. Expected OnDemand or Preload.");
    }

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

    private static IEnumerable<string> SplitList(string value)
    {
        return value.Split([';', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}
