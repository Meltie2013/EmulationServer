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
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

/**
  * File overview: src/EmulationServer.Game/Maps/Runtime/MapServiceManager.cs
  * This file belongs to the map service runtime, grid ownership, service state transitions, and health reporting portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Game.Maps.Runtime;

/**
  * Coordinates all map and instance services hosted by a server process and routes control commands to the correct service.
  * It coordinates a collection of related runtime objects and keeps ownership rules in one place.
  */
public sealed class MapServiceManager : IAsyncDisposable
{
    /**
      * Stores the owner server name dependency or runtime value for MapServiceManager.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly string _ownerServerName;
    /**
      * Stores the settings dependency or runtime value for MapServiceManager.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly MapRuntimeSettings _settings;
    private readonly Func<MapServiceSnapshot, CancellationToken, Task> _reportStatusAsync;
    /**
      * Stores the services dependency or runtime value for MapServiceManager.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly List<MapService> _services = [];
    private readonly Dictionary<string, DbcDataStore> _dbcStores;

    /**
      * Stores the stop cancellation dependency or runtime value for MapServiceManager.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private CancellationTokenSource? _stopCancellation;
    /**
      * Stores the report task dependency or runtime value for MapServiceManager.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private Task? _reportTask;
    /**
      * Stores the started dependency or runtime value for MapServiceManager.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private int _started;
    /**
      * Stores the stopping dependency or runtime value for MapServiceManager.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private int _stopping;

    /**
      * Creates a new MapServiceManager instance and stores the dependencies required by the component.
      * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
      */
    public MapServiceManager(
        string ownerServerName,
        MapRuntimeSettings settings,
        Func<MapServiceSnapshot, CancellationToken, Task> reportStatusAsync)
    {
        if (string.IsNullOrWhiteSpace(ownerServerName))
        {
            throw new ArgumentException("Owner server name is required.", nameof(ownerServerName));
        }

        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _ownerServerName = ownerServerName;
        _settings = settings;
        _reportStatusAsync = reportStatusAsync ?? throw new ArgumentNullException(nameof(reportStatusAsync));

        if (!settings.Enabled)
        {
            _dbcStores = new Dictionary<string, DbcDataStore>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        _dbcStores = settings.LoadDbcStores
            ? DbcStoreLoader.LoadRequiredStores(
                GameDataPathResolver.ResolveDirectory(settings.DataDirectory, settings.DbcDirectory),
                settings.RequiredDbcFiles,
                ownerServerName)
            : new Dictionary<string, DbcDataStore>(StringComparer.OrdinalIgnoreCase);

        string? mapsDirectory = settings.LoadMapTiles
            ? GameDataPathResolver.ResolveDirectory(settings.DataDirectory, settings.MapsDirectory)
            : null;

        foreach (MapServiceDefinition definition in settings.Services)
        {
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
      * Gets or stores the services value used by MapServiceManager.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public IReadOnlyList<MapService> Services => _services;

    public IReadOnlyDictionary<string, DbcDataStore> DbcStores => _dbcStores;

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of MapServiceManager and keeps this workflow isolated from the caller.
      */
    public IReadOnlyList<MapServiceSnapshot> GetSnapshots()
    {
        return _services
            .Select(service => service.GetSnapshot())
            .ToArray();
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of MapServiceManager and keeps this workflow isolated from the caller.
      */
    public IReadOnlyList<MapServiceSnapshot> GetSnapshots(int mapId)
    {
        return _services
            .Where(service => service.Definition.MapId == mapId)
            .Select(service => service.GetSnapshot())
            .ToArray();
    }

    /**
      * Starts the component and prepares the runtime state required before it can accept work.
      * The method is part of MapServiceManager and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _started, 1) == 1)
        {
            throw new InvalidOperationException($"{_ownerServerName} map service manager has already been started.");
        }

        if (!_settings.Enabled)
        {
            Logger.Write(LogType.INFORMATION, $"{_ownerServerName} map services are disabled by configuration.", nameof(MapServiceManager));
            return;
        }

        _stopCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        foreach (MapService service in _services)
        {
            await service.StartAsync(_stopCancellation.Token);
        }

        _reportTask = Task.Run(() => RunStatusReportLoopAsync(_stopCancellation.Token), CancellationToken.None);

        Logger.Write(LogType.SUCCESS, $"{_ownerServerName} map service manager started with {_services.Count} service(s).", nameof(MapServiceManager));
    }

    /**
      * Stops the component and releases runtime resources in a controlled order.
      * The method is part of MapServiceManager and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
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
            Logger.Write(LogType.NETWORK, $"{_ownerServerName} map service manager stopped.", nameof(MapServiceManager));
        }
    }

    /**
      * Executes the requested command after parsing and validation are complete.
      * The method is part of MapServiceManager and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    public async Task<IReadOnlyList<MapServiceControlResult>> ExecuteControlCommandAsync(
        MapServiceControlAction action,
        int mapId,
        CancellationToken cancellationToken)
    {
        if (mapId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mapId), "Map ID cannot be negative.");
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
                $"{_ownerServerName} has no configured map service for MapId={mapId}.")];
        }

        List<MapServiceControlResult> results = [];
        foreach (MapService service in matchingServices)
        {
            results.Add(await ExecuteControlCommandAsync(service, action, cancellationToken));
        }

        return results;
    }

    /**
      * Releases owned resources and ensures background work is stopped safely.
      * The method is part of MapServiceManager and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
    }

    /**
      * Executes the requested command after parsing and validation are complete.
      * The method is part of MapServiceManager and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
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
                        $"Started map service '{service.Definition.Name}'.");

                case MapServiceControlAction.Shutdown:
                    await service.ShutdownAsync(cancellationToken);
                    return MapServiceControlResult.FromSnapshot(
                        service.GetSnapshot(),
                        MapServiceControlResultCode.Success,
                        $"Shutdown map service '{service.Definition.Name}'.");

                case MapServiceControlAction.Restart:
                    await service.RestartAsync(GetServiceLifetimeToken(cancellationToken));
                    return MapServiceControlResult.FromSnapshot(
                        service.GetSnapshot(),
                        MapServiceControlResultCode.Success,
                        $"Restarted map service '{service.Definition.Name}'.");

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
            Logger.Write(LogType.WARNING, $"{_ownerServerName} failed to execute {action} for map service '{service.Definition.Name}': {exception.Message}", nameof(MapServiceManager));

            return MapServiceControlResult.FromSnapshot(
                snapshot,
                MapServiceControlResultCode.Failed,
                exception.Message);
        }
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of MapServiceManager and keeps this workflow isolated from the caller.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private CancellationToken GetServiceLifetimeToken(CancellationToken fallbackToken)
    {
        return _stopCancellation?.Token ?? fallbackToken;
    }

    /**
      * Runs the main loop for this component until cancellation or shutdown is requested.
      * The method is part of MapServiceManager and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
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
            Logger.Write(LogType.CRITICAL, exception.ToString(), nameof(MapServiceManager));
        }
    }

    /**
      * Performs the report all services async operation for MapServiceManager.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async Task ReportAllServicesAsync(CancellationToken cancellationToken)
    {
        foreach (MapService service in _services)
        {
            await _reportStatusAsync(service.GetSnapshot(), cancellationToken);
        }
    }

    /**
      * Performs the report services async operation for MapServiceManager.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async Task ReportServicesAsync(int mapId, CancellationToken cancellationToken)
    {
        foreach (MapService service in _services.Where(service => service.Definition.MapId == mapId))
        {
            await _reportStatusAsync(service.GetSnapshot(), cancellationToken);
        }
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of MapServiceManager and keeps this workflow isolated from the caller.
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
      * Formats runtime values into a stable human-readable message for logging or diagnostics.
      * The method is part of MapServiceManager and keeps this workflow isolated from the caller.
      */
    private static string FormatInfoMessage(MapServiceSnapshot snapshot)
    {
        return $"{snapshot.OwnerServerName} {snapshot.Kind} map service '{snapshot.Name}' is {snapshot.State}: map={snapshot.MapId}, instance={snapshot.InstanceId}, tick={snapshot.Tick}, players={snapshot.ActivePlayers}, grids={snapshot.ActiveGrids}, load={snapshot.LoadPercent:0.##}%, avgTick={snapshot.AverageTickMilliseconds:0.###} ms.";
    }
}
