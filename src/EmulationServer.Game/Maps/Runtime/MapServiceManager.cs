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

using EmulationServer.Game.Data;
using EmulationServer.Game.Data.Dbc;
using EmulationServer.Game.Data.Dbc.Maps;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

/**
  * File overview: src/EmulationServer.Game/Maps/Runtime/MapServiceManager.cs
  * Documents the MapServiceManager source file in the runtime map-player state tracking area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Maps.Runtime;

/**
  * Coordinates all map and instance services hosted by a server process and routes control commands to the correct service.
  * It also owns typed map DBC metadata so registered services can be validated and described from Map.dbc and related area files.
  */
public sealed class MapServiceManager : IAsyncDisposable
{
    /**
      * Holds the private owner server name state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly string _ownerServerName;
    /**
      * Holds the private settings state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly MapRuntimeSettings _settings;
    private readonly Func<MapServiceSnapshot, CancellationToken, Task> _reportStatusAsync;
    /**
      * Holds the private services state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly List<MapService> _services = [];
    private readonly Dictionary<string, DbcDataStore> _dbcStores;
    /**
      * Holds the private map data state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly MapDbcDataStore _mapData;

    /**
      * Holds the private stop cancellation state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private CancellationTokenSource? _stopCancellation;
    /**
      * Holds the private report task state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private Task? _reportTask;
    /**
      * Holds the private started state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private int _started;
    /**
      * Holds the private stopping state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private int _stopping;

    /**
      * Creates the service manager, loads configured DBC data, creates typed map metadata, and registers configured map services.
      */
    public MapServiceManager(
        string ownerServerName,
        MapRuntimeSettings settings,
        Func<MapServiceSnapshot, CancellationToken, Task> reportStatusAsync)
    {
        if (string.IsNullOrWhiteSpace(ownerServerName))
        {
            throw new ArgumentException("Owner server name is required.");
        }

        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _ownerServerName = ownerServerName;
        _settings = settings;
        _reportStatusAsync = reportStatusAsync ?? throw new ArgumentNullException();

        if (!settings.Enabled)
        {
            _dbcStores = new Dictionary<string, DbcDataStore>(StringComparer.OrdinalIgnoreCase);
            _mapData = MapDbcDataStore.Empty;
            return;
        }

        _dbcStores = settings.LoadDbcStores
            ? DbcStoreLoader.LoadRequiredStores(
                GameDataPathResolver.ResolveDirectory(settings.DataDirectory, settings.DbcDirectory),
                settings.RequiredDbcFiles,
                ownerServerName)
            : new Dictionary<string, DbcDataStore>(StringComparer.OrdinalIgnoreCase);

        _mapData = settings.LoadDbcStores
            ? MapDbcDataStore.FromDbcStores(_dbcStores, ownerServerName)
            : MapDbcDataStore.Empty;

        string? mapsDirectory = settings.LoadMapTiles
            ? GameDataPathResolver.ResolveDirectory(settings.DataDirectory, settings.MapsDirectory)
            : null;

        foreach (MapServiceDefinition configuredDefinition in settings.Services)
        {
            MapServiceDefinition definition = ApplyMapDbcMetadata(configuredDefinition);

            MapGridManager? gridManager = settings.LoadMapTiles
                ? new MapGridManager(
                    definition,
                    mapsDirectory!,
                    settings.GridLoadingMode,
                    settings.KeepLoadedGrids,
                    settings.GridIdleUnloadDelay)
                : null;

            _services.Add(new MapService(ownerServerName, definition, gridManager, settings.StartupGrids, _reportStatusAsync));
        }
    }

    /**
      * Gets all map or instance services registered with this manager.
      */
    public IReadOnlyList<MapService> Services => _services;

    /**
      * Gets the raw DBC stores loaded by this server for systems that still need generic DBC access.
      */
    public IReadOnlyDictionary<string, DbcDataStore> DbcStores => _dbcStores;

    /**
      * Gets typed map, area, trigger, continent, and overlay DBC data for the hosted services.
      */
    public MapDbcDataStore MapData => _mapData;

    /**
      * Returns snapshots for every registered map service.
      */
    public IReadOnlyList<MapServiceSnapshot> GetSnapshots()
    {
        return _services
            .Select(service => service.GetSnapshot())
            .ToArray();
    }

    /**
      * Returns snapshots for every registered service matching the supplied map id.
      */
    public IReadOnlyList<MapServiceSnapshot> GetSnapshots(int mapId)
    {
        return _services
            .Where(service => service.Definition.MapId == mapId)
            .Select(service => service.GetSnapshot())
            .ToArray();
    }

    /**
      * Applies the active player counts collected by the owning map or instance server to every hosted service.
      */
    public void SetActivePlayerCounts(IReadOnlyDictionary<uint, int> activePlayersByMap)
    {
        ArgumentNullException.ThrowIfNull(activePlayersByMap);

        foreach (MapService service in _services)
        {
            uint mapId = unchecked((uint)service.Definition.MapId);
            activePlayersByMap.TryGetValue(mapId, out int activePlayers);
            service.SetActivePlayerCount(activePlayers);
        }
    }

    /**
      * Starts every registered service and begins the periodic status report loop.
      */
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _started, 1) == 1)
        {
            throw new InvalidOperationException($"{_ownerServerName} map service manager has already been started.");
        }

        if (!_settings.Enabled)
        {
            Logger.Write(LogType.INFORMATION, $"{_ownerServerName} map services are disabled by configuration.", "MapServiceManager");
            return;
        }

        _stopCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        foreach (MapService service in _services)
        {
            await service.StartAsync(_stopCancellation.Token);
        }

        _reportTask = Task.Run(() => RunStatusReportLoopAsync(_stopCancellation.Token), CancellationToken.None);

        Logger.Write(LogType.THREAD, $"{_ownerServerName} map service status report loop started with interval {_settings.StatusReportInterval.TotalSeconds:0.##} second(s).", "MapServiceManager");
        Logger.Write(LogType.SYSTEM, $"{_ownerServerName} map service manager started with {_services.Count} service(s).", "MapServiceManager");
    }

    /**
      * Stops the stop workflow and releases owned runtime resources in a controlled order.
      * Shutdown logic is centralized to avoid dangling connections, incomplete saves, or partially registered services.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _stopping, 1) == 1)
        {
            return;
        }

        CancellationTokenSource? stopCancellation = _stopCancellation;
        if (stopCancellation is not null)
        {
            await stopCancellation.CancelAsync();
        }

        if (_reportTask is not null)
        {
            try
            {
                Task completedTask = await Task.WhenAny(_reportTask, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken));
                if (completedTask == _reportTask)
                {
                    await _reportTask;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Expected during shutdown.
            }
        }

        foreach (MapService service in _services)
        {
            await service.StopAsync(cancellationToken);
        }

        stopCancellation?.Dispose();
        _stopCancellation = null;

        if (_settings.Enabled)
        {
            Logger.Write(LogType.THREAD, $"{_ownerServerName} map service status report loop stopped.", "MapServiceManager");
            Logger.Write(LogType.SYSTEM, $"{_ownerServerName} map service manager stopped.", "MapServiceManager");
        }
    }

    /**
      * Executes a start, shutdown, restart, or info command for every service matching the requested map id.
      */
    public async Task<IReadOnlyList<MapServiceControlResult>> ExecuteControlCommandAsync(
        MapServiceControlAction action,
        int mapId,
        CancellationToken cancellationToken)
    {
        if (mapId < 0)
        {
            throw new ArgumentOutOfRangeException(null, "Map ID cannot be negative.");
        }

        if (!_settings.Enabled)
        {
            return [new MapServiceControlResult(
                _ownerServerName,
                GetDefaultServiceKind(),
                mapId,
                0,
                MapServiceControlResultCode.Ignored,
                MapServiceState.Offline,
                $"{_ownerServerName} map services are disabled by configuration.")];
        }

        MapService[] matchingServices = _services
            .Where(service => service.Definition.MapId == mapId)
            .ToArray();

        if (matchingServices.Length == 0)
        {
            return [new MapServiceControlResult(
                _ownerServerName,
                GetDefaultServiceKind(),
                mapId,
                0,
                MapServiceControlResultCode.NotFound,
                MapServiceState.Offline,
                $"{_ownerServerName} has no configured map service for MapId={mapId}. {_mapData.DescribeMap(mapId)}")];
        }

        List<MapServiceControlResult> results = [];
        foreach (MapService service in matchingServices)
        {
            results.Add(await ExecuteControlCommandAsync(service, action, cancellationToken));
        }

        return results;
    }

    /**
      * Stops owned background work and releases the service manager.
      */
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
    }

    /**
      * Sends one status report for every registered service immediately.
      */
    public async Task ReportAllServicesAsync(CancellationToken cancellationToken)
    {
        foreach (MapService service in _services)
        {
            await _reportStatusAsync(service.GetSnapshot(), cancellationToken);
        }
    }

    /**
      * Sends one status report for every registered service matching a map id immediately.
      */
    public async Task ReportServicesAsync(int mapId, CancellationToken cancellationToken)
    {
        foreach (MapService service in _services.Where(service => service.Definition.MapId == mapId))
        {
            await _reportStatusAsync(service.GetSnapshot(), cancellationToken);
        }
    }

    /**
      * Executes a map control command against a single service and converts the result to a protocol-safe response.
      */
    private async Task<MapServiceControlResult> ExecuteControlCommandAsync(
        MapService service,
        MapServiceControlAction action,
        CancellationToken cancellationToken)
    {
        try
        {
            switch (action)
            {
                case MapServiceControlAction.Start:
                    await service.StartAsync(GetServiceLifetimeToken(cancellationToken));
                    return MapServiceControlResult.FromSnapshot(
                        service.GetSnapshot(),
                        MapServiceControlResultCode.Success,
                        $"Started map service '{service.Definition.Name}'. {_mapData.DescribeMap(service.Definition.MapId)}");

                case MapServiceControlAction.Shutdown:
                    await service.ShutdownAsync(cancellationToken);
                    return MapServiceControlResult.FromSnapshot(
                        service.GetSnapshot(),
                        MapServiceControlResultCode.Success,
                        $"Shutdown map service '{service.Definition.Name}'. {_mapData.DescribeMap(service.Definition.MapId)}");

                case MapServiceControlAction.Restart:
                    await service.RestartAsync(GetServiceLifetimeToken(cancellationToken));
                    return MapServiceControlResult.FromSnapshot(
                        service.GetSnapshot(),
                        MapServiceControlResultCode.Success,
                        $"Restarted map service '{service.Definition.Name}'. {_mapData.DescribeMap(service.Definition.MapId)}");

                case MapServiceControlAction.Info:
                    MapServiceSnapshot snapshot = service.GetSnapshot();
                    return MapServiceControlResult.FromSnapshot(
                        snapshot,
                        MapServiceControlResultCode.Success,
                        FormatInfoMessage(snapshot));

                default:
                    return MapServiceControlResult.FromSnapshot(
                        service.GetSnapshot(),
                        MapServiceControlResultCode.Failed,
                        $"Unsupported map service command '{action}'.");
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            MapServiceSnapshot snapshot = service.GetSnapshot();
            Logger.Write(LogType.WARNING, $"{_ownerServerName} failed to execute {action} for map service '{service.Definition.Name}': {exception.Message}", "MapServiceManager");

            return MapServiceControlResult.FromSnapshot(
                snapshot,
                MapServiceControlResultCode.Failed,
                exception.Message);
        }
    }

    /**
      * Uses typed Map.dbc data to validate configured service ids, improve service names, and log area/trigger counts.
      */
    private MapServiceDefinition ApplyMapDbcMetadata(MapServiceDefinition definition)
    {
        definition.Validate();

        if (!_mapData.TryGetMap(definition.MapId, out MapDbcRecord map))
        {
            Logger.Write(LogType.WARNING, $"{_ownerServerName} configured map service '{definition.Name}' for MapId={definition.MapId}, but that id was not found in Map.dbc.", "MapServiceManager");
            return definition;
        }

        MapServiceKind expectedKind = map.IsWorldMap ? MapServiceKind.World : MapServiceKind.Instance;
        if (definition.Kind != expectedKind)
        {
            Logger.Write(LogType.WARNING, $"{_ownerServerName} configured MapId={definition.MapId} as {definition.Kind}, but Map.dbc identifies '{map.DisplayName}' as {map.Type}.", "MapServiceManager");
        }

        string configuredDefaultName = definition.Kind == MapServiceKind.Instance
            ? $"Instance Map {definition.MapId}"
            : $"Map {definition.MapId}";

        string serviceName = string.Equals(definition.Name, configuredDefaultName, StringComparison.OrdinalIgnoreCase)
            ? map.DisplayName
            : definition.Name;

        Logger.Write(LogType.SYSTEM, $"{_ownerServerName} registered {definition.Kind} service: {_mapData.DescribeMap(definition.MapId)}.", "MapServiceManager");

        return new MapServiceDefinition
        {
            MapId = definition.MapId,
            InstanceId = definition.InstanceId,
            Name = serviceName,
            Kind = definition.Kind,
            TickInterval = definition.TickInterval,
            LogTicks = definition.LogTicks,
        };
    }

    /**
      * Returns the token that should control long-running service work created after manager startup.
      */
    private CancellationToken GetServiceLifetimeToken(CancellationToken fallbackToken)
    {
        return _stopCancellation?.Token ?? fallbackToken;
    }

    /**
      * Runs the periodic status report loop until the server shuts down.
      */
    private async Task RunStatusReportLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await ReportAllServicesAsync(cancellationToken);
                await Task.Delay(_settings.StatusReportInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown.
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.CRITICAL, exception.ToString(), "MapServiceManager");
        }
    }

    /**
      * Returns the best default service kind for a command result when no actual service was found.
      */
    private MapServiceKind GetDefaultServiceKind()
    {
        MapService? service = _services.FirstOrDefault();
        if (service is not null)
        {
            return service.Definition.Kind;
        }

        return _ownerServerName.Contains("Instance", StringComparison.OrdinalIgnoreCase)
            ? MapServiceKind.Instance
            : MapServiceKind.World;
    }

    /**
      * Formats one map info response with both runtime status and DBC-backed map metadata.
      */
    private string FormatInfoMessage(MapServiceSnapshot snapshot)
    {
        List<string> lines = [
            $"Map service: {snapshot.Name}",
            $"Owner: {snapshot.OwnerServerName}",
            $"Kind: {snapshot.Kind}",
            $"Map ID: {snapshot.MapId}",
            $"Instance ID: {snapshot.InstanceId}",
            $"State: {snapshot.State}",
            $"Uptime: {FormatUptime(snapshot.State, snapshot.StartedUtc)}",
            $"Tick: {snapshot.Tick}",
            $"Players: {snapshot.ActivePlayers}",
            $"Grids: {snapshot.ActiveGrids}",
            $"Load: {snapshot.LoadPercent:0.##}%",
            $"Average Tick: {snapshot.AverageTickMilliseconds:0.###} ms"
        ];

        AppendMapMetadata(lines, snapshot.MapId);
        return string.Join('\n', lines);
    }

    /**
      * Appends DBC-backed metadata as short chat-safe lines.
      */
    private void AppendMapMetadata(List<string> lines, int mapId)
    {
        if (!_mapData.TryGetMap(mapId, out MapDbcRecord map))
        {
            lines.Add($"DBC: MapId={mapId} is not present in Map.dbc.");
            return;
        }

        lines.Add($"Name: {map.DisplayName}");
        lines.Add($"Type: {map.Type}");
        lines.Add($"Areas: {_mapData.GetAreasForMap(mapId).Count}");
        lines.Add($"Triggers: {_mapData.GetTriggersForMap(mapId).Count}");
        lines.Add($"Continents: {_mapData.GetContinentsForMap(mapId).Count}");
    }

    /**
      * Formats an online uptime counter while keeping offline and unknown values explicit.
      */
    private static string FormatUptime(MapServiceState state, DateTimeOffset startedUtc)
    {
        if (state != MapServiceState.Online)
        {
            return "offline";
        }

        if (startedUtc <= DateTimeOffset.UnixEpoch)
        {
            return "unknown";
        }

        TimeSpan uptime = DateTimeOffset.UtcNow - startedUtc;
        if (uptime < TimeSpan.Zero)
        {
            uptime = TimeSpan.Zero;
        }

        return FormatDuration(uptime);
    }

    /**
      * Formats a compact day/hour/minute/second duration for in-game chat output.
      */
    private static string FormatDuration(TimeSpan duration)
    {
        return duration.TotalDays >= 1
            ? $"{duration.Days}d {duration.Hours:D2}h {duration.Minutes:D2}m {duration.Seconds:D2}s"
            : $"{duration.Hours:D2}h {duration.Minutes:D2}m {duration.Seconds:D2}s";
    }
}
