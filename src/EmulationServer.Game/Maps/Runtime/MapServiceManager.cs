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
// File: src/EmulationServer.Game/Maps/Runtime/MapServiceManager.cs
// Purpose: Contains map service manager code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Game.Creatures;
using EmulationServer.Game.Data;
using EmulationServer.Game.Data.Dbc;
using EmulationServer.Game.Data.Dbc.Maps;
using EmulationServer.Game.GameObjects;
using EmulationServer.Game.WorldData;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.Shared.Timing;

namespace EmulationServer.Game.Maps.Runtime;

// Type: MapServiceManager
// Purpose: Provides map service manager behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class MapServiceManager : IAsyncDisposable
{

    // Field: Stores the owner server name state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current owner server name backing value maintained by the owning type.
    private readonly string _ownerServerName;

    // Field: Stores the settings state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current settings backing value maintained by the owning type.
    private readonly MapRuntimeSettings _settings;
    // Field: Stores the map service snapshot state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current map service snapshot backing value maintained by the owning type.
    private readonly Func<MapServiceSnapshot, CancellationToken, Task> _reportStatusAsync;
    // Field: Stores the int state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current int backing value maintained by the owning type.
    private readonly Func<int, CancellationToken, Task<IReadOnlyList<GameObjectSpawnRecord>>>? _loadGameObjectSpawnsAsync;
    // Field: Stores the uint state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current uint backing value maintained by the owning type.
    private readonly Func<uint, GameObjectTemplateRecord?>? _resolveGameObjectTemplate;
    // Field: Stores the int state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current int backing value maintained by the owning type.
    private readonly Func<int, CancellationToken, Task<IReadOnlyList<CreatureSpawnRecord>>>? _loadCreatureSpawnsAsync;
    // Field: Stores the uint state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current uint backing value maintained by the owning type.
    private readonly Func<uint, CreatureTemplateRecord?>? _resolveCreatureTemplate;
    // Field: Stores the clock state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current clock backing value maintained by the owning type.
    private readonly ISteadyClock _clock;

    // Field: Stores the services state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current services backing value maintained by the owning type.
    private readonly List<MapService> _services = [];
    // Field: Stores the string state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current string backing value maintained by the owning type.
    private readonly Dictionary<string, DbcDataStore> _dbcStores;

    // Field: Stores the map data state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current map data backing value maintained by the owning type.
    private readonly MapDbcDataStore _mapData;

    // Field: Stores the stop cancellation state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current stop cancellation backing value maintained by the owning type.
    private CancellationTokenSource? _stopCancellation;

    // Field: Stores the report task state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current report task backing value maintained by the owning type.
    private Task? _reportTask;

    // Field: Stores the started state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current started backing value maintained by the owning type.
    private int _started;

    // Field: Stores the stopping state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current stopping backing value maintained by the owning type.
    private int _stopping;

    // Constructor: MapServiceManager
    // Purpose: Initializes a new MapServiceManager instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // - settings: Settings values that control how this operation should run.
    // - reportStatusAsync: Report status async value supplied by the caller for this operation.
    // - loadGameObjectSpawnsAsync: Load game object spawns async value supplied by the caller for this operation.
    // - resolveGameObjectTemplate: Resolve game object template value supplied by the caller for this operation.
    // - loadCreatureSpawnsAsync: Load creature spawns async value supplied by the caller for this operation.
    // - resolveCreatureTemplate: Resolve creature template value supplied by the caller for this operation.
    // - clock: Clock value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapServiceManager so callers do not duplicate validation, protocol, or persistence rules.
    public MapServiceManager(
        string ownerServerName,
        MapRuntimeSettings settings,
        Func<MapServiceSnapshot, CancellationToken, Task> reportStatusAsync,
        Func<int, CancellationToken, Task<IReadOnlyList<GameObjectSpawnRecord>>>? loadGameObjectSpawnsAsync = null,
        Func<uint, GameObjectTemplateRecord?>? resolveGameObjectTemplate = null,
        Func<int, CancellationToken, Task<IReadOnlyList<CreatureSpawnRecord>>>? loadCreatureSpawnsAsync = null,
        Func<uint, CreatureTemplateRecord?>? resolveCreatureTemplate = null,
        ISteadyClock? clock = null)
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
        _loadGameObjectSpawnsAsync = loadGameObjectSpawnsAsync;
        _resolveGameObjectTemplate = resolveGameObjectTemplate;
        _loadCreatureSpawnsAsync = loadCreatureSpawnsAsync;
        _resolveCreatureTemplate = resolveCreatureTemplate;
        _clock = clock ?? SystemSteadyClock.Instance;

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

        string mapsDirectory = GameDataPathResolver.ResolveDirectory(settings.DataDirectory, settings.MapsDirectory);

        foreach (MapServiceDefinition configuredDefinition in settings.Services)
        {
            MapServiceDefinition definition = ApplyMapDbcMetadata(configuredDefinition);

            MapGridManager gridManager = new(
                definition,
                mapsDirectory);

            GameObjectMapRuntime? gameObjectRuntime = CreateGameObjectRuntime(definition);
            CreatureMapRuntime? creatureRuntime = CreateCreatureRuntime(definition);

            _services.Add(new MapService(ownerServerName, definition, gridManager, gameObjectRuntime, creatureRuntime, _reportStatusAsync, _clock));
        }
    }

    // Property: Gets or sets the services value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: services value exposed by the owning type.
    public IReadOnlyList<MapService> Services => _services;

    // Property: Gets or sets the string value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: string value exposed by the owning type.
    public IReadOnlyDictionary<string, DbcDataStore> DbcStores => _dbcStores;

    // Property: Gets or sets the map data value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: map data value exposed by the owning type.
    public MapDbcDataStore MapData => _mapData;

    // Method: CreateGameObjectRuntime
    // Purpose: Applies create game object runtime changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - definition: Definition value supplied by the caller for this operation.
    // Returns: Returns the game object map runtime? value produced by this operation.
    // Notes: This keeps the operation scoped to MapServiceManager so callers do not duplicate validation, protocol, or persistence rules.
    private GameObjectMapRuntime? CreateGameObjectRuntime(MapServiceDefinition definition)
    {
        if (_loadGameObjectSpawnsAsync is null || _resolveGameObjectTemplate is null)
        {
            return null;
        }

        return new GameObjectMapRuntime(
            definition.MapId,
            _loadGameObjectSpawnsAsync,
            _resolveGameObjectTemplate);
    }

    // Method: CreateCreatureRuntime
    // Purpose: Applies create creature runtime changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - definition: Definition value supplied by the caller for this operation.
    // Returns: Returns the creature map runtime? value produced by this operation.
    // Notes: This keeps the operation scoped to MapServiceManager so callers do not duplicate validation, protocol, or persistence rules.
    private CreatureMapRuntime? CreateCreatureRuntime(MapServiceDefinition definition)
    {
        if (_loadCreatureSpawnsAsync is null || _resolveCreatureTemplate is null)
        {
            return null;
        }

        return new CreatureMapRuntime(
            definition.MapId,
            _loadCreatureSpawnsAsync,
            _resolveCreatureTemplate);
    }

    // Method: GetSnapshots
    // Purpose: Retrieves get snapshots data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to MapServiceManager so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyList<MapServiceSnapshot> GetSnapshots()
    {
        return _services
            .Select(service => service.GetSnapshot())
            .ToArray();
    }

    // Method: GetSnapshots
    // Purpose: Retrieves get snapshots data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to MapServiceManager so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyList<MapServiceSnapshot> GetSnapshots(int mapId)
    {
        return _services
            .Where(service => service.Definition.MapId == mapId)
            .Select(service => service.GetSnapshot())
            .ToArray();
    }

    // Method: SetActivePlayerCounts
    // Purpose: Applies set active player counts changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - activePlayersByMap: Active players by map value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapServiceManager so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: StartAsync
    // Purpose: Controls the start lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MapServiceManager so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: StopAsync
    // Purpose: Controls the stop lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MapServiceManager so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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
                Task completedTask = await Task.WhenAny(_reportTask, _clock.DelayAsync(TimeSpan.FromSeconds(5), cancellationToken).AsTask());
                if (completedTask == _reportTask)
                {
                    await _reportTask;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {

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

    // Method: ExecuteControlCommandAsync
    // Purpose: Controls the execute control command lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - action: Action value supplied by the caller for this operation.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to MapServiceManager so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: DisposeAsync
    // Purpose: Controls the dispose lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MapServiceManager so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
    }

    // Method: ReportAllServicesAsync
    // Purpose: Executes the report all services operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MapServiceManager so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task ReportAllServicesAsync(CancellationToken cancellationToken)
    {
        foreach (MapService service in _services)
        {
            await _reportStatusAsync(service.GetSnapshot(), cancellationToken);
        }
    }

    // Method: ReportServicesAsync
    // Purpose: Executes the report services operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MapServiceManager so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task ReportServicesAsync(int mapId, CancellationToken cancellationToken)
    {
        foreach (MapService service in _services.Where(service => service.Definition.MapId == mapId))
        {
            await _reportStatusAsync(service.GetSnapshot(), cancellationToken);
        }
    }

    // Method: ReloadGameObjectsAsync
    // Purpose: Executes the reload game objects operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MapServiceManager so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task ReloadGameObjectsAsync(int mapId, CancellationToken cancellationToken)
    {
        foreach (MapService service in _services.Where(service => service.Definition.MapId == mapId))
        {
            await service.ReloadGameObjectsAsync(cancellationToken);
        }
    }

    // Method: ReloadCreaturesAsync
    // Purpose: Executes the reload creatures operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MapServiceManager so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task ReloadCreaturesAsync(int mapId, CancellationToken cancellationToken)
    {
        foreach (MapService service in _services.Where(service => service.Definition.MapId == mapId))
        {
            await service.ReloadCreaturesAsync(cancellationToken);
        }
    }

    // Method: ExecuteControlCommandAsync
    // Purpose: Controls the execute control command lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - service: Service value supplied by the caller for this operation.
    // - action: Action value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to MapServiceManager so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: ApplyMapDbcMetadata
    // Purpose: Applies apply map DBC metadata changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - definition: Definition value supplied by the caller for this operation.
    // Returns: Returns the map service definition value produced by this operation.
    // Notes: This keeps the operation scoped to MapServiceManager so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: GetServiceLifetimeToken
    // Purpose: Retrieves get service lifetime token data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - fallbackToken: Fallback token value supplied by the caller for this operation.
    // Returns: Returns the cancellation token value produced by this operation.
    // Notes: This keeps the operation scoped to MapServiceManager so callers do not duplicate validation, protocol, or persistence rules.
    private CancellationToken GetServiceLifetimeToken(CancellationToken fallbackToken)
    {
        return _stopCancellation?.Token ?? fallbackToken;
    }

    // Method: RunStatusReportLoopAsync
    // Purpose: Controls the run status report loop lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MapServiceManager so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task RunStatusReportLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await ReportAllServicesAsync(cancellationToken);
                await _clock.DelayAsync(_settings.StatusReportInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {

        }
        catch (Exception exception)
        {
            Logger.Write(LogType.CRITICAL, exception.ToString(), "MapServiceManager");
        }
    }

    // Method: GetDefaultServiceKind
    // Purpose: Retrieves get default service kind data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the map service kind value produced by this operation.
    // Notes: This keeps the operation scoped to MapServiceManager so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: FormatInfoMessage
    // Purpose: Executes the format info message operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - snapshot: Snapshot value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to MapServiceManager so callers do not duplicate validation, protocol, or persistence rules.
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
            $"GameObjects: {GetActiveGameObjectsForSnapshot(snapshot)}",
            $"Creatures: {GetActiveCreaturesForSnapshot(snapshot)}",
            $"Load: {snapshot.LoadPercent:0.##}%",
            $"Average Tick: {snapshot.AverageTickMilliseconds:0.###} ms"
        ];

        AppendMapMetadata(lines, snapshot.MapId);
        return string.Join('\n', lines);
    }

    // Method: GetActiveGameObjectsForSnapshot
    // Purpose: Retrieves get active game objects for snapshot data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - snapshot: Snapshot value supplied by the caller for this operation.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to MapServiceManager so callers do not duplicate validation, protocol, or persistence rules.
    private int GetActiveGameObjectsForSnapshot(MapServiceSnapshot snapshot)
    {
        return _services.FirstOrDefault(service =>
                service.Definition.MapId == snapshot.MapId &&
                service.Definition.InstanceId == snapshot.InstanceId &&
                service.Definition.Kind == snapshot.Kind)
            ?.ActiveGameObjectCount ?? 0;
    }

    // Method: GetActiveCreaturesForSnapshot
    // Purpose: Retrieves get active creatures for snapshot data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - snapshot: Snapshot value supplied by the caller for this operation.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to MapServiceManager so callers do not duplicate validation, protocol, or persistence rules.
    private int GetActiveCreaturesForSnapshot(MapServiceSnapshot snapshot)
    {
        return _services.FirstOrDefault(service =>
                service.Definition.MapId == snapshot.MapId &&
                service.Definition.InstanceId == snapshot.InstanceId &&
                service.Definition.Kind == snapshot.Kind)
            ?.ActiveCreatureCount ?? 0;
    }

    // Method: AppendMapMetadata
    // Purpose: Executes the append map metadata operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - lines: Lines value supplied by the caller for this operation.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapServiceManager so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: FormatUptime
    // Purpose: Executes the format uptime operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - state: State value supplied by the caller for this operation.
    // - startedUtc: Started utc value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to MapServiceManager so callers do not duplicate validation, protocol, or persistence rules.
    private string FormatUptime(MapServiceState state, DateTimeOffset startedUtc)
    {
        if (state != MapServiceState.Online)
        {
            return "offline";
        }

        if (startedUtc <= DateTimeOffset.UnixEpoch)
        {
            return "unknown";
        }

        TimeSpan uptime = _clock.UtcNow - startedUtc;
        if (uptime < TimeSpan.Zero)
        {
            uptime = TimeSpan.Zero;
        }

        return FormatDuration(uptime);
    }

    // Method: FormatDuration
    // Purpose: Executes the format duration operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - duration: Duration value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to MapServiceManager so callers do not duplicate validation, protocol, or persistence rules.
    private static string FormatDuration(TimeSpan duration)
    {
        return duration.TotalDays >= 1
            ? $"{duration.Days}d {duration.Hours:D2}h {duration.Minutes:D2}m {duration.Seconds:D2}s"
            : $"{duration.Hours:D2}h {duration.Minutes:D2}m {duration.Seconds:D2}s";
    }
}
