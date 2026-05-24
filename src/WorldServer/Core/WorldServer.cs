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
using WorldConsoleCommandService = EmulationServer.WorldServer.Commands.WorldConsoleCommandService;
using GameInGameCommandService = EmulationServer.Game.Commands.InGameCommandService;
using EmulationServer.WorldServer.Characters;
using GameChatSystem = EmulationServer.Game.Chat.ChatSystem;
using EmulationServer.WorldServer.Configuration;
using EmulationServer.WorldServer.Database.Accounts;
using EmulationServer.WorldServer.Database.Characters;
using EmulationServer.Game.Characters;
using EmulationServer.WorldServer.Internal;
using GameItemSystem = EmulationServer.Game.Items.ItemSystem;
using EmulationServer.WorldServer.Networking.Packets;
using EmulationServer.WorldServer.Networking.Sessions;
using EmulationServer.Game.Players;
using WorldPlayerSessionRegistry = EmulationServer.WorldServer.Players.PlayerSessionRegistry;
using EmulationServer.WorldServer.Networking.Socket;
using EmulationServer.Game.WorldData;
using EmulationServer.Game.Movement;

/**
  * File overview: src/WorldServer/Core/WorldServer.cs
  * Documents the WorldServer source file in the world server startup, client networking, gameplay routing, and persistence area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.WorldServer.Core;

/**
  * Owns the world server behavior for the world server startup, client networking, gameplay routing, and persistence layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class WorldServer : IAsyncDisposable
{
    /**
      * Holds the private settings state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly WorldServerSettings _settings;
    /**
      * Holds the private host state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly EmulationServerHost _host;
    /**
      * Holds the private realm status reporter state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly WorldRealmStatusReporter _realmStatusReporter;
    /**
      * Holds the private command service state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly WorldConsoleCommandService _commandService;
    /**
      * Holds the private auth database state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly MySqlDatabaseService _authDatabase;
    /**
      * Holds the private character database state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly MySqlDatabaseService _characterDatabase;
    /**
      * Holds the private world database state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly MySqlDatabaseService _worldDatabase;
    /**
      * Holds the private account repository state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly WorldAccountRepository _accountRepository;
    /**
      * Holds the private character repository state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly CharacterRepository _characterRepository;
    /**
      * Holds the private world template repository state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly WorldTemplateRepository _worldTemplateRepository;
    /**
      * Holds the private character creation service state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly CharacterCreationService _characterCreationService;
    /**
      * Holds the private item system state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly GameItemSystem _itemSystem;
    /**
      * Holds the private chat system state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly GameChatSystem _chatSystem;
    /**
      * Holds the private in game command service state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly GameInGameCommandService _inGameCommandService;
    /**
      * Holds the private player session registry state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly WorldPlayerSessionRegistry _playerSessionRegistry;
    /**
      * Holds the private client listener state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly WorldClientSocketListener _clientListener;
    /**
      * Holds the private world template data state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private WorldTemplateDataStore _worldTemplateData = WorldTemplateDataStore.Empty;
    private readonly ConcurrentDictionary<string, InternalPeerConnection> _peerConnections = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, InternalServerSession> _serverSessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, InternalMapServiceStatusPacket> _mapServiceStatuses = new(StringComparer.OrdinalIgnoreCase);

    /**
      * Holds the private game data state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private WorldGameDataStore _gameData = WorldGameDataStore.Empty;

    /**
      * Initializes a new WorldServer instance with the dependencies required by the world server startup, client networking, gameplay routing, and persistence workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: settings.
      */
    public WorldServer(WorldServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _settings = settings;
        _host = new EmulationServerHost("WorldServer", settings.InternalNetwork, CreateCallbacks());
        _commandService = new WorldConsoleCommandService(ExecuteMapCommandAsync);

        _authDatabase = new MySqlDatabaseService(settings.Databases.Auth);
        _characterDatabase = new MySqlDatabaseService(settings.Databases.Character);
        _worldDatabase = new MySqlDatabaseService(settings.Databases.World);
        _accountRepository = new WorldAccountRepository(_authDatabase);
        _characterRepository = new CharacterRepository(
            _characterDatabase,
            entry => _worldTemplateData.TryGetItemTemplate(entry, out ItemTemplateRecord itemTemplate) ? itemTemplate : null,
            () => _worldTemplateData);
        _worldTemplateRepository = new WorldTemplateRepository(_worldDatabase);
        _characterCreationService = new CharacterCreationService(_characterRepository, () => _gameData, () => _worldTemplateData);
        _itemSystem = new GameItemSystem(() => _worldTemplateData);
        _playerSessionRegistry = new WorldPlayerSessionRegistry();
        _chatSystem = new GameChatSystem(() => _gameData);
        _inGameCommandService = new GameInGameCommandService();
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
                _characterRepository,
                _characterCreationService,
                _itemSystem,
                _chatSystem,
                _inGameCommandService,
                _playerSessionRegistry,
                ResolveMapAvailabilityForLogin,
                NotifyMapServicePlayerEnteredWorldAsync,
                NotifyMapServicePlayerLeftWorldAsync,
                NotifyMapServicePlayerMovementAsync,
                NotifyMapServicePlayerClientPacketAsync,
                settings.MessageOfTheDay,
                settings.PlayerSaveInterval,
                _realmStatusReporter.SetActiveConnections,
                _realmStatusReporter.SendCharacterCountSnapshotNowAsync));
    }

    /**
      * Starts the start workflow and prepares the component to accept runtime work.
      * Startup is ordered so validation and dependency setup finish before services are announced as available.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LoadGameDataIfEnabled();
        await ValidateDatabaseConnectionsAsync(cancellationToken);
        await LogCharacterPlayerStateTablesAsync(cancellationToken);
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
      * Stops the stop workflow and releases owned runtime resources in a controlled order.
      * Shutdown logic is centralized to avoid dangling connections, incomplete saves, or partially registered services.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _realmStatusReporter.StopAsync(cancellationToken);
        await _clientListener.StopAsync(cancellationToken);
        await _host.StopAsync(cancellationToken);
    }

    /**
      * Stops the dispose workflow and releases owned runtime resources in a controlled order.
      * Shutdown logic is centralized to avoid dangling connections, incomplete saves, or partially registered services.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
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
      * Creates the callbacks result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
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
      * Handles the on server authenticated event for the world server startup, client networking, gameplay routing, and persistence workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: session, remoteServerName, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task OnServerAuthenticatedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        _serverSessions[remoteServerName] = session;
        Logger.Write(LogType.NETWORK, $"WorldServer registered incoming internal session from {remoteServerName}.", "WorldServer");

        if (string.Equals(remoteServerName, "ProxyServer", StringComparison.OrdinalIgnoreCase))
        {
            await AnnounceWorldCapacityAsync(session.SendPacketAsync, remoteServerName, cancellationToken);
        }
    }

    /**
      * Handles the on server disconnected event for the world server startup, client networking, gameplay routing, and persistence workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: session, remoteServerName, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private Task OnServerDisconnectedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        _serverSessions.TryRemove(remoteServerName, out _);
        Logger.Write(LogType.NETWORK, $"WorldServer removed incoming internal session from {remoteServerName}.", "WorldServer");

        return Task.CompletedTask;
    }

    /**
      * Handles the on peer authenticated event for the world server startup, client networking, gameplay routing, and persistence workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: connection, remoteServerName, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
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
      * Handles the on peer disconnected event for the world server startup, client networking, gameplay routing, and persistence workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: connection, remoteServerName, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private Task OnPeerDisconnectedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        _peerConnections.TryRemove(remoteServerName, out _);
        Logger.Write(LogType.NETWORK, $"WorldServer removed outgoing internal peer {remoteServerName}.", "WorldServer");

        return Task.CompletedTask;
    }

    /**
      * Handles the on peer packet received event for the world server startup, client networking, gameplay routing, and persistence workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: connection, remoteServerName, packet, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
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
      * Handles the on session packet received event for the world server startup, client networking, gameplay routing, and persistence workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: session, remoteServerName, packet, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
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
      * Resolves whether the player's current map has an online MapServer or InstanceServer owner.
      */
    private MapAvailabilityResult ResolveMapAvailabilityForLogin(PlayerLoginRecord player)
    {
        ArgumentNullException.ThrowIfNull(player);

        int mapId = unchecked((int)player.Map);
        string requiredKind = "World";
        if (_gameData.MapData.TryGetMap(mapId, out EmulationServer.Game.Data.Dbc.Maps.MapDbcRecord map) && map.IsInstanceMap)
        {
            requiredKind = "Instance";
        }

        InternalMapServiceStatusPacket[] candidates = _mapServiceStatuses.Values
            .Where(status => status.MapId == mapId)
            .Where(status => string.Equals(status.State, "Online", StringComparison.OrdinalIgnoreCase))
            .Where(status => IsConnectedMapOwner(status.OwnerServerName))
            .ToArray();

        InternalMapServiceStatusPacket? selected = candidates.FirstOrDefault(status => string.Equals(status.Kind, requiredKind, StringComparison.OrdinalIgnoreCase))
            ?? candidates.FirstOrDefault();

        if (selected is not null)
        {
            return MapAvailabilityResult.Available(selected.OwnerServerName, string.Equals(requiredKind, "Instance", StringComparison.OrdinalIgnoreCase));
        }

        if (candidates.Length == 0)
        {
            return MapAvailabilityResult.Unavailable($"No online map service is currently reporting ownership for map {mapId}.", string.Equals(requiredKind, "Instance", StringComparison.OrdinalIgnoreCase));
        }

        return MapAvailabilityResult.Unavailable($"Map {mapId} is online only on unsupported service kind(s): {string.Join(',', candidates.Select(candidate => candidate.Kind).Distinct(StringComparer.OrdinalIgnoreCase))}.", string.Equals(requiredKind, "Instance", StringComparison.OrdinalIgnoreCase));
    }

    /**
      * Returns whether the named internal map owner is still connected to WorldServer.
      */
    private bool IsConnectedMapOwner(string ownerServerName)
    {
        return _peerConnections.ContainsKey(ownerServerName) || _serverSessions.ContainsKey(ownerServerName);
    }

    /**
      * Notifies the selected map service that a player has entered the game world while the client socket remains on WorldServer.
      */
    private async Task NotifyMapServicePlayerEnteredWorldAsync(
        PlayerLoginRecord player,
        string ownerServerName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (string.IsNullOrWhiteSpace(ownerServerName))
        {
            return;
        }

        string packet = string.Create(
            CultureInfo.InvariantCulture,
            $"{InternalProtocol.PlayerEnterWorld} {player.AccountId} {player.Guid} {player.Name} {player.Map} {player.Zone} {player.PositionX:0.###} {player.PositionY:0.###} {player.PositionZ:0.###} {player.Orientation:0.###}");

        int sent = await SendPacketToServerAsync(ownerServerName, packet, cancellationToken);
        if (sent == 0)
        {
            Logger.Write(LogType.WARNING, $"WorldServer could not notify {ownerServerName} that player '{player.Name}' entered map {player.Map}; no active internal connection was available.", "WorldServer");
            return;
        }

        Logger.Write(LogType.NETWORK, $"WorldServer notified {ownerServerName} that player '{player.Name}' entered map {player.Map}.", "WorldServer");
    }

    /**
      * Notifies the selected map service that a player has left the game world.
      */
    private async Task NotifyMapServicePlayerLeftWorldAsync(
        PlayerLoginRecord player,
        string ownerServerName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (string.IsNullOrWhiteSpace(ownerServerName))
        {
            return;
        }

        string packet = string.Create(
            CultureInfo.InvariantCulture,
            $"{InternalProtocol.PlayerLeaveWorld} {player.AccountId} {player.Guid} {player.Name} {player.Map} {player.Zone}");

        int sent = await SendPacketToServerAsync(ownerServerName, packet, cancellationToken);
        if (sent == 0)
        {
            Logger.Write(LogType.WARNING, $"WorldServer could not notify {ownerServerName} that player '{player.Name}' left map {player.Map}; no active internal connection was available.", "WorldServer");
            return;
        }

        Logger.Write(LogType.NETWORK, $"WorldServer notified {ownerServerName} that player '{player.Name}' left map {player.Map}.", "WorldServer");
    }

    /**
      * Notifies the selected map service about the latest authoritative player movement state.
      */
    private async Task NotifyMapServicePlayerMovementAsync(
        PlayerLoginRecord player,
        string ownerServerName,
        PlayerMovementState movement,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(movement);

        if (string.IsNullOrWhiteSpace(ownerServerName))
        {
            return;
        }

        string packet = string.Create(
            CultureInfo.InvariantCulture,
            $"{InternalProtocol.PlayerMovement} {player.AccountId} {player.Guid} 0x{movement.Opcode:X4} {movement.Map} {movement.Zone} {movement.PositionX:0.###} {movement.PositionY:0.###} {movement.PositionZ:0.###} {movement.Orientation:0.###} {(uint)movement.Flags} {movement.ClientTime}");

        int sent = await SendPacketToServerAsync(ownerServerName, packet, cancellationToken);
        if (sent == 0)
        {
            Logger.Write(LogType.WARNING, $"WorldServer could not route movement for player '{player.Name}' to {ownerServerName}; no active internal connection was available.", "WorldServer");
            return;
        }

        Logger.Write(LogType.TRACE, $"WorldServer routed movement {movement.Opcode:X4} for player '{player.Name}' to {ownerServerName}: map={movement.Map}, zone={movement.Zone}, position=({movement.PositionX:0.##}, {movement.PositionY:0.##}, {movement.PositionZ:0.##}).", "WorldServer");
    }

    /**
      * Forwards unhandled in-world client packets to the selected map service while WorldServer keeps owning the socket.
      */
    private async Task NotifyMapServicePlayerClientPacketAsync(
        PlayerLoginRecord player,
        string ownerServerName,
        WorldPacket worldPacket,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(worldPacket);

        if (string.IsNullOrWhiteSpace(ownerServerName))
        {
            return;
        }

        string payloadHex = Convert.ToHexString(worldPacket.Payload);
        string packet = string.Create(
            CultureInfo.InvariantCulture,
            $"{InternalProtocol.PlayerClientPacket} {player.AccountId} {player.Guid} 0x{(ushort)worldPacket.Opcode:X4} {payloadHex}");

        if (packet.Length > InternalProtocol.MaximumPacketLineLength)
        {
            Logger.Write(LogType.WARNING, $"WorldServer skipped forwarding {worldPacket.Opcode} for player '{player.Name}' because the routed packet line was too large ({packet.Length} characters).", "WorldServer");
            return;
        }

        int sent = await SendPacketToServerAsync(ownerServerName, packet, cancellationToken);
        if (sent == 0)
        {
            Logger.Write(LogType.WARNING, $"WorldServer could not forward {worldPacket.Opcode} for player '{player.Name}' to {ownerServerName}; no active internal connection was available.", "WorldServer");
            return;
        }

        Logger.Write(LogType.TRACE, $"WorldServer forwarded {worldPacket.Opcode} from player '{player.Name}' to {ownerServerName}.", "WorldServer");
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
            Logger.Write(LogType.WARNING, $"WorldServer has no connected MapServer or InstanceServer targets for map command '{action}' MapId={mapId}.", "WorldServer");
            return;
        }

        foreach (string target in targets)
        {
            int sent = await SendPacketToServerAsync(target, packet, cancellationToken);
            if (sent == 0)
            {
                Logger.Write(LogType.WARNING, $"WorldServer could not send map {action} command for MapId={mapId} to {target}; no active connection was available.", "WorldServer");
                continue;
            }

            Logger.Write(LogType.NETWORK, $"WorldServer sent map {action} command for MapId={mapId} to {target} ({sent} connection(s)).", "WorldServer");
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
      * Performs the announce world capacity operation for the world server startup, client networking, gameplay routing, and persistence workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: sendPacketAsync, remoteServerName, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task AnnounceWorldCapacityAsync(
        Func<string, CancellationToken, Task> sendPacketAsync,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        string packet = $"{InternalProtocol.WorldCapacity} {_settings.MaxConnections}";
        await sendPacketAsync(packet, cancellationToken);

        Logger.Write(LogType.NETWORK, $"WorldServer announced max connections to {remoteServerName}: {_settings.MaxConnections}.", "WorldServer");
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
                Logger.Write(LogType.SUCCESS, message, "WorldServer");
                break;

            case "notfound":
                Logger.Write(LogType.TRACE, message, "WorldServer");
                break;

            case "ignored":
                Logger.Write(LogType.INFORMATION, message, "WorldServer");
                break;

            default:
                Logger.Write(LogType.WARNING, message, "WorldServer");
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
            Logger.Write(LogType.WARNING, $"WorldServer received invalid MAP_SERVICE_STATUS packet from {remoteServerName}: {packet}", "WorldServer");
            return;
        }

        _mapServiceStatuses[GetStatusKey(status)] = status;

        string message = $"WorldServer received {status.OwnerServerName} {status.Kind} map service status: map={status.MapId}, instance={status.InstanceId}, state={status.State}, tick={status.Tick}, players={status.ActivePlayers}, grids={status.ActiveGrids}, load={status.LoadPercent:0.##}%, avgTick={status.AverageTickMilliseconds:0.###} ms.";

        if (status.LoadPercent >= 85d)
        {
            Logger.Write(LogType.WARNING, message, "WorldServer");
            return;
        }

        Logger.Write(LogType.TRACE, message, "WorldServer");
    }

    /**
      * Writes write cached map info data to the target packet, stream, or persistent store.
      * The method keeps binary layout and serialization rules centralized for easier packet review and compatibility fixes.
      * Inputs used by this operation: mapId.
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
            Logger.Write(LogType.WARNING, $"WorldServer has no cached map service status for MapId={mapId}. {dbcDescription} Sending live info request to connected map services...", "WorldServer");
            return;
        }

        Logger.Write(LogType.TRACE, $"Cached map service info for MapId={mapId}: {dbcDescription}", "WorldServer");
        foreach (InternalMapServiceStatusPacket status in statuses)
        {
            Logger.Write(
                LogType.TRACE,
                $"  {status.OwnerServerName} {status.Kind}: instance={status.InstanceId}, state={status.State}, tick={status.Tick}, players={status.ActivePlayers}, grids={status.ActiveGrids}, load={status.LoadPercent:0.##}%, avgTick={status.AverageTickMilliseconds:0.###} ms.",
                "WorldServer");
        }
    }

    /**
      * Determines whether map control server for the world server startup, client networking, gameplay routing, and persistence workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: remoteServerName.
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
        Logger.Write(LogType.DATABASE, "WorldServer validating Auth, Character, and World database connections...", "WorldServer");

        await _authDatabase.ValidateConnectionAsync(cancellationToken);
        Logger.Write(LogType.DATABASE, $"WorldServer Auth database is reachable: {_settings.Databases.Auth.Database}.", "WorldServer");

        await _characterDatabase.ValidateConnectionAsync(cancellationToken);
        Logger.Write(LogType.DATABASE, $"WorldServer Character database is reachable: {_settings.Databases.Character.Database}.", "WorldServer");

        await _worldDatabase.ValidateConnectionAsync(cancellationToken);
        Logger.Write(LogType.DATABASE, $"WorldServer World database is reachable: {_settings.Databases.World.Database}.", "WorldServer");
    }

    /**
      * Logs the character-side state tables that are loaded during character creation and world login.
      */
    private async Task LogCharacterPlayerStateTablesAsync(CancellationToken cancellationToken)
    {
        Logger.Write(LogType.DATABASE, "WorldServer checking character player-state tables used by world login and equipment loading...", "WorldServer");

        IReadOnlyDictionary<string, bool> availability = await _characterRepository.GetPlayerStateTableAvailabilityAsync(cancellationToken);
        foreach (KeyValuePair<string, bool> table in availability.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
        {
            LogType logType = table.Value ? LogType.DATABASE : LogType.WARNING;
            string state = table.Value ? "available" : "missing";
            Logger.Write(logType, $"Character database table `{table.Key}` is {state}.", "WorldServer");
        }
    }

    /**
      * Loads MaNGOS world database templates needed by character creation and world login into memory.
      */
    private async Task LoadWorldTemplateDataAsync(CancellationToken cancellationToken)
    {
        Logger.Write(
            LogType.DATABASE,
            "WorldServer loading MaNGOS world database templates into memory: playercreateinfo, item_template, player level stats, XP, and playercreateinfo_* tables...",
            "WorldServer");

        _worldTemplateData = await _worldTemplateRepository.LoadAsync(cancellationToken);

        if (_worldTemplateData.PlayerCreateInfo.Count == 0)
        {
            throw new InvalidOperationException("World database table `playercreateinfo` is empty. Character creation cannot resolve race/class start positions.");
        }

        if (_worldTemplateData.ItemTemplates.Count == 0)
        {
            throw new InvalidOperationException("World database table `item_template` is empty. Character creation cannot resolve starter items or equipment display data.");
        }

        LogOptionalWorldTemplateCount("player_levelstats", _worldTemplateData.PlayerLevelStatsCount, "base race/class/level stats will fall back to generated defaults");
        LogOptionalWorldTemplateCount("player_classlevelstats", _worldTemplateData.PlayerClassLevelStatsCount, "base health/mana will fall back to generated defaults");
        LogOptionalWorldTemplateCount("player_xp_for_level", _worldTemplateData.PlayerLevelExperienceCount, "next-level XP will fall back to generated defaults");
        LogOptionalWorldTemplateCount("playercreateinfo_action", _worldTemplateData.PlayerCreateActionCount, "new characters will fall back to hardcoded starter action buttons");
        LogOptionalWorldTemplateCount("playercreateinfo_item", _worldTemplateData.PlayerCreateItemCount, "new characters will fall back to CharStartOutfit.dbc starter items");
        LogOptionalWorldTemplateCount("playercreateinfo_spell", _worldTemplateData.PlayerCreateSpellCount, "new characters will fall back to hardcoded starter spells");

        Logger.Write(
            LogType.DATABASE,
            $"World database templates ready (playercreateinfo={_worldTemplateData.PlayerCreateInfo.Count}, item_template={_worldTemplateData.ItemTemplates.Count}, player_levelstats={_worldTemplateData.PlayerLevelStatsCount}, player_classlevelstats={_worldTemplateData.PlayerClassLevelStatsCount}, player_xp_for_level={_worldTemplateData.PlayerLevelExperienceCount}, playercreateinfo_action={_worldTemplateData.PlayerCreateActionCount}, playercreateinfo_item={_worldTemplateData.PlayerCreateItemCount}, playercreateinfo_spell={_worldTemplateData.PlayerCreateSpellCount}).",
            "WorldServer");
    }

    /**
      * Performs the log optional world template count operation for the world server startup, client networking, gameplay routing, and persistence workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: tableName, count, fallbackMessage.
      */
    private static void LogOptionalWorldTemplateCount(string tableName, int count, string fallbackMessage)
    {
        if (count == 0)
        {
            Logger.Write(LogType.WARNING, $"World database table `{tableName}` was not loaded or is empty; {fallbackMessage}.", "WorldServer");
        }
        else
        {
            Logger.Write(LogType.DATABASE, $"World database table `{tableName}` loaded {count} row(s).", "WorldServer");
        }
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
            Logger.Write(LogType.INFORMATION, "WorldServer game data loading is disabled. Enable [GameData] when extracted DBC data is ready.", "WorldServer");
            return;
        }

        Logger.Write(LogType.NOTICE, "WorldServer loading required DBC data into memory...");

        _gameData = WorldGameDataStore.Load(
            gameDataSettings.DataDirectory,
            gameDataSettings.DbcDirectory,
            gameDataSettings.RequiredDbcFiles);

        Logger.Write(
            LogType.SUCCESS,
            $"Game data ready (dbcStores={_gameData.DbcStores.Count}, maps={_gameData.MapData.Maps.Count}, areas={_gameData.MapData.Areas.Count}, races={_gameData.CharacterData.Races.Count}, classes={_gameData.CharacterData.Classes.Count}, starterOutfits={_gameData.CharacterData.StartOutfits.Count}, itemDisplays={_gameData.ItemData.DisplayInfo.Count}, spells={_gameData.SpellData.Spells.Count}, factions={_gameData.FactionData.Factions.Count}, chatChannels={_gameData.ChatData.Records.Count}).",
            "WorldServer");
    }
}
