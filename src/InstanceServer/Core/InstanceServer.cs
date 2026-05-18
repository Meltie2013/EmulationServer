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

namespace EmulationServer.InstanceServer.Core;

public sealed class InstanceServer : IAsyncDisposable
{
    private readonly EmulationServerHost _host;
    private readonly MapServiceManager _instanceServices;
    private readonly ConcurrentDictionary<string, InternalPeerConnection> _peerConnections = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, InternalServerSession> _serverSessions = new(StringComparer.OrdinalIgnoreCase);
    private int _worldCapacityLimit;

    public InstanceServer(InstanceServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _instanceServices = new MapServiceManager(
            nameof(InstanceServer),
            settings.InstanceServices,
            ReportInstanceServiceStatusAsync);

        _host = new EmulationServerHost(nameof(InstanceServer), settings.InternalNetwork, CreateCallbacks());
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _instanceServices.StartAsync(cancellationToken);

        try
        {
            await _host.StartAsync(cancellationToken);
        }
        finally
        {
            await _instanceServices.StopAsync(CancellationToken.None);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _instanceServices.StopAsync(cancellationToken);
        await _host.StopAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        await _instanceServices.DisposeAsync();
        await _host.DisposeAsync();
    }

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

    private async Task OnServerAuthenticatedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        _serverSessions[remoteServerName] = session;

        Logger.Write(LogType.NETWORK, $"InstanceServer registered incoming instance-service control/status session '{remoteServerName}'.", nameof(InstanceServer));
        await _instanceServices.ReportAllServicesAsync(cancellationToken);
    }

    private Task OnServerDisconnectedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        _serverSessions.TryRemove(remoteServerName, out _);
        Logger.Write(LogType.NETWORK, $"InstanceServer removed incoming instance-service control/status session '{remoteServerName}'.", nameof(InstanceServer));

        return Task.CompletedTask;
    }

    private async Task OnPeerAuthenticatedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        _peerConnections[remoteServerName] = connection;

        Logger.Write(LogType.NETWORK, $"InstanceServer registered outgoing instance-service status peer '{remoteServerName}'.", nameof(InstanceServer));
        await _instanceServices.ReportAllServicesAsync(cancellationToken);
    }

    private Task OnPeerDisconnectedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        _peerConnections.TryRemove(remoteServerName, out _);
        Logger.Write(LogType.NETWORK, $"InstanceServer removed outgoing instance-service status peer '{remoteServerName}'.", nameof(InstanceServer));

        return Task.CompletedTask;
    }

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

        IReadOnlyList<MapServiceControlResult> results = await _instanceServices.ExecuteControlCommandAsync(
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

        await _instanceServices.ReportServicesAsync(command.MapId, cancellationToken);
    }

    private async Task ReportInstanceServiceStatusAsync(
        MapServiceSnapshot snapshot,
        CancellationToken cancellationToken)
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

        string packet = status.ToPacketLine();

        InternalPeerConnection[] peers = _peerConnections.Values.ToArray();
        InternalServerSession[] sessions = _serverSessions.Values.ToArray();
        if (peers.Length == 0 && sessions.Length == 0)
        {
            Logger.Write(LogType.TRACE, $"InstanceServer has no connected status peers for instance service '{snapshot.Name}' (MapId={snapshot.MapId}, InstanceId={snapshot.InstanceId}).", nameof(InstanceServer));
            return;
        }

        foreach (InternalPeerConnection peer in peers)
        {
            try
            {
                await peer.SendPacketAsync(packet, cancellationToken);
                Logger.Write(LogType.TRACE, $"InstanceServer reported instance service '{snapshot.Name}' to {peer.RemoteServerName}: state={snapshot.State}, tick={snapshot.Tick}, load={snapshot.LoadPercent:0.##}%.", nameof(InstanceServer));
            }
            catch (Exception exception) when (exception is IOException or ObjectDisposedException or InvalidOperationException)
            {
                Logger.Write(LogType.WARNING, $"InstanceServer could not report instance service '{snapshot.Name}' to {peer.RemoteServerName}: {exception.Message}", nameof(InstanceServer));
            }
        }

        foreach (InternalServerSession session in sessions)
        {
            try
            {
                await session.SendPacketAsync(packet, cancellationToken);
                Logger.Write(LogType.TRACE, $"InstanceServer reported instance service '{snapshot.Name}' to {session.RemoteServerName}: state={snapshot.State}, tick={snapshot.Tick}, load={snapshot.LoadPercent:0.##}%.", nameof(InstanceServer));
            }
            catch (Exception exception) when (exception is IOException or ObjectDisposedException or InvalidOperationException)
            {
                Logger.Write(LogType.WARNING, $"InstanceServer could not report instance service '{snapshot.Name}' to {session.RemoteServerName}: {exception.Message}", nameof(InstanceServer));
            }
        }
    }

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
