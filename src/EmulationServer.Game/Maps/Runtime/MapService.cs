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

using System.Diagnostics;

using EmulationServer.Game.Data.Maps;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

/**
  * File overview: src/EmulationServer.Game/Maps/Runtime/MapService.cs
  * This file belongs to the map service runtime, grid ownership, service state transitions, and health reporting portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Game.Maps.Runtime;

/**
  * Owns the lifecycle, tick loop, status snapshots, and restart flow for one world map or instance service.
  * It encapsulates a focused runtime behavior so callers can use a small public API instead of duplicating workflow code.
  */
public sealed class MapService : IAsyncDisposable
{
    /**
      * Stores the owner server name dependency or runtime value for MapService.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly string _ownerServerName;
    /**
      * Stores the definition dependency or runtime value for MapService.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly MapServiceDefinition _definition;
    /**
      * Stores the sync root dependency or runtime value for MapService.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly object _syncRoot = new();
    /**
      * Stores the lifecycle lock dependency or runtime value for MapService.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    /**
      * Stores the grid manager dependency or runtime value for MapService.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly MapGridManager? _gridManager;
    /**
      * Stores the startup grids dependency or runtime value for MapService.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly IReadOnlyList<MapTileKey> _startupGrids;
    private readonly Func<MapServiceSnapshot, CancellationToken, Task>? _reportStatusAsync;

    /**
      * Stores the stop cancellation dependency or runtime value for MapService.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private CancellationTokenSource? _stopCancellation;
    /**
      * Stores the tick task dependency or runtime value for MapService.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private Task? _tickTask;
    /**
      * Stores the state dependency or runtime value for MapService.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private MapServiceState _state = MapServiceState.Offline;
    /**
      * Stores the tick dependency or runtime value for MapService.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private long _tick;
    /**
      * Stores the active players dependency or runtime value for MapService.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private int _activePlayers;
    /**
      * Stores the active grids dependency or runtime value for MapService.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private int _activeGrids;
    /**
      * Stores the last tick milliseconds dependency or runtime value for MapService.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private double _lastTickMilliseconds;
    /**
      * Stores the average tick milliseconds dependency or runtime value for MapService.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private double _averageTickMilliseconds;
    /**
      * Stores the started utc dependency or runtime value for MapService.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private DateTimeOffset _startedUtc;
    /**
      * Stores the last tick utc dependency or runtime value for MapService.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private DateTimeOffset _lastTickUtc;

    /**
      * Creates a new MapService instance and stores the dependencies required by the component.
      * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
      */
    public MapService(
        string ownerServerName,
        MapServiceDefinition definition,
        MapGridManager? gridManager = null,
        IReadOnlyList<MapTileKey>? startupGrids = null,
        Func<MapServiceSnapshot, CancellationToken, Task>? reportStatusAsync = null)
    {
        if (string.IsNullOrWhiteSpace(ownerServerName))
        {
            throw new ArgumentException("Owner server name is required.", nameof(ownerServerName));
        }

        definition.Validate();

        _ownerServerName = ownerServerName;
        _definition = definition;
        _gridManager = gridManager;
        _startupGrids = startupGrids ?? [];
        _reportStatusAsync = reportStatusAsync;
    }

    /**
      * Gets or stores the definition value used by MapService.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public MapServiceDefinition Definition => _definition;

    /**
      * Gets or stores the state value used by MapService.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
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

    /**
      * Starts the component and prepares the runtime state required before it can accept work.
      * The method is part of MapService and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
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

    /**
      * Performs an intentional shutdown path that drains work before moving the component offline.
      * The method is part of MapService and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
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

    /**
      * Restarts the component by shutting down active runtime state and bringing it back online.
      * The method is part of MapService and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async Task RestartAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            MapServiceState currentState = State;
            if (currentState == MapServiceState.Offline)
            {
                Logger.Write(LogType.WARNING, $"{FormatService()} restart requested while Offline. Starting service instead.", nameof(MapService));
                await StartCoreAsync(cancellationToken);
                return;
            }

            await SetStateAsync(MapServiceState.RestartRequested, "restart requested", cancellationToken);
            await SetStateAsync(MapServiceState.DrainingPlayers, "blocking new joins and draining active players", cancellationToken);
            await SetStateAsync(MapServiceState.SavingPlayers, "saving active player state", cancellationToken);
            await SetStateAsync(MapServiceState.UnloadingObjects, "despawning creatures, gameobjects, and active grids", cancellationToken);

            await StopTickLoopAsync(cancellationToken);
            _gridManager?.UnloadAllGrids("map restart");

            await SetStateAsync(MapServiceState.ReloadingData, "reloading map runtime data", cancellationToken);
            if (_gridManager is not null)
            {
                await _gridManager.InitializeAsync(_startupGrids, cancellationToken);
            }

            await SetStateAsync(MapServiceState.RespawningObjects, "respawning runtime objects", cancellationToken);
            StartTickLoop(cancellationToken);
            await SetStateAsync(MapServiceState.Online, "restart complete", cancellationToken);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    /**
      * Stops the component and releases runtime resources in a controlled order.
      * The method is part of MapService and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
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

    /**
      * Releases owned resources and ensures background work is stopped safely.
      * The method is part of MapService and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        _lifecycleLock.Dispose();
    }

    /**
      * Updates the stored value after validating that the new value is safe to use.
      * The method is part of MapService and keeps this workflow isolated from the caller.
      */
    public void SetActivePlayerCount(int activePlayers)
    {
        if (activePlayers < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(activePlayers), "Active player count cannot be negative.");
        }

        lock (_syncRoot)
        {
            _activePlayers = activePlayers;
        }
    }

    /**
      * Updates the stored value after validating that the new value is safe to use.
      * The method is part of MapService and keeps this workflow isolated from the caller.
      */
    public void SetActiveGridCount(int activeGrids)
    {
        if (activeGrids < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(activeGrids), "Active grid count cannot be negative.");
        }

        lock (_syncRoot)
        {
            _activeGrids = activeGrids;
        }
    }

    /**
      * Updates the stored value after validating that the new value is safe to use.
      * The method is part of MapService and keeps this workflow isolated from the caller.
      */
    public void SetRuntimeCounts(int activePlayers, int activeGrids)
    {
        if (activePlayers < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(activePlayers), "Active player count cannot be negative.");
        }

        if (activeGrids < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(activeGrids), "Active grid count cannot be negative.");
        }

        lock (_syncRoot)
        {
            _activePlayers = activePlayers;
            _activeGrids = activeGrids;
        }
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of MapService and keeps this workflow isolated from the caller.
      */
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

    /**
      * Starts the component and prepares the runtime state required before it can accept work.
      * The method is part of MapService and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task StartCoreAsync(CancellationToken cancellationToken)
    {
        MapServiceState currentState = State;
        if (currentState is MapServiceState.Online or MapServiceState.Starting)
        {
            Logger.Write(LogType.INFORMATION, $"{FormatService()} start requested but service is already {currentState}.", nameof(MapService));
            await PublishStatusAsync(cancellationToken);
            return;
        }

        _stopCancellation?.Dispose();
        _stopCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        lock (_syncRoot)
        {
            _tick = 0;
            _lastTickMilliseconds = 0;
            _averageTickMilliseconds = 0;
            _startedUtc = DateTimeOffset.UtcNow;
            _lastTickUtc = _startedUtc;
        }

        await SetStateAsync(MapServiceState.Starting, "registering map service and loading startup data", cancellationToken);

        if (_gridManager is not null)
        {
            await _gridManager.InitializeAsync(_startupGrids, _stopCancellation.Token);
        }

        StartTickLoop(cancellationToken);
        await SetStateAsync(MapServiceState.Online, "map service is online and accepting work", cancellationToken);
    }

    /**
      * Performs an intentional shutdown path that drains work before moving the component offline.
      * The method is part of MapService and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task ShutdownCoreAsync(string reason, CancellationToken cancellationToken)
    {
        MapServiceState currentState = State;
        if (currentState == MapServiceState.Offline)
        {
            Logger.Write(LogType.INFORMATION, $"{FormatService()} shutdown requested but service is already Offline.", nameof(MapService));
            await PublishStatusAsync(cancellationToken);
            return;
        }

        await SetStateAsync(MapServiceState.DrainingPlayers, "blocking new joins and draining active players", cancellationToken);
        await SetStateAsync(MapServiceState.SavingPlayers, "saving active player state", cancellationToken);
        await SetStateAsync(MapServiceState.UnloadingObjects, "despawning creatures, gameobjects, and active grids", cancellationToken);
        await StopCoreAsync(reason, cancellationToken);
    }

    /**
      * Stops the component and releases runtime resources in a controlled order.
      * The method is part of MapService and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
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
        _gridManager?.UnloadAllGrids(reason);
        await SetStateAsync(MapServiceState.Offline, "map service is offline", cancellationToken);
    }

    /**
      * Starts the component and prepares the runtime state required before it can accept work.
      * The method is part of MapService and keeps this workflow isolated from the caller.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private void StartTickLoop(CancellationToken cancellationToken)
    {
        if (_tickTask is not null && !_tickTask.IsCompleted)
        {
            return;
        }

        CancellationTokenSource stopCancellation = _stopCancellation ?? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _stopCancellation = stopCancellation;
        _tickTask = Task.Run(() => RunTickLoopAsync(stopCancellation.Token), CancellationToken.None);
    }

    /**
      * Stops the component and releases runtime resources in a controlled order.
      * The method is part of MapService and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
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
                Task completedTask = await Task.WhenAny(_tickTask, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken));
                if (completedTask == _tickTask)
                {
                    await _tickTask;
                }
                else
                {
                    Logger.Write(LogType.WARNING, $"Stopped waiting for {FormatService()} tick loop because shutdown timed out.", nameof(MapService));
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Expected during shutdown.
            }
        }

        stopCancellation?.Dispose();
        _stopCancellation = null;
        _tickTask = null;
    }

    /**
      * Runs the main loop for this component until cancellation or shutdown is requested.
      * The method is part of MapService and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task RunTickLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            using PeriodicTimer timer = new(_definition.TickInterval);

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                RunTick();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown.
        }
        catch (Exception exception)
        {
            SetState(MapServiceState.Faulted);
            Logger.Write(LogType.CRITICAL, exception.ToString(), nameof(MapService));

            try
            {
                await PublishStatusAsync(CancellationToken.None);
            }
            catch (Exception publishException)
            {
                Logger.Write(LogType.WARNING, $"Could not publish faulted state for {FormatService()}: {publishException.Message}", nameof(MapService));
            }
        }
    }

    /**
      * Runs the main loop for this component until cancellation or shutdown is requested.
      * The method is part of MapService and keeps this workflow isolated from the caller.
      */
    private void RunTick()
    {
        long startTimestamp = Stopwatch.GetTimestamp();

        // This is the future hook for player movement, creature/gameobject updates,
        // visibility processing, and soft-restart state machines.
        _gridManager?.UnloadIdleGrids();
        Thread.Yield();

        double tickMilliseconds = GetElapsedMilliseconds(startTimestamp);
        long tick = Interlocked.Increment(ref _tick);

        lock (_syncRoot)
        {
            _lastTickMilliseconds = tickMilliseconds;
            _averageTickMilliseconds = _averageTickMilliseconds <= 0
                ? tickMilliseconds
                : (_averageTickMilliseconds * 0.90d) + (tickMilliseconds * 0.10d);
            _lastTickUtc = DateTimeOffset.UtcNow;
        }

        if (_definition.LogTicks)
        {
            Logger.Write(LogType.TRACE, $"{_ownerServerName} ticked map service '{_definition.Name}' (MapId={_definition.MapId}, Tick={tick}, TickTime={tickMilliseconds:0.###} ms).", nameof(MapService));
        }
    }

    /**
      * Updates the stored value after validating that the new value is safe to use.
      * The method is part of MapService and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task SetStateAsync(MapServiceState state, string reason, CancellationToken cancellationToken)
    {
        SetState(state);
        Logger.Write(LogType.NETWORK, $"{FormatService()} state changed to {state}. {reason}", nameof(MapService));
        await PublishStatusAsync(cancellationToken);
    }

    /**
      * Updates the stored value after validating that the new value is safe to use.
      * The method is part of MapService and keeps this workflow isolated from the caller.
      */
    private void SetState(MapServiceState state)
    {
        lock (_syncRoot)
        {
            _state = state;
        }
    }

    /**
      * Publishes the latest status snapshot so other services can observe this component.
      * The method is part of MapService and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task PublishStatusAsync(CancellationToken cancellationToken)
    {
        if (_reportStatusAsync is null)
        {
            return;
        }

        await _reportStatusAsync(GetSnapshot(), cancellationToken);
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of MapService and keeps this workflow isolated from the caller.
      */
    private double GetLoadPercent(double tickMilliseconds)
    {
        double intervalMilliseconds = _definition.TickInterval.TotalMilliseconds;
        if (intervalMilliseconds <= 0)
        {
            return 0;
        }

        return Math.Clamp((tickMilliseconds / intervalMilliseconds) * 100d, 0d, 100d);
    }

    /**
      * Formats runtime values into a stable human-readable message for logging or diagnostics.
      * The method is part of MapService and keeps this workflow isolated from the caller.
      */
    private string FormatService()
    {
        return $"{_ownerServerName} {_definition.Kind.ToString().ToLowerInvariant()} map service '{_definition.Name}' (MapId={_definition.MapId}, InstanceId={_definition.InstanceId})";
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of MapService and keeps this workflow isolated from the caller.
      */
    private static double GetElapsedMilliseconds(long startTimestamp)
    {
        long elapsedTicks = Stopwatch.GetTimestamp() - startTimestamp;
        double elapsedSeconds = elapsedTicks / (double)Stopwatch.Frequency;

        return TimeSpan.FromSeconds(elapsedSeconds).TotalMilliseconds;
    }
}
