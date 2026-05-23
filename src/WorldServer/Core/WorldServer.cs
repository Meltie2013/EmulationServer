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
using EmulationServer.Database.Services;
using EmulationServer.Game.Data.Stores;
using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Peers;
using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Network.Networking.Sessions;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.WorldServer.Commands;
using EmulationServer.WorldServer.Characters;
using EmulationServer.WorldServer.Configuration;
using EmulationServer.WorldServer.Database.Accounts;
using EmulationServer.WorldServer.Database.Characters;
using EmulationServer.WorldServer.Internal;
using EmulationServer.WorldServer.Networking.Sessions;
using EmulationServer.WorldServer.Networking.Socket;
using EmulationServer.WorldServer.WorldData;

/**
  * File overview: src/WorldServer/Core/WorldServer.cs
  * This file belongs to the server startup, shutdown, and dependency orchestration portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.WorldServer.Core;

/**
  * Represents the world server component in the server startup, shutdown, and dependency orchestration area.
  * It owns the server startup, shutdown, and dependency wiring for this process.
  */
public sealed class WorldServer : IAsyncDisposable
{
    /**
      * Stores the settings dependency or runtime value for WorldServer.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly WorldServerSettings _settings;
    /**
      * Stores the host dependency or runtime value for WorldServer.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly EmulationServerHost _host;
    /**
      * Stores the realm status reporter dependency or runtime value for WorldServer.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly WorldRealmStatusReporter _realmStatusReporter;
    /**
      * Stores the command service dependency or runtime value for WorldServer.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly WorldConsoleCommandService _commandService;
    private readonly MySqlDatabaseService _authDatabase;
    private readonly MySqlDatabaseService _characterDatabase;
    private readonly MySqlDatabaseService _worldDatabase;
    private readonly WorldAccountRepository _accountRepository;
    private readonly CharacterRepository _characterRepository;
    private readonly WorldTemplateRepository _worldTemplateRepository;
    private readonly CharacterCreationService _characterCreationService;
    private readonly WorldClientSocketListener _clientListener;
    private WorldTemplateDataStore _worldTemplateData = WorldTemplateDataStore.Empty;
    private readonly ConcurrentDictionary<string, InternalPeerConnection> _peerConnections = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, InternalServerSession> _serverSessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, InternalMapServiceStatusPacket> _mapServiceStatuses = new(StringComparer.OrdinalIgnoreCase);

    /**
      * Stores the game data dependency or runtime value for WorldServer.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private WorldGameDataStore _gameData = WorldGameDataStore.Empty;

    /**
      * Creates a new WorldServer instance and stores the dependencies required by the component.
      * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
      */
    public WorldServer(WorldServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _settings = settings;
        _host = new EmulationServerHost(nameof(WorldServer), settings.InternalNetwork, CreateCallbacks());
        _commandService = new WorldConsoleCommandService(ExecuteMapCommandAsync);

        _authDatabase = new MySqlDatabaseService(settings.Databases.Auth);
        _characterDatabase = new MySqlDatabaseService(settings.Databases.Character);
        _worldDatabase = new MySqlDatabaseService(settings.Databases.World);
        _accountRepository = new WorldAccountRepository(_authDatabase);
        _characterRepository = new CharacterRepository(
            _characterDatabase,
            entry => _worldTemplateData.TryGetItemTemplate(entry, out ItemTemplateRecord itemTemplate) ? itemTemplate : null);
        _worldTemplateRepository = new WorldTemplateRepository(_worldDatabase);
        _characterCreationService = new CharacterCreationService(_characterRepository, () => _gameData, () => _worldTemplateData);
        _realmStatusReporter = new WorldRealmStatusReporter(
            settings.RealmStatus,
            settings.InternalNetwork.RegistrationKey,
            settings.MaxConnections,
            settings.InternalNetwork.LatencyReportInterval,
            settings.InternalNetwork.PingTimeout,
            settings.InternalNetwork.ReceiveBufferSize,
            settings.InternalNetwork.SendBufferSize,
            settings.InternalNetwork.KeepAlive,
            settings.InternalNetwork.KeepAliveTimeSeconds,
            settings.InternalNetwork.KeepAliveIntervalSeconds,
            settings.InternalNetwork.AuthenticationTimeout,
            _characterRepository.GetCharacterCountsByAccountAsync);
        _clientListener = new WorldClientSocketListener(
            settings.ClientNetwork,
            client => new WorldClientSession(
                client,
                settings.RealmStatus.RealmId,
                settings.ClientNetwork.MaximumPacketSize,
                _accountRepository,
                _characterCreationService,
                _realmStatusReporter.SendCharacterCountSnapshotNowAsync));
    }

    /**
      * Starts the component and prepares the runtime state required before it can accept work.
      * The method is part of WorldServer and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LoadGameDataIfEnabled();
        await ValidateDatabaseConnectionsAsync(cancellationToken);
        await LoadWorldTemplateDataAsync(cancellationToken);

        Task hostTask = _host.StartAsync(cancellationToken);
        Task clientTask = _clientListener.StartAsync(cancellationToken);

        try
        {
            await _host.StartupCompleted.WaitAsync(cancellationToken);

            _commandService.Start(cancellationToken);
            await _realmStatusReporter.StartAsync(cancellationToken);

            await Task.WhenAll(hostTask, clientTask);
        }
        finally
        {
            await _realmStatusReporter.StopAsync(CancellationToken.None);
            await _clientListener.StopAsync(CancellationToken.None);
        }
    }

    /**
      * Stops the component and releases runtime resources in a controlled order.
      * The method is part of WorldServer and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _realmStatusReporter.StopAsync(cancellationToken);
        await _clientListener.StopAsync(cancellationToken);
        await _host.StopAsync(cancellationToken);
    }

    /**
      * Releases owned resources and ensures background work is stopped safely.
      * The method is part of WorldServer and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        await _realmStatusReporter.DisposeAsync();
        await _clientListener.DisposeAsync();
        await _host.DisposeAsync();
        await _authDatabase.DisposeAsync();
        await _characterDatabase.DisposeAsync();
        await _worldDatabase.DisposeAsync();
    }

    /**
      * Creates a new object with validated defaults so callers receive a ready-to-use instance.
      * The method is part of WorldServer and keeps this workflow isolated from the caller.
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
      * Performs the on server authenticated async operation for WorldServer.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
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

    /**
      * Performs the on server disconnected async operation for WorldServer.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    private Task OnServerDisconnectedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        _serverSessions.TryRemove(remoteServerName, out _);
        Logger.Write(LogType.NETWORK, $"WorldServer removed incoming internal session from {remoteServerName}.", nameof(WorldServer));

        return Task.CompletedTask;
    }

    /**
      * Performs the on peer authenticated async operation for WorldServer.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
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

    /**
      * Performs the on peer disconnected async operation for WorldServer.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    private Task OnPeerDisconnectedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        _peerConnections.TryRemove(remoteServerName, out _);
        Logger.Write(LogType.NETWORK, $"WorldServer removed outgoing internal peer {remoteServerName}.", nameof(WorldServer));

        return Task.CompletedTask;
    }

    /**
      * Performs the on peer packet received async operation for WorldServer.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    private Task OnPeerPacketReceivedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        string packet,
        CancellationToken cancellationToken)
    {
        HandleMapServicePacket(remoteServerName, packet);
        return Task.CompletedTask;
    }

    /**
      * Performs the on session packet received async operation for WorldServer.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    private Task OnSessionPacketReceivedAsync(
        InternalServerSession session,
        string remoteServerName,
        string packet,
        CancellationToken cancellationToken)
    {
        HandleMapServicePacket(remoteServerName, packet);
        return Task.CompletedTask;
    }

    /**
      * Executes the requested command after parsing and validation are complete.
      * The method is part of WorldServer and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
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

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of WorldServer and keeps this workflow isolated from the caller.
      */
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

    /**
      * Sends a protocol message or status update to a connected peer.
      * The method is part of WorldServer and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
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

    /**
      * Performs the announce world capacity async operation for WorldServer.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    private async Task AnnounceWorldCapacityAsync(
        Func<string, CancellationToken, Task> sendPacketAsync,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        string packet = $"{InternalProtocol.WorldCapacity} {_settings.MaxConnections}";
        await sendPacketAsync(packet, cancellationToken);

        Logger.Write(LogType.NETWORK, $"WorldServer announced max connections to {remoteServerName}: {_settings.MaxConnections}.", nameof(WorldServer));
    }

    /**
      * Handles a single operation or packet and keeps the calling code focused on flow control.
      * The method is part of WorldServer and keeps this workflow isolated from the caller.
      */
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

    /**
      * Handles a single operation or packet and keeps the calling code focused on flow control.
      * The method is part of WorldServer and keeps this workflow isolated from the caller.
      */
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

    /**
      * Handles a single operation or packet and keeps the calling code focused on flow control.
      * The method is part of WorldServer and keeps this workflow isolated from the caller.
      */
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

    /**
      * Writes the supplied data to the target destination using the project protocol or file format.
      * The method is part of WorldServer and keeps this workflow isolated from the caller.
      */
    private void WriteCachedMapInfo(int mapId)
    {
        InternalMapServiceStatusPacket[] statuses = _mapServiceStatuses.Values
            .Where(status => status.MapId == mapId)
            .OrderBy(status => status.OwnerServerName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(status => status.InstanceId)
            .ToArray();

        string dbcDescription = _gameData.MapData.DescribeMap(mapId);
        if (statuses.Length == 0)
        {
            Logger.Write(LogType.WARNING, $"WorldServer has no cached map service status for MapId={mapId}. {dbcDescription} Sending live info request to connected map services...", nameof(WorldServer));
            return;
        }

        Logger.Write(LogType.TRACE, $"Cached map service info for MapId={mapId}: {dbcDescription}", nameof(WorldServer));
        foreach (InternalMapServiceStatusPacket status in statuses)
        {
            Logger.Write(
                LogType.TRACE,
                $"  {status.OwnerServerName} {status.Kind}: instance={status.InstanceId}, state={status.State}, tick={status.Tick}, players={status.ActivePlayers}, grids={status.ActiveGrids}, load={status.LoadPercent:0.##}%, avgTick={status.AverageTickMilliseconds:0.###} ms.",
                nameof(WorldServer));
        }
    }

    /**
      * Performs the is map control server operation for WorldServer.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    private static bool IsMapControlServer(string remoteServerName)
    {
        return string.Equals(remoteServerName, "MapServer", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(remoteServerName, "InstanceServer", StringComparison.OrdinalIgnoreCase);
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of WorldServer and keeps this workflow isolated from the caller.
      */
    private static string GetStatusKey(InternalMapServiceStatusPacket status)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{status.OwnerServerName}|{status.Kind}|{status.MapId}|{status.InstanceId}");
    }

    /**
      * Validates the MaNGOS-compatible auth, character, and world database connections before the realm is advertised.
      */
    private async Task ValidateDatabaseConnectionsAsync(CancellationToken cancellationToken)
    {
        Logger.Write(LogType.NOTICE, "WorldServer validating Auth, Character, and World database connections...", nameof(WorldServer));

        await _authDatabase.ValidateConnectionAsync(cancellationToken);
        Logger.Write(LogType.SUCCESS, $"WorldServer Auth database is reachable: {_settings.Databases.Auth.Database}.", nameof(WorldServer));

        await _characterDatabase.ValidateConnectionAsync(cancellationToken);
        Logger.Write(LogType.SUCCESS, $"WorldServer Character database is reachable: {_settings.Databases.Character.Database}.", nameof(WorldServer));

        await _worldDatabase.ValidateConnectionAsync(cancellationToken);
        Logger.Write(LogType.SUCCESS, $"WorldServer World database is reachable: {_settings.Databases.World.Database}.", nameof(WorldServer));
    }

    /**
      * Loads MaNGOS world database templates needed by the character screen into memory.
      */
    private async Task LoadWorldTemplateDataAsync(CancellationToken cancellationToken)
    {
        Logger.Write(LogType.NOTICE, "WorldServer loading MaNGOS world templates into memory: playercreateinfo and item_template...", nameof(WorldServer));

        _worldTemplateData = await _worldTemplateRepository.LoadAsync(cancellationToken);

        if (_worldTemplateData.PlayerCreateInfo.Count == 0)
        {
            throw new InvalidOperationException("World database table `playercreateinfo` is empty. Character creation cannot resolve race/class start positions.");
        }

        if (_worldTemplateData.ItemTemplates.Count == 0)
        {
            throw new InvalidOperationException("World database table `item_template` is empty. Character creation cannot resolve starter items.");
        }

        Logger.Write(LogType.SUCCESS, $"WorldServer world templates are ready in memory: playercreateinfo={_worldTemplateData.PlayerCreateInfo.Count}, item_template={_worldTemplateData.ItemTemplates.Count}.", nameof(WorldServer));
    }

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of WorldServer and keeps this workflow isolated from the caller.
      */
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

        Logger.Write(LogType.SUCCESS, $"WorldServer game data is ready in memory: {_gameData.DbcStores.Count} DBC store(s), maps={_gameData.MapData.Maps.Count}, areas={_gameData.MapData.Areas.Count}, races={_gameData.CharacterData.Races.Count}, classes={_gameData.CharacterData.Classes.Count}, starterOutfits={_gameData.CharacterData.StartOutfits.Count}, itemDisplays={_gameData.ItemData.DisplayInfo.Count}, spells={_gameData.SpellData.Spells.Count}, factions={_gameData.FactionData.Factions.Count}.", nameof(WorldServer));
    }
}
