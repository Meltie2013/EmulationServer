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
// File: src/InstanceServer/Core/InstanceServer.cs
// Purpose: Contains instance server code for the instance server runtime, dungeon-map ownership, and internal-service coordination.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using EmulationServer.Core.Servers;
using EmulationServer.Game.Creatures;
using EmulationServer.Game.GameObjects;
using EmulationServer.Game.Maps.Runtime;
using EmulationServer.InstanceServer.Configuration;
using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Peers;
using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Network.Networking.Sessions;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.InstanceServer.Core;

// Type: InstanceServer
// Purpose: Provides instance server behavior for the instance server runtime, dungeon-map ownership, and internal-service coordination.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class InstanceServer : IAsyncDisposable
{
    // Field: Stores the required runtime internal servers state used by the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Value: current required runtime internal servers backing value maintained by the owning type.
    private static readonly string[] RequiredRuntimeInternalServers = ["ProxyServer", "WorldServer"];

    // Field: Stores the host state used by the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Value: current host backing value maintained by the owning type.
    private readonly EmulationServerHost _host;

    // Field: Stores the settings state used by the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Value: current settings backing value maintained by the owning type.
    private readonly InstanceServerSettings _settings;

    // Field: Stores the instance services state used by the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Value: current instance services backing value maintained by the owning type.
    private MapServiceManager? _instanceServices;
    private readonly ConcurrentDictionary<string, InternalPeerConnection> _peerConnections = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, InternalServerSession> _serverSessions = new(StringComparer.OrdinalIgnoreCase);

    private readonly MapPlayerTracker _playerTracker = new();
    private readonly GameObjectSnapshotStore _gameObjectSnapshots = new("InstanceServer");
    private readonly CreatureSnapshotStore _creatureSnapshots = new("InstanceServer");

    // Field: Stores the world capacity limit state used by the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Value: current world capacity limit backing value maintained by the owning type.
    private int _worldCapacityLimit;

    // Constructor: InstanceServer
    // Purpose: Initializes a new InstanceServer instance with dependencies and values required by the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - settings: Settings values that control how this operation should run.
    // Returns: none.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    public InstanceServer(InstanceServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _settings = settings;
        _host = new EmulationServerHost("InstanceServer", settings.InternalNetwork, CreateCallbacks());
    }

    // Method: StartAsync
    // Purpose: Controls the start lifecycle step for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Task hostTask = _host.StartAsync(cancellationToken);

        try
        {
            await _host.StartupCompleted.WaitAsync(cancellationToken);

            await _host.WaitForInternalServersAsync(
                RequiredRuntimeInternalServers,
                "InstanceServer will keep instance services offline until ProxyServer and WorldServer are online.",
                cancellationToken);

            MapServiceManager serviceManager = CreateInstanceServiceManager();
            _instanceServices = serviceManager;
            await serviceManager.StartAsync(cancellationToken);

            await hostTask;
        }
        finally
        {
            if (_instanceServices is not null)
            {
                await _instanceServices.StopAsync(CancellationToken.None);
            }
        }
    }

    // Method: StopAsync
    // Purpose: Controls the stop lifecycle step for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_instanceServices is not null)
        {
            await _instanceServices.StopAsync(cancellationToken);
        }

        await _host.StopAsync(cancellationToken);
    }

    // Method: DisposeAsync
    // Purpose: Controls the dispose lifecycle step for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters: none.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);

        if (_instanceServices is not null)
        {
            await _instanceServices.DisposeAsync();
        }

        await _host.DisposeAsync();
    }

    // Method: CreateInstanceServiceManager
    // Purpose: Applies create instance service manager changes for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters: none.
    // Returns: Returns the map service manager value produced by this operation.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    private MapServiceManager CreateInstanceServiceManager()
    {
        return new MapServiceManager(
            "InstanceServer",
            _settings.InstanceServices,
            ReportInstanceServiceStatusAsync,
            (mapId, _) => Task.FromResult(_gameObjectSnapshots.GetSpawnsForMap(mapId)),
            entry => _gameObjectSnapshots.GetTemplateOrDefault(entry),
            (mapId, _) => Task.FromResult(_creatureSnapshots.GetSpawnsForMap(mapId)),
            entry => _creatureSnapshots.GetTemplateOrDefault(entry));
    }

    // Method: CreateCallbacks
    // Purpose: Applies create callbacks changes for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters: none.
    // Returns: Returns the internal network callbacks value produced by this operation.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    private InternalNetworkCallbacks CreateCallbacks()
    {
        return new InternalNetworkCallbacks
        {
            ServerAuthenticatedAsync = OnServerAuthenticatedAsync,
            PacketReceivedAsync = OnSessionPacketReceivedAsync,
            ServerDisconnectedAsync = OnServerDisconnectedAsync,
            PeerAuthenticatedAsync = OnPeerAuthenticatedAsync,
            PeerPacketReceivedAsync = OnPeerPacketReceivedAsync,
            PeerDisconnectedAsync = OnPeerDisconnectedAsync,
        };
    }

    // Method: OnServerAuthenticatedAsync
    // Purpose: Executes the on server authenticated operation for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - session: Session value supplied by the caller for this operation.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task OnServerAuthenticatedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        _serverSessions[remoteServerName] = session;

        Logger.Write(LogType.NETWORK, $"InstanceServer registered incoming instance-service control/status session '{remoteServerName}'.", "InstanceServer");
        await SendInstanceServiceStatusesToSessionAsync(session, cancellationToken);
    }

    // Method: OnServerDisconnectedAsync
    // Purpose: Executes the on server disconnected operation for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - session: Session value supplied by the caller for this operation.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private Task OnServerDisconnectedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        _serverSessions.TryRemove(remoteServerName, out _);
        Logger.Write(LogType.NETWORK, $"InstanceServer removed incoming instance-service control/status session '{remoteServerName}'.", "InstanceServer");

        return Task.CompletedTask;
    }

    // Method: OnPeerAuthenticatedAsync
    // Purpose: Executes the on peer authenticated operation for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task OnPeerAuthenticatedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        _peerConnections[remoteServerName] = connection;

        Logger.Write(LogType.NETWORK, $"InstanceServer registered outgoing instance-service status peer '{remoteServerName}'.", "InstanceServer");
        await SendInstanceServiceStatusesToPeerAsync(connection, cancellationToken);
    }

    // Method: OnPeerDisconnectedAsync
    // Purpose: Executes the on peer disconnected operation for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private Task OnPeerDisconnectedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        _peerConnections.TryRemove(remoteServerName, out _);
        Logger.Write(LogType.NETWORK, $"InstanceServer removed outgoing instance-service status peer '{remoteServerName}'.", "InstanceServer");

        return Task.CompletedTask;
    }

    // Method: OnPeerPacketReceivedAsync
    // Purpose: Executes the on peer packet received operation for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private Task OnPeerPacketReceivedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        string packet,
        CancellationToken cancellationToken)
    {
        return HandleInternalPacketAsync(
            remoteServerName,
            packet,
            responsePacket => connection.SendPacketAsync(responsePacket, cancellationToken),
            cancellationToken);
    }

    // Method: OnSessionPacketReceivedAsync
    // Purpose: Executes the on session packet received operation for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - session: Session value supplied by the caller for this operation.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private Task OnSessionPacketReceivedAsync(
        InternalServerSession session,
        string remoteServerName,
        string packet,
        CancellationToken cancellationToken)
    {
        return HandleInternalPacketAsync(
            remoteServerName,
            packet,
            responsePacket => session.SendPacketAsync(responsePacket, cancellationToken),
            cancellationToken);
    }

    // Method: HandleInternalPacketAsync
    // Purpose: Handles handle internal packet work for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - sendResponseAsync: Send response async value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task HandleInternalPacketAsync(
        string remoteServerName,
        string packet,
        Func<string, Task> sendResponseAsync,
        CancellationToken cancellationToken)
    {
        if (await HandleGameObjectSnapshotPacketAsync(remoteServerName, packet, cancellationToken))
        {
            return;
        }

        if (await HandleCreatureSnapshotPacketAsync(remoteServerName, packet, cancellationToken))
        {
            return;
        }

        if (InternalMapServiceCommandPacket.TryParse(packet, out InternalMapServiceCommandPacket command))
        {
            await HandleMapServiceCommandAsync(remoteServerName, command, sendResponseAsync, cancellationToken);
            return;
        }

        if (await HandlePlayerRoutingPacketAsync(remoteServerName, packet, cancellationToken))
        {
            return;
        }

        string[] parts = packet.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return;
        }

        if (!string.Equals(parts[0], InternalProtocol.WorldCapacity, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        int capacityIndex = parts.Length == 3 ? 2 : 1;
        if (parts.Length is not (2 or 3) || !int.TryParse(parts[capacityIndex], NumberStyles.Integer, CultureInfo.InvariantCulture, out int capacityLimit) || capacityLimit <= 0)
        {
            Logger.Write(LogType.WARNING, $"InstanceServer received invalid WorldServer capacity packet from {remoteServerName}: {packet}", "InstanceServer");
            return;
        }

        string capacitySource = parts.Length == 3 ? parts[1] : remoteServerName;
        Volatile.Write(ref _worldCapacityLimit, capacityLimit);
        Logger.Write(LogType.NETWORK, $"InstanceServer received WorldServer capacity limit from {remoteServerName}: {capacitySource}={capacityLimit}.", "InstanceServer");
    }

    // Method: HandleGameObjectSnapshotPacketAsync
    // Purpose: Handles handle game object snapshot packet work for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous Boolean result that is true when handle game object snapshot packet async succeeds or the requested condition is met.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task<bool> HandleGameObjectSnapshotPacketAsync(string remoteServerName, string packet, CancellationToken cancellationToken)
    {
        if (!_gameObjectSnapshots.TryHandleSnapshotPacket(remoteServerName, packet, out GameObjectSnapshotApplyResult result))
        {
            return false;
        }

        if (result.Completed)
        {
            MapServiceManager? instanceServices = _instanceServices;
            if (instanceServices is not null)
            {
                await instanceServices.ReloadGameObjectsAsync(result.MapId, cancellationToken);
                await instanceServices.ReportServicesAsync(result.MapId, cancellationToken);
            }

            Logger.Write(LogType.SYSTEM, $"InstanceServer refreshed gameobject runtime for MapId={result.MapId} from WorldServer snapshot: templates={result.TemplateCount}, spawns={result.SpawnCount}.", "InstanceServer");
        }

        return true;
    }

    // Method: HandleCreatureSnapshotPacketAsync
    // Purpose: Handles handle creature snapshot packet work for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous Boolean result that is true when handle creature snapshot packet async succeeds or the requested condition is met.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task<bool> HandleCreatureSnapshotPacketAsync(string remoteServerName, string packet, CancellationToken cancellationToken)
    {
        if (!_creatureSnapshots.TryHandleSnapshotPacket(remoteServerName, packet, out CreatureSnapshotApplyResult result))
        {
            return false;
        }

        if (result.Completed)
        {
            MapServiceManager? instanceServices = _instanceServices;
            if (instanceServices is not null)
            {
                await instanceServices.ReloadCreaturesAsync(result.MapId, cancellationToken);
                await instanceServices.ReportServicesAsync(result.MapId, cancellationToken);
            }

            Logger.Write(LogType.SYSTEM, $"InstanceServer refreshed creature runtime for MapId={result.MapId} from WorldServer snapshot: templates={result.TemplateCount}, spawns={result.SpawnCount}.", "InstanceServer");
        }

        return true;
    }

    // Method: HandlePlayerRoutingPacketAsync
    // Purpose: Handles handle player routing packet work for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous Boolean result that is true when handle player routing packet async succeeds or the requested condition is met.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task<bool> HandlePlayerRoutingPacketAsync(string remoteServerName, string packet, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(packet))
        {
            return false;
        }

        string[] parts = packet.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        if (string.Equals(parts[0], InternalProtocol.PlayerEnterWorld, StringComparison.OrdinalIgnoreCase))
        {
            if (TryReadPlayerEnterRoute(parts, out MapPlayerRuntimeState? state))
            {
                _playerTracker.PlayerEntered(state);
                await RefreshInstanceServicePlayerCountsAsync(cancellationToken, state.Map);
                MapPlayerRuntimeLogger.LogPlayerEntered("InstanceServer", remoteServerName, state, _playerTracker.ActivePlayerCount);
            }
            else
            {
                Logger.Write(LogType.WARNING, $"InstanceServer received invalid player enter-world route from {remoteServerName}: {packet}", "InstanceServer");
            }

            return true;
        }

        if (string.Equals(parts[0], InternalProtocol.PlayerLeaveWorld, StringComparison.OrdinalIgnoreCase))
        {
            uint guid = parts.Length > 2 && uint.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint parsedGuid)
                ? parsedGuid
                : 0;

            if (guid != 0 && _playerTracker.PlayerLeft(guid, out MapPlayerRuntimeState? removedPlayer) && removedPlayer is not null)
            {
                await RefreshInstanceServicePlayerCountsAsync(cancellationToken, removedPlayer.Map);
                MapPlayerRuntimeLogger.LogPlayerLeft("InstanceServer", remoteServerName, removedPlayer, _playerTracker.ActivePlayerCount);
            }

            return true;
        }

        if (string.Equals(parts[0], InternalProtocol.PlayerMovement, StringComparison.OrdinalIgnoreCase))
        {
            if (TryReadPlayerMovementRoute(parts, out uint accountId, out uint guid, out ushort opcode, out uint map, out uint zone, out float x, out float y, out float z, out float orientation, out uint flags, out uint clientTime))
            {
                _playerTracker.TryGetPlayer(guid, out MapPlayerRuntimeState? previousState);
                MapPlayerRuntimeState state = _playerTracker.PlayerMoved(accountId, guid, map, zone, x, y, z, orientation, opcode, flags, clientTime, out uint previousMap, out bool serviceCountChanged);
                if (serviceCountChanged)
                {
                    await RefreshInstanceServicePlayerCountsAsync(cancellationToken, previousMap, state.Map);
                }

                MapPlayerRuntimeLogger.LogPlayerTransition("InstanceServer", remoteServerName, previousState, state, _playerTracker.ActivePlayerCount);
            }
            else
            {
                Logger.Write(LogType.WARNING, $"InstanceServer received invalid player movement route from {remoteServerName}: {packet}", "InstanceServer");
            }

            return true;
        }

        if (string.Equals(parts[0], InternalProtocol.PlayerClientPacket, StringComparison.OrdinalIgnoreCase))
        {

            return true;
        }

        return false;
    }

    // Method: RefreshInstanceServicePlayerCountsAsync
    // Purpose: Executes the refresh instance service player counts operation for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // - uintaffectedMapIds: Uintaffected map ids value supplied by the caller for this operation.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task RefreshInstanceServicePlayerCountsAsync(CancellationToken cancellationToken, params uint[] affectedMapIds)
    {
        MapServiceManager? instanceServices = _instanceServices;
        if (instanceServices is null)
        {
            return;
        }

        instanceServices.SetActivePlayerCounts(_playerTracker.CountPlayersByMap());

        foreach (uint affectedMapId in affectedMapIds.Distinct())
        {
            if (affectedMapId <= int.MaxValue)
            {
                await instanceServices.ReportServicesAsync(unchecked((int)affectedMapId), cancellationToken);
            }
        }
    }

    // Method: TryReadPlayerEnterRoute
    // Purpose: Attempts to retrieve or parse try read player enter route data without treating normal misses as failures.
    // Parameters:
    // - stringparts: Stringparts value supplied by the caller for this operation.
    // - state: State value supplied by the caller for this operation.
    // Returns: Returns true when try read player enter route succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    private static bool TryReadPlayerEnterRoute(string[] parts, [NotNullWhen(true)] out MapPlayerRuntimeState? state)
    {
        state = null;
        if (parts.Length < 10 ||
            !uint.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint accountId) ||
            !uint.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint guid) ||
            !uint.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint map) ||
            !uint.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint zone) ||
            !float.TryParse(parts[6], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
            !float.TryParse(parts[7], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) ||
            !float.TryParse(parts[8], NumberStyles.Float, CultureInfo.InvariantCulture, out float z) ||
            !float.TryParse(parts[9], NumberStyles.Float, CultureInfo.InvariantCulture, out float orientation))
        {
            return false;
        }

        state = new MapPlayerRuntimeState(accountId, guid, parts[3], map, zone, x, y, z, orientation, 0, 0, 0, DateTimeOffset.UtcNow);
        return true;
    }

    // Method: TryReadPlayerMovementRoute
    // Purpose: Attempts to retrieve or parse try read player movement route data without treating normal misses as failures.
    // Parameters:
    // - stringparts: Stringparts value supplied by the caller for this operation.
    // - accountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // - guid: Guid identifier used to select the exact record, object, or runtime owner.
    // - opcode: Opcode value supplied by the caller for this operation.
    // - map: Map value supplied by the caller for this operation.
    // - zone: Zone value supplied by the caller for this operation.
    // - x: X value supplied by the caller for this operation.
    // - y: Y value supplied by the caller for this operation.
    // - z: Z value supplied by the caller for this operation.
    // - orientation: Orientation value supplied by the caller for this operation.
    // - flags: Flags value supplied by the caller for this operation.
    // - clientTime: Client time value supplied by the caller for this operation.
    // Returns: Returns true when try read player movement route succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    private static bool TryReadPlayerMovementRoute(
        string[] parts,
        out uint accountId,
        out uint guid,
        out ushort opcode,
        out uint map,
        out uint zone,
        out float x,
        out float y,
        out float z,
        out float orientation,
        out uint flags,
        out uint clientTime)
    {
        accountId = 0;
        guid = 0;
        opcode = 0;
        map = 0;
        zone = 0;
        x = 0;
        y = 0;
        z = 0;
        orientation = 0;
        flags = 0;
        clientTime = 0;

        return parts.Length >= 12 &&
            uint.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out accountId) &&
            uint.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out guid) &&
            TryParseOpcode(parts[3], out opcode) &&
            uint.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out map) &&
            uint.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out zone) &&
            float.TryParse(parts[6], NumberStyles.Float, CultureInfo.InvariantCulture, out x) &&
            float.TryParse(parts[7], NumberStyles.Float, CultureInfo.InvariantCulture, out y) &&
            float.TryParse(parts[8], NumberStyles.Float, CultureInfo.InvariantCulture, out z) &&
            float.TryParse(parts[9], NumberStyles.Float, CultureInfo.InvariantCulture, out orientation) &&
            uint.TryParse(parts[10], NumberStyles.Integer, CultureInfo.InvariantCulture, out flags) &&
            uint.TryParse(parts[11], NumberStyles.Integer, CultureInfo.InvariantCulture, out clientTime);
    }

    // Method: TryParseOpcode
    // Purpose: Attempts to retrieve or parse try parse opcode data without treating normal misses as failures.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // - opcode: Opcode value supplied by the caller for this operation.
    // Returns: Returns true when try parse opcode succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    private static bool TryParseOpcode(string value, out ushort opcode)
    {
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return ushort.TryParse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out opcode);
        }

        return ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out opcode);
    }

    // Method: HandleMapServiceCommandAsync
    // Purpose: Handles handle map service command work for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - command: Database command used to execute this operation without opening unnecessary additional state.
    // - sendResponseAsync: Send response async value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task HandleMapServiceCommandAsync(
        string remoteServerName,
        InternalMapServiceCommandPacket command,
        Func<string, Task> sendResponseAsync,
        CancellationToken cancellationToken)
    {
        if (!TryParseMapServiceControlAction(command.Action, out MapServiceControlAction action))
        {
            InternalMapServiceCommandResultPacket invalidResult = new(
                command.CommandId,
                "InstanceServer",
                nameof(MapServiceKind.Instance),
                command.MapId,
                0,
                nameof(MapServiceControlResultCode.Failed),
                nameof(MapServiceState.Offline),
                $"Invalid map command action '{command.Action}'.");

            await sendResponseAsync(invalidResult.ToPacketLine());
            return;
        }

        Logger.Write(LogType.NETWORK, $"InstanceServer received map {command.Action} command for MapId={command.MapId} from {remoteServerName}.", "InstanceServer");

        MapServiceManager? instanceServices = _instanceServices;
        if (instanceServices is null)
        {
            InternalMapServiceCommandResultPacket unavailableResult = new(
                command.CommandId,
                "InstanceServer",
                nameof(MapServiceKind.Instance),
                command.MapId,
                0,
                nameof(MapServiceControlResultCode.Failed),
                nameof(MapServiceState.Offline),
                "InstanceServer instance service manager is not started yet.");

            await sendResponseAsync(unavailableResult.ToPacketLine());
            return;
        }

        IReadOnlyList<MapServiceControlResult> results = await instanceServices.ExecuteControlCommandAsync(
            action,
            command.MapId,
            cancellationToken);

        foreach (MapServiceControlResult result in results)
        {
            InternalMapServiceCommandResultPacket response = new(
                command.CommandId,
                result.OwnerServerName,
                result.Kind.ToString(),
                result.MapId,
                result.InstanceId,
                result.ResultCode.ToString(),
                result.State.ToString(),
                result.Message);

            await sendResponseAsync(response.ToPacketLine());
        }

        await instanceServices.ReportServicesAsync(command.MapId, cancellationToken);
    }

    // Method: ReportInstanceServiceStatusAsync
    // Purpose: Executes the report instance service status operation for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - snapshot: Snapshot value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task ReportInstanceServiceStatusAsync(
        MapServiceSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        InternalPeerConnection[] peers = [.. _peerConnections.Values];
        InternalServerSession[] sessions = [.. _serverSessions.Values];
        if (peers.Length == 0 && sessions.Length == 0)
        {
            return;
        }

        int sentCount = 0;
        foreach (InternalPeerConnection peer in peers)
        {
            if (await SendInstanceServiceStatusToPeerAsync(snapshot, peer, cancellationToken))
            {
                sentCount++;
            }
        }

        foreach (InternalServerSession session in sessions)
        {
            if (await SendInstanceServiceStatusToSessionAsync(snapshot, session, cancellationToken))
            {
                sentCount++;
            }
        }

        _ = sentCount;
    }

    // Method: SendInstanceServiceStatusesToPeerAsync
    // Purpose: Handles send instance service statuses to peer work for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task SendInstanceServiceStatusesToPeerAsync(
        InternalPeerConnection connection,
        CancellationToken cancellationToken)
    {
        MapServiceManager? instanceServices = _instanceServices;
        if (instanceServices is null)
        {
            return;
        }

        int sentCount = 0;
        foreach (MapServiceSnapshot snapshot in instanceServices.GetSnapshots())
        {
            if (await SendInstanceServiceStatusToPeerAsync(snapshot, connection, cancellationToken))
            {
                sentCount++;
            }
        }

        if (sentCount > 0)
        {
            Logger.Write(LogType.TRACE, $"InstanceServer sent {sentCount} initial instance service status snapshot(s) to {connection.RemoteServerName}.", "InstanceServer");
        }
    }

    // Method: SendInstanceServiceStatusesToSessionAsync
    // Purpose: Handles send instance service statuses to session work for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - session: Session value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task SendInstanceServiceStatusesToSessionAsync(
        InternalServerSession session,
        CancellationToken cancellationToken)
    {
        MapServiceManager? instanceServices = _instanceServices;
        if (instanceServices is null)
        {
            return;
        }

        int sentCount = 0;
        foreach (MapServiceSnapshot snapshot in instanceServices.GetSnapshots())
        {
            if (await SendInstanceServiceStatusToSessionAsync(snapshot, session, cancellationToken))
            {
                sentCount++;
            }
        }

        if (sentCount > 0)
        {
            Logger.Write(LogType.TRACE, $"InstanceServer sent {sentCount} initial instance service status snapshot(s) to {session.RemoteServerName}.", "InstanceServer");
        }
    }

    // Method: SendInstanceServiceStatusToPeerAsync
    // Purpose: Handles send instance service status to peer work for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - snapshot: Snapshot value supplied by the caller for this operation.
    // - peer: Peer value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous Boolean result that is true when send instance service status to peer async succeeds or the requested condition is met.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task<bool> SendInstanceServiceStatusToPeerAsync(
        MapServiceSnapshot snapshot,
        InternalPeerConnection peer,
        CancellationToken cancellationToken)
    {
        try
        {
            await peer.SendPacketAsync(CreateInstanceServiceStatusPacket(snapshot), cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException or InvalidOperationException)
        {
            Logger.Write(LogType.WARNING, $"InstanceServer could not report instance service '{snapshot.Name}' to {peer.RemoteServerName}: {exception.Message}", "InstanceServer");
            return false;
        }
    }

    // Method: SendInstanceServiceStatusToSessionAsync
    // Purpose: Handles send instance service status to session work for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - snapshot: Snapshot value supplied by the caller for this operation.
    // - session: Session value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous Boolean result that is true when send instance service status to session async succeeds or the requested condition is met.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task<bool> SendInstanceServiceStatusToSessionAsync(
        MapServiceSnapshot snapshot,
        InternalServerSession session,
        CancellationToken cancellationToken)
    {
        try
        {
            await session.SendPacketAsync(CreateInstanceServiceStatusPacket(snapshot), cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException or InvalidOperationException)
        {
            Logger.Write(LogType.WARNING, $"InstanceServer could not report instance service '{snapshot.Name}' to {session.RemoteServerName}: {exception.Message}", "InstanceServer");
            return false;
        }
    }

    // Method: CreateInstanceServiceStatusPacket
    // Purpose: Applies create instance service status packet changes for the instance server runtime, dungeon-map ownership, and internal-service coordination.
    // Parameters:
    // - snapshot: Snapshot value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    private static string CreateInstanceServiceStatusPacket(MapServiceSnapshot snapshot)
    {
        InternalMapServiceStatusPacket status = new(
            snapshot.OwnerServerName,
            snapshot.Kind.ToString(),
            snapshot.MapId,
            snapshot.InstanceId,
            snapshot.State.ToString(),
            snapshot.Tick,
            snapshot.ActivePlayers,
            snapshot.ActiveGrids,
            snapshot.LastTickMilliseconds,
            snapshot.AverageTickMilliseconds,
            snapshot.LoadPercent,
            snapshot.StartedUtc);

        return status.ToPacketLine();
    }

    // Method: TryParseMapServiceControlAction
    // Purpose: Attempts to retrieve or parse try parse map service control action data without treating normal misses as failures.
    // Parameters:
    // - action: Action value supplied by the caller for this operation.
    // - controlAction: Control action value supplied by the caller for this operation.
    // Returns: Returns true when try parse map service control action succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to InstanceServer so callers do not duplicate validation, protocol, or persistence rules.
    private static bool TryParseMapServiceControlAction(string action, out MapServiceControlAction controlAction)
    {
        switch (action.ToLowerInvariant())
        {
            case "start":
                controlAction = MapServiceControlAction.Start;
                return true;

            case "shutdown":
            case "stop":
                controlAction = MapServiceControlAction.Shutdown;
                return true;

            case "restart":
                controlAction = MapServiceControlAction.Restart;
                return true;

            case "info":
                controlAction = MapServiceControlAction.Info;
                return true;

            default:
                controlAction = default;
                return false;
        }
    }
}
