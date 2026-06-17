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
// File: src/EmulationServer.Game/Maps/Runtime/MapService.cs
// Purpose: Contains map service code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Game.Creatures;
using EmulationServer.Game.Data.Maps;
using EmulationServer.Game.GameObjects;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.Shared.Timing;

namespace EmulationServer.Game.Maps.Runtime;

// Type: MapService
// Purpose: Provides map service behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class MapService : IAsyncDisposable
{

    // Field: Stores the owner server name state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current owner server name backing value maintained by the owning type.
    private readonly string _ownerServerName;

    // Field: Stores the definition state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current definition backing value maintained by the owning type.
    private readonly MapServiceDefinition _definition;

    private readonly object _syncRoot = new();

    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);

    // Field: Stores the grid manager state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current grid manager backing value maintained by the owning type.
    private readonly MapGridManager? _gridManager;
    // Field: Stores the game object runtime state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current game object runtime backing value maintained by the owning type.
    private readonly GameObjectMapRuntime? _gameObjectRuntime;
    // Field: Stores the creature runtime state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current creature runtime backing value maintained by the owning type.
    private readonly CreatureMapRuntime? _creatureRuntime;
    // Field: Stores the map service snapshot state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current map service snapshot backing value maintained by the owning type.
    private readonly Func<MapServiceSnapshot, CancellationToken, Task>? _reportStatusAsync;
    // Field: Stores the clock state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current clock backing value maintained by the owning type.
    private readonly ISteadyClock _clock;

    // Field: Stores the stop cancellation state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current stop cancellation backing value maintained by the owning type.
    private CancellationTokenSource? _stopCancellation;

    // Field: Stores the tick task state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current tick task backing value maintained by the owning type.
    private Task? _tickTask;

    // Field: Stores the state state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current state backing value maintained by the owning type.
    private MapServiceState _state = MapServiceState.Offline;

    // Field: Stores the tick state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current tick backing value maintained by the owning type.
    private long _tick;

    // Field: Stores the active players state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current active players backing value maintained by the owning type.
    private int _activePlayers;

    // Field: Stores the active grids state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current active grids backing value maintained by the owning type.
    private int _activeGrids;

    // Field: Stores the last tick milliseconds state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current last tick milliseconds backing value maintained by the owning type.
    private double _lastTickMilliseconds;

    // Field: Stores the average tick milliseconds state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current average tick milliseconds backing value maintained by the owning type.
    private double _averageTickMilliseconds;

    // Field: Stores the started utc state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current started utc backing value maintained by the owning type.
    private DateTimeOffset _startedUtc;

    // Field: Stores the last tick utc state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current last tick utc backing value maintained by the owning type.
    private DateTimeOffset _lastTickUtc;

    // Constructor: MapService
    // Purpose: Initializes a new MapService instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // - definition: Definition value supplied by the caller for this operation.
    // - gridManager: Grid manager value supplied by the caller for this operation.
    // - gameObjectRuntime: Game object runtime value supplied by the caller for this operation.
    // - creatureRuntime: Creature runtime value supplied by the caller for this operation.
    // - reportStatusAsync: Report status async value supplied by the caller for this operation.
    // - clock: Clock value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapService so callers do not duplicate validation, protocol, or persistence rules.
    public MapService(
        string ownerServerName,
        MapServiceDefinition definition,
        MapGridManager? gridManager = null,
        GameObjectMapRuntime? gameObjectRuntime = null,
        CreatureMapRuntime? creatureRuntime = null,
        Func<MapServiceSnapshot, CancellationToken, Task>? reportStatusAsync = null,
        ISteadyClock? clock = null)
    {
        if (string.IsNullOrWhiteSpace(ownerServerName))
        {
            throw new ArgumentException("Owner server name is required.");
        }

        definition.Validate();

        _ownerServerName = ownerServerName;
        _definition = definition;
        _gridManager = gridManager;
        _gameObjectRuntime = gameObjectRuntime;
        _creatureRuntime = creatureRuntime;
        _reportStatusAsync = reportStatusAsync;
        _clock = clock ?? SystemSteadyClock.Instance;
    }

    // Property: Gets or sets the definition value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: definition value exposed by the owning type.
    public MapServiceDefinition Definition => _definition;

    public MapServiceState State
    {
        get
        {
            lock (_syncRoot)
            {
                return _state;
            }
        }
    }

    // Property: Gets or sets the active game object count value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: active game object count value exposed by the owning type.
    public int ActiveGameObjectCount => _gameObjectRuntime?.ActiveSpawnCount ?? 0;

    // Property: Gets or sets the active creature count value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: active creature count value exposed by the owning type.
    public int ActiveCreatureCount => _creatureRuntime?.ActiveSpawnCount ?? 0;

    // Method: ReloadGameObjectsAsync
    // Purpose: Executes the reload game objects operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MapService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task ReloadGameObjectsAsync(CancellationToken cancellationToken = default)
    {
        if (_gameObjectRuntime is null)
        {
            return;
        }

        await _lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            if (State == MapServiceState.Offline)
            {
                return;
            }

            await _gameObjectRuntime.LoadAsync(cancellationToken);
            await PublishStatusAsync(cancellationToken);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    // Method: ReloadCreaturesAsync
    // Purpose: Executes the reload creatures operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MapService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task ReloadCreaturesAsync(CancellationToken cancellationToken = default)
    {
        if (_creatureRuntime is null)
        {
            return;
        }

        await _lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            if (State == MapServiceState.Offline)
            {
                return;
            }

            await _creatureRuntime.LoadAsync(cancellationToken);
            await PublishStatusAsync(cancellationToken);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    // Method: StartAsync
    // Purpose: Controls the start lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MapService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            await StartCoreAsync(cancellationToken);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    // Method: ShutdownAsync
    // Purpose: Controls the shutdown lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MapService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            await ShutdownCoreAsync("shutdown requested", cancellationToken);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    // Method: RestartAsync
    // Purpose: Controls the restart lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MapService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task RestartAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            MapServiceState currentState = State;
            if (currentState == MapServiceState.Offline)
            {
                Logger.Write(LogType.WARNING, $"{FormatService()} restart requested while Offline. Starting service instead.", "MapService");
                await StartCoreAsync(cancellationToken);
                return;
            }

            await SetStateAsync(MapServiceState.RestartRequested, "restart requested", cancellationToken);
            await SetStateAsync(MapServiceState.DrainingPlayers, "blocking new joins and draining active players", cancellationToken);
            await SetStateAsync(MapServiceState.SavingPlayers, "saving active player state", cancellationToken);
            await SetStateAsync(MapServiceState.UnloadingObjects, "despawning creatures, gameobjects, and active grids", cancellationToken);

            await StopTickLoopAsync(cancellationToken);
            _creatureRuntime?.DespawnAll("map restart");
            _gameObjectRuntime?.DespawnAll("map restart");
            _gridManager?.UnloadAllGrids("map restart");

            await SetStateAsync(MapServiceState.ReloadingData, "reloading map runtime data", cancellationToken);
            if (_gridManager is not null)
            {
                await _gridManager.InitializeAsync(cancellationToken);
            }

            if (_creatureRuntime is not null)
            {
                await _creatureRuntime.LoadAsync(cancellationToken);
            }

            if (_gameObjectRuntime is not null)
            {
                await _gameObjectRuntime.LoadAsync(cancellationToken);
            }

            ResetRuntimeCounters(_clock.UtcNow);
            await SetStateAsync(MapServiceState.RespawningObjects, "respawning runtime objects", cancellationToken);
            StartTickLoop(cancellationToken);
            await SetStateAsync(MapServiceState.Online, "restart complete", cancellationToken);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    // Method: StopAsync
    // Purpose: Controls the stop lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MapService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            await StopCoreAsync("server shutdown", cancellationToken);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    // Method: DisposeAsync
    // Purpose: Controls the dispose lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MapService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        _lifecycleLock.Dispose();
    }

    // Method: SetActivePlayerCount
    // Purpose: Applies set active player count changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - activePlayers: Active players value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapService so callers do not duplicate validation, protocol, or persistence rules.
    public void SetActivePlayerCount(int activePlayers)
    {
        if (activePlayers < 0)
        {
            throw new ArgumentOutOfRangeException(null, "Active player count cannot be negative.");
        }

        lock (_syncRoot)
        {
            _activePlayers = activePlayers;
        }
    }

    // Method: SetActiveGridCount
    // Purpose: Applies set active grid count changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - activeGrids: Active grids value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapService so callers do not duplicate validation, protocol, or persistence rules.
    public void SetActiveGridCount(int activeGrids)
    {
        if (activeGrids < 0)
        {
            throw new ArgumentOutOfRangeException(null, "Active grid count cannot be negative.");
        }

        lock (_syncRoot)
        {
            _activeGrids = activeGrids;
        }
    }

    // Method: SetRuntimeCounts
    // Purpose: Applies set runtime counts changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - activePlayers: Active players value supplied by the caller for this operation.
    // - activeGrids: Active grids value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapService so callers do not duplicate validation, protocol, or persistence rules.
    public void SetRuntimeCounts(int activePlayers, int activeGrids)
    {
        if (activePlayers < 0)
        {
            throw new ArgumentOutOfRangeException(null, "Active player count cannot be negative.");
        }

        if (activeGrids < 0)
        {
            throw new ArgumentOutOfRangeException(null, "Active grid count cannot be negative.");
        }

        lock (_syncRoot)
        {
            _activePlayers = activePlayers;
            _activeGrids = activeGrids;
        }
    }

    // Method: GetSnapshot
    // Purpose: Retrieves get snapshot data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the map service snapshot value produced by this operation.
    // Notes: This keeps the operation scoped to MapService so callers do not duplicate validation, protocol, or persistence rules.
    public MapServiceSnapshot GetSnapshot()
    {
        lock (_syncRoot)
        {
            return new MapServiceSnapshot(
                _ownerServerName,
                _definition.Kind,
                _definition.MapId,
                _definition.InstanceId,
                _definition.Name,
                _state,
                _tick,
                _activePlayers,
                _gridManager?.LoadedGridCount ?? _activeGrids,
                _lastTickMilliseconds,
                _averageTickMilliseconds,
                GetLoadPercent(_lastTickMilliseconds),
                _startedUtc,
                _lastTickUtc);
        }
    }

    // Method: ResetRuntimeCounters
    // Purpose: Executes the reset runtime counters operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - startedUtc: Started utc value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapService so callers do not duplicate validation, protocol, or persistence rules.
    private void ResetRuntimeCounters(DateTimeOffset startedUtc)
    {
        lock (_syncRoot)
        {
            _tick = 0;
            _lastTickMilliseconds = 0;
            _averageTickMilliseconds = 0;
            _startedUtc = startedUtc;
            _lastTickUtc = startedUtc;
        }
    }

    // Method: StartCoreAsync
    // Purpose: Controls the start core lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MapService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task StartCoreAsync(CancellationToken cancellationToken)
    {
        MapServiceState currentState = State;
        if (currentState is MapServiceState.Online or MapServiceState.Starting)
        {
            Logger.Write(LogType.WARNING, $"{FormatService()} start requested but service is already {currentState}.", "MapService");
            await PublishStatusAsync(cancellationToken);
            return;
        }

        _stopCancellation?.Dispose();
        _stopCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        ResetRuntimeCounters(_clock.UtcNow);

        await SetStateAsync(MapServiceState.Starting, "registering map service and loading startup data", cancellationToken);

        if (_gridManager is not null)
        {
            await _gridManager.InitializeAsync(_stopCancellation.Token);
        }

        if (_creatureRuntime is not null)
        {
            await _creatureRuntime.LoadAsync(_stopCancellation.Token);
        }

        if (_gameObjectRuntime is not null)
        {
            await _gameObjectRuntime.LoadAsync(_stopCancellation.Token);
        }

        StartTickLoop(cancellationToken);
        await SetStateAsync(MapServiceState.Online, "map service is online and accepting work", cancellationToken);
    }

    // Method: ShutdownCoreAsync
    // Purpose: Controls the shutdown core lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - reason: Reason value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MapService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task ShutdownCoreAsync(string reason, CancellationToken cancellationToken)
    {
        MapServiceState currentState = State;
        if (currentState == MapServiceState.Offline)
        {
            Logger.Write(LogType.WARNING, $"{FormatService()} shutdown requested but service is already Offline.", "MapService");
            await PublishStatusAsync(cancellationToken);
            return;
        }

        await SetStateAsync(MapServiceState.DrainingPlayers, "blocking new joins and draining active players", cancellationToken);
        await SetStateAsync(MapServiceState.SavingPlayers, "saving active player state", cancellationToken);
        await SetStateAsync(MapServiceState.UnloadingObjects, "despawning creatures, gameobjects, and active grids", cancellationToken);
        await StopCoreAsync(reason, cancellationToken);
    }

    // Method: StopCoreAsync
    // Purpose: Controls the stop core lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - reason: Reason value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MapService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task StopCoreAsync(string reason, CancellationToken cancellationToken)
    {
        MapServiceState currentState = State;
        if (currentState == MapServiceState.Offline)
        {
            await PublishStatusAsync(cancellationToken);
            return;
        }

        await SetStateAsync(MapServiceState.Stopping, reason, cancellationToken);
        await StopTickLoopAsync(cancellationToken);
        _creatureRuntime?.DespawnAll(reason);
        _gameObjectRuntime?.DespawnAll(reason);
        _gridManager?.UnloadAllGrids(reason);
        await SetStateAsync(MapServiceState.Offline, "map service is offline", cancellationToken);
    }

    // Method: StartTickLoop
    // Purpose: Controls the start tick loop lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapService so callers do not duplicate validation, protocol, or persistence rules.
    private void StartTickLoop(CancellationToken cancellationToken)
    {
        if (_tickTask is not null && !_tickTask.IsCompleted)
        {
            return;
        }

        CancellationTokenSource stopCancellation = _stopCancellation ?? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _stopCancellation = stopCancellation;
        _tickTask = Task.Run(() => RunTickLoopAsync(stopCancellation.Token), CancellationToken.None);

        Logger.Write(LogType.THREAD, $"{FormatService()} tick loop started with interval {_definition.TickInterval.TotalMilliseconds:0.##} ms.", "MapService");
    }

    // Method: StopTickLoopAsync
    // Purpose: Controls the stop tick loop lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MapService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task StopTickLoopAsync(CancellationToken cancellationToken)
    {
        CancellationTokenSource? stopCancellation = _stopCancellation;
        if (stopCancellation is not null)
        {
            await stopCancellation.CancelAsync();
        }

        if (_tickTask is not null)
        {
            try
            {
                Task completedTask = await Task.WhenAny(_tickTask, _clock.DelayAsync(TimeSpan.FromSeconds(5), cancellationToken).AsTask());
                if (completedTask == _tickTask)
                {
                    await _tickTask;
                }
                else
                {
                    Logger.Write(LogType.WARNING, $"Stopped waiting for {FormatService()} tick loop because shutdown timed out.", "MapService");
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {

            }
        }

        stopCancellation?.Dispose();
        _stopCancellation = null;
        _tickTask = null;

        Logger.Write(LogType.THREAD, $"{FormatService()} tick loop stopped.", "MapService");
    }

    // Method: RunTickLoopAsync
    // Purpose: Controls the run tick loop lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MapService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task RunTickLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            long nextTickTimestamp = _clock.Add(_clock.Timestamp, _definition.TickInterval);

            while (!cancellationToken.IsCancellationRequested)
            {
                await _clock.DelayUntilAsync(nextTickTimestamp, cancellationToken);
                RunTick();

                nextTickTimestamp = _clock.Add(nextTickTimestamp, _definition.TickInterval);
                if (_clock.GetElapsedTime(_clock.Timestamp, nextTickTimestamp) <= TimeSpan.Zero)
                {
                    nextTickTimestamp = _clock.Add(_clock.Timestamp, _definition.TickInterval);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {

        }
        catch (Exception exception)
        {
            SetState(MapServiceState.Faulted);
            Logger.Write(LogType.CRITICAL, exception.ToString(), "MapService");

            try
            {
                await PublishStatusAsync(CancellationToken.None);
            }
            catch (Exception publishException)
            {
                Logger.Write(LogType.WARNING, $"Could not publish faulted state for {FormatService()}: {publishException.Message}", "MapService");
            }
        }
    }

    // Method: RunTick
    // Purpose: Controls the run tick lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapService so callers do not duplicate validation, protocol, or persistence rules.
    private void RunTick()
    {
        long startTimestamp = _clock.Timestamp;

        _gridManager?.UnloadIdleGrids();
        Thread.Yield();

        double tickMilliseconds = _clock.GetElapsedTime(startTimestamp).TotalMilliseconds;
        long tick = Interlocked.Increment(ref _tick);

        lock (_syncRoot)
        {
            _lastTickMilliseconds = tickMilliseconds;
            _averageTickMilliseconds = _averageTickMilliseconds <= 0
                ? tickMilliseconds
                : (_averageTickMilliseconds * 0.90d) + (tickMilliseconds * 0.10d);
            _lastTickUtc = _clock.UtcNow;
        }

        if (_definition.LogTicks)
        {
            Logger.Write(LogType.TRACE, $"{_ownerServerName} ticked map service '{_definition.Name}' (MapId={_definition.MapId}, Tick={tick}, TickTime={tickMilliseconds:0.###} ms).", "MapService");
        }
    }

    // Method: SetStateAsync
    // Purpose: Applies set state changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - state: State value supplied by the caller for this operation.
    // - reason: Reason value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MapService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task SetStateAsync(MapServiceState state, string reason, CancellationToken cancellationToken)
    {
        SetState(state);
        Logger.Write(LogType.SYSTEM, $"{FormatService()} state changed to {state}. {reason}", "MapService");
        await PublishStatusAsync(cancellationToken);
    }

    // Method: SetState
    // Purpose: Applies set state changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - state: State value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapService so callers do not duplicate validation, protocol, or persistence rules.
    private void SetState(MapServiceState state)
    {
        lock (_syncRoot)
        {
            _state = state;
        }
    }

    // Method: PublishStatusAsync
    // Purpose: Executes the publish status operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MapService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task PublishStatusAsync(CancellationToken cancellationToken)
    {
        if (_reportStatusAsync is null)
        {
            return;
        }

        await _reportStatusAsync(GetSnapshot(), cancellationToken);
    }

    // Method: GetLoadPercent
    // Purpose: Retrieves get load percent data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - tickMilliseconds: Tick milliseconds value supplied by the caller for this operation.
    // Returns: Returns the double value produced by this operation.
    // Notes: This keeps the operation scoped to MapService so callers do not duplicate validation, protocol, or persistence rules.
    private double GetLoadPercent(double tickMilliseconds)
    {
        double intervalMilliseconds = _definition.TickInterval.TotalMilliseconds;
        if (intervalMilliseconds <= 0)
        {
            return 0;
        }

        return Math.Clamp((tickMilliseconds / intervalMilliseconds) * 100d, 0d, 100d);
    }

    // Method: FormatService
    // Purpose: Executes the format service operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to MapService so callers do not duplicate validation, protocol, or persistence rules.
    private string FormatService()
    {
        return $"{_ownerServerName} {_definition.Kind.ToString().ToLowerInvariant()} map service '{_definition.Name}' (MapId={_definition.MapId}, InstanceId={_definition.InstanceId})";
    }
}
