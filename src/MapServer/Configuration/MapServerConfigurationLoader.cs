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
// File: src/MapServer/Configuration/MapServerConfigurationLoader.cs
// Purpose: Contains map server configuration loader code for the map server runtime, world-map ownership, and grid/tile coordination.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Core.Configuration;
using EmulationServer.Game.Data.Dbc.Creatures;
using EmulationServer.Game.Data.Dbc.Maps;
using EmulationServer.Game.Maps.Runtime;
using EmulationServer.Shared.Configuration;

namespace EmulationServer.MapServer.Configuration;

// Type: MapServerConfigurationLoader
// Purpose: Provides map server configuration loader behavior for the map server runtime, world-map ownership, and grid/tile coordination.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class MapServerConfigurationLoader
{

    // Constant: Defines the map server section constant used by the map server runtime, world-map ownership, and grid/tile coordination.
    // Value: fixed map server section value used anywhere this rule or protocol value is needed.
    private const string MapServerSection = "MapServer";

    // Constant: Defines the map services section constant used by the map server runtime, world-map ownership, and grid/tile coordination.
    // Value: fixed map services section value used anywhere this rule or protocol value is needed.
    private const string MapServicesSection = "MapServices";

    // Property: Gets or sets the default required DBC files value used by the map server runtime, world-map ownership, and grid/tile coordination.
    // Value: default required DBC files value exposed by the owning type.
    public static IReadOnlyList<string> DefaultRequiredDbcFiles { get; } =
    [
        MapDbcFileNames.AreaTable,
        MapDbcFileNames.AreaTrigger,
        "Faction.dbc",
        "FactionTemplate.dbc",
        "GameObjectDisplayInfo.dbc",
        ..CreatureDbcFileNames.CoreCreatureDbcFiles,
        "LiquidType.dbc",
        MapDbcFileNames.Map,
        "WMOAreaTable.dbc",
        MapDbcFileNames.WorldMapArea,
        MapDbcFileNames.WorldMapContinent,
        MapDbcFileNames.WorldMapOverlay,
        "WorldSafeLocs.dbc",
    ];

    // Method: Load
    // Purpose: Retrieves load data for the map server runtime, world-map ownership, and grid/tile coordination.
    // Parameters:
    // - path: Path value supplied by the caller for this operation.
    // Returns: Returns the map server settings value produced by this operation.
    // Notes: This keeps the operation scoped to MapServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: LoadMapServices
    // Purpose: Retrieves load map services data for the map server runtime, world-map ownership, and grid/tile coordination.
    // Parameters:
    // - configuration: Configuration values that control how this operation should run.
    // Returns: Returns the map runtime settings value produced by this operation.
    // Notes: This keeps the operation scoped to MapServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
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

            RequiredDbcFiles = SplitList(requiredDbcFiles).ToArray(),
            Services = ParseMapServices(maps, MapServiceKind.World, tickInterval, logTicks),
        };
    }

    // Method: ParseMapServices
    // Purpose: Converts incoming data into parse map services form for the map server runtime, world-map ownership, and grid/tile coordination.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // - kind: Kind value supplied by the caller for this operation.
    // - tickInterval: Tick interval value supplied by the caller for this operation.
    // - logTicks: Log ticks value supplied by the caller for this operation.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to MapServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ParseMapService
    // Purpose: Converts incoming data into parse map service form for the map server runtime, world-map ownership, and grid/tile coordination.
    // Parameters:
    // - entry: Entry value supplied by the caller for this operation.
    // - kind: Kind value supplied by the caller for this operation.
    // - tickInterval: Tick interval value supplied by the caller for this operation.
    // - logTicks: Log ticks value supplied by the caller for this operation.
    // Returns: Returns the map service definition value produced by this operation.
    // Notes: This keeps the operation scoped to MapServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: SplitList
    // Purpose: Executes the split list operation for the map server runtime, world-map ownership, and grid/tile coordination.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the I enumerable value produced by this operation.
    // Notes: This keeps the operation scoped to MapServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
    private static IEnumerable<string> SplitList(string value)
    {
        return value.Split([';', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}
