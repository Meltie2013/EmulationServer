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
using System.Globalization;

using EmulationServer.Core.Servers;
using EmulationServer.Game.Maps.Runtime;
using EmulationServer.InstanceServer.Configuration;
using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Peers;
using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Network.Networking.Sessions;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

/**
  * File overview: src/InstanceServer/Core/InstanceServer.cs
  * This file belongs to the server startup, shutdown, and dependency orchestration portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.InstanceServer.Core;

/**
  * Represents the instance server component in the server startup, shutdown, and dependency orchestration area.
  * It owns the server startup, shutdown, and dependency wiring for this process.
  */
public sealed class InstanceServer : IAsyncDisposable
{
    /**
      * Stores the host dependency or runtime value for InstanceServer.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly EmulationServerHost _host;
    /**
      * Stores the instance services dependency or runtime value for InstanceServer.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly InstanceServerSettings _settings;
    private MapServiceManager? _instanceServices;
    private readonly ConcurrentDictionary<string, InternalPeerConnection> _peerConnections = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, InternalServerSession> _serverSessions = new(StringComparer.OrdinalIgnoreCase);
    /**
      * Stores the world capacity limit dependency or runtime value for InstanceServer.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private int _worldCapacityLimit;

    /**
      * Creates a new InstanceServer instance and stores the dependencies required by the component.
      * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
      */
    public InstanceServer(InstanceServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _settings = settings;
        _host = new EmulationServerHost(nameof(InstanceServer), settings.InternalNetwork, CreateCallbacks());
    }

    /**
      * Starts the component and prepares the runtime state required before it can accept work.
      * The method is part of InstanceServer and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Task hostTask = _host.StartAsync(cancellationToken);

        try
        {
            await _host.StartupCompleted.WaitAsync(cancellationToken);

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

    /**
      * Stops the component and releases runtime resources in a controlled order.
      * The method is part of InstanceServer and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_instanceServices is not null)
        {
            await _instanceServices.StopAsync(cancellationToken);
        }

        await _host.StopAsync(cancellationToken);
    }

    /**
      * Releases owned resources and ensures background work is stopped safely.
      * The method is part of InstanceServer and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);

        if (_instanceServices is not null)
        {
            await _instanceServices.DisposeAsync();
        }

        await _host.DisposeAsync();
    }


    /**
      * Creates the instance service manager after the host has finished startup validation.
      * Delaying construction keeps DBC loading, instance registration, and service startup after the server has validated and announced startup.
      */
    private MapServiceManager CreateInstanceServiceManager()
    {
        return new MapServiceManager(
            nameof(InstanceServer),
            _settings.InstanceServices,
            ReportInstanceServiceStatusAsync);
    }

    /**
      * Creates a new object with validated defaults so callers receive a ready-to-use instance.
      * The method is part of InstanceServer and keeps this workflow isolated from the caller.
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
      * Performs the on server authenticated async operation for InstanceServer.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    private async Task OnServerAuthenticatedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        _serverSessions[remoteServerName] = session;

        Logger.Write(LogType.NETWORK, $"InstanceServer registered incoming instance-service control/status session '{remoteServerName}'.", nameof(InstanceServer));
        await SendInstanceServiceStatusesToSessionAsync(session, cancellationToken);
    }

    /**
      * Performs the on server disconnected async operation for InstanceServer.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    private Task OnServerDisconnectedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        _serverSessions.TryRemove(remoteServerName, out _);
        Logger.Write(LogType.NETWORK, $"InstanceServer removed incoming instance-service control/status session '{remoteServerName}'.", nameof(InstanceServer));

        return Task.CompletedTask;
    }

    /**
      * Performs the on peer authenticated async operation for InstanceServer.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    private async Task OnPeerAuthenticatedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        _peerConnections[remoteServerName] = connection;

        Logger.Write(LogType.NETWORK, $"InstanceServer registered outgoing instance-service status peer '{remoteServerName}'.", nameof(InstanceServer));
        await SendInstanceServiceStatusesToPeerAsync(connection, cancellationToken);
    }

    /**
      * Performs the on peer disconnected async operation for InstanceServer.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    private Task OnPeerDisconnectedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        _peerConnections.TryRemove(remoteServerName, out _);
        Logger.Write(LogType.NETWORK, $"InstanceServer removed outgoing instance-service status peer '{remoteServerName}'.", nameof(InstanceServer));

        return Task.CompletedTask;
    }

    /**
      * Performs the on peer packet received async operation for InstanceServer.
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
      * Performs the on session packet received async operation for InstanceServer.
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
      * The method is part of InstanceServer and keeps this workflow isolated from the caller.
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

        string[] parts = packet.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (!string.Equals(parts[0], InternalProtocol.WorldCapacity, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        int capacityIndex = parts.Length == 3 ? 2 : 1;
        if (parts.Length is not (2 or 3) || !int.TryParse(parts[capacityIndex], NumberStyles.Integer, CultureInfo.InvariantCulture, out int capacityLimit) || capacityLimit <= 0)
        {
            Logger.Write(LogType.WARNING, $"InstanceServer received invalid WorldServer capacity packet from {remoteServerName}: {packet}", nameof(InstanceServer));
            return;
        }

        string capacitySource = parts.Length == 3 ? parts[1] : remoteServerName;
        Volatile.Write(ref _worldCapacityLimit, capacityLimit);
        Logger.Write(LogType.NETWORK, $"InstanceServer received WorldServer capacity limit from {remoteServerName}: {capacitySource}={capacityLimit}.", nameof(InstanceServer));
    }

    /**
      * Handles a single operation or packet and keeps the calling code focused on flow control.
      * The method is part of InstanceServer and keeps this workflow isolated from the caller.
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
                nameof(InstanceServer),
                MapServiceKind.Instance.ToString(),
                command.MapId,
                0,
                MapServiceControlResultCode.Failed.ToString(),
                MapServiceState.Offline.ToString(),
                $"Invalid map command action '{command.Action}'.");

            await sendResponseAsync(invalidResult.ToPacketLine());
            return;
        }

        Logger.Write(LogType.NETWORK, $"InstanceServer received map {command.Action} command for MapId={command.MapId} from {remoteServerName}.", nameof(InstanceServer));

        MapServiceManager? instanceServices = _instanceServices;
        if (instanceServices is null)
        {
            InternalMapServiceCommandResultPacket unavailableResult = new(
                command.CommandId,
                nameof(InstanceServer),
                MapServiceKind.Instance.ToString(),
                command.MapId,
                0,
                MapServiceControlResultCode.Failed.ToString(),
                MapServiceState.Offline.ToString(),
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

    /**
      * Broadcasts one instance service status snapshot to all currently connected status peers.
      * Startup and peer-authentication paths use targeted helpers so a newly connected peer does not cause every existing peer to receive a duplicate startup snapshot.
      */
    private async Task ReportInstanceServiceStatusAsync(
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

        if (sentCount > 0)
        {
            Logger.Write(LogType.TRACE, $"InstanceServer published instance service '{snapshot.Name}' status to {sentCount} status peer(s): state={snapshot.State}, tick={snapshot.Tick}, load={snapshot.LoadPercent:0.##}%.", nameof(InstanceServer));
        }
    }

    /**
      * Sends all current instance service snapshots to one newly authenticated outgoing peer.
      * This avoids the duplicate reporting that happened when the authentication callback rebroadcast every service to every existing peer.
      */
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
            Logger.Write(LogType.TRACE, $"InstanceServer sent {sentCount} initial instance service status snapshot(s) to {connection.RemoteServerName}.", nameof(InstanceServer));
        }
    }

    /**
      * Sends all current instance service snapshots to one newly authenticated incoming session.
      * This keeps status synchronization targeted to the new connection instead of rebroadcasting to all peers.
      */
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
            Logger.Write(LogType.TRACE, $"InstanceServer sent {sentCount} initial instance service status snapshot(s) to {session.RemoteServerName}.", nameof(InstanceServer));
        }
    }

    /**
      * Sends a single instance service status snapshot to one outgoing internal peer connection.
      */
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
            Logger.Write(LogType.WARNING, $"InstanceServer could not report instance service '{snapshot.Name}' to {peer.RemoteServerName}: {exception.Message}", nameof(InstanceServer));
            return false;
        }
    }

    /**
      * Sends a single instance service status snapshot to one incoming internal server session.
      */
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
            Logger.Write(LogType.WARNING, $"InstanceServer could not report instance service '{snapshot.Name}' to {session.RemoteServerName}: {exception.Message}", nameof(InstanceServer));
            return false;
        }
    }

    /**
      * Converts a runtime instance service snapshot into the shared internal protocol line used by WorldServer and ProxyServer.
      */
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
            snapshot.LoadPercent);

        return status.ToPacketLine();
    }

    /**
      * Attempts the operation without treating a normal failure as an exceptional condition.
      * The method is part of InstanceServer and keeps this workflow isolated from the caller.
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
