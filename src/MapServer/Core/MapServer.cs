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

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using EmulationServer.Core.Servers;
using EmulationServer.Game.Maps.Runtime;
using EmulationServer.MapServer.Configuration;
using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Peers;
using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Network.Networking.Sessions;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

/**
  * File overview: src/MapServer/Core/MapServer.cs
  * This file belongs to the server startup, shutdown, and dependency orchestration portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.MapServer.Core;

/**
  * Represents the map server component in the server startup, shutdown, and dependency orchestration area.
  * It owns the server startup, shutdown, and dependency wiring for this process.
  */
public sealed class MapServer : IAsyncDisposable
{
    /**
      * Stores the host dependency or runtime value for MapServer.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly EmulationServerHost _host;
    /**
      * Stores the map services dependency or runtime value for MapServer.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly MapServerSettings _settings;
    private MapServiceManager? _mapServices;
    private readonly ConcurrentDictionary<string, InternalPeerConnection> _peerConnections = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, InternalServerSession> _serverSessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly MapPlayerTracker _playerTracker = new();
    /**
      * Stores the world capacity limit dependency or runtime value for MapServer.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private int _worldCapacityLimit;

    /**
      * Creates a new MapServer instance and stores the dependencies required by the component.
      * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
      */
    public MapServer(MapServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _settings = settings;
        _host = new EmulationServerHost(nameof(MapServer), settings.InternalNetwork, CreateCallbacks());
    }

    /**
      * Starts the component and prepares the runtime state required before it can accept work.
      * The method is part of MapServer and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Task hostTask = _host.StartAsync(cancellationToken);

        try
        {
            await _host.StartupCompleted.WaitAsync(cancellationToken);

            MapServiceManager serviceManager = CreateMapServiceManager();
            _mapServices = serviceManager;
            await serviceManager.StartAsync(cancellationToken);

            await hostTask;
        }
        finally
        {
            if (_mapServices is not null)
            {
                await _mapServices.StopAsync(CancellationToken.None);
            }
        }
    }

    /**
      * Stops the component and releases runtime resources in a controlled order.
      * The method is part of MapServer and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_mapServices is not null)
        {
            await _mapServices.StopAsync(cancellationToken);
        }

        await _host.StopAsync(cancellationToken);
    }

    /**
      * Releases owned resources and ensures background work is stopped safely.
      * The method is part of MapServer and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);

        if (_mapServices is not null)
        {
            await _mapServices.DisposeAsync();
        }

        await _host.DisposeAsync();
    }


    /**
      * Creates the map service manager after the host has finished startup validation.
      * Delaying construction keeps DBC loading, map registration, and service startup after the server has validated and announced startup.
      */
    private MapServiceManager CreateMapServiceManager()
    {
        return new MapServiceManager(
            nameof(MapServer),
            _settings.MapServices,
            ReportMapServiceStatusAsync);
    }

    /**
      * Creates a new object with validated defaults so callers receive a ready-to-use instance.
      * The method is part of MapServer and keeps this workflow isolated from the caller.
      */
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

    /**
      * Performs the on server authenticated async operation for MapServer.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    private async Task OnServerAuthenticatedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        _serverSessions[remoteServerName] = session;

        Logger.Write(LogType.NETWORK, $"MapServer registered incoming map-service control/status session '{remoteServerName}'.", nameof(MapServer));
        await SendMapServiceStatusesToSessionAsync(session, cancellationToken);
    }

    /**
      * Performs the on server disconnected async operation for MapServer.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    private Task OnServerDisconnectedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        _serverSessions.TryRemove(remoteServerName, out _);
        Logger.Write(LogType.NETWORK, $"MapServer removed incoming map-service control/status session '{remoteServerName}'.", nameof(MapServer));

        return Task.CompletedTask;
    }

    /**
      * Performs the on peer authenticated async operation for MapServer.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    private async Task OnPeerAuthenticatedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        _peerConnections[remoteServerName] = connection;

        Logger.Write(LogType.NETWORK, $"MapServer registered outgoing map-service status peer '{remoteServerName}'.", nameof(MapServer));
        await SendMapServiceStatusesToPeerAsync(connection, cancellationToken);
    }

    /**
      * Performs the on peer disconnected async operation for MapServer.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    private Task OnPeerDisconnectedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        _peerConnections.TryRemove(remoteServerName, out _);
        Logger.Write(LogType.NETWORK, $"MapServer removed outgoing map-service status peer '{remoteServerName}'.", nameof(MapServer));

        return Task.CompletedTask;
    }

    /**
      * Performs the on peer packet received async operation for MapServer.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
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

    /**
      * Performs the on session packet received async operation for MapServer.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
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

    /**
      * Handles a single operation or packet and keeps the calling code focused on flow control.
      * The method is part of MapServer and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    private async Task HandleInternalPacketAsync(
        string remoteServerName,
        string packet,
        Func<string, Task> sendResponseAsync,
        CancellationToken cancellationToken)
    {
        if (InternalMapServiceCommandPacket.TryParse(packet, out InternalMapServiceCommandPacket command))
        {
            await HandleMapServiceCommandAsync(remoteServerName, command, sendResponseAsync, cancellationToken);
            return;
        }

        if (HandlePlayerRoutingPacket(remoteServerName, packet))
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
            Logger.Write(LogType.WARNING, $"MapServer received invalid WorldServer capacity packet from {remoteServerName}: {packet}", nameof(MapServer));
            return;
        }

        string capacitySource = parts.Length == 3 ? parts[1] : remoteServerName;
        Volatile.Write(ref _worldCapacityLimit, capacityLimit);
        Logger.Write(LogType.NETWORK, $"MapServer received WorldServer capacity limit from {remoteServerName}: {capacitySource}={capacityLimit}.", nameof(MapServer));
    }

    /**
      * Handles player routing notifications from WorldServer while the client socket stays owned by WorldServer.
      */
    private bool HandlePlayerRoutingPacket(string remoteServerName, string packet)
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
                Logger.Write(LogType.NETWORK, $"MapServer tracked player '{state.Name}' ({state.Guid}) entering map={state.Map}, zone={state.Zone} from {remoteServerName}. Active players={_playerTracker.ActivePlayerCount}.", nameof(MapServer));
            }
            else
            {
                Logger.Write(LogType.WARNING, $"MapServer received invalid player enter-world route from {remoteServerName}: {packet}", nameof(MapServer));
            }

            return true;
        }

        if (string.Equals(parts[0], InternalProtocol.PlayerLeaveWorld, StringComparison.OrdinalIgnoreCase))
        {
            uint guid = parts.Length > 2 && uint.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint parsedGuid)
                ? parsedGuid
                : 0;

            bool removed = guid != 0 && _playerTracker.PlayerLeft(guid);
            Logger.Write(LogType.NETWORK, $"MapServer tracked player leave-world route from {remoteServerName}: guid={(guid == 0 ? "unknown" : guid.ToString(CultureInfo.InvariantCulture))}, removed={removed}, active players={_playerTracker.ActivePlayerCount}.", nameof(MapServer));
            return true;
        }

        if (string.Equals(parts[0], InternalProtocol.PlayerMovement, StringComparison.OrdinalIgnoreCase))
        {
            if (TryReadPlayerMovementRoute(parts, out uint accountId, out uint guid, out ushort opcode, out uint map, out uint zone, out float x, out float y, out float z, out float orientation, out uint flags, out uint clientTime))
            {
                MapPlayerRuntimeState state = _playerTracker.PlayerMoved(accountId, guid, map, zone, x, y, z, orientation, opcode, flags, clientTime);
                Logger.Write(LogType.TRACE, $"MapServer updated movement for guid={state.Guid}: opcode=0x{state.LastMovementOpcode:X4}, map={state.Map}, zone={state.Zone}, position=({state.PositionX:0.##}, {state.PositionY:0.##}, {state.PositionZ:0.##}), active players={_playerTracker.ActivePlayerCount}.", nameof(MapServer));
            }
            else
            {
                Logger.Write(LogType.WARNING, $"MapServer received invalid player movement route from {remoteServerName}: {packet}", nameof(MapServer));
            }

            return true;
        }

        if (string.Equals(parts[0], InternalProtocol.PlayerClientPacket, StringComparison.OrdinalIgnoreCase))
        {
            string opcode = parts.Length > 3 ? parts[3] : "unknown";
            int payloadBytes = parts.Length > 4 ? parts[4].Length / 2 : 0;
            Logger.Write(LogType.TRACE, $"MapServer received player client-packet route from {remoteServerName}: account={(parts.Length > 1 ? parts[1] : "unknown")}, guid={(parts.Length > 2 ? parts[2] : "unknown")}, opcode={opcode}, payload={payloadBytes} byte(s).", nameof(MapServer));
            return true;
        }

        return false;
    }

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

    private static bool TryParseOpcode(string value, out ushort opcode)
    {
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return ushort.TryParse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out opcode);
        }

        return ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out opcode);
    }

    /**
      * Handles a single operation or packet and keeps the calling code focused on flow control.
      * The method is part of MapServer and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
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
                nameof(MapServer),
                MapServiceKind.World.ToString(),
                command.MapId,
                0,
                MapServiceControlResultCode.Failed.ToString(),
                MapServiceState.Offline.ToString(),
                $"Invalid map command action '{command.Action}'.");

            await sendResponseAsync(invalidResult.ToPacketLine());
            return;
        }

        Logger.Write(LogType.NETWORK, $"MapServer received map {command.Action} command for MapId={command.MapId} from {remoteServerName}.", nameof(MapServer));

        MapServiceManager? mapServices = _mapServices;
        if (mapServices is null)
        {
            InternalMapServiceCommandResultPacket unavailableResult = new(
                command.CommandId,
                nameof(MapServer),
                MapServiceKind.World.ToString(),
                command.MapId,
                0,
                MapServiceControlResultCode.Failed.ToString(),
                MapServiceState.Offline.ToString(),
                "MapServer map service manager is not started yet.");

            await sendResponseAsync(unavailableResult.ToPacketLine());
            return;
        }

        IReadOnlyList<MapServiceControlResult> results = await mapServices.ExecuteControlCommandAsync(
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

        await mapServices.ReportServicesAsync(command.MapId, cancellationToken);
    }

    /**
      * Broadcasts one map service status snapshot to all currently connected status peers.
      * Startup and peer-authentication paths use targeted helpers so a newly connected peer does not cause every existing peer to receive a duplicate startup snapshot.
      */
    private async Task ReportMapServiceStatusAsync(
        MapServiceSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        InternalPeerConnection[] peers = _peerConnections.Values.ToArray();
        InternalServerSession[] sessions = _serverSessions.Values.ToArray();
        if (peers.Length == 0 && sessions.Length == 0)
        {
            return;
        }

        int sentCount = 0;
        foreach (InternalPeerConnection peer in peers)
        {
            if (await SendMapServiceStatusToPeerAsync(snapshot, peer, cancellationToken))
            {
                sentCount++;
            }
        }

        foreach (InternalServerSession session in sessions)
        {
            if (await SendMapServiceStatusToSessionAsync(snapshot, session, cancellationToken))
            {
                sentCount++;
            }
        }

        if (sentCount > 0)
        {
            Logger.Write(LogType.TRACE, $"MapServer published map service '{snapshot.Name}' status to {sentCount} status peer(s): state={snapshot.State}, tick={snapshot.Tick}, load={snapshot.LoadPercent:0.##}%.", nameof(MapServer));
        }
    }

    /**
      * Sends all current map service snapshots to one newly authenticated outgoing peer.
      * This avoids the duplicate reporting that happened when the authentication callback rebroadcast every service to every existing peer.
      */
    private async Task SendMapServiceStatusesToPeerAsync(
        InternalPeerConnection connection,
        CancellationToken cancellationToken)
    {
        MapServiceManager? mapServices = _mapServices;
        if (mapServices is null)
        {
            return;
        }

        int sentCount = 0;
        foreach (MapServiceSnapshot snapshot in mapServices.GetSnapshots())
        {
            if (await SendMapServiceStatusToPeerAsync(snapshot, connection, cancellationToken))
            {
                sentCount++;
            }
        }

        if (sentCount > 0)
        {
            Logger.Write(LogType.TRACE, $"MapServer sent {sentCount} initial map service status snapshot(s) to {connection.RemoteServerName}.", nameof(MapServer));
        }
    }

    /**
      * Sends all current map service snapshots to one newly authenticated incoming session.
      * This keeps status synchronization targeted to the new connection instead of rebroadcasting to all peers.
      */
    private async Task SendMapServiceStatusesToSessionAsync(
        InternalServerSession session,
        CancellationToken cancellationToken)
    {
        MapServiceManager? mapServices = _mapServices;
        if (mapServices is null)
        {
            return;
        }

        int sentCount = 0;
        foreach (MapServiceSnapshot snapshot in mapServices.GetSnapshots())
        {
            if (await SendMapServiceStatusToSessionAsync(snapshot, session, cancellationToken))
            {
                sentCount++;
            }
        }

        if (sentCount > 0)
        {
            Logger.Write(LogType.TRACE, $"MapServer sent {sentCount} initial map service status snapshot(s) to {session.RemoteServerName}.", nameof(MapServer));
        }
    }

    /**
      * Sends a single map service status snapshot to one outgoing internal peer connection.
      */
    private static async Task<bool> SendMapServiceStatusToPeerAsync(
        MapServiceSnapshot snapshot,
        InternalPeerConnection peer,
        CancellationToken cancellationToken)
    {
        try
        {
            await peer.SendPacketAsync(CreateMapServiceStatusPacket(snapshot), cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException or InvalidOperationException)
        {
            Logger.Write(LogType.WARNING, $"MapServer could not report map service '{snapshot.Name}' to {peer.RemoteServerName}: {exception.Message}", nameof(MapServer));
            return false;
        }
    }

    /**
      * Sends a single map service status snapshot to one incoming internal server session.
      */
    private static async Task<bool> SendMapServiceStatusToSessionAsync(
        MapServiceSnapshot snapshot,
        InternalServerSession session,
        CancellationToken cancellationToken)
    {
        try
        {
            await session.SendPacketAsync(CreateMapServiceStatusPacket(snapshot), cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException or InvalidOperationException)
        {
            Logger.Write(LogType.WARNING, $"MapServer could not report map service '{snapshot.Name}' to {session.RemoteServerName}: {exception.Message}", nameof(MapServer));
            return false;
        }
    }

    /**
      * Converts a runtime map service snapshot into the shared internal protocol line used by WorldServer and ProxyServer.
      */
    private static string CreateMapServiceStatusPacket(MapServiceSnapshot snapshot)
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
            snapshot.LoadPercent);

        return status.ToPacketLine();
    }

    /**
      * Attempts the operation without treating a normal failure as an exceptional condition.
      * The method is part of MapServer and keeps this workflow isolated from the caller.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
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
