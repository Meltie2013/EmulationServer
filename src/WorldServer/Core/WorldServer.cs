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
using EmulationServer.Game.Data.Dbc.Maps;
using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Peers;
using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Network.Networking.Sessions;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.Database.Accounts;
using EmulationServer.Game.Commands;
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
using EmulationServer.Shared.Timing;

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
public sealed class WorldServer : IInGameMapCommandExecutor, IInGameRbacCommandExecutor, IInGameServerCommandExecutor, IAsyncDisposable
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
      * Holds the private WorldServer health status cancellation state used by the owning component.
      */
    private CancellationTokenSource? _worldHealthStatusCancellation;
    /**
      * Holds the private WorldServer health status task state used by the owning component.
      */
    private Task? _worldHealthStatusTask;
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
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _scheduledMapControlTimers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ISteadyClock _clock = SystemSteadyClock.Instance;
    private CancellationTokenSource? _serverControlTimerCancellation;
    private int _serverControlRequested;

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

        _authDatabase = new MySqlDatabaseService(settings.Databases.Auth);
        _characterDatabase = new MySqlDatabaseService(settings.Databases.Character);
        _worldDatabase = new MySqlDatabaseService(settings.Databases.World);
        _accountRepository = new WorldAccountRepository(_authDatabase);
        _characterRepository = new CharacterRepository(
            _characterDatabase,
            entry => _worldTemplateData.TryGetItemTemplate(entry, out ItemTemplateRecord itemTemplate) ? itemTemplate : null,
            () => _worldTemplateData,
            () => _gameData);
        _worldTemplateRepository = new WorldTemplateRepository(_worldDatabase);
        _characterCreationService = new CharacterCreationService(_characterRepository, () => _gameData, () => _worldTemplateData);
        _itemSystem = new GameItemSystem(() => _worldTemplateData);
        _playerSessionRegistry = new WorldPlayerSessionRegistry();
        _chatSystem = new GameChatSystem(() => _gameData);
        _inGameCommandService = new GameInGameCommandService(new InGameCommandDependencies
        {
            AccountCommands = new DatabaseInGameAccountCommandExecutor(new AccountRepository(_authDatabase)),
            MapCommands = this,
            RbacCommands = this,
            ServerCommands = this,
        });
        _realmStatusReporter = new WorldRealmStatusReporter(
            settings.RealmStatus,
            settings.InternalNetwork.RegistrationKey,
            settings.MaxConnections,
            settings.InternalNetwork.LatencyReportInterval,
            settings.InternalNetwork.LatencyLoggingEnabled,
            settings.InternalNetwork.LatencyLogInterval,
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
                NotifyActivePlayerCountChanged,
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

            await _realmStatusReporter.StartAsync(cancellationToken);
            StartWorldHealthStatusLoop(cancellationToken);

            await Task.WhenAll(hostTask, clientTask);
        }
        finally
        {
            await StopWorldHealthStatusLoopAsync(CancellationToken.None);
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
        CancellationTokenSource? serverControlTimerCancellation = _serverControlTimerCancellation;
        if (serverControlTimerCancellation is not null)
        {
            await serverControlTimerCancellation.CancelAsync();
        }

        foreach (CancellationTokenSource timerCancellation in _scheduledMapControlTimers.Values)
        {
            await timerCancellation.CancelAsync();
        }

        await StopWorldHealthStatusLoopAsync(cancellationToken);
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
            await AnnounceWorldHealthStatusAsync(session.SendPacketAsync, remoteServerName, cancellationToken);
        }
    }

    /**
      * Handles the on server disconnected event for the world server startup, client networking, gameplay routing, and persistence workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: session, remoteServerName, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task OnServerDisconnectedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        if (!_serverSessions.TryGetValue(remoteServerName, out InternalServerSession? currentSession) || !ReferenceEquals(currentSession, session))
        {
            Logger.Write(LogType.TRACE, $"Ignored stale incoming internal disconnect from {remoteServerName}; a newer session is already registered.", "WorldServer");
            return;
        }

        ((ICollection<KeyValuePair<string, InternalServerSession>>)_serverSessions).Remove(new KeyValuePair<string, InternalServerSession>(remoteServerName, session));
        Logger.Write(LogType.NETWORK, $"WorldServer removed incoming internal session from {remoteServerName}.", "WorldServer");

        if (IsMapControlServer(remoteServerName))
        {
            await MarkMapOwnerUnavailableAsync(remoteServerName, "incoming internal connection disconnected", cancellationToken);
        }
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
            await AnnounceWorldHealthStatusAsync(connection.SendPacketAsync, remoteServerName, cancellationToken);
        }
    }

    /**
      * Handles the on peer disconnected event for the world server startup, client networking, gameplay routing, and persistence workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: connection, remoteServerName, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task OnPeerDisconnectedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        if (!_peerConnections.TryGetValue(remoteServerName, out InternalPeerConnection? currentConnection) || !ReferenceEquals(currentConnection, connection))
        {
            Logger.Write(LogType.TRACE, $"Ignored stale outgoing internal peer disconnect from {remoteServerName}; a newer connection is already registered.", "WorldServer");
            return;
        }

        ((ICollection<KeyValuePair<string, InternalPeerConnection>>)_peerConnections).Remove(new KeyValuePair<string, InternalPeerConnection>(remoteServerName, connection));
        Logger.Write(LogType.NETWORK, $"WorldServer removed outgoing internal peer {remoteServerName}.", "WorldServer");

        if (IsMapControlServer(remoteServerName))
        {
            await MarkMapOwnerUnavailableAsync(remoteServerName, "outgoing internal peer disconnected", cancellationToken);
        }
    }

    /**
      * Handles the on peer packet received event for the world server startup, client networking, gameplay routing, and persistence workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: connection, remoteServerName, packet, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task OnPeerPacketReceivedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        string packet,
        CancellationToken cancellationToken)
    {
        await HandleMapServicePacketAsync(remoteServerName, packet, cancellationToken);
    }

    /**
      * Handles the on session packet received event for the world server startup, client networking, gameplay routing, and persistence workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: session, remoteServerName, packet, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task OnSessionPacketReceivedAsync(
        InternalServerSession session,
        string remoteServerName,
        string packet,
        CancellationToken cancellationToken)
    {
        await HandleMapServicePacketAsync(remoteServerName, packet, cancellationToken);
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

        Logger.Write(LogType.SYSTEM, $"WorldServer notified {ownerServerName} that player '{player.Name}' entered map {player.Map}.", "WorldServer");
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

        Logger.Write(LogType.SYSTEM, $"WorldServer notified {ownerServerName} that player '{player.Name}' left map {player.Map}.", "WorldServer");
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

    }

    /**
      * Executes a map control command from the in-game RBAC command system and returns chat-safe feedback.
      */
    public async Task<string> ExecuteMapCommandAsync(string action, int mapId, TimeSpan delay, string requestedBy, CancellationToken cancellationToken)
    {
        string normalizedAction = (action ?? string.Empty).Trim().ToLowerInvariant();
        if (normalizedAction is not ("info" or "start" or "shutdown" or "restart"))
        {
            return $"Unknown map command action '{action}'.";
        }

        if (delay > TimeSpan.Zero && normalizedAction is not ("shutdown" or "restart"))
        {
            return $"Map {normalizedAction} does not support a timer.";
        }

        if (delay > TimeSpan.Zero)
        {
            return ScheduleMapControlAsync(normalizedAction, mapId, delay, requestedBy, cancellationToken);
        }

        string info = string.Equals(normalizedAction, "info", StringComparison.OrdinalIgnoreCase)
            ? FormatCachedMapInfo(mapId)
            : string.Empty;

        MapCommandDispatchResult dispatch = await SendMapCommandToTargetsAsync(normalizedAction, mapId, cancellationToken);
        if (dispatch.TargetCount == 0)
        {
            string message = $"No connected MapServer or InstanceServer targets are available for map {mapId}.";
            return string.IsNullOrWhiteSpace(info) ? message : $"{info}\n{message}";
        }

        string dispatchMessage = dispatch.SentConnections == 0
            ? $"Map {normalizedAction} command for map {mapId} could not be delivered to any active connection."
            : $"Map {normalizedAction} command for map {mapId} was sent to {dispatch.SentConnections} connection(s) across {dispatch.TargetCount} target(s).";

        return string.IsNullOrWhiteSpace(info) ? dispatchMessage : $"{info}\n{dispatchMessage}";
    }

    /**
      * Sends the internal map service command packet to the best available MapServer or InstanceServer targets.
      */
    private async Task<MapCommandDispatchResult> SendMapCommandToTargetsAsync(string action, int mapId, CancellationToken cancellationToken)
    {
        string commandId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        InternalMapServiceCommandPacket command = new(commandId, action, mapId);
        string packet = command.ToPacketLine();

        string[] targets = GetMapCommandTargets(mapId);
        if (targets.Length == 0)
        {
            Logger.Write(LogType.WARNING, $"WorldServer has no connected MapServer or InstanceServer targets for map command '{action}' MapId={mapId}.", "WorldServer");
            return new MapCommandDispatchResult(0, 0);
        }

        int sentConnections = 0;
        foreach (string target in targets)
        {
            int sent = await SendPacketToServerAsync(target, packet, cancellationToken);
            if (sent == 0)
            {
                Logger.Write(LogType.WARNING, $"WorldServer could not send map {action} command for MapId={mapId} to {target}; no active connection was available.", "WorldServer");
                continue;
            }

            sentConnections += sent;
            Logger.Write(LogType.NETWORK, $"WorldServer sent map {action} command for MapId={mapId} to {target} ({sent} connection(s)).", "WorldServer");
        }

        return new MapCommandDispatchResult(targets.Length, sentConnections);
    }

    /**
      * Schedules a delayed map restart or shutdown using the shared steady-clock countdown path.
      */
    private string ScheduleMapControlAsync(string action, int mapId, TimeSpan delay, string requestedBy, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string key = mapId.ToString(CultureInfo.InvariantCulture);
        CancellationTokenSource timerCancellation = new();
        if (!_scheduledMapControlTimers.TryAdd(key, timerCancellation))
        {
            timerCancellation.Dispose();
            return $"A map shutdown or restart is already scheduled for map {mapId}.";
        }

        string safeRequestedBy = string.IsNullOrWhiteSpace(requestedBy) ? "Unknown" : requestedBy.Trim();
        _ = Task.Run(() => ExecuteScheduledMapControlAsync(key, action, mapId, delay, safeRequestedBy, timerCancellation), CancellationToken.None);

        string when = delay <= TimeSpan.Zero ? "immediately" : $"in {CommandArgumentParser.FormatDuration(delay)}";
        return $"Map {mapId} {action} scheduled {when} by {safeRequestedBy}. Players on that map will receive countdown warnings.";
    }

    /**
      * Runs the delayed map-control workflow outside the chat packet handler.
      */
    private async Task ExecuteScheduledMapControlAsync(string key, string action, int mapId, TimeSpan delay, string requestedBy, CancellationTokenSource timerCancellation)
    {
        try
        {
            CancellationToken cancellationToken = timerCancellation.Token;
            await BroadcastMapControlWarningAsync(action, mapId, delay, requestedBy, cancellationToken);

            await SteadyCountdownRunner.RunAsync(
                _clock,
                delay,
                SteadyCountdownRunner.DefaultWarningThresholds,
                (remaining, warningCancellationToken) => BroadcastMapControlWarningAsync(action, mapId, remaining, requestedBy, warningCancellationToken),
                async elapsedCancellationToken =>
                {
                    await BroadcastMapControlNowAsync(action, mapId, requestedBy, elapsedCancellationToken);
                    MapCommandDispatchResult dispatch = await SendMapCommandToTargetsAsync(action, mapId, elapsedCancellationToken);
                    Logger.Write(
                        dispatch.SentConnections > 0 ? LogType.NETWORK : LogType.WARNING,
                        $"Scheduled map {action} for MapId={mapId} dispatched to {dispatch.SentConnections} connection(s) across {dispatch.TargetCount} target(s). RequestedBy={requestedBy}",
                        "WorldServer");
                },
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Logger.Write(LogType.WARNING, $"Scheduled map {action} for MapId={mapId} was canceled.", "WorldServer");
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.FAILED, $"Scheduled map {action} for MapId={mapId} failed: {exception.Message}", "WorldServer");
        }
        finally
        {
            _scheduledMapControlTimers.TryRemove(key, out _);
            timerCancellation.Dispose();
        }
    }

    /**
      * Broadcasts a countdown notice to active players currently on the affected map.
      */
    private Task BroadcastMapControlWarningAsync(string action, int mapId, TimeSpan remaining, string requestedBy, CancellationToken cancellationToken)
    {
        string message = $"Map {mapId} will {action} in {CommandArgumentParser.FormatDuration(remaining)}. Requested by {requestedBy}.";
        return BroadcastSystemMessageAsync(message, session => session.CurrentPlayer?.Map == unchecked((uint)mapId), cancellationToken);
    }

    /**
      * Broadcasts the final map-control notice to active players currently on the affected map.
      */
    private Task BroadcastMapControlNowAsync(string action, int mapId, string requestedBy, CancellationToken cancellationToken)
    {
        string message = $"Map {mapId} is {FormatActionProgress(action)} now. Requested by {requestedBy}.";
        return BroadcastSystemMessageAsync(message, session => session.CurrentPlayer?.Map == unchecked((uint)mapId), cancellationToken);
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
      * Applies the active player count update locally and reports the new WorldServer health snapshot to ProxyServer.
      */
    private void NotifyActivePlayerCountChanged(int activePlayerCount)
    {
        _realmStatusReporter.SetActiveConnections(activePlayerCount);
        _ = SendWorldHealthStatusSafelyAsync(CancellationToken.None);
    }

    /**
      * Starts the WorldServer health status loop used to feed ProxyServer with active player load data.
      */
    private void StartWorldHealthStatusLoop(CancellationToken cancellationToken)
    {
        if (_worldHealthStatusTask is not null)
        {
            return;
        }

        _worldHealthStatusCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _worldHealthStatusTask = Task.Run(() => RunWorldHealthStatusLoopAsync(_worldHealthStatusCancellation.Token), CancellationToken.None);

        Logger.Write(LogType.THREAD, $"WorldServer health status report loop started with interval {_settings.InternalNetwork.LatencyReportInterval.TotalSeconds:0.##} second(s).", "WorldServer");
    }

    /**
      * Stops the WorldServer health status loop during normal server shutdown.
      */
    private async Task StopWorldHealthStatusLoopAsync(CancellationToken cancellationToken)
    {
        CancellationTokenSource? healthCancellation = _worldHealthStatusCancellation;
        if (healthCancellation is not null)
        {
            await healthCancellation.CancelAsync();
        }

        Task? healthTask = _worldHealthStatusTask;
        _worldHealthStatusTask = null;
        _worldHealthStatusCancellation = null;

        if (healthTask is not null)
        {
            try
            {
                Task completedTask = await Task.WhenAny(healthTask, _clock.DelayAsync(TimeSpan.FromSeconds(2), cancellationToken).AsTask());
                if (completedTask == healthTask)
                {
                    await healthTask;
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
        }

        healthCancellation?.Dispose();
    }

    /**
      * Runs periodic WorldServer health status reporting until shutdown.
      */
    private async Task RunWorldHealthStatusLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await SendWorldHealthStatusSafelyAsync(cancellationToken);
                await _clock.DelayAsync(_settings.InternalNetwork.LatencyReportInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown.
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.CRITICAL, exception.ToString(), "WorldServer");
        }
    }

    /**
      * Sends the latest WorldServer health status to ProxyServer if a connection is available.
      */
    private async Task SendWorldHealthStatusSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            string packet = CreateWorldHealthStatusPacket();
            int sent = await SendPacketToServerAsync("ProxyServer", packet, cancellationToken);
            if (sent > 0)
            {
                Logger.Write(LogType.TRACE, $"WorldServer reported health status to ProxyServer: players={_playerSessionRegistry.ActivePlayerCount}/{_settings.MaxConnections}.", "WorldServer");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown.
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException or InvalidOperationException)
        {
            Logger.Write(LogType.DEBUG, $"WorldServer could not report health status to ProxyServer: {exception.Message}", "WorldServer");
        }
    }

    /**
      * Sends a WorldServer health status snapshot to a newly authenticated ProxyServer connection.
      */
    private async Task AnnounceWorldHealthStatusAsync(
        Func<string, CancellationToken, Task> sendPacketAsync,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        string packet = CreateWorldHealthStatusPacket();
        await sendPacketAsync(packet, cancellationToken);

        Logger.Write(LogType.NETWORK, $"WorldServer announced health status to {remoteServerName}: players={_playerSessionRegistry.ActivePlayerCount}/{_settings.MaxConnections}.", "WorldServer");
    }

    /**
      * Creates the protocol packet carrying WorldServer health input values for ProxyServer.
      */
    private string CreateWorldHealthStatusPacket()
    {
        InternalWorldHealthStatusPacket status = new(
            "WorldServer",
            _playerSessionRegistry.ActivePlayerCount,
            _settings.MaxConnections,
            _clock.UtcNow);

        return status.ToPacketLine();
    }

    /**
      * Handles a single operation or packet and keeps the calling code focused on flow control.
      * The method is part of WorldServer and keeps this workflow isolated from the caller.
      */
    private async Task HandleMapServicePacketAsync(string remoteServerName, string packet, CancellationToken cancellationToken)
    {
        if (InternalMapServiceCommandResultPacket.TryParse(packet, out InternalMapServiceCommandResultPacket result))
        {
            HandleMapServiceCommandResult(remoteServerName, result);
            return;
        }

        if (packet.StartsWith(InternalProtocol.MapServiceStatus, StringComparison.OrdinalIgnoreCase))
        {
            await HandleMapServiceStatusPacketAsync(remoteServerName, packet, cancellationToken);
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
                // Delivery of the in-game map command is already logged when WorldServer sends it to Map/Instance targets.
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
    private async Task HandleMapServiceStatusPacketAsync(string remoteServerName, string packet, CancellationToken cancellationToken)
    {
        if (!InternalMapServiceStatusPacket.TryParse(packet, out InternalMapServiceStatusPacket status))
        {
            Logger.Write(LogType.WARNING, $"WorldServer received invalid MAP_SERVICE_STATUS packet from {remoteServerName}: {packet}", "WorldServer");
            return;
        }

        string key = GetStatusKey(status);
        _mapServiceStatuses.TryGetValue(key, out InternalMapServiceStatusPacket? previous);
        _mapServiceStatuses[key] = status;

        bool isOnline = IsMapServiceOnline(status.State);
        bool previousIsOnline = previous is not null && IsMapServiceOnline(previous.State);
        bool becameUnavailable = previousIsOnline && !isOnline;
        bool loadWarning = isOnline && status.LoadPercent >= 85d;
        bool loadWarningStarted = loadWarning && (previous is null || previous.LoadPercent < 85d);

        // WorldServer receives map service snapshots from both the direct MapServer/InstanceServer
        // connection and ProxyServer's cached forwarding path. Only log and perform forced logout on
        // an actual Online -> non-Online transition so duplicate Offline snapshots do not double-post.
        if (becameUnavailable)
        {
            Logger.Write(LogType.WARNING, $"WorldServer cached offline map service state for {status.OwnerServerName}: kind={status.Kind}, map={status.MapId}, instance={status.InstanceId}, players={status.ActivePlayers}.", "WorldServer");
            await DisconnectPlayersForUnavailableMapServiceAsync(status, $"{status.OwnerServerName} reported {status.Kind} map service map={status.MapId}, instance={status.InstanceId} as {status.State}.", cancellationToken);
            return;
        }

        if (loadWarningStarted)
        {
            Logger.Write(LogType.WARNING, $"WorldServer cached high-load map service state for {status.OwnerServerName}: kind={status.Kind}, map={status.MapId}, instance={status.InstanceId}, load={status.LoadPercent:0.##}%, avgTick={status.AverageTickMilliseconds:0.###} ms.", "WorldServer");
            return;
        }

        // Routine, first-startup, and duplicate status packets are cached silently.
    }

    /**
      * Marks all cached services for a disconnected map owner as offline and removes players routed through it.
      */
    private async Task MarkMapOwnerUnavailableAsync(string ownerServerName, string reason, CancellationToken cancellationToken)
    {
        InternalMapServiceStatusPacket[] affectedStatuses = _mapServiceStatuses.Values
            .Where(status => string.Equals(status.OwnerServerName, ownerServerName, StringComparison.OrdinalIgnoreCase))
            .Select(status => status with { State = "Offline" })
            .ToArray();

        foreach (InternalMapServiceStatusPacket status in affectedStatuses)
        {
            _mapServiceStatuses[GetStatusKey(status)] = status;
        }

        if (affectedStatuses.Length > 0)
        {
            Logger.Write(LogType.WARNING, $"WorldServer marked {affectedStatuses.Length} cached map service status snapshot(s) for {ownerServerName} as Offline because {reason}.", "WorldServer");
        }

        await DisconnectPlayersForMapOwnerAsync(ownerServerName, affectedStatuses, $"Map service owner {ownerServerName} is unavailable: {reason}.", cancellationToken);
    }

    /**
      * Disconnects active player sessions affected by an explicit non-online service status packet.
      */
    private async Task DisconnectPlayersForUnavailableMapServiceAsync(InternalMapServiceStatusPacket status, string reason, CancellationToken cancellationToken)
    {
        await DisconnectPlayersForMapOwnerAsync(status.OwnerServerName, new[] { status }, reason, cancellationToken);
    }

    /**
      * Finds in-world players currently routed through a map owner and forces a safe logout cleanup.
      */
    private async Task DisconnectPlayersForMapOwnerAsync(
        string ownerServerName,
        IReadOnlyCollection<InternalMapServiceStatusPacket> statuses,
        string reason,
        CancellationToken cancellationToken)
    {
        HashSet<uint> affectedMapIds = statuses
            .Select(status => unchecked((uint)status.MapId))
            .ToHashSet();

        WorldClientSession[] affectedSessions = _playerSessionRegistry.SnapshotSessions()
            .Where(session => string.Equals(session.CurrentMapOwnerServerName, ownerServerName, StringComparison.OrdinalIgnoreCase))
            .Where(session => session.CurrentPlayer is not null)
            .Where(session => affectedMapIds.Count == 0 || affectedMapIds.Contains(session.CurrentPlayer!.Map))
            .ToArray();

        if (affectedSessions.Length == 0)
        {
            return;
        }

        Logger.Write(LogType.WARNING, $"WorldServer disconnecting {affectedSessions.Length} in-world player session(s) routed through {ownerServerName}. {reason}", "WorldServer");

        foreach (WorldClientSession session in affectedSessions)
        {
            try
            {
                await session.DisconnectForMapServiceUnavailableAsync(ownerServerName, reason, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                PlayerLoginRecord? player = session.CurrentPlayer;
                string playerName = player is null ? "unknown" : $"{player.Name} ({player.Guid})";
                Logger.Write(LogType.WARNING, $"WorldServer failed to force-disconnect player {playerName} after {ownerServerName} became unavailable: {exception.Message}", "WorldServer");
            }
        }

        NotifyActivePlayerCountChanged(_playerSessionRegistry.ActivePlayerCount);
    }

    /**
      * Formats cached map service status for an in-game map info response.
      */
    private string FormatCachedMapInfo(int mapId)
    {
        InternalMapServiceStatusPacket[] statuses = _mapServiceStatuses.Values
            .Where(status => status.MapId == mapId)
            .OrderBy(status => status.OwnerServerName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(status => status.InstanceId)
            .ToArray();

        string dbcDescription = _gameData.MapData.DescribeMap(mapId);
        List<string> lines = [$"Map {mapId} info:"];
        AppendMapMetadataLines(lines, mapId);

        if (statuses.Length == 0)
        {
            Logger.Write(LogType.WARNING, $"WorldServer has no cached map service status for MapId={mapId}. {dbcDescription} Sending live info request to connected map services...", "WorldServer");
            lines.Add("Cached services: 0");
            lines.Add("No cached service status is available yet.");
            return string.Join('\n', lines);
        }

        Logger.Write(LogType.TRACE, $"Cached map service info for MapId={mapId}: {dbcDescription}", "WorldServer");

        lines.Add($"Cached services: {statuses.Length}");
        foreach (InternalMapServiceStatusPacket status in statuses)
        {
            lines.Add($"{status.OwnerServerName} {status.Kind} service:");
            lines.Add($"  Instance: {status.InstanceId}");
            lines.Add($"  State: {status.State}");
            lines.Add($"  Uptime: {FormatCachedMapUptime(status)}");
            lines.Add($"  Tick: {status.Tick}");
            lines.Add($"  Players: {status.ActivePlayers}");
            lines.Add($"  Grids: {status.ActiveGrids}");
            lines.Add($"  Load: {status.LoadPercent:0.##}%");
            lines.Add($"  Average Tick: {status.AverageTickMilliseconds:0.###} ms");

            Logger.Write(LogType.TRACE, $"Cached map service info for MapId={mapId}: owner={status.OwnerServerName}, kind={status.Kind}, instance={status.InstanceId}, state={status.State}, uptime={FormatCachedMapUptime(status)}, tick={status.Tick}, players={status.ActivePlayers}, grids={status.ActiveGrids}, load={status.LoadPercent:0.##}%, avgTick={status.AverageTickMilliseconds:0.###} ms.", "WorldServer");
        }

        return string.Join('\n', lines);
    }

    /**
      * Appends map DBC metadata as short chat-safe lines.
      */
    private void AppendMapMetadataLines(List<string> lines, int mapId)
    {
        if (!_gameData.MapData.TryGetMap(mapId, out MapDbcRecord map))
        {
            lines.Add($"DBC: MapId={mapId} is not present in Map.dbc.");
            return;
        }

        lines.Add($"Name: {map.DisplayName}");
        lines.Add($"Type: {map.Type}");
        lines.Add($"Areas: {_gameData.MapData.GetAreasForMap(mapId).Count}");
        lines.Add($"Triggers: {_gameData.MapData.GetTriggersForMap(mapId).Count}");
        lines.Add($"Continents: {_gameData.MapData.GetContinentsForMap(mapId).Count}");
    }

    /**
      * Formats the uptime for one cached map service status line.
      */
    private static string FormatCachedMapUptime(InternalMapServiceStatusPacket status)
    {
        if (!IsMapServiceOnline(status.State))
        {
            return "offline";
        }

        if (status.StartedUtc <= DateTimeOffset.UnixEpoch)
        {
            return "unknown";
        }

        TimeSpan uptime = DateTimeOffset.UtcNow - status.StartedUtc;
        if (uptime < TimeSpan.Zero)
        {
            uptime = TimeSpan.Zero;
        }

        return FormatDuration(uptime);
    }

    /**
      * Formats a compact day/hour/minute/second duration for in-game chat output.
      */
    private static string FormatDuration(TimeSpan duration)
    {
        return duration.TotalDays >= 1
            ? $"{duration.Days}d {duration.Hours:D2}h {duration.Minutes:D2}m {duration.Seconds:D2}s"
            : $"{duration.Hours:D2}h {duration.Minutes:D2}m {duration.Seconds:D2}s";
    }

    /**
      * Reloads RBAC data for every active in-world session so permission changes apply without forcing a relog.
      */
    public async Task<string> ReloadRbacAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<WorldClientSession> sessions = _playerSessionRegistry.SnapshotSessions();
        int reloaded = 0;
        int failed = 0;

        foreach (WorldClientSession session in sessions)
        {
            try
            {
                await session.ReloadPermissionsAsync(cancellationToken);
                reloaded++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failed++;
                Logger.Write(LogType.WARNING, $"WorldServer failed to reload RBAC for session {session.Id}: {exception.Message}", "WorldServer");
            }
        }

        return failed == 0
            ? $"RBAC data was reloaded for {reloaded} active session(s)."
            : $"RBAC data was reloaded for {reloaded} active session(s); {failed} session(s) failed.";
    }

    /**
      * Schedules a shutdown request for the realm connection, connected internal services, and this WorldServer.
      */
    public Task<string> ScheduleShutdownAsync(TimeSpan delay, string requestedBy, CancellationToken cancellationToken)
    {
        return ScheduleServerControlAsync("shutdown", delay, requestedBy, cancellationToken);
    }

    /**
      * Schedules a restart request for the realm connection, connected internal services, and this WorldServer.
      * The internal protocol carries this as a shutdown request with a restart reason so an external supervisor can bring services back up.
      */
    public Task<string> ScheduleRestartAsync(TimeSpan delay, string requestedBy, CancellationToken cancellationToken)
    {
        return ScheduleServerControlAsync("restart", delay, requestedBy, cancellationToken);
    }

    /**
      * Creates one delayed server-control task and prevents overlapping shutdown/restart requests.
      */
    private Task<string> ScheduleServerControlAsync(string action, TimeSpan delay, string requestedBy, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Interlocked.Exchange(ref _serverControlRequested, 1) == 1)
        {
            return Task.FromResult("A server shutdown or restart is already scheduled.");
        }

        string safeRequestedBy = string.IsNullOrWhiteSpace(requestedBy) ? "Unknown" : requestedBy.Trim();
        CancellationTokenSource timerCancellation = new();
        _serverControlTimerCancellation = timerCancellation;
        _ = Task.Run(() => ExecuteScheduledServerControlAsync(action, delay, safeRequestedBy, timerCancellation), CancellationToken.None);

        string when = delay <= TimeSpan.Zero ? "immediately" : $"in {CommandArgumentParser.FormatDuration(delay)}";
        string restartNote = string.Equals(action, "restart", StringComparison.OrdinalIgnoreCase)
            ? " Restart is delivered as a shutdown request with a restart reason for the service supervisor."
            : string.Empty;

        return Task.FromResult($"Server {action} scheduled {when} by {safeRequestedBy}.{restartNote}");
    }

    /**
      * Performs the delayed server-control operation outside the chat packet handler.
      */
    private async Task ExecuteScheduledServerControlAsync(string action, TimeSpan delay, string requestedBy, CancellationTokenSource timerCancellation)
    {
        try
        {
            CancellationToken cancellationToken = timerCancellation.Token;
            if (delay > TimeSpan.Zero)
            {
                await BroadcastServerControlWarningAsync(action, delay, requestedBy, cancellationToken);
            }

            await SteadyCountdownRunner.RunAsync(
                _clock,
                delay,
                SteadyCountdownRunner.DefaultWarningThresholds,
                (remaining, warningCancellationToken) => BroadcastServerControlWarningAsync(action, remaining, requestedBy, warningCancellationToken),
                async elapsedCancellationToken =>
                {
                    await BroadcastServerControlNowAsync(action, requestedBy, elapsedCancellationToken);

                    string reason = string.Equals(action, "restart", StringComparison.OrdinalIgnoreCase)
                        ? $"RestartRequestedBy:{requestedBy}"
                        : $"ShutdownRequestedBy:{requestedBy}";

                    Logger.Write(LogType.WARNING, $"WorldServer executing scheduled server {action}. Reason={reason}", "WorldServer");
                    await BroadcastServerControlRequestAsync(reason, elapsedCancellationToken);
                    await StopAsync(CancellationToken.None);
                },
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Logger.Write(LogType.WARNING, $"Scheduled server {action} was canceled.", "WorldServer");
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.FAILED, $"Scheduled server {action} failed: {exception.Message}", "WorldServer");
        }
        finally
        {
            if (ReferenceEquals(_serverControlTimerCancellation, timerCancellation))
            {
                _serverControlTimerCancellation = null;
            }

            timerCancellation.Dispose();
        }
    }

    /**
      * Broadcasts a countdown notice to every active in-world player.
      */
    private Task BroadcastServerControlWarningAsync(string action, TimeSpan remaining, string requestedBy, CancellationToken cancellationToken)
    {
        string message = $"Server will {action} in {CommandArgumentParser.FormatDuration(remaining)}. Requested by {requestedBy}.";
        return BroadcastSystemMessageAsync(message, null, cancellationToken);
    }

    /**
      * Broadcasts the final server-control notice to every active in-world player.
      */
    private Task BroadcastServerControlNowAsync(string action, string requestedBy, CancellationToken cancellationToken)
    {
        string message = $"Server is {FormatActionProgress(action)} now. Requested by {requestedBy}.";
        return BroadcastSystemMessageAsync(message, null, cancellationToken);
    }

    /**
      * Sends an in-game system message to all active sessions matching the optional predicate.
      */
    private async Task<int> BroadcastSystemMessageAsync(string message, Func<WorldClientSession, bool>? predicate, CancellationToken cancellationToken)
    {
        int sent = 0;
        IReadOnlyList<WorldClientSession> sessions = _playerSessionRegistry.SnapshotSessions();
        foreach (WorldClientSession session in sessions)
        {
            if (predicate is not null && !predicate(session))
            {
                continue;
            }

            try
            {
                await session.SendSystemMessageAsync(message, cancellationToken);
                sent++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Logger.Write(LogType.TRACE, $"WorldServer could not send scheduled control notice to session {session.Id}: {exception.Message}", "WorldServer");
            }
        }

        return sent;
    }

    /**
      * Converts a control action into a readable in-progress phrase for countdown announcements.
      */
    private static string FormatActionProgress(string action)
    {
        return string.Equals(action, "restart", StringComparison.OrdinalIgnoreCase)
            ? "restarting"
            : "shutting down";
    }

    /**
      * Sends shutdown/restart intent to RealmServer through the realm status connection and to connected internal peers/sessions.
      */
    private async Task BroadcastServerControlRequestAsync(string reason, CancellationToken cancellationToken)
    {
        string packet = $"{InternalProtocol.ShutdownRequest} WorldServer {reason}";

        bool realmNotified = await _realmStatusReporter.SendShutdownRequestAsync(reason, cancellationToken);
        Logger.Write(
            realmNotified ? LogType.NETWORK : LogType.WARNING,
            realmNotified
                ? $"WorldServer sent shutdown request to RealmServer. Reason={reason}"
                : $"WorldServer could not send shutdown request to RealmServer; realm status connection is not active. Reason={reason}",
            "WorldServer");

        string[] targets = _peerConnections.Keys
            .Concat(_serverSessions.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (string target in targets)
        {
            try
            {
                int sent = await SendPacketToServerAsync(target, packet, cancellationToken);
                Logger.Write(
                    sent > 0 ? LogType.NETWORK : LogType.WARNING,
                    sent > 0
                        ? $"WorldServer sent shutdown request to {target} ({sent} connection(s)). Reason={reason}"
                        : $"WorldServer could not send shutdown request to {target}; no active connection was available. Reason={reason}",
                    "WorldServer");
            }
            catch (Exception exception) when (exception is IOException or ObjectDisposedException or InvalidOperationException)
            {
                Logger.Write(LogType.WARNING, $"WorldServer could not send shutdown request to {target}: {exception.Message}", "WorldServer");
            }
        }
    }

    /**
      * Returns whether a map service status should be treated as available for player routing.
      */
    private static bool IsMapServiceOnline(string state)
    {
        return string.Equals(state, "Online", StringComparison.OrdinalIgnoreCase);
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
      * Summarizes internal map command packet delivery for command feedback.
      */
    private readonly record struct MapCommandDispatchResult(int TargetCount, int SentConnections);

    /**
      * Validates the auth, character, and world database connections before the realm is advertised.
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
      * Loads world database templates needed by character creation, item lookup, and world login into memory.
      */
    private async Task LoadWorldTemplateDataAsync(CancellationToken cancellationToken)
    {
        _worldTemplateData = await _worldTemplateRepository.LoadAsync(cancellationToken);

        if (_worldTemplateData.PlayerCreateInfo.Count == 0)
        {
            throw new InvalidOperationException("World database table `playercreateinfo` is empty. Character creation cannot resolve race/class start positions.");
        }

        if (_worldTemplateData.ItemTemplates.Count == 0)
        {
            throw new InvalidOperationException("World database table `item_template` is empty. Character creation cannot resolve starter items or equipment display data.");
        }

        Logger.Write(LogType.DATABASE, $"World database table `item_template` loaded {_worldTemplateData.ItemTemplates.Count} row(s).", "WorldServer");

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
            Logger.Write(LogType.WARNING, "WorldServer game data loading is disabled. Enable [GameData] when extracted DBC data is ready.", "WorldServer");
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
