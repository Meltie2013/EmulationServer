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
// File: src/InstanceServer/Configuration/InstanceServerConfigurationLoader.cs
// Purpose: Contains instance server configuration loader code for the instance server runtime, dungeon-map ownership, and internal-service coordination.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Core.Configuration;
using EmulationServer.Game.Data.Dbc.Creatures;
using EmulationServer.Game.Data.Dbc.Maps;
using EmulationServer.Game.Maps.Runtime;
using EmulationServer.Shared.Configuration;

namespace EmulationServer.InstanceServer.Configuration;

// Type: InstanceServerConfigurationLoader
// Purpose: Provides instance server configuration loader behavior for the instance server runtime, dungeon-map ownership, and internal-service coordination.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class InstanceServerConfigurationLoader
{

    // Constant: Defines the instance server section constant used by the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Value: fixed instance server section value used anywhere this rule or protocol value is needed.
    private const string InstanceServerSection = "InstanceServer";

    // Constant: Defines the instance services section constant used by the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Value: fixed instance services section value used anywhere this rule or protocol value is needed.
    private const string InstanceServicesSection = "InstanceServices";

    // Property: Gets or sets the default required DBC files value used by the instance server runtime, dungeon-map ownership, and internal-service coordination.
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
    // Purpose: Retrieves load data for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - path: Path value supplied by the caller for this operation.
    // Returns: Returns the instance server settings value produced by this operation.
    // Notes: This keeps the operation scoped to InstanceServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: LoadInstanceServices
    // Purpose: Retrieves load instance services data for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - configuration: Configuration values that control how this operation should run.
    // Returns: Returns the map runtime settings value produced by this operation.
    // Notes: This keeps the operation scoped to InstanceServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
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

        return new MapRuntimeSettings
        {
            Enabled = configuration.GetBool(InstanceServicesSection, "Enabled", true),
            TickInterval = tickInterval,
            StatusReportInterval = configuration.GetTimeSpan(InstanceServicesSection, "StatusReportInterval", TimeSpan.FromSeconds(15)),
            LogTicks = logTicks,
            DataDirectory = configuration.GetString(InstanceServicesSection, "DataDirectory", "Data"),
            DbcDirectory = configuration.GetString(InstanceServicesSection, "DbcDirectory", "dbc"),
            MapsDirectory = configuration.GetString(InstanceServicesSection, "MapsDirectory", "mapstore"),
            LoadDbcStores = configuration.GetBool(InstanceServicesSection, "LoadDbcStores", true),

            RequiredDbcFiles = SplitList(requiredDbcFiles).ToArray(),
            Services = ParseInstanceServices(instances, tickInterval, logTicks),
        };
    }

    // Method: ParseInstanceServices
    // Purpose: Converts incoming data into parse instance services form for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // - tickInterval: Tick interval value supplied by the caller for this operation.
    // - logTicks: Log ticks value supplied by the caller for this operation.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to InstanceServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ParseInstanceService
    // Purpose: Converts incoming data into parse instance service form for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - entry: Entry value supplied by the caller for this operation.
    // - tickInterval: Tick interval value supplied by the caller for this operation.
    // - logTicks: Log ticks value supplied by the caller for this operation.
    // Returns: Returns the map service definition value produced by this operation.
    // Notes: This keeps the operation scoped to InstanceServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: SplitList
    // Purpose: Executes the split list operation for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the I enumerable value produced by this operation.
    // Notes: This keeps the operation scoped to InstanceServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
    private static IEnumerable<string> SplitList(string value)
    {
        return value.Split([';', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}
