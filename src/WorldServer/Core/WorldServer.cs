using System.Collections.Concurrent;
using System.Globalization;

using EmulationServer.Core.Servers;
using EmulationServer.Game.Data.Stores;
using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Peers;
using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Network.Networking.Sessions;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.WorldServer.Commands;
using EmulationServer.WorldServer.Configuration;
using EmulationServer.WorldServer.Internal;

namespace EmulationServer.WorldServer.Core;

public sealed class WorldServer : IAsyncDisposable
{
    private readonly WorldServerSettings _settings;
    private readonly EmulationServerHost _host;
    private readonly WorldRealmStatusReporter _realmStatusReporter;
    private readonly WorldConsoleCommandService _commandService;
    private readonly ConcurrentDictionary<string, InternalPeerConnection> _peerConnections = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, InternalServerSession> _serverSessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, InternalMapServiceStatusPacket> _mapServiceStatuses = new(StringComparer.OrdinalIgnoreCase);

    private WorldGameDataStore _gameData = WorldGameDataStore.Empty;

    public WorldServer(WorldServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _settings = settings;
        _host = new EmulationServerHost(nameof(WorldServer), settings.Database, settings.InternalNetwork, CreateCallbacks());
        _realmStatusReporter = new WorldRealmStatusReporter(
            settings.RealmStatus,
            settings.InternalNetwork.RegistrationKey,
            settings.MaxConnections,
            settings.InternalNetwork.LatencyReportInterval,
            settings.InternalNetwork.PingTimeout);
        _commandService = new WorldConsoleCommandService(ExecuteMapCommandAsync);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LoadGameDataIfEnabled();
        _commandService.Start(cancellationToken);

        Task hostTask = _host.StartAsync(cancellationToken);

        try
        {
            await _host.StartupCompleted.WaitAsync(cancellationToken);

            await _realmStatusReporter.StartAsync(cancellationToken);

            await hostTask;
        }
        finally
        {
            await _realmStatusReporter.StopAsync(CancellationToken.None);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _realmStatusReporter.StopAsync(cancellationToken);
        await _host.StopAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        await _realmStatusReporter.DisposeAsync();
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
        Logger.Write(LogType.NETWORK, $"WorldServer registered incoming internal session from {remoteServerName}.", nameof(WorldServer));

        if (string.Equals(remoteServerName, "ProxyServer", StringComparison.OrdinalIgnoreCase))
        {
            await AnnounceWorldCapacityAsync(session.SendPacketAsync, remoteServerName, cancellationToken);
        }
    }

    private Task OnServerDisconnectedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        _serverSessions.TryRemove(remoteServerName, out _);
        Logger.Write(LogType.NETWORK, $"WorldServer removed incoming internal session from {remoteServerName}.", nameof(WorldServer));

        return Task.CompletedTask;
    }

    private async Task OnPeerAuthenticatedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        _peerConnections[remoteServerName] = connection;

        if (string.Equals(remoteServerName, "ProxyServer", StringComparison.OrdinalIgnoreCase))
        {
            await AnnounceWorldCapacityAsync(connection.SendPacketAsync, remoteServerName, cancellationToken);
        }
    }

    private Task OnPeerDisconnectedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        _peerConnections.TryRemove(remoteServerName, out _);
        Logger.Write(LogType.NETWORK, $"WorldServer removed outgoing internal peer {remoteServerName}.", nameof(WorldServer));

        return Task.CompletedTask;
    }

    private Task OnPeerPacketReceivedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        string packet,
        CancellationToken cancellationToken)
    {
        HandleMapServicePacket(remoteServerName, packet);
        return Task.CompletedTask;
    }

    private Task OnSessionPacketReceivedAsync(
        InternalServerSession session,
        string remoteServerName,
        string packet,
        CancellationToken cancellationToken)
    {
        HandleMapServicePacket(remoteServerName, packet);
        return Task.CompletedTask;
    }

    private async Task ExecuteMapCommandAsync(string action, int mapId, CancellationToken cancellationToken)
    {
        if (string.Equals(action, "info", StringComparison.OrdinalIgnoreCase))
        {
            WriteCachedMapInfo(mapId);
        }

        string commandId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        InternalMapServiceCommandPacket command = new(commandId, action, mapId);
        string packet = command.ToPacketLine();

        string[] targets = GetMapCommandTargets(mapId);
        if (targets.Length == 0)
        {
            Logger.Write(LogType.WARNING, $"WorldServer has no connected MapServer or InstanceServer targets for map command '{action}' MapId={mapId}.", nameof(WorldServer));
            return;
        }

        foreach (string target in targets)
        {
            int sent = await SendPacketToServerAsync(target, packet, cancellationToken);
            if (sent == 0)
            {
                Logger.Write(LogType.WARNING, $"WorldServer could not send map {action} command for MapId={mapId} to {target}; no active connection was available.", nameof(WorldServer));
                continue;
            }

            Logger.Write(LogType.NETWORK, $"WorldServer sent map {action} command for MapId={mapId} to {target} ({sent} connection(s)).", nameof(WorldServer));
        }
    }

    private string[] GetMapCommandTargets(int mapId)
    {
        string[] owners = _mapServiceStatuses.Values
            .Where(status => status.MapId == mapId)
            .Select(status => status.OwnerServerName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (owners.Length > 0)
        {
            return owners;
        }

        return _peerConnections.Keys
            .Concat(_serverSessions.Keys)
            .Where(IsMapControlServer)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task<int> SendPacketToServerAsync(string remoteServerName, string packet, CancellationToken cancellationToken)
    {
        int sent = 0;

        if (_peerConnections.TryGetValue(remoteServerName, out InternalPeerConnection? peerConnection))
        {
            await peerConnection.SendPacketAsync(packet, cancellationToken);
            sent++;
        }

        if (_serverSessions.TryGetValue(remoteServerName, out InternalServerSession? session))
        {
            await session.SendPacketAsync(packet, cancellationToken);
            sent++;
        }

        return sent;
    }

    private async Task AnnounceWorldCapacityAsync(
        Func<string, CancellationToken, Task> sendPacketAsync,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        string packet = $"{InternalProtocol.WorldCapacity} {_settings.MaxConnections}";
        await sendPacketAsync(packet, cancellationToken);

        Logger.Write(LogType.NETWORK, $"WorldServer announced max connections to {remoteServerName}: {_settings.MaxConnections}.", nameof(WorldServer));
    }

    private void HandleMapServicePacket(string remoteServerName, string packet)
    {
        if (InternalMapServiceCommandResultPacket.TryParse(packet, out InternalMapServiceCommandResultPacket result))
        {
            HandleMapServiceCommandResult(remoteServerName, result);
            return;
        }

        if (packet.StartsWith(InternalProtocol.MapServiceStatus, StringComparison.OrdinalIgnoreCase))
        {
            HandleMapServiceStatusPacket(remoteServerName, packet);
        }
    }

    private void HandleMapServiceCommandResult(string remoteServerName, InternalMapServiceCommandResultPacket result)
    {
        string message = $"WorldServer received map command result from {remoteServerName}: {result.OwnerServerName} {result.Kind} map={result.MapId}, instance={result.InstanceId}, state={result.State}, result={result.ResultCode}. {result.Message}";

        switch (result.ResultCode.ToLowerInvariant())
        {
            case "success":
                Logger.Write(LogType.SUCCESS, message, nameof(WorldServer));
                break;

            case "notfound":
                Logger.Write(LogType.TRACE, message, nameof(WorldServer));
                break;

            case "ignored":
                Logger.Write(LogType.INFORMATION, message, nameof(WorldServer));
                break;

            default:
                Logger.Write(LogType.WARNING, message, nameof(WorldServer));
                break;
        }
    }

    private void HandleMapServiceStatusPacket(string remoteServerName, string packet)
    {
        if (!InternalMapServiceStatusPacket.TryParse(packet, out InternalMapServiceStatusPacket status))
        {
            Logger.Write(LogType.WARNING, $"WorldServer received invalid MAP_SERVICE_STATUS packet from {remoteServerName}: {packet}", nameof(WorldServer));
            return;
        }

        _mapServiceStatuses[GetStatusKey(status)] = status;

        string message = $"WorldServer received {status.OwnerServerName} {status.Kind} map service status: map={status.MapId}, instance={status.InstanceId}, state={status.State}, tick={status.Tick}, players={status.ActivePlayers}, grids={status.ActiveGrids}, load={status.LoadPercent:0.##}%, avgTick={status.AverageTickMilliseconds:0.###} ms.";

        if (status.LoadPercent >= 85d)
        {
            Logger.Write(LogType.WARNING, message, nameof(WorldServer));
            return;
        }

        Logger.Write(LogType.TRACE, message, nameof(WorldServer));
    }

    private void WriteCachedMapInfo(int mapId)
    {
        InternalMapServiceStatusPacket[] statuses = _mapServiceStatuses.Values
            .Where(status => status.MapId == mapId)
            .OrderBy(status => status.OwnerServerName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(status => status.InstanceId)
            .ToArray();

        if (statuses.Length == 0)
        {
            Logger.Write(LogType.WARNING, $"WorldServer has no cached map service status for MapId={mapId}. Sending live info request to connected map services...", nameof(WorldServer));
            return;
        }

        Logger.Write(LogType.TRACE, $"Cached map service info for MapId={mapId}:", nameof(WorldServer));
        foreach (InternalMapServiceStatusPacket status in statuses)
        {
            Logger.Write(
                LogType.TRACE,
                $"  {status.OwnerServerName} {status.Kind}: instance={status.InstanceId}, state={status.State}, tick={status.Tick}, players={status.ActivePlayers}, grids={status.ActiveGrids}, load={status.LoadPercent:0.##}%, avgTick={status.AverageTickMilliseconds:0.###} ms.",
                nameof(WorldServer));
        }
    }

    private static bool IsMapControlServer(string remoteServerName)
    {
        return string.Equals(remoteServerName, "MapServer", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(remoteServerName, "InstanceServer", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetStatusKey(InternalMapServiceStatusPacket status)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{status.OwnerServerName}|{status.Kind}|{status.MapId}|{status.InstanceId}");
    }

    private void LoadGameDataIfEnabled()
    {
        GameDataSettings gameDataSettings = _settings.GameData;
        if (!gameDataSettings.Enabled)
        {
            Logger.Write(LogType.INFORMATION, "WorldServer game data loading is disabled. Enable [GameData] when extracted DBC data is ready.", nameof(WorldServer));
            return;
        }

        Logger.Write(LogType.NOTICE, "WorldServer loading required DBC data into memory...");

        _gameData = WorldGameDataStore.Load(
            gameDataSettings.DataDirectory,
            gameDataSettings.DbcDirectory,
            gameDataSettings.RequiredDbcFiles);

        Logger.Write(LogType.SUCCESS, $"WorldServer game data is ready in memory: {_gameData.DbcStores.Count} DBC store(s).", nameof(WorldServer));
    }
}
