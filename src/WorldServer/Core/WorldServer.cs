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
// File: src/WorldServer/Core/WorldServer.cs
// Purpose: Contains world server code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Collections.Concurrent;
using System.Globalization;
using EmulationServer.Core.Servers;
using EmulationServer.Database.Accounts;
using EmulationServer.Database.Services;
using EmulationServer.Game.Commands;
using EmulationServer.Game.Creatures;
using EmulationServer.Game.Data;
using EmulationServer.Game.Data.Dbc.Maps;
using EmulationServer.Game.Data.Maps;
using EmulationServer.Game.Data.Stores;
using EmulationServer.Game.GameObjects;
using EmulationServer.Game.Movement;
using EmulationServer.Game.Players;
using EmulationServer.Game.WorldData;
using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Peers;
using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Network.Networking.Sessions;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.Shared.Timing;
using EmulationServer.WorldServer.Characters;
using EmulationServer.WorldServer.Configuration;
using EmulationServer.WorldServer.Database.Accounts;
using EmulationServer.WorldServer.Database.Characters;
using EmulationServer.WorldServer.Internal;
using EmulationServer.WorldServer.Networking.Packets;
using EmulationServer.WorldServer.Networking.Sessions;
using EmulationServer.WorldServer.Networking.Socket;
using GameChatSystem = EmulationServer.Game.Chat.ChatSystem;
using GameInGameCommandService = EmulationServer.Game.Commands.InGameCommandService;
using GameItemSystem = EmulationServer.Game.Items.ItemSystem;
using WorldPlayerSessionRegistry = EmulationServer.WorldServer.Players.PlayerSessionRegistry;

namespace EmulationServer.WorldServer.Core;

// Type: WorldServer
// Purpose: Provides world server behavior for the world server gameplay, session, and character runtime layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class WorldServer : IInGameMapCommandExecutor, IInGameRbacCommandExecutor, IInGameServerCommandExecutor, IAsyncDisposable
{
    // Field: Stores the default world game object snapshot map ids state used by the world server gameplay, session, and character runtime layer.
    // Value: current default world game object snapshot map ids backing value maintained by the owning type.
    private static readonly int[] DefaultWorldGameObjectSnapshotMapIds = [0, 1];
    // Field: Stores the default world creature snapshot map ids state used by the world server gameplay, session, and character runtime layer.
    // Value: current default world creature snapshot map ids backing value maintained by the owning type.
    private static readonly int[] DefaultWorldCreatureSnapshotMapIds = [0, 1];
    // Field: Stores the required public client internal servers state used by the world server gameplay, session, and character runtime layer.
    // Value: current required public client internal servers backing value maintained by the owning type.
    private static readonly string[] RequiredPublicClientInternalServers = ["ProxyServer"];

    // Field: Stores the settings state used by the world server gameplay, session, and character runtime layer.
    // Value: current settings backing value maintained by the owning type.
    private readonly WorldServerSettings _settings;

    // Field: Stores the host state used by the world server gameplay, session, and character runtime layer.
    // Value: current host backing value maintained by the owning type.
    private readonly EmulationServerHost _host;

    // Field: Stores the realm status reporter state used by the world server gameplay, session, and character runtime layer.
    // Value: current realm status reporter backing value maintained by the owning type.
    private readonly WorldRealmStatusReporter _realmStatusReporter;

    // Field: Stores the world health status cancellation state used by the world server gameplay, session, and character runtime layer.
    // Value: current world health status cancellation backing value maintained by the owning type.
    private CancellationTokenSource? _worldHealthStatusCancellation;

    // Field: Stores the world health status task state used by the world server gameplay, session, and character runtime layer.
    // Value: current world health status task backing value maintained by the owning type.
    private Task? _worldHealthStatusTask;

    // Field: Stores the auth database state used by the world server gameplay, session, and character runtime layer.
    // Value: current auth database backing value maintained by the owning type.
    private readonly MySqlDatabaseService _authDatabase;

    // Field: Stores the character database state used by the world server gameplay, session, and character runtime layer.
    // Value: current character database backing value maintained by the owning type.
    private readonly MySqlDatabaseService _characterDatabase;

    // Field: Stores the world database state used by the world server gameplay, session, and character runtime layer.
    // Value: current world database backing value maintained by the owning type.
    private readonly MySqlDatabaseService _worldDatabase;

    // Field: Stores the account repository state used by the world server gameplay, session, and character runtime layer.
    // Value: current account repository backing value maintained by the owning type.
    private readonly WorldAccountRepository _accountRepository;

    // Field: Stores the character repository state used by the world server gameplay, session, and character runtime layer.
    // Value: current character repository backing value maintained by the owning type.
    private readonly CharacterRepository _characterRepository;

    // Field: Stores the world template repository state used by the world server gameplay, session, and character runtime layer.
    // Value: current world template repository backing value maintained by the owning type.
    private readonly WorldTemplateRepository _worldTemplateRepository;

    // Field: Stores the character creation service state used by the world server gameplay, session, and character runtime layer.
    // Value: current character creation service backing value maintained by the owning type.
    private readonly CharacterCreationService _characterCreationService;

    // Field: Stores the item system state used by the world server gameplay, session, and character runtime layer.
    // Value: current item system backing value maintained by the owning type.
    private readonly GameItemSystem _itemSystem;

    // Field: Stores the chat system state used by the world server gameplay, session, and character runtime layer.
    // Value: current chat system backing value maintained by the owning type.
    private readonly GameChatSystem _chatSystem;

    // Field: Stores the in game command service state used by the world server gameplay, session, and character runtime layer.
    // Value: current in game command service backing value maintained by the owning type.
    private readonly GameInGameCommandService _inGameCommandService;

    // Field: Stores the player session registry state used by the world server gameplay, session, and character runtime layer.
    // Value: current player session registry backing value maintained by the owning type.
    private readonly WorldPlayerSessionRegistry _playerSessionRegistry;

    // Field: Stores the client listener state used by the world server gameplay, session, and character runtime layer.
    // Value: current client listener backing value maintained by the owning type.
    private readonly WorldClientSocketListener _clientListener;

    // Field: Stores the world template data state used by the world server gameplay, session, and character runtime layer.
    // Value: current world template data backing value maintained by the owning type.
    private WorldTemplateDataStore _worldTemplateData = WorldTemplateDataStore.Empty;
    private readonly ConcurrentDictionary<string, InternalPeerConnection> _peerConnections = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, InternalServerSession> _serverSessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, InternalMapServiceStatusPacket> _mapServiceStatuses = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _sentGameObjectSnapshotKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _sentCreatureSnapshotKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _scheduledMapControlTimers = new(StringComparer.OrdinalIgnoreCase);
    // Field: Stores the clock state used by the world server gameplay, session, and character runtime layer.
    // Value: current clock backing value maintained by the owning type.
    private readonly SystemSteadyClock _clock = SystemSteadyClock.Instance;
    // Field: Stores the server control timer cancellation state used by the world server gameplay, session, and character runtime layer.
    // Value: current server control timer cancellation backing value maintained by the owning type.
    private CancellationTokenSource? _serverControlTimerCancellation;
    // Field: Stores the server control requested state used by the world server gameplay, session, and character runtime layer.
    // Value: current server control requested backing value maintained by the owning type.
    private int _serverControlRequested;

    // Field: Stores the game data state used by the world server gameplay, session, and character runtime layer.
    // Value: current game data backing value maintained by the owning type.
    private WorldGameDataStore _gameData = WorldGameDataStore.Empty;

    // Constructor: WorldServer
    // Purpose: Initializes a new WorldServer instance with dependencies and values required by the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - settings: Settings values that control how this operation should run.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
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
                () => _worldTemplateData,
                settings.MessageOfTheDay,
                settings.PlayerSaveInterval,
                NotifyActivePlayerCountChanged,
                _realmStatusReporter.SendCharacterCountSnapshotNowAsync),
            ArePublicClientDependenciesReady);
    }

    // Method: StartAsync
    // Purpose: Controls the start lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LoadGameDataIfEnabled();
        await ValidateDatabaseConnectionsAsync(cancellationToken);
        await LogCharacterPlayerStateTablesAsync(cancellationToken);
        await LoadWorldTemplateDataAsync(cancellationToken);

        Task hostTask = _host.StartAsync(cancellationToken);

        try
        {
            await _host.StartupCompleted.WaitAsync(cancellationToken);

            await _host.WaitForInternalServersAsync(
                RequiredPublicClientInternalServers,
                "WorldServer will keep the socket closed until ProxyServer is online.",
                cancellationToken);

            await _realmStatusReporter.StartAsync(cancellationToken);
            await _realmStatusReporter.WaitForConnectionAsync(
                "WorldServer will keep socket closed until RealmServer can receive realm-status updates.",
                cancellationToken);

            StartWorldHealthStatusLoop(cancellationToken);

            Task clientTask = _clientListener.StartAsync(cancellationToken);
            await Task.WhenAll(hostTask, clientTask);
        }
        finally
        {
            await StopWorldHealthStatusLoopAsync(CancellationToken.None);
            await _realmStatusReporter.StopAsync(CancellationToken.None);
            await _clientListener.StopAsync(CancellationToken.None);
        }
    }

    // Method: StopAsync
    // Purpose: Controls the stop lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: DisposeAsync
    // Purpose: Controls the dispose lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: ArePublicClientDependenciesReady
    // Purpose: Executes the are public client dependencies ready operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns true when are public client dependencies ready succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    private bool ArePublicClientDependenciesReady()
    {
        return RequiredPublicClientInternalServers.All(_host.IsInternalServerConnected) &&
            (!_settings.RealmStatus.Enabled || _realmStatusReporter.IsConnected);
    }

    // Method: CreateCallbacks
    // Purpose: Applies create callbacks changes for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the internal network callbacks value produced by this operation.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
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
    // Purpose: Executes the on server authenticated operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - session: Session value supplied by the caller for this operation.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

        if (IsMapControlServer(remoteServerName))
        {
            await SendInitialGameObjectSnapshotsToMapOwnerAsync(remoteServerName, cancellationToken);
            await SendInitialCreatureSnapshotsToMapOwnerAsync(remoteServerName, cancellationToken);
        }
    }

    // Method: OnServerDisconnectedAsync
    // Purpose: Executes the on server disconnected operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - session: Session value supplied by the caller for this operation.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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
            ClearGameObjectSnapshotKeysForOwner(remoteServerName);
            ClearCreatureSnapshotKeysForOwner(remoteServerName);
            await MarkMapOwnerUnavailableAsync(remoteServerName, "incoming internal connection disconnected", cancellationToken);
        }
    }

    // Method: OnPeerAuthenticatedAsync
    // Purpose: Executes the on peer authenticated operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

        if (IsMapControlServer(remoteServerName))
        {
            await SendInitialGameObjectSnapshotsToMapOwnerAsync(remoteServerName, cancellationToken);
            await SendInitialCreatureSnapshotsToMapOwnerAsync(remoteServerName, cancellationToken);
        }
    }

    // Method: OnPeerDisconnectedAsync
    // Purpose: Executes the on peer disconnected operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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
            ClearGameObjectSnapshotKeysForOwner(remoteServerName);
            ClearCreatureSnapshotKeysForOwner(remoteServerName);
            await MarkMapOwnerUnavailableAsync(remoteServerName, "outgoing internal peer disconnected", cancellationToken);
        }
    }

    // Method: OnPeerPacketReceivedAsync
    // Purpose: Executes the on peer packet received operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private Task OnPeerPacketReceivedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        string packet,
        CancellationToken cancellationToken)
    {
        return HandleMapServicePacketAsync(remoteServerName, packet, cancellationToken);
    }

    // Method: OnSessionPacketReceivedAsync
    // Purpose: Executes the on session packet received operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - session: Session value supplied by the caller for this operation.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private Task OnSessionPacketReceivedAsync(
        InternalServerSession session,
        string remoteServerName,
        string packet,
        CancellationToken cancellationToken)
    {
        return HandleMapServicePacketAsync(remoteServerName, packet, cancellationToken);
    }

    // Method: ResolveMapAvailabilityForLogin
    // Purpose: Retrieves resolve map availability for login data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // Returns: Returns the map availability result value produced by this operation.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    private MapAvailabilityResult ResolveMapAvailabilityForLogin(PlayerLoginRecord player)
    {
        ArgumentNullException.ThrowIfNull(player);

        int mapId = unchecked((int)player.Map);
        string requiredKind = "World";
        if (_gameData.MapData.TryGetMap(mapId, out EmulationServer.Game.Data.Dbc.Maps.MapDbcRecord map) && map.IsInstanceMap)
        {
            requiredKind = "Instance";
        }

        InternalMapServiceStatusPacket[] candidates = [.. _mapServiceStatuses.Values.Where(status => status.MapId == mapId &&
            string.Equals(status.State, "Online", StringComparison.OrdinalIgnoreCase) && IsConnectedMapOwner(status.OwnerServerName))];

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

        return MapAvailabilityResult.Unavailable($"Map {mapId} is online only on unsupported service kind(s): {string.Join(',',
            candidates.Select(candidate => candidate.Kind).Distinct(StringComparer.OrdinalIgnoreCase))}.", string.Equals(requiredKind, "Instance", StringComparison.OrdinalIgnoreCase));
    }

    // Method: IsConnectedMapOwner
    // Purpose: Validates or evaluates is connected map owner rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // Returns: Returns true when is connected map owner succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    private bool IsConnectedMapOwner(string ownerServerName)
    {
        return _peerConnections.ContainsKey(ownerServerName) || _serverSessions.ContainsKey(ownerServerName);
    }

    // Method: NotifyMapServicePlayerEnteredWorldAsync
    // Purpose: Executes the notify map service player entered world operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: NotifyMapServicePlayerLeftWorldAsync
    // Purpose: Executes the notify map service player left world operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: NotifyMapServicePlayerMovementAsync
    // Purpose: Executes the notify map service player movement operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // - movement: Movement value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: NotifyMapServicePlayerClientPacketAsync
    // Purpose: Executes the notify map service player client packet operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // - worldPacket: World packet value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: ExecuteMapCommandAsync
    // Purpose: Controls the execute map command lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - action: Action value supplied by the caller for this operation.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - delay: Delay value supplied by the caller for this operation.
    // - requestedBy: Requested by value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: SendMapCommandToTargetsAsync
    // Purpose: Handles send map command to targets work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - action: Action value supplied by the caller for this operation.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task<MapCommandDispatchResult> SendMapCommandToTargetsAsync(string action, int mapId, CancellationToken cancellationToken)
    {
        bool shouldRefreshGameObjectSnapshot = string.Equals(action, "start", StringComparison.OrdinalIgnoreCase) || string.Equals(action, "restart", StringComparison.OrdinalIgnoreCase);
        if (shouldRefreshGameObjectSnapshot)
        {
            await ReloadGameObjectDataForMapAsync(mapId, cancellationToken);
            await ReloadCreatureDataForMapAsync(mapId, cancellationToken);
        }

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
            if (shouldRefreshGameObjectSnapshot)
            {
                await SendGameObjectSnapshotToTargetAsync(target, mapId, cancellationToken);
                await SendCreatureSnapshotToTargetAsync(target, mapId, cancellationToken);
            }

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

    // Method: ScheduleMapControlAsync
    // Purpose: Executes the schedule map control operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - action: Action value supplied by the caller for this operation.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - delay: Delay value supplied by the caller for this operation.
    // - requestedBy: Requested by value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ExecuteScheduledMapControlAsync
    // Purpose: Controls the execute scheduled map control lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - key: Key value supplied by the caller for this operation.
    // - action: Action value supplied by the caller for this operation.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - delay: Delay value supplied by the caller for this operation.
    // - requestedBy: Requested by value supplied by the caller for this operation.
    // - timerCancellation: Timer cancellation value supplied by the caller for this operation.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: BroadcastMapControlWarningAsync
    // Purpose: Executes the broadcast map control warning operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - action: Action value supplied by the caller for this operation.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - remaining: Remaining value supplied by the caller for this operation.
    // - requestedBy: Requested by value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private Task BroadcastMapControlWarningAsync(string action, int mapId, TimeSpan remaining, string requestedBy, CancellationToken cancellationToken)
    {
        string message = $"Map {mapId} will {action} in {CommandArgumentParser.FormatDuration(remaining)}. Requested by {requestedBy}.";
        return BroadcastSystemMessageAsync(message, session => session.CurrentPlayer?.Map == unchecked((uint)mapId), cancellationToken);
    }

    // Method: BroadcastMapControlNowAsync
    // Purpose: Executes the broadcast map control now operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - action: Action value supplied by the caller for this operation.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - requestedBy: Requested by value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private Task BroadcastMapControlNowAsync(string action, int mapId, string requestedBy, CancellationToken cancellationToken)
    {
        string message = $"Map {mapId} is {FormatActionProgress(action)} now. Requested by {requestedBy}.";
        return BroadcastSystemMessageAsync(message, session => session.CurrentPlayer?.Map == unchecked((uint)mapId), cancellationToken);
    }

    // Method: GetMapCommandTargets
    // Purpose: Retrieves get map command targets data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the string[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    private string[] GetMapCommandTargets(int mapId)
    {
        string[] owners = [.. _mapServiceStatuses.Values
            .Where(status => status.MapId == mapId)
            .Select(status => status.OwnerServerName)
            .Distinct(StringComparer.OrdinalIgnoreCase)];

        if (owners.Length > 0)
        {
            return owners;
        }

        return [.. _peerConnections.Keys
            .Concat(_serverSessions.Keys)
            .Where(IsMapControlServer)
            .Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    // Method: SendPacketToServerAsync
    // Purpose: Handles send packet to server work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: SendInitialGameObjectSnapshotsToMapOwnerAsync
    // Purpose: Handles send initial game object snapshots to map owner work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task SendInitialGameObjectSnapshotsToMapOwnerAsync(string ownerServerName, CancellationToken cancellationToken)
    {
        foreach (int mapId in GetInitialGameObjectSnapshotMapIds(ownerServerName))
        {
            await SendGameObjectSnapshotIfNeededAsync(ownerServerName, mapId, cancellationToken);
        }
    }

    // Method: GetInitialGameObjectSnapshotMapIds
    // Purpose: Retrieves get initial game object snapshot map ids data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // Returns: Returns the int[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    private int[] GetInitialGameObjectSnapshotMapIds(string ownerServerName)
    {
        IEnumerable<int> reportedMaps = _mapServiceStatuses.Values
            .Where(status => string.Equals(status.OwnerServerName, ownerServerName, StringComparison.OrdinalIgnoreCase))
            .Select(status => status.MapId);

        if (string.Equals(ownerServerName, "MapServer", StringComparison.OrdinalIgnoreCase))
        {
            reportedMaps = reportedMaps.Concat(DefaultWorldGameObjectSnapshotMapIds);
        }

        return [.. reportedMaps
            .Where(mapId => mapId >= 0 && mapId <= ushort.MaxValue)
            .Distinct()
            .Order()];
    }

    // Method: SendGameObjectSnapshotIfNeededAsync
    // Purpose: Handles send game object snapshot if needed work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task SendGameObjectSnapshotIfNeededAsync(string ownerServerName, int mapId, CancellationToken cancellationToken)
    {
        string key = GetGameObjectSnapshotKey(ownerServerName, mapId);
        if (!_sentGameObjectSnapshotKeys.TryAdd(key, 0))
        {
            return;
        }

        int sentLines = await SendGameObjectSnapshotToTargetAsync(ownerServerName, mapId, cancellationToken);
        if (sentLines == 0)
        {
            _sentGameObjectSnapshotKeys.TryRemove(key, out _);
            return;
        }

        _sentGameObjectSnapshotKeys[key] = 1;
    }

    // Method: SendGameObjectSnapshotToTargetAsync
    // Purpose: Handles send game object snapshot to target work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task<int> SendGameObjectSnapshotToTargetAsync(string ownerServerName, int mapId, CancellationToken cancellationToken)
    {
        if (mapId < 0 || mapId > ushort.MaxValue)
        {
            Logger.Write(LogType.WARNING, $"WorldServer cannot send gameobject snapshot for invalid MapId={mapId} to {ownerServerName}.", "WorldServer");
            return 0;
        }

        ushort safeMapId = unchecked((ushort)mapId);
        IReadOnlyList<GameObjectSpawnRecord> spawns = _worldTemplateData.GetGameObjectSpawnsForMap(safeMapId);
        GameObjectTemplateRecord[] templates = [.. spawns
            .Select(spawn => spawn.Entry)
            .Distinct()
            .Select(entry => _worldTemplateData.TryGetGameObjectTemplate(entry, out GameObjectTemplateRecord template) ? template : null)
            .OfType<GameObjectTemplateRecord>()
            .OrderBy(template => template.Entry)];

        string snapshotId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        int sentLines = 0;

        async Task SendSnapshotLineAsync(string line)
        {
            if (line.Length > InternalProtocol.MaximumPacketLineLength)
            {
                Logger.Write(LogType.WARNING, $"WorldServer skipped oversized gameobject snapshot packet for MapId={mapId} to {ownerServerName}: {line.Length} characters.", "WorldServer");
                return;
            }

            int sent = await SendPacketToServerAsync(ownerServerName, line, cancellationToken);
            if (sent > 0)
            {
                sentLines++;
            }
        }

        await SendSnapshotLineAsync(GameObjectSnapshotProtocol.CreateBeginPacket(snapshotId, mapId, templates.Length, spawns.Count));
        foreach (GameObjectTemplateRecord template in templates)
        {
            await SendSnapshotLineAsync(GameObjectSnapshotProtocol.CreateTemplatePacket(snapshotId, template));
        }

        foreach (GameObjectSpawnRecord spawn in spawns.OrderBy(spawn => spawn.Guid))
        {
            await SendSnapshotLineAsync(GameObjectSnapshotProtocol.CreateSpawnPacket(snapshotId, spawn));
        }

        await SendSnapshotLineAsync(GameObjectSnapshotProtocol.CreateEndPacket(snapshotId, mapId));

        if (sentLines == 0)
        {
            Logger.Write(LogType.WARNING, $"WorldServer could not send gameobject snapshot for MapId={mapId} to {ownerServerName}; no active connection was available.", "WorldServer");
            return 0;
        }

        _sentGameObjectSnapshotKeys[GetGameObjectSnapshotKey(ownerServerName, mapId)] = 1;
        Logger.Write(LogType.NETWORK, $"WorldServer sent gameobject snapshot {snapshotId} for MapId={mapId} to {ownerServerName}: templates={templates.Length}, spawns={spawns.Count}, packetLines={sentLines}.", "WorldServer");
        return sentLines;
    }

    // Method: ClearGameObjectSnapshotKeysForOwner
    // Purpose: Applies clear game object snapshot keys for owner changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    private void ClearGameObjectSnapshotKeysForOwner(string ownerServerName)
    {
        string prefix = string.Create(CultureInfo.InvariantCulture, $"{ownerServerName}|");
        foreach (string key in _sentGameObjectSnapshotKeys.Keys.Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToArray())
        {
            _sentGameObjectSnapshotKeys.TryRemove(key, out _);
        }
    }

    // Method: GetGameObjectSnapshotKey
    // Purpose: Retrieves get game object snapshot key data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    private static string GetGameObjectSnapshotKey(string ownerServerName, int mapId)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{ownerServerName}|{mapId}");
    }

    // Method: SendInitialCreatureSnapshotsToMapOwnerAsync
    // Purpose: Handles send initial creature snapshots to map owner work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task SendInitialCreatureSnapshotsToMapOwnerAsync(string ownerServerName, CancellationToken cancellationToken)
    {
        foreach (int mapId in GetInitialCreatureSnapshotMapIds(ownerServerName))
        {
            await SendCreatureSnapshotIfNeededAsync(ownerServerName, mapId, cancellationToken);
        }
    }

    // Method: GetInitialCreatureSnapshotMapIds
    // Purpose: Retrieves get initial creature snapshot map ids data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // Returns: Returns the int[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    private int[] GetInitialCreatureSnapshotMapIds(string ownerServerName)
    {
        IEnumerable<int> reportedMaps = _mapServiceStatuses.Values
            .Where(status => string.Equals(status.OwnerServerName, ownerServerName, StringComparison.OrdinalIgnoreCase))
            .Select(status => status.MapId);

        if (string.Equals(ownerServerName, "MapServer", StringComparison.OrdinalIgnoreCase))
        {
            reportedMaps = reportedMaps.Concat(DefaultWorldCreatureSnapshotMapIds);
        }

        return [.. reportedMaps
            .Where(mapId => mapId >= 0 && mapId <= ushort.MaxValue)
            .Distinct()
            .Order()];
    }

    // Method: SendCreatureSnapshotIfNeededAsync
    // Purpose: Handles send creature snapshot if needed work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task SendCreatureSnapshotIfNeededAsync(string ownerServerName, int mapId, CancellationToken cancellationToken)
    {
        string key = GetCreatureSnapshotKey(ownerServerName, mapId);
        if (!_sentCreatureSnapshotKeys.TryAdd(key, 0))
        {
            return;
        }

        int sentLines = await SendCreatureSnapshotToTargetAsync(ownerServerName, mapId, cancellationToken);
        if (sentLines == 0)
        {
            _sentCreatureSnapshotKeys.TryRemove(key, out _);
            return;
        }

        _sentCreatureSnapshotKeys[key] = 1;
    }

    // Method: SendCreatureSnapshotToTargetAsync
    // Purpose: Handles send creature snapshot to target work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task<int> SendCreatureSnapshotToTargetAsync(string ownerServerName, int mapId, CancellationToken cancellationToken)
    {
        if (mapId < 0 || mapId > ushort.MaxValue)
        {
            Logger.Write(LogType.WARNING, $"WorldServer cannot send creature snapshot for invalid MapId={mapId} to {ownerServerName}.", "WorldServer");
            return 0;
        }

        ushort safeMapId = unchecked((ushort)mapId);
        IReadOnlyList<CreatureSpawnRecord> spawns = _worldTemplateData.GetCreatureSpawnsForMap(safeMapId);
        CreatureTemplateRecord[] templates = [.. spawns
            .Select(spawn => spawn.Entry)
            .Distinct()
            .Select(entry => _worldTemplateData.TryGetCreatureTemplate(entry, out CreatureTemplateRecord template) ? template : null)
            .OfType<CreatureTemplateRecord>()
            .OrderBy(template => template.Entry)];

        string snapshotId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        int sentLines = 0;

        async Task SendSnapshotLineAsync(string line)
        {
            if (line.Length > InternalProtocol.MaximumPacketLineLength)
            {
                Logger.Write(LogType.WARNING, $"WorldServer skipped oversized creature snapshot packet for MapId={mapId} to {ownerServerName}: {line.Length} characters.", "WorldServer");
                return;
            }

            int sent = await SendPacketToServerAsync(ownerServerName, line, cancellationToken);
            if (sent > 0)
            {
                sentLines++;
            }
        }

        await SendSnapshotLineAsync(CreatureSnapshotProtocol.CreateBeginPacket(snapshotId, mapId, templates.Length, spawns.Count));
        foreach (CreatureTemplateRecord template in templates)
        {
            await SendSnapshotLineAsync(CreatureSnapshotProtocol.CreateTemplatePacket(snapshotId, template));
        }

        foreach (CreatureSpawnRecord spawn in spawns.OrderBy(spawn => spawn.Guid))
        {
            await SendSnapshotLineAsync(CreatureSnapshotProtocol.CreateSpawnPacket(snapshotId, spawn));
        }

        await SendSnapshotLineAsync(CreatureSnapshotProtocol.CreateEndPacket(snapshotId, mapId));

        if (sentLines == 0)
        {
            Logger.Write(LogType.WARNING, $"WorldServer could not send creature snapshot for MapId={mapId} to {ownerServerName}; no active connection was available.", "WorldServer");
            return 0;
        }

        _sentCreatureSnapshotKeys[GetCreatureSnapshotKey(ownerServerName, mapId)] = 1;
        Logger.Write(LogType.NETWORK, $"WorldServer sent creature snapshot {snapshotId} for MapId={mapId} to {ownerServerName}: templates={templates.Length}, spawns={spawns.Count}, packetLines={sentLines}.", "WorldServer");
        return sentLines;
    }

    // Method: ClearCreatureSnapshotKeysForOwner
    // Purpose: Applies clear creature snapshot keys for owner changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    private void ClearCreatureSnapshotKeysForOwner(string ownerServerName)
    {
        string prefix = string.Create(CultureInfo.InvariantCulture, $"{ownerServerName}|");
        foreach (string key in _sentCreatureSnapshotKeys.Keys.Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToArray())
        {
            _sentCreatureSnapshotKeys.TryRemove(key, out _);
        }
    }

    // Method: GetCreatureSnapshotKey
    // Purpose: Retrieves get creature snapshot key data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    private static string GetCreatureSnapshotKey(string ownerServerName, int mapId)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{ownerServerName}|{mapId}");
    }

    // Method: AnnounceWorldCapacityAsync
    // Purpose: Executes the announce world capacity operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - sendPacketAsync: Send packet async value supplied by the caller for this operation.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task AnnounceWorldCapacityAsync(
        Func<string, CancellationToken, Task> sendPacketAsync,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        string packet = $"{InternalProtocol.WorldCapacity} {_settings.MaxConnections}";
        await sendPacketAsync(packet, cancellationToken);

        Logger.Write(LogType.NETWORK, $"WorldServer announced max connections to {remoteServerName}: {_settings.MaxConnections}.", "WorldServer");
    }

    // Method: NotifyActivePlayerCountChanged
    // Purpose: Executes the notify active player count changed operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - activePlayerCount: Active player count value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    private void NotifyActivePlayerCountChanged(int activePlayerCount)
    {
        _realmStatusReporter.SetActiveConnections(activePlayerCount);
        _ = _realmStatusReporter.SendRealmStatusNowAsync(CancellationToken.None);
        _ = SendWorldHealthStatusSafelyAsync(CancellationToken.None);
    }

    // Method: StartWorldHealthStatusLoop
    // Purpose: Controls the start world health status loop lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: StopWorldHealthStatusLoopAsync
    // Purpose: Controls the stop world health status loop lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

            }
        }

        healthCancellation?.Dispose();
    }

    // Method: RunWorldHealthStatusLoopAsync
    // Purpose: Controls the run world health status loop lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

        }
        catch (Exception exception)
        {
            Logger.Write(LogType.CRITICAL, exception.ToString(), "WorldServer");
        }
    }

    // Method: SendWorldHealthStatusSafelyAsync
    // Purpose: Handles send world health status safely work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException or InvalidOperationException)
        {
            Logger.Write(LogType.DEBUG, $"WorldServer could not report health status to ProxyServer: {exception.Message}", "WorldServer");
        }
    }

    // Method: AnnounceWorldHealthStatusAsync
    // Purpose: Executes the announce world health status operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - sendPacketAsync: Send packet async value supplied by the caller for this operation.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task AnnounceWorldHealthStatusAsync(
        Func<string, CancellationToken, Task> sendPacketAsync,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        string packet = CreateWorldHealthStatusPacket();
        await sendPacketAsync(packet, cancellationToken);

        Logger.Write(LogType.NETWORK, $"WorldServer announced health status to {remoteServerName}: players={_playerSessionRegistry.ActivePlayerCount}/{_settings.MaxConnections}.", "WorldServer");
    }

    // Method: CreateWorldHealthStatusPacket
    // Purpose: Applies create world health status packet changes for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    private string CreateWorldHealthStatusPacket()
    {
        InternalWorldHealthStatusPacket status = new(
            "WorldServer",
            _playerSessionRegistry.ActivePlayerCount,
            _settings.MaxConnections,
            _clock.UtcNow);

        return status.ToPacketLine();
    }

    // Method: HandleMapServicePacketAsync
    // Purpose: Handles handle map service packet work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: HandleMapServiceCommandResult
    // Purpose: Handles handle map service command result work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - result: Result value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    private static void HandleMapServiceCommandResult(string remoteServerName, InternalMapServiceCommandResultPacket result)
    {
        string message = $"WorldServer received map command result from {remoteServerName}: {result.OwnerServerName} {result.Kind} map={result.MapId}, instance={result.InstanceId}, state={result.State}, result={result.ResultCode}. {result.Message}";

        switch (result.ResultCode.ToLowerInvariant())
        {
            case "success":

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

    // Method: HandleMapServiceStatusPacketAsync
    // Purpose: Handles handle map service status packet work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

        if (isOnline && IsConnectedMapOwner(status.OwnerServerName))
        {
            await SendGameObjectSnapshotIfNeededAsync(status.OwnerServerName, status.MapId, cancellationToken);
            await SendCreatureSnapshotIfNeededAsync(status.OwnerServerName, status.MapId, cancellationToken);
        }

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

    }

    // Method: MarkMapOwnerUnavailableAsync
    // Purpose: Executes the mark map owner unavailable operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // - reason: Reason value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task MarkMapOwnerUnavailableAsync(string ownerServerName, string reason, CancellationToken cancellationToken)
    {
        InternalMapServiceStatusPacket[] affectedStatuses = [.. _mapServiceStatuses.Values
            .Where(status => string.Equals(status.OwnerServerName, ownerServerName, StringComparison.OrdinalIgnoreCase))
            .Select(status => status with { State = "Offline" })];

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

    // Method: DisconnectPlayersForUnavailableMapServiceAsync
    // Purpose: Executes the disconnect players for unavailable map service operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - status: Status value supplied by the caller for this operation.
    // - reason: Reason value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private Task DisconnectPlayersForUnavailableMapServiceAsync(InternalMapServiceStatusPacket status, string reason, CancellationToken cancellationToken)
    {
        return DisconnectPlayersForMapOwnerAsync(status.OwnerServerName, [status], reason, cancellationToken);
    }

    // Method: DisconnectPlayersForMapOwnerAsync
    // Purpose: Executes the disconnect players for map owner operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // - statuses: Statuses value supplied by the caller for this operation.
    // - reason: Reason value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task DisconnectPlayersForMapOwnerAsync(
        string ownerServerName,
        IReadOnlyCollection<InternalMapServiceStatusPacket> statuses,
        string reason,
        CancellationToken cancellationToken)
    {
        HashSet<uint> affectedMapIds = [.. statuses.Select(status => unchecked((uint)status.MapId))];

        WorldClientSession[] affectedSessions = [.. _playerSessionRegistry.SnapshotSessions().Where(session => string.Equals(session.CurrentMapOwnerServerName, ownerServerName, StringComparison.OrdinalIgnoreCase) && session.CurrentPlayer is not null && (affectedMapIds.Count == 0 || affectedMapIds.Contains(session.CurrentPlayer!.Map)))];

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

    // Method: FormatCachedMapInfo
    // Purpose: Executes the format cached map info operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    private string FormatCachedMapInfo(int mapId)
    {
        InternalMapServiceStatusPacket[] statuses = [.. _mapServiceStatuses.Values
            .Where(status => status.MapId == mapId)
            .OrderBy(status => status.OwnerServerName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(status => status.InstanceId)];

        string dbcDescription = _gameData.MapData.DescribeMap(mapId);
        List<string> lines = [$"Map {mapId} info:"];
        AppendMapMetadataLines(lines, mapId);
        AppendGameObjectMetadataLines(lines, mapId);
        AppendCreatureMetadataLines(lines, mapId);

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

    // Method: ReloadGameObjectDataForMapAsync
    // Purpose: Executes the reload game object data for map operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task ReloadGameObjectDataForMapAsync(int mapId, CancellationToken cancellationToken)
    {
        if (mapId < 0 || mapId > ushort.MaxValue)
        {
            Logger.Write(LogType.WARNING, $"WorldServer cannot reload gameobject spawns for invalid MapId={mapId}.", "WorldServer");
            return;
        }

        ushort safeMapId = unchecked((ushort)mapId);
        IReadOnlyList<GameObjectTemplateRecord> templates = await _worldTemplateRepository.LoadGameObjectTemplatesAsync(cancellationToken);
        IReadOnlyList<GameObjectSpawnRecord> spawns = await _worldTemplateRepository.LoadGameObjectSpawnsForMapAsync(safeMapId, cancellationToken);
        spawns = await ResolveAndPersistGameObjectAreaDataAsync(spawns, $"MapId={mapId} reload", cancellationToken);

        _worldTemplateData = _worldTemplateData.WithGameObjectDataForMap(safeMapId, templates, spawns);

        HashSet<uint> templateEntries = templates.Select(template => template.Entry).ToHashSet();
        int zones = spawns.Where(spawn => spawn.ZoneId != 0).Select(spawn => spawn.ZoneId).Distinct().Count();
        int areas = spawns.Where(spawn => spawn.AreaId != 0).Select(spawn => spawn.AreaId).Distinct().Count();
        int missingTemplates = spawns.Count(spawn => !templateEntries.Contains(spawn.Entry));

        Logger.Write(
            missingTemplates == 0 ? LogType.DATABASE : LogType.WARNING,
            $"WorldServer reloaded gameobject data for MapId={mapId}: spawns={spawns.Count}, templates={templates.Count}, zones={zones}, areas={areas}, missingTemplates={missingTemplates}.",
            "WorldServer");
    }

    // Method: AppendGameObjectMetadataLines
    // Purpose: Executes the append game object metadata lines operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - lines: Lines value supplied by the caller for this operation.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    private void AppendGameObjectMetadataLines(List<string> lines, int mapId)
    {
        if (mapId < 0 || mapId > ushort.MaxValue)
        {
            lines.Add("GameObjects: unsupported map id for spawn cache lookup.");
            return;
        }

        IReadOnlyList<GameObjectSpawnRecord> spawns = _worldTemplateData.GetGameObjectSpawnsForMap(unchecked((ushort)mapId));
        int templateReferences = spawns.Select(spawn => spawn.Entry).Distinct().Count();
        int missingTemplates = spawns.Count(spawn => !_worldTemplateData.GameObjectTemplates.ContainsKey(spawn.Entry));
        int zones = spawns.Where(spawn => spawn.ZoneId != 0).Select(spawn => spawn.ZoneId).Distinct().Count();
        int areas = spawns.Where(spawn => spawn.AreaId != 0).Select(spawn => spawn.AreaId).Distinct().Count();

        lines.Add($"GameObjects: {spawns.Count} spawn(s)");
        lines.Add($"GO Templates: {templateReferences} referenced / {_worldTemplateData.GameObjectTemplateCount} loaded");
        lines.Add($"GO Location Index: {zones} zone(s), {areas} area(s)");
        if (missingTemplates > 0)
        {
            lines.Add($"GO Missing Templates: {missingTemplates}");
        }
    }

    // Method: ReloadCreatureDataForMapAsync
    // Purpose: Executes the reload creature data for map operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task ReloadCreatureDataForMapAsync(int mapId, CancellationToken cancellationToken)
    {
        if (mapId < 0 || mapId > ushort.MaxValue)
        {
            Logger.Write(LogType.WARNING, $"WorldServer cannot reload creature spawns for invalid MapId={mapId}.", "WorldServer");
            return;
        }

        ushort safeMapId = unchecked((ushort)mapId);
        IReadOnlyList<CreatureTemplateRecord> templates = await _worldTemplateRepository.LoadCreatureTemplatesAsync(cancellationToken);
        IReadOnlyList<CreatureSpawnRecord> spawns = await _worldTemplateRepository.LoadCreatureSpawnsForMapAsync(safeMapId, cancellationToken);
        spawns = await ResolveAndPersistCreatureAreaDataAsync(spawns, $"MapId={mapId} reload", cancellationToken);

        _worldTemplateData = _worldTemplateData.WithCreatureDataForMap(safeMapId, templates, spawns);

        HashSet<uint> templateEntries = [.. templates.Select(template => template.Entry)];
        int zones = spawns.Where(spawn => spawn.ZoneId != 0).Select(spawn => spawn.ZoneId).Distinct().Count();
        int areas = spawns.Where(spawn => spawn.AreaId != 0).Select(spawn => spawn.AreaId).Distinct().Count();
        int missingTemplates = spawns.Count(spawn => !templateEntries.Contains(spawn.Entry));

        Logger.Write(
            missingTemplates == 0 ? LogType.DATABASE : LogType.WARNING,
            $"WorldServer reloaded creature data for MapId={mapId}: spawns={spawns.Count}, templates={templates.Count}, zones={zones}, areas={areas}, missingTemplates={missingTemplates}.",
            "WorldServer");
    }

    // Method: AppendCreatureMetadataLines
    // Purpose: Executes the append creature metadata lines operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - lines: Lines value supplied by the caller for this operation.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    private void AppendCreatureMetadataLines(List<string> lines, int mapId)
    {
        if (mapId < 0 || mapId > ushort.MaxValue)
        {
            lines.Add("Creatures: unsupported map id for spawn cache lookup.");
            return;
        }

        IReadOnlyList<CreatureSpawnRecord> spawns = _worldTemplateData.GetCreatureSpawnsForMap(unchecked((ushort)mapId));
        int templateReferences = spawns.Select(spawn => spawn.Entry).Distinct().Count();
        int missingTemplates = spawns.Count(spawn => !_worldTemplateData.CreatureTemplates.ContainsKey(spawn.Entry));
        int zones = spawns.Where(spawn => spawn.ZoneId != 0).Select(spawn => spawn.ZoneId).Distinct().Count();
        int areas = spawns.Where(spawn => spawn.AreaId != 0).Select(spawn => spawn.AreaId).Distinct().Count();

        lines.Add($"Creatures: {spawns.Count} spawn(s)");
        lines.Add($"Creature Templates: {templateReferences} referenced / {_worldTemplateData.CreatureTemplateCount} loaded");
        lines.Add($"Creature Location Index: {zones} zone(s), {areas} area(s)");
        if (missingTemplates > 0)
        {
            lines.Add($"Creature Missing Templates: {missingTemplates}");
        }
    }

    // Method: AppendMapMetadataLines
    // Purpose: Executes the append map metadata lines operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - lines: Lines value supplied by the caller for this operation.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: FormatCachedMapUptime
    // Purpose: Executes the format cached map uptime operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - status: Status value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: FormatDuration
    // Purpose: Executes the format duration operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - duration: Duration value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    private static string FormatDuration(TimeSpan duration)
    {
        return duration.TotalDays >= 1
            ? $"{duration.Days}d {duration.Hours:D2}h {duration.Minutes:D2}m {duration.Seconds:D2}s"
            : $"{duration.Hours:D2}h {duration.Minutes:D2}m {duration.Seconds:D2}s";
    }

    // Method: ReloadRbacAsync
    // Purpose: Executes the reload RBAC operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: ScheduleShutdownAsync
    // Purpose: Executes the schedule shutdown operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - delay: Delay value supplied by the caller for this operation.
    // - requestedBy: Requested by value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public Task<string> ScheduleShutdownAsync(TimeSpan delay, string requestedBy, CancellationToken cancellationToken)
    {
        return ScheduleServerControlAsync("shutdown", delay, requestedBy, cancellationToken);
    }

    // Method: ScheduleRestartAsync
    // Purpose: Executes the schedule restart operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - delay: Delay value supplied by the caller for this operation.
    // - requestedBy: Requested by value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public Task<string> ScheduleRestartAsync(TimeSpan delay, string requestedBy, CancellationToken cancellationToken)
    {
        return ScheduleServerControlAsync("restart", delay, requestedBy, cancellationToken);
    }

    // Method: ScheduleServerControlAsync
    // Purpose: Executes the schedule server control operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - action: Action value supplied by the caller for this operation.
    // - delay: Delay value supplied by the caller for this operation.
    // - requestedBy: Requested by value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: ExecuteScheduledServerControlAsync
    // Purpose: Controls the execute scheduled server control lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - action: Action value supplied by the caller for this operation.
    // - delay: Delay value supplied by the caller for this operation.
    // - requestedBy: Requested by value supplied by the caller for this operation.
    // - timerCancellation: Timer cancellation value supplied by the caller for this operation.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: BroadcastServerControlWarningAsync
    // Purpose: Executes the broadcast server control warning operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - action: Action value supplied by the caller for this operation.
    // - remaining: Remaining value supplied by the caller for this operation.
    // - requestedBy: Requested by value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private Task BroadcastServerControlWarningAsync(string action, TimeSpan remaining, string requestedBy, CancellationToken cancellationToken)
    {
        string message = $"Server will {action} in {CommandArgumentParser.FormatDuration(remaining)}. Requested by {requestedBy}.";
        return BroadcastSystemMessageAsync(message, null, cancellationToken);
    }

    // Method: BroadcastServerControlNowAsync
    // Purpose: Executes the broadcast server control now operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - action: Action value supplied by the caller for this operation.
    // - requestedBy: Requested by value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private Task BroadcastServerControlNowAsync(string action, string requestedBy, CancellationToken cancellationToken)
    {
        string message = $"Server is {FormatActionProgress(action)} now. Requested by {requestedBy}.";
        return BroadcastSystemMessageAsync(message, null, cancellationToken);
    }

    // Method: BroadcastSystemMessageAsync
    // Purpose: Executes the broadcast system message operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - message: Message value supplied by the caller for this operation.
    // - predicate: Predicate value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: FormatActionProgress
    // Purpose: Executes the format action progress operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - action: Action value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    private static string FormatActionProgress(string action)
    {
        return string.Equals(action, "restart", StringComparison.OrdinalIgnoreCase) ? "restarting" : "shutting down";
    }

    // Method: BroadcastServerControlRequestAsync
    // Purpose: Executes the broadcast server control request operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - reason: Reason value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

        string[] targets = [.. _peerConnections.Keys
            .Concat(_serverSessions.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)];

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

    // Method: IsMapServiceOnline
    // Purpose: Validates or evaluates is map service online rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - state: State value supplied by the caller for this operation.
    // Returns: Returns true when is map service online succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    private static bool IsMapServiceOnline(string state)
    {
        return string.Equals(state, "Online", StringComparison.OrdinalIgnoreCase);
    }

    // Method: IsMapControlServer
    // Purpose: Validates or evaluates is map control server rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // Returns: Returns true when is map control server succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    private static bool IsMapControlServer(string remoteServerName)
    {
        return string.Equals(remoteServerName, "MapServer", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(remoteServerName, "InstanceServer", StringComparison.OrdinalIgnoreCase);
    }

    // Method: GetStatusKey
    // Purpose: Retrieves get status key data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - status: Status value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    private static string GetStatusKey(InternalMapServiceStatusPacket status)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{status.OwnerServerName}|{status.Kind}|{status.MapId}|{status.InstanceId}");
    }

    // Type: MapCommandDispatchResult
    // Purpose: Represents map command dispatch result data passed through the world server gameplay, session, and character runtime layer.
    // Constructor values:
    // - TargetCount: Target count value supplied by the caller for this operation.
    // - SentConnections: Sent connections value supplied by the caller for this operation.
    // Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
    private readonly record struct MapCommandDispatchResult(int TargetCount, int SentConnections);

    // Method: ValidateDatabaseConnectionsAsync
    // Purpose: Validates or evaluates validate database connections rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: LogCharacterPlayerStateTablesAsync
    // Purpose: Executes the log character player state tables operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: LoadWorldTemplateDataAsync
    // Purpose: Retrieves load world template data data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task LoadWorldTemplateDataAsync(CancellationToken cancellationToken)
    {
        _worldTemplateData = await _worldTemplateRepository.LoadAsync(cancellationToken);
        await EnrichAndPersistGameObjectAreaDataAsync(cancellationToken);
        await EnrichAndPersistCreatureAreaDataAsync(cancellationToken);

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
        LogOptionalWorldTemplateCount("gameobject_template", _worldTemplateData.GameObjectTemplateCount, "game object templates are unavailable until Mangos Zero data is imported");
        LogOptionalWorldTemplateCount("gameobject", _worldTemplateData.GameObjectSpawnCount, "no game object spawns will be tracked by map/zone/area until data is imported");
        LogOptionalWorldTemplateCount("creature_template", _worldTemplateData.CreatureTemplateCount, "creature/NPC templates are unavailable until Mangos Zero data is imported");
        LogOptionalWorldTemplateCount("creature", _worldTemplateData.CreatureSpawnCount, "no creature/NPC spawns will be tracked by map/zone/area until data is imported");

        Logger.Write(LogType.SUCCESS, "World database templates ready.", "WorldServer");
    }

    // Method: EnrichAndPersistGameObjectAreaDataAsync
    // Purpose: Executes the enrich and persist game object area data operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task EnrichAndPersistGameObjectAreaDataAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<GameObjectSpawnRecord> rawSpawns = await _worldTemplateRepository.LoadGameObjectSpawnsAsync(cancellationToken);
        if (rawSpawns.Count == 0)
        {
            return;
        }

        IReadOnlyList<GameObjectSpawnRecord> enrichedSpawns = await ResolveAndPersistGameObjectAreaDataAsync(
            rawSpawns,
            "startup",
            cancellationToken);

        _worldTemplateData = _worldTemplateData.WithGameObjectSpawns(enrichedSpawns);
    }

    // Method: ResolveAndPersistGameObjectAreaDataAsync
    // Purpose: Retrieves resolve and persist game object area data data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - spawns: Spawns value supplied by the caller for this operation.
    // - reason: Reason value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task<IReadOnlyList<GameObjectSpawnRecord>> ResolveAndPersistGameObjectAreaDataAsync(
        IReadOnlyList<GameObjectSpawnRecord> spawns,
        string reason,
        CancellationToken cancellationToken)
    {
        if (spawns.Count == 0)
        {
            return spawns;
        }

        if (!_settings.GameData.Enabled || _gameData.MapData.Areas.Count == 0)
        {
            Logger.Write(LogType.WARNING, $"WorldServer skipped gameobject zone/area resolution for {reason}; game data or AreaTable.dbc is unavailable.", "WorldServer");
            return spawns;
        }

        string mapStoreDirectory = GameDataPathResolver.ResolveDirectory(_settings.GameData.DataDirectory, _settings.GameData.MapStoreDirectory);
        MapStoreAreaLookupService areaLookup = new(mapStoreDirectory, _gameData.MapData);
        GameObjectAreaEnrichmentResult result = await _worldTemplateRepository.ResolveAndPersistGameObjectAreasAsync(spawns, areaLookup, cancellationToken);

        string sourceSummary = FormatAreaSourceSummary(result.SourceCounts);
        LogType logType = result.UnresolvedCount == 0 ? LogType.DATABASE : LogType.WARNING;
        Logger.Write(
            logType,
            $"WorldServer resolved gameobject zone/area data for {reason}: spawns={result.Spawns.Count}, resolved={result.ResolvedCount}, unresolved={result.UnresolvedCount}, changed={result.ChangedCount}, persisted={result.PersistedCount}, sources={sourceSummary}.",
            "WorldServer");

        return result.Spawns;
    }

    // Method: EnrichAndPersistCreatureAreaDataAsync
    // Purpose: Executes the enrich and persist creature area data operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task EnrichAndPersistCreatureAreaDataAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<CreatureSpawnRecord> rawSpawns = await _worldTemplateRepository.LoadCreatureSpawnsAsync(cancellationToken);
        if (rawSpawns.Count == 0)
        {
            return;
        }

        IReadOnlyList<CreatureSpawnRecord> enrichedSpawns = await ResolveAndPersistCreatureAreaDataAsync(
            rawSpawns,
            "startup",
            cancellationToken);

        _worldTemplateData = _worldTemplateData.WithCreatureSpawns(enrichedSpawns);
    }

    // Method: ResolveAndPersistCreatureAreaDataAsync
    // Purpose: Retrieves resolve and persist creature area data data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - spawns: Spawns value supplied by the caller for this operation.
    // - reason: Reason value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task<IReadOnlyList<CreatureSpawnRecord>> ResolveAndPersistCreatureAreaDataAsync(
        IReadOnlyList<CreatureSpawnRecord> spawns,
        string reason,
        CancellationToken cancellationToken)
    {
        if (spawns.Count == 0)
        {
            return spawns;
        }

        if (!_settings.GameData.Enabled || _gameData.MapData.Areas.Count == 0)
        {
            Logger.Write(LogType.WARNING, $"WorldServer skipped creature zone/area resolution for {reason}; game data or AreaTable.dbc is unavailable.", "WorldServer");
            return spawns;
        }

        string mapStoreDirectory = GameDataPathResolver.ResolveDirectory(_settings.GameData.DataDirectory, _settings.GameData.MapStoreDirectory);
        MapStoreAreaLookupService areaLookup = new(mapStoreDirectory, _gameData.MapData);
        CreatureAreaEnrichmentResult result = await _worldTemplateRepository.ResolveAndPersistCreatureAreasAsync(spawns, areaLookup, cancellationToken);

        string sourceSummary = FormatAreaSourceSummary(result.SourceCounts);
        LogType logType = result.UnresolvedCount == 0 ? LogType.DATABASE : LogType.WARNING;
        Logger.Write(
            logType,
            $"WorldServer resolved creature zone/area data for {reason}: spawns={result.Spawns.Count}, resolved={result.ResolvedCount}, unresolved={result.UnresolvedCount}, changed={result.ChangedCount}, persisted={result.PersistedCount}, sources={sourceSummary}.",
            "WorldServer");

        return result.Spawns;
    }

    // Method: FormatAreaSourceSummary
    // Purpose: Executes the format area source summary operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - sourceCounts: Source counts value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
    private static string FormatAreaSourceSummary(IReadOnlyDictionary<string, int> sourceCounts)
    {
        if (sourceCounts.Count == 0)
        {
            return "none";
        }

        return string.Join(", ", sourceCounts.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase).Select(entry => $"{entry.Key}:{entry.Value}"));
    }

    // Method: LogOptionalWorldTemplateCount
    // Purpose: Executes the log optional world template count operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - tableName: Table name value supplied by the caller for this operation.
    // - count: Count value supplied by the caller for this operation.
    // - fallbackMessage: Fallback message value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: LoadGameDataIfEnabled
    // Purpose: Retrieves load game data if enabled data for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldServer so callers do not duplicate validation, protocol, or persistence rules.
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
            string.Join(Environment.NewLine,
                "Game data ready:",
                $"  DBC stores: {_gameData.DbcStores.Count}",
                $"  Maps: {_gameData.MapData.Maps.Count}",
                $"  Areas: {_gameData.MapData.Areas.Count}",
                $"  Races: {_gameData.CharacterData.Races.Count}",
                $"  Classes: {_gameData.CharacterData.Classes.Count}",
                $"  Starter outfits: {_gameData.CharacterData.StartOutfits.Count}",
                $"  Item displays: {_gameData.ItemData.DisplayInfo.Count}",
                $"  Spells: {_gameData.SpellData.Spells.Count}",
                $"  Factions: {_gameData.FactionData.Factions.Count}",
                $"  Chat channels: {_gameData.ChatData.Records.Count}"),
            "WorldServer");
    }
}
