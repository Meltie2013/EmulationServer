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

using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Channels;

using EmulationServer.Database.Accounts;
using EmulationServer.Game.Characters;
using EmulationServer.Game.Chat;
using EmulationServer.Game.Commands;
using GameInGameCommandService = EmulationServer.Game.Commands.InGameCommandService;
using GameItemSystem = EmulationServer.Game.Items.ItemSystem;
using GameChatSystem = EmulationServer.Game.Chat.ChatSystem;
using WorldPlayerSessionRegistry = EmulationServer.WorldServer.Players.PlayerSessionRegistry;
using EmulationServer.Game.Players;
using EmulationServer.Game.WorldData;
using EmulationServer.Game.Movement;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.WorldServer.Auth;
using EmulationServer.WorldServer.Characters;
using EmulationServer.WorldServer.Database.Accounts;
using EmulationServer.WorldServer.Database.Characters;
using EmulationServer.WorldServer.Networking.Packets;
using EmulationServer.WorldServer.Networking.Movement;

/**
  * File overview: src/WorldServer/Networking/Sessions/WorldClientSession.cs
  * Documents the WorldClientSession source file in the connected world client session lifecycle and packet dispatch area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.WorldServer.Networking.Sessions;

/**
  * Owns the world client session behavior for the connected world client session lifecycle and packet dispatch layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class WorldClientSession : IChatSession, IInGameCommandSession, IAsyncDisposable
{
    /**
      * Defines the constant value for maximum movement broadcast distance squared.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const float MaximumMovementBroadcastDistanceSquared = 200.0f * 200.0f;

    /**
      * Defines the short grace window used after terminal auth failures so the vanilla client can render the exact failure text before the socket closes.
      */
    private static readonly TimeSpan TerminalAuthFailureDeliveryDelay = TimeSpan.FromMilliseconds(250);
    /**
      * Defines how long character-list refresh responses are delayed after a login failure so the client can render the failure dialog.
      */
    private static readonly TimeSpan CharacterLoginFailureDeliveryDelay = TimeSpan.FromMilliseconds(1000);
    /**
      * Defines how often the session may notify an already-in-world player about map-service delivery problems.
      */
    private static readonly TimeSpan MapServiceFailureNotificationCooldown = TimeSpan.FromSeconds(5);
    /**
      * Limits internal movement telemetry to Map/Instance services so client packet handling is not blocked by every movement opcode.
      */
    private static readonly TimeSpan MapServiceMovementRouteInterval = TimeSpan.FromSeconds(1);
    /**
      * Defines how often account/IP ban state is rechecked after authentication.
      * The check runs on a background monitor so movement and ping packets never wait on database queries.
      */
    private static readonly TimeSpan BanRecheckInterval = TimeSpan.FromSeconds(30);
    /**
      * Limits how often the full player login record is cloned for movement-only position changes.
      * CurrentMovement keeps the exact latest position while the heavier persistence record is coalesced.
      */
    private static readonly TimeSpan PlayerRecordMovementUpdateInterval = TimeSpan.FromMilliseconds(250);
    /**
      * Limits queued movement broadcasts per recipient so slow sockets drop old movement instead of back-pressuring the sender.
      */
    private const int MovementBroadcastQueueCapacity = 256;
    /**
      * Keeps only the newest World -> Map/Instance movement sample when the internal route is busy.
      */
    private const int MapServiceMovementQueueCapacity = 1;
    /**
      * Keeps generated system-chat lines short enough to stay readable in the vanilla client chat frame.
      */
    private const int SystemChatLineLength = 160;

    /**
      * Holds the compact queued movement packet sent by the per-session movement writer.
      */
    private readonly record struct QueuedMovementPacket(WorldOpcode Opcode, byte[] Payload);

    /**
      * Holds one coalesced movement sample for the routed Map/Instance telemetry writer.
      */
    private readonly record struct QueuedMapServiceMovement(PlayerLoginRecord Player, string OwnerServerName, PlayerMovementState Movement);

    private readonly record struct InventoryClientPosition(byte Bag, byte Slot);

    private readonly record struct InventoryStorageLocation(uint BagGuid, byte Slot);

    private const byte ClientBackpackBag = 0xFF;
    private const byte InventoryChangeFailureItemDoesntGoToSlot = 0x0D;
    private const byte InventoryChangeFailureItemNotFound = 0x2A;
    private const byte InventoryChangeFailureBagFull = 0x04;

    /**
      * Holds the private client state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly TcpClient _client;
    /**
      * Holds the private realm id state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly uint _realmId;
    /**
      * Holds the private maximum packet size state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly int _maximumPacketSize;
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
      * Holds the private character service state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly CharacterCreationService _characterService;
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
      * Holds the private command service state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly GameInGameCommandService _commandService;
    /**
      * Holds the private player session registry state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly WorldPlayerSessionRegistry _playerSessionRegistry;
    private readonly Func<PlayerLoginRecord, MapAvailabilityResult> _mapAvailabilityResolver;
    private readonly Func<PlayerLoginRecord, string, CancellationToken, Task> _playerEnteredWorldAsync;
    private readonly Func<PlayerLoginRecord, string, CancellationToken, Task> _playerLeftWorldAsync;
    private readonly Func<PlayerLoginRecord, string, PlayerMovementState, CancellationToken, Task> _playerMovementAsync;
    private readonly Func<PlayerLoginRecord, string, WorldPacket, CancellationToken, Task> _playerClientPacketAsync;
    /**
      * Holds the private player save interval state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly TimeSpan _playerSaveInterval;
    /**
      * Holds the private player save lock state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly SemaphoreSlim _playerSaveLock = new(1, 1);
    /**
      * Serializes every server-to-client packet write.
      * The WoW header cipher is stateful, so concurrent writes must never encrypt headers out of order or interleave header/body data.
      */
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    /**
      * Carries high-frequency movement broadcasts on a small bounded queue so one sender does not wait on every recipient socket.
      */
    private readonly Channel<QueuedMovementPacket> _movementBroadcastQueue = Channel.CreateBounded<QueuedMovementPacket>(new BoundedChannelOptions(MovementBroadcastQueueCapacity)
    {
        SingleReader = true,
        SingleWriter = false,
        FullMode = BoundedChannelFullMode.DropOldest,
        AllowSynchronousContinuations = false,
    });
    /**
      * Carries coalesced movement telemetry to Map/Instance services on one serialized background path.
      * This prevents slow internal networking from creating unbounded fire-and-forget movement tasks.
      */
    private readonly Channel<QueuedMapServiceMovement> _mapServiceMovementQueue = Channel.CreateBounded<QueuedMapServiceMovement>(new BoundedChannelOptions(MapServiceMovementQueueCapacity)
    {
        SingleReader = true,
        SingleWriter = false,
        FullMode = BoundedChannelFullMode.DropOldest,
        AllowSynchronousContinuations = false,
    });
    /**
      * Carries regular non-movement gameplay packets to a per-session worker.
      * This keeps slower database/control handlers from blocking the socket receive loop and delaying CMSG_PING/CMSG_PONG latency.
      */
    private readonly Channel<WorldPacket> _gameplayPacketQueue = Channel.CreateBounded<WorldPacket>(new BoundedChannelOptions(1024)
    {
        SingleReader = true,
        SingleWriter = true,
        FullMode = BoundedChannelFullMode.Wait,
        AllowSynchronousContinuations = false,
    });
    /**
      * Holds the private active player count changed state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly Action<int> _activePlayerCountChanged;
    private readonly Func<CancellationToken, Task> _characterCountChangedAsync;
    /**
      * Holds the private disconnect state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly CancellationTokenSource _disconnect = new();
    /**
      * Holds the private chat channels state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly HashSet<string> _chatChannels = new(StringComparer.OrdinalIgnoreCase);
    /**
      * Holds the private reported unhandled opcodes state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly HashSet<WorldOpcode> _reportedUnhandledOpcodes = [];
    /**
      * Holds the private server seed state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly uint _serverSeed;
    /**
      * Holds the private message of the day state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly string _messageOfTheDay;

    /**
      * Holds the private stream state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private NetworkStream? _stream;
    /**
      * Holds the private crypt state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private WorldHeaderCrypt? _crypt;
    /**
      * Holds the private account state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private WorldAccountSessionRecord? _account;
    /**
      * Holds the private current map owner server name state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private string _currentMapOwnerServerName = string.Empty;
    /**
      * Holds the private player save cancellation state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private CancellationTokenSource? _playerSaveCancellation;
    /**
      * Holds the private player save loop state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private Task? _playerSaveLoop;
    /**
      * Runs the per-session movement broadcast writer.
      * This keeps recipient socket writes away from another player's movement receive loop.
      */
    private Task? _movementBroadcastLoop;
    /**
      * Runs the serialized World -> Map/Instance movement telemetry writer.
      */
    private Task? _mapServiceMovementRouteLoop;
    /**
      * Runs the serialized non-movement gameplay packet worker for this client.
      */
    private Task? _gameplayPacketLoop;
    /**
      * Cancels the authenticated-account ban monitor.
      */
    private CancellationTokenSource? _banMonitorCancellation;
    /**
      * Runs account/IP ban checks away from the packet receive loop.
      */
    private Task? _banMonitorLoop;
    /**
      * Prevents duplicate ban disconnect work if the monitor races with session shutdown.
      */
    private int _banDisconnectStarted;
    /**
      * Stores the last time CurrentPlayer was cloned for movement-only coordinates.
      */
    private DateTimeOffset _lastPlayerRecordMovementUpdateUtc = DateTimeOffset.MinValue;
    /**
      * Holds the private player state dirty state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private bool _playerStateDirty;
    /**
      * Holds the private last player time save utc state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private DateTimeOffset _lastPlayerTimeSaveUtc;
    /**
      * Tracks the last time an in-world map-service problem was surfaced to the player so movement/packet retries do not spam chat.
      */
    private DateTimeOffset _lastMapServiceFailureNotificationUtc = DateTimeOffset.MinValue;
    /**
      * Tracks the last movement state routed to the current map service so high-frequency movement packets can be coalesced.
      */
    private DateTimeOffset _lastMapServiceMovementRouteUtc = DateTimeOffset.MinValue;
    private uint _lastMapServiceMovementRouteMap;
    private uint _lastMapServiceMovementRouteZone;
    private bool _hasLastMapServiceMovementRoute;
    /**
      * Prevents the automatic CMSG_CHAR_ENUM sent by the client after SMSG_CHARACTER_LOGIN_FAILED from hiding the failure dialog immediately.
      */
    private DateTimeOffset _delayCharacterEnumUntilUtc = DateTimeOffset.MinValue;
    /**
      * Prevents duplicate forced logout work when a map or instance owner becomes unavailable.
      */
    private int _serviceDisconnectStarted;
    /**
      * Holds the private disposed state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private bool _disposed;

    /**
      * Initializes a new WorldClientSession instance with the dependencies required by the connected world client session lifecycle and packet dispatch workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: client, realmId, maximumPacketSize, accountRepository, characterRepository, characterService....
      */
    public WorldClientSession(
        TcpClient client,
        uint realmId,
        int maximumPacketSize,
        WorldAccountRepository accountRepository,
        CharacterRepository characterRepository,
        CharacterCreationService characterService,
        GameItemSystem itemSystem,
        GameChatSystem chatSystem,
        GameInGameCommandService commandService,
        WorldPlayerSessionRegistry playerSessionRegistry,
        Func<PlayerLoginRecord, MapAvailabilityResult> mapAvailabilityResolver,
        Func<PlayerLoginRecord, string, CancellationToken, Task> playerEnteredWorldAsync,
        Func<PlayerLoginRecord, string, CancellationToken, Task> playerLeftWorldAsync,
        Func<PlayerLoginRecord, string, PlayerMovementState, CancellationToken, Task> playerMovementAsync,
        Func<PlayerLoginRecord, string, WorldPacket, CancellationToken, Task> playerClientPacketAsync,
        string messageOfTheDay,
        TimeSpan playerSaveInterval,
        Action<int>? activePlayerCountChanged = null,
        Func<CancellationToken, Task>? characterCountChangedAsync = null)
    {
        _client = client ?? throw new ArgumentNullException();
        _realmId = realmId;
        _maximumPacketSize = maximumPacketSize;
        _accountRepository = accountRepository ?? throw new ArgumentNullException();
        _characterRepository = characterRepository ?? throw new ArgumentNullException();
        _characterService = characterService ?? throw new ArgumentNullException();
        _itemSystem = itemSystem ?? throw new ArgumentNullException();
        _chatSystem = chatSystem ?? throw new ArgumentNullException();
        _commandService = commandService ?? throw new ArgumentNullException();
        _playerSessionRegistry = playerSessionRegistry ?? throw new ArgumentNullException();
        _mapAvailabilityResolver = mapAvailabilityResolver ?? throw new ArgumentNullException();
        _playerEnteredWorldAsync = playerEnteredWorldAsync ?? throw new ArgumentNullException();
        _playerLeftWorldAsync = playerLeftWorldAsync ?? throw new ArgumentNullException();
        _playerMovementAsync = playerMovementAsync ?? throw new ArgumentNullException();
        _playerClientPacketAsync = playerClientPacketAsync ?? throw new ArgumentNullException();
        _messageOfTheDay = string.IsNullOrWhiteSpace(messageOfTheDay) ? "Welcome to Emulation Server." : messageOfTheDay;
        _playerSaveInterval = playerSaveInterval <= TimeSpan.Zero ? TimeSpan.FromSeconds(60) : playerSaveInterval;
        _activePlayerCountChanged = activePlayerCountChanged ?? (_ => { });
        _characterCountChangedAsync = characterCountChangedAsync ?? (_ => Task.CompletedTask);
        _serverSeed = unchecked((uint)RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue));
        Id = Guid.NewGuid();
    }

    /**
      * Exposes the id value to callers that need this runtime or configuration data.
      * The property keeps the public surface strongly typed and documents which part of the server workflow owns the value.
      */
    public Guid Id { get; }

    /**
      * Exposes the current player value to callers that need this runtime or configuration data.
      * The property keeps the public surface strongly typed and documents which part of the server workflow owns the value.
      */
    public PlayerLoginRecord? CurrentPlayer { get; private set; }

    /**
      * Exposes the current movement value to callers that need this runtime or configuration data.
      * The property keeps the public surface strongly typed and documents which part of the server workflow owns the value.
      */
    public PlayerMovementState? CurrentMovement { get; private set; }

    /**
      * Exposes the current map service owner used by the player so WorldServer can evict sessions when that owner goes down.
      */
    public string CurrentMapOwnerServerName => _currentMapOwnerServerName;

    /**
      * Exposes the authenticated account id to command handlers.
      */
    public uint AccountId => _account?.Id ?? 0;

    /**
      * Exposes the authenticated account name to command handlers.
      */
    public string AccountName => _account?.Username ?? string.Empty;

    /**
      * Exposes the RBAC-derived account security level to command handlers.
      */
    public AccountSecurityLevel AccountSecurityLevel => _account?.SecurityLevel ?? AccountSecurityLevel.Player;

    /**
      * Checks the final RBAC permission set for a command or role permission id.
      */
    public bool HasPermission(uint permissionId)
    {
        return _account?.Permissions.HasPermission(permissionId) == true;
    }

    /**
      * Reloads this session's RBAC data from the account database.
      */
    public async Task ReloadPermissionsAsync(CancellationToken cancellationToken)
    {
        WorldAccountSessionRecord account = RequireAccount();
        WorldAccountSessionRecord? reloaded = await _accountRepository.GetAccountSessionAsync(account.Username, _realmId, cancellationToken);
        if (reloaded is null)
        {
            throw new InvalidOperationException($"Account '{account.Username}' could not be reloaded.");
        }

        _account = reloaded;
    }

    /**
      * Stores the default active player count value used when the caller does not supply an override.
      * Centralizing the default keeps configuration and packet behavior consistent across the server process.
      */
    public int ActivePlayerCount => _playerSessionRegistry.ActivePlayerCount;

    /**
      * Stores the default message of the day value used when the caller does not supply an override.
      * Centralizing the default keeps configuration and packet behavior consistent across the server process.
      */
    public string MessageOfTheDay => _messageOfTheDay;

    /**
      * Stores the default remote end point value used when the caller does not supply an override.
      * Centralizing the default keeps configuration and packet behavior consistent across the server process.
      */
    public string RemoteEndPoint => _client.Client.RemoteEndPoint?.ToString() ?? "unknown";

    /**
      * Stores the normalized remote IP address used by account and IP-ban checks.
      */
    private string RemoteAddress => (_client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? string.Empty;

    /**
      * Performs the process operation for the connected world client session lifecycle and packet dispatch workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: serverCancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async Task ProcessAsync(CancellationToken serverCancellationToken)
    {
        using CancellationTokenSource linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            serverCancellationToken,
            _disconnect.Token);

        CancellationToken cancellationToken = linkedCancellation.Token;
        _stream = _client.GetStream();
        WorldMovementDiagnostics.LogEnabledOnce();
        StartMovementBroadcastLoop(cancellationToken);
        StartMapServiceMovementRouteLoop(cancellationToken);
        StartGameplayPacketLoop(cancellationToken);

        try
        {
            await SendAsync(WorldOpcode.SMSG_AUTH_CHALLENGE, WorldPacketBuilders.BuildAuthChallenge(_serverSeed), null, cancellationToken);
            await AuthenticateAsync(cancellationToken);
            await ProcessAuthenticatedPacketsAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (EndOfStreamException)
        {
            Logger.Write(LogType.NETWORK, $"World client disconnected: {RemoteEndPoint}.", "WorldClientSession");
        }
        catch (IOException exception)
        {
            Logger.Write(LogType.NETWORK, $"World client socket closed: {RemoteEndPoint}. {exception.Message}", "WorldClientSession");
        }
        catch (SocketException exception)
        {
            Logger.Write(LogType.NETWORK, $"World client socket failed: {RemoteEndPoint}. {exception.Message}", "WorldClientSession");
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.FAILED, $"World client session failed for {RemoteEndPoint}: {exception}", "WorldClientSession");
        }
        finally
        {
            await CleanupCurrentPlayerAsync(CancellationToken.None);
            await DisconnectAsync();
        }
    }

    /**
      * Performs the disconnect operation for the connected world client session lifecycle and packet dispatch workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async Task DisconnectAsync()
    {
        _movementBroadcastQueue.Writer.TryComplete();
        _mapServiceMovementQueue.Writer.TryComplete();
        _gameplayPacketQueue.Writer.TryComplete();
        await StopBanMonitorAsync();

        if (!_disconnect.IsCancellationRequested)
        {
            await _disconnect.CancelAsync();
        }

        await WaitForNetworkBackgroundLoopsAsync();

        try
        {
            if (_stream is not null)
            {
                await _stream.FlushAsync(CancellationToken.None);
            }
        }
        catch
        {
            // Ignore shutdown races.
        }

        try
        {
            _client.Client.Shutdown(SocketShutdown.Send);
        }
        catch
        {
            // Ignore shutdown races.
        }

        try
        {
            _client.Close();
        }
        catch
        {
            // Ignore shutdown races.
        }
    }


    /**
      * Waits for per-session networking helper loops to observe cancellation before owned resources are disposed.
      */
    private async Task WaitForNetworkBackgroundLoopsAsync()
    {
        Task? movementLoop = _movementBroadcastLoop;
        Task? mapRouteLoop = _mapServiceMovementRouteLoop;
        Task? gameplayLoop = _gameplayPacketLoop;
        _movementBroadcastLoop = null;
        _mapServiceMovementRouteLoop = null;
        _gameplayPacketLoop = null;

        await WaitForBackgroundLoopAsync(movementLoop);
        await WaitForBackgroundLoopAsync(mapRouteLoop);
        await WaitForBackgroundLoopAsync(gameplayLoop);
    }

    /**
      * Suppresses expected shutdown exceptions from a helper loop.
      */
    private static async Task WaitForBackgroundLoopAsync(Task? loop)
    {
        if (loop is null || loop.IsCompleted)
        {
            return;
        }

        try
        {
            Task completedTask = await Task.WhenAny(loop, Task.Delay(TimeSpan.FromSeconds(1)));
            if (completedTask == loop)
            {
                await loop;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    /**
      * Forces an in-world player through the same logout cleanup path when their map or instance owner disappears.
      */
    public async Task DisconnectForMapServiceUnavailableAsync(string ownerServerName, string reason, CancellationToken cancellationToken)
    {
        PlayerLoginRecord? player = CurrentPlayer;
        if (player is null)
        {
            return;
        }

        if (Interlocked.Exchange(ref _serviceDisconnectStarted, 1) == 1)
        {
            return;
        }

        Logger.Write(LogType.WARNING, $"Disconnecting player '{player.Name}' ({player.Guid}) because map service owner '{ownerServerName}' is unavailable. {reason}", "WorldClientSession");

        try
        {
            await SendAsync(WorldOpcode.SMSG_NOTIFICATION, WorldPacketBuilders.BuildNotification(reason), _crypt, CancellationToken.None);
            await SendAsync(WorldOpcode.SMSG_LOGOUT_RESPONSE, WorldPacketBuilders.BuildLogoutResponse(), _crypt, CancellationToken.None);
        }
        catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException or InvalidOperationException)
        {
            Logger.Write(LogType.TRACE, $"Could not send map-service disconnect notice to player '{player.Name}' ({player.Guid}): {exception.Message}", "WorldClientSession");
        }

        await CleanupCurrentPlayerAsync(CancellationToken.None, notifyMapService: false);

        try
        {
            await SendAsync(WorldOpcode.SMSG_LOGOUT_COMPLETE, WorldPacketBuilders.BuildLogoutComplete(), _crypt, CancellationToken.None);
        }
        catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException or InvalidOperationException)
        {
            Logger.Write(LogType.TRACE, $"Could not send logout complete after map-service disconnect for player '{player.Name}' ({player.Guid}): {exception.Message}", "WorldClientSession");
        }

        await DisconnectAsync();
    }

    /**
      * Requires current player for the connected world client session lifecycle and packet dispatch workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      */
    public PlayerLoginRecord RequireCurrentPlayer()
    {
        return CurrentPlayer ?? throw new InvalidOperationException("World client has not entered the game world.");
    }

    /**
      * Opens the player bank using the Vanilla SMSG_SHOW_BANK packet.
      */
    public async Task OpenBankAsync(CancellationToken cancellationToken)
    {
        PlayerLoginRecord player = RequireCurrentPlayer();
        await SendAsync(WorldOpcode.SMSG_SHOW_BANK, WorldPacketBuilders.BuildShowBank(player.ClientGuid), _crypt, cancellationToken);
    }

    /**
      * Determines whether in chat channel for the connected world client session lifecycle and packet dispatch workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: channelName.
      */
    public bool IsInChatChannel(string channelName)
    {
        return _chatChannels.Contains(ChatSystem.NormalizeChannelName(channelName));
    }

    /**
      * Applies the join chat channel state transition to the current runtime session.
      * State changes are routed through one method so logging, validation, and side effects stay aligned with the server lifecycle.
      * Inputs used by this operation: channelName.
      */
    public void JoinChatChannel(string channelName)
    {
        _chatChannels.Add(ChatSystem.NormalizeChannelName(channelName));
    }

    /**
      * Applies the leave chat channel state transition to the current runtime session.
      * State changes are routed through one method so logging, validation, and side effects stay aligned with the server lifecycle.
      * Inputs used by this operation: channelName.
      */
    public void LeaveChatChannel(string channelName)
    {
        _chatChannels.Remove(ChatSystem.NormalizeChannelName(channelName));
    }

    /**
      * Performs the authenticate operation for the connected world client session lifecycle and packet dispatch workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task AuthenticateAsync(CancellationToken cancellationToken)
    {
        WorldPacket packet = await WorldPacketIO.ReadClientPacketAsync(GetStream(), null, _maximumPacketSize, cancellationToken);
        if (packet.Opcode != WorldOpcode.CMSG_AUTH_SESSION)
        {
            Logger.Write(LogType.WARNING, $"World client {RemoteEndPoint} sent {packet.Opcode} before CMSG_AUTH_SESSION.", "WorldClientSession");
            await RejectAuthenticationAsync(AuthResponseCode.Failed, null, "World client did not send CMSG_AUTH_SESSION first.", cancellationToken);
            return;
        }

        WorldAuthSessionRequest request = WorldAuthSessionParser.Parse(packet.Payload);
        string username = WorldAccountRepository.NormalizeUsername(request.Username);
        if (await _accountRepository.IsIpBannedAsync(RemoteAddress, cancellationToken))
        {
            Logger.Write(LogType.WARNING, $"World auth rejected for '{username}' from {RemoteEndPoint}: IP address is banned.", "WorldClientSession");
            await RejectAuthenticationAsync(AuthResponseCode.Banned, null, "World client IP is banned.", cancellationToken);
            return;
        }

        WorldAccountSessionRecord? account = await _accountRepository.GetAccountSessionAsync(username, _realmId, cancellationToken);
        if (account is null || account.Locked)
        {
            Logger.Write(LogType.WARNING, $"World auth rejected for '{username}' from {RemoteEndPoint}: account missing or locked.", "WorldClientSession");
            await RejectAuthenticationAsync(AuthResponseCode.Failed, null, "World account authentication failed.", cancellationToken);
            return;
        }

        AccountBanStatus banStatus = await _accountRepository.GetAccountBanStatusAsync(account.Id, cancellationToken);
        if (banStatus.IsBanned)
        {
            AuthResponseCode responseCode = banStatus.IsPermanent ? AuthResponseCode.Banned : AuthResponseCode.Suspended;
            string banType = banStatus.IsPermanent ? "permanently banned" : "temporarily suspended";
            Logger.Write(LogType.WARNING, $"World auth rejected for '{username}' from {RemoteEndPoint}: account is {banType}.", "WorldClientSession");
            await RejectAuthenticationAsync(responseCode, null, "World account is banned.", cancellationToken);
            return;
        }

        byte[] sessionKey = WorldAuthCryptography.ParseSessionKey(account.SessionKey);
        if (!WorldAuthCryptography.ProofMatches(username, request.ClientSeed, _serverSeed, sessionKey, request.ClientProof))
        {
            Logger.Write(LogType.WARNING, $"World auth proof failed for '{username}' from {RemoteEndPoint}.", "WorldClientSession");
            await RejectAuthenticationAsync(AuthResponseCode.Failed, null, "World account proof failed.", cancellationToken);
            return;
        }

        _account = account;
        _crypt = new WorldHeaderCrypt(sessionKey);
        await _accountRepository.SetActiveRealmAsync(account.Id, _realmId, cancellationToken);

        await SendAsync(WorldOpcode.SMSG_ADDON_INFO, WorldPacketBuilders.BuildAddonInfo(request.AddonInfo), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_AUTH_RESPONSE, WorldPacketBuilders.BuildAuthResponse(AuthResponseCode.Ok), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_ACCOUNT_DATA_TIMES, WorldPacketBuilders.BuildAccountDataTimes(), _crypt, cancellationToken);
        StartBanMonitor();

        Logger.Write(LogType.SUCCESS, $"World client authenticated account '{account.Username}' ({account.Id}) from {RemoteEndPoint}.", "WorldClientSession");
    }

    /**
      * Sends a terminal authentication response and keeps the socket alive long enough for the client to consume it.
      */
    private async Task RejectAuthenticationAsync(AuthResponseCode responseCode, WorldHeaderCrypt? crypt, string exceptionMessage, CancellationToken cancellationToken)
    {
        await SendAsync(WorldOpcode.SMSG_AUTH_RESPONSE, WorldPacketBuilders.BuildAuthResponse(responseCode), crypt, cancellationToken);
        await AllowTerminalResponseDeliveryAsync(cancellationToken);
        throw new UnauthorizedAccessException(exceptionMessage);
    }

    /**
      * Starts the authenticated-account ban monitor.
      * The monitor intentionally runs outside ProcessAuthenticatedPacketsAsync so client movement and ping packets never wait on database work.
      */
    private void StartBanMonitor()
    {
        if (_banMonitorLoop is not null && !_banMonitorLoop.IsCompleted)
        {
            return;
        }

        _banMonitorCancellation?.Cancel();
        _banMonitorCancellation?.Dispose();
        _banMonitorCancellation = CancellationTokenSource.CreateLinkedTokenSource(_disconnect.Token);
        _banMonitorLoop = Task.Run(() => RunBanMonitorAsync(_banMonitorCancellation.Token), CancellationToken.None);
    }

    /**
      * Performs low-frequency ban checks without blocking the socket receive loop.
      */
    private async Task RunBanMonitorAsync(CancellationToken cancellationToken)
    {
        try
        {
            using PeriodicTimer timer = new(BanRecheckInterval);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (await DisconnectIfBanBecameActiveAsync(cancellationToken))
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during disconnect/shutdown.
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.WARNING, $"Ban monitor stopped for {RemoteEndPoint}: {exception.Message}", "WorldClientSession");
        }
    }

    /**
      * Stops the ban monitor without making shutdown noisy.
      */
    private async Task StopBanMonitorAsync()
    {
        CancellationTokenSource? banCancellation = _banMonitorCancellation;
        Task? banLoop = _banMonitorLoop;
        _banMonitorCancellation = null;
        _banMonitorLoop = null;

        if (banCancellation is null)
        {
            return;
        }

        await banCancellation.CancelAsync();
        if (banLoop is not null && Task.CurrentId != banLoop.Id)
        {
            try
            {
                await banLoop;
            }
            catch (OperationCanceledException)
            {
            }
        }

        banCancellation.Dispose();
    }

    /**
      * Sends the matching in-client auth failure when a ban becomes active after the account has already authenticated.
      */
    private async Task<bool> DisconnectIfBanBecameActiveAsync(CancellationToken cancellationToken)
    {
        WorldAccountSessionRecord? account = _account;
        if (account is null || _crypt is null || _disconnect.IsCancellationRequested)
        {
            return false;
        }

        bool ipBanned = await _accountRepository.IsIpBannedAsync(RemoteAddress, cancellationToken);
        AccountBanStatus banStatus = await _accountRepository.GetAccountBanStatusAsync(account.Id, cancellationToken);
        if (!ipBanned && !banStatus.IsBanned)
        {
            return false;
        }

        if (Interlocked.Exchange(ref _banDisconnectStarted, 1) == 1)
        {
            return true;
        }

        AuthResponseCode responseCode = ipBanned || banStatus.IsPermanent ? AuthResponseCode.Banned : AuthResponseCode.Suspended;
        string banType = ipBanned ? "IP banned" : (banStatus.IsPermanent ? "permanently banned" : "temporarily suspended");
        Logger.Write(LogType.WARNING, $"World client {RemoteEndPoint} disconnected because account '{account.Username}' is now {banType}.", "WorldClientSession");
        await SendAsync(WorldOpcode.SMSG_AUTH_RESPONSE, WorldPacketBuilders.BuildAuthResponse(responseCode), _crypt, cancellationToken);
        await AllowTerminalResponseDeliveryAsync(cancellationToken);
        await DisconnectAsync();
        return true;
    }

    /**
      * Gives terminal authentication packets a brief delivery window before the owning session closes the socket.
      */
    private static async Task AllowTerminalResponseDeliveryAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TerminalAuthFailureDeliveryDelay, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutdown cancellation should not turn an already-sent auth failure into a noisy session error.
        }
    }

    /**
      * Performs the process authenticated packets operation for the connected world client session lifecycle and packet dispatch workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task ProcessAuthenticatedPacketsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            WorldPacket packet = await WorldPacketIO.ReadClientPacketAsync(GetStream(), _crypt, _maximumPacketSize, cancellationToken);

            if (packet.Opcode == WorldOpcode.CMSG_PING)
            {
                await HandlePingAsync(packet, cancellationToken);
                continue;
            }

            if (WorldMovementOpcode.IsMovementOpcode(packet.Opcode))
            {
                HandleMovementPacket(packet);
                continue;
            }

            await QueueGameplayPacketAsync(packet, cancellationToken);
        }
    }

    /**
      * Queues regular gameplay/control packets behind a per-session worker so the socket receive loop can keep reading pings and movement.
      * Movement remains on the hot path because the latest position should be accepted immediately; slower DB/control handlers are serialized here.
      */
    private async Task QueueGameplayPacketAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        await _gameplayPacketQueue.Writer.WriteAsync(packet, cancellationToken);
    }

    /**
      * Starts the regular gameplay packet worker once for this session.
      */
    private void StartGameplayPacketLoop(CancellationToken cancellationToken)
    {
        _gameplayPacketLoop ??= Task.Run(() => ProcessGameplayPacketQueueAsync(cancellationToken), CancellationToken.None);
    }

    /**
      * Processes queued non-movement packets in order while the receive loop stays free for latency-sensitive pings and movement.
      */
    private async Task ProcessGameplayPacketQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _gameplayPacketQueue.Reader.WaitToReadAsync(cancellationToken))
            {
                while (_gameplayPacketQueue.Reader.TryRead(out WorldPacket? packet))
                {
                    if (packet is null)
                    {
                        continue;
                    }

                    await DispatchAuthenticatedPacketAsync(packet, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during disconnect/shutdown.
        }
        catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException or InvalidOperationException)
        {
            Logger.Write(LogType.NETWORK, $"Gameplay packet worker stopped for {RemoteEndPoint}: {exception.Message}", "WorldClientSession");
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.CRITICAL, $"Gameplay packet worker failed for {RemoteEndPoint}: {exception}", "WorldClientSession");
            await DisconnectAsync();
        }
    }

    /**
      * Dispatches non-hot-path client packets from the per-session gameplay worker.
      */
    private async Task DispatchAuthenticatedPacketAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        switch (packet.Opcode)
            {
                case WorldOpcode.CMSG_PING:
                    await HandlePingAsync(packet, cancellationToken);
                    break;

                case WorldOpcode.CMSG_CHAR_ENUM:
                    await HandleCharacterEnumAsync(cancellationToken);
                    break;

                case WorldOpcode.CMSG_CHAR_CREATE:
                    await HandleCharacterCreateAsync(packet, cancellationToken);
                    break;

                case WorldOpcode.CMSG_REQUEST_ACCOUNT_DATA:
                    await HandleRequestAccountDataAsync(packet, cancellationToken);
                    break;

                case WorldOpcode.CMSG_UPDATE_ACCOUNT_DATA:
                    Logger.Write(LogType.TRACE, $"Received CMSG_UPDATE_ACCOUNT_DATA from {RemoteEndPoint}; persistence is not implemented yet.", "WorldClientSession");
                    break;

                case WorldOpcode.CMSG_CHAR_DELETE:
                    await HandleCharacterDeleteAsync(packet, cancellationToken);
                    break;

                case WorldOpcode.CMSG_PLAYER_LOGIN:
                    await HandlePlayerLoginAsync(packet, cancellationToken);
                    break;

                case WorldOpcode.CMSG_ITEM_QUERY_SINGLE:
                    await HandleItemQuerySingleAsync(packet, cancellationToken);
                    break;

                case WorldOpcode.CMSG_ITEM_NAME_QUERY:
                    await HandleItemNameQueryAsync(packet, cancellationToken);
                    break;

                case WorldOpcode.CMSG_NAME_QUERY:
                    await HandleNameQueryAsync(packet, cancellationToken);
                    break;

                case WorldOpcode.CMSG_WHO:
                    await HandleWhoAsync(cancellationToken);
                    break;

                case WorldOpcode.CMSG_PLAYER_LOGOUT:
                case WorldOpcode.CMSG_LOGOUT_REQUEST:
                    await HandleLogoutRequestAsync(cancellationToken);
                    break;

                case WorldOpcode.CMSG_LOGOUT_CANCEL:
                    await SendAsync(WorldOpcode.SMSG_LOGOUT_CANCEL_ACK, WorldPacketBuilders.BuildLogoutCancelAck(), _crypt, cancellationToken);
                    break;

                case WorldOpcode.CMSG_MESSAGECHAT:
                    await HandleMessageChatAsync(packet, cancellationToken);
                    break;

                case WorldOpcode.CMSG_JOIN_CHANNEL:
                    await HandleJoinChannelAsync(packet, cancellationToken);
                    break;

                case WorldOpcode.CMSG_LEAVE_CHANNEL:
                    await HandleLeaveChannelAsync(packet, cancellationToken);
                    break;

                case WorldOpcode.CMSG_CHANNEL_LIST:
                    await HandleChannelListAsync(packet, cancellationToken);
                    break;

                case WorldOpcode.CMSG_QUERY_TIME:
                    await SendAsync(WorldOpcode.SMSG_QUERY_TIME_RESPONSE, WorldPacketBuilders.BuildServerTime(DateTimeOffset.Now), _crypt, cancellationToken);
                    break;

                case WorldOpcode.CMSG_SERVERTIME:
                    await SendAsync(WorldOpcode.SMSG_SERVERTIME, WorldPacketBuilders.BuildServerTime(DateTimeOffset.Now), _crypt, cancellationToken);
                    break;

                case WorldOpcode.CMSG_PLAYED_TIME:
                    await HandlePlayedTimeAsync(cancellationToken);
                    break;

                case WorldOpcode.CMSG_BANKER_ACTIVATE:
                    await OpenBankAsync(cancellationToken);
                    break;

                case WorldOpcode.CMSG_SWAP_INV_ITEM:
                    await HandleSwapInvItemAsync(packet, cancellationToken);
                    break;

                case WorldOpcode.CMSG_SWAP_ITEM:
                    await HandleSwapItemAsync(packet, cancellationToken);
                    break;

                case WorldOpcode.CMSG_AUTOEQUIP_ITEM:
                    await HandleAutoEquipItemAsync(packet, cancellationToken);
                    break;

                case WorldOpcode.CMSG_AUTOEQUIP_ITEM_SLOT:
                    await HandleAutoEquipItemSlotAsync(packet, cancellationToken);
                    break;

                case WorldOpcode.CMSG_AUTOSTORE_BAG_ITEM:
                    await HandleAutoStoreBagItemAsync(packet, cancellationToken);
                    break;

                case WorldOpcode.CMSG_SPLIT_ITEM:
                    await HandleSplitItemAsync(packet, cancellationToken);
                    break;

                case WorldOpcode.CMSG_DESTROYITEM:
                    await SendInventoryFailureAsync(InventoryChangeFailureItemDoesntGoToSlot, 0, 0, cancellationToken);
                    break;

                case WorldOpcode.CMSG_OPENING_CINEMATIC:
                case WorldOpcode.CMSG_NEXT_CINEMATIC_CAMERA:
                case WorldOpcode.CMSG_COMPLETE_CINEMATIC:
                case WorldOpcode.CMSG_TUTORIAL_FLAG:
                case WorldOpcode.CMSG_TUTORIAL_CLEAR:
                case WorldOpcode.CMSG_TUTORIAL_RESET:
                case WorldOpcode.CMSG_STANDSTATECHANGE:
                case WorldOpcode.CMSG_SET_ACTION_BUTTON:
                case WorldOpcode.CMSG_SET_ACTIONBAR_TOGGLES:
                    Logger.Write(LogType.TRACE, $"Accepted client interface opcode {packet.Opcode} from {RemoteEndPoint}; persistence is not implemented yet.", "WorldClientSession");
                    break;

                case WorldOpcode.CMSG_AREATRIGGER:
                    await ForwardPacketToMapServiceAsync(packet, cancellationToken);
                    break;

                case WorldOpcode.CMSG_ZONEUPDATE:
                    await HandleZoneUpdateAsync(packet, cancellationToken);
                    break;

                case var movementOpcode when WorldMovementOpcode.IsMovementOpcode(movementOpcode):
                    HandleMovementPacket(packet);
                    break;

                default:
                    // Do not forward every unknown client opcode to MapServer. A Vanilla
                    // client can send high-frequency UI/movement-related packets after
                    // entering the world, and routing each one through the internal text
                    // stream can make the world feel laggy. Packets should be forwarded
                    // only after a concrete Map/Instance handler exists for them.
                    if (_reportedUnhandledOpcodes.Add(packet.Opcode))
                    {
                        Logger.Write(LogType.TRACE, $"Unhandled world opcode from {RemoteEndPoint}: {packet.Opcode} (0x{(ushort)packet.Opcode:X4}), payload={packet.Payload.Length} byte(s). Future packets with this opcode will be accepted silently until a handler is implemented.", "WorldClientSession");
                    }
                    break;
            }
    }


    /**
      * Handles the handle ping event for the connected world client session lifecycle and packet dispatch workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: packet, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task HandlePingAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        WorldPacketReader reader = new(packet.Payload);
        uint sequence = reader.ReadUInt32();
        _ = packet.Payload.Length >= 8 ? reader.ReadUInt32() : 0;

        await SendAsync(WorldOpcode.SMSG_PONG, WorldPacketBuilders.BuildPong(sequence), _crypt, cancellationToken);
    }

    /**
      * Delays a character enum response immediately after a failed player-login attempt.
      * Vanilla clients automatically request a fresh character list after SMSG_CHARACTER_LOGIN_FAILED; answering that request too quickly can clear the visible failure popup.
      */
    private async Task DelayCharacterEnumAfterLoginFailureAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset delayUntilUtc = _delayCharacterEnumUntilUtc;
        if (delayUntilUtc <= DateTimeOffset.UtcNow)
        {
            return;
        }

        TimeSpan remainingDelay = delayUntilUtc - DateTimeOffset.UtcNow;
        try
        {
            await Task.Delay(remainingDelay, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutdown cancellation should not turn a deliberately delayed character-list refresh into a noisy session error.
        }
        finally
        {
            _delayCharacterEnumUntilUtc = DateTimeOffset.MinValue;
        }
    }

    /**
      * Handles the handle character enum event for the connected world client session lifecycle and packet dispatch workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task HandleCharacterEnumAsync(CancellationToken cancellationToken)
    {
        await DelayCharacterEnumAfterLoginFailureAsync(cancellationToken);

        WorldAccountSessionRecord account = RequireAccount();

        try
        {
            IReadOnlyList<CharacterListEntry> characters = await _characterService.GetCharacterListAsync(account.Id, cancellationToken);
            byte[] payload = WorldPacketBuilders.BuildCharacterEnum(characters);
            await SendAsync(WorldOpcode.SMSG_CHAR_ENUM, payload, _crypt, cancellationToken);

            Logger.Write(LogType.NETWORK, $"Sent character list to account '{account.Username}': {characters.Count} character(s), payload={payload.Length} byte(s).", "WorldClientSession");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Logger.Write(LogType.FAILED, $"Failed to build/send character list for account '{account.Username}' ({account.Id}): {exception}", "WorldClientSession");
            await SendAsync(WorldOpcode.SMSG_CHAR_ENUM, WorldPacketBuilders.BuildCharacterEnum(Array.Empty<CharacterListEntry>()), _crypt, cancellationToken);
        }
    }

    /**
      * Handles the handle player login event for the connected world client session lifecycle and packet dispatch workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: packet, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task HandlePlayerLoginAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        WorldAccountSessionRecord account = RequireAccount();
        uint characterGuid = CharacterGuid.FromClientGuid(ReadClientGuid(packet.Payload));
        if (characterGuid == 0)
        {
            await SendCharacterLoginFailedWithReasonAsync(CharacterLoginFailureCode.NotFound, $"Player login rejected for account '{account.Username}': client sent an empty character guid.", cancellationToken);
            return;
        }

        PlayerLoginRecord? player = await _characterRepository.GetPlayerForLoginAsync(account.Id, characterGuid, ResolveFactionForRace, cancellationToken);
        if (player is null)
        {
            await SendCharacterLoginFailedWithReasonAsync(CharacterLoginFailureCode.NotFound, $"Player login rejected for account '{account.Username}': guid={characterGuid} was not found or was not owned by the account.", cancellationToken);
            return;
        }

        MapAvailabilityResult mapAvailability = _mapAvailabilityResolver(player);
        if (!mapAvailability.IsAvailable)
        {
            await SendMapUnavailableLoginFailedAsync(player, mapAvailability, $"Player login rejected for '{player.Name}' ({player.Guid}): map={player.Map} is unavailable. {mapAvailability.Reason}", cancellationToken);
            return;
        }

        if (!_playerSessionRegistry.TryRegister(player, this))
        {
            await SendCharacterLoginFailedWithReasonAsync(CharacterLoginFailureCode.DuplicateLogin, $"Player login rejected for '{player.Name}' ({player.Guid}): duplicate account or character session.", cancellationToken);
            return;
        }

        try
        {
            await _characterRepository.SetCharacterOnlineAsync(player.Guid, true, cancellationToken);
            CurrentPlayer = player;
            CurrentMovement = PlayerMovementState.FromPlayer(player);
            _lastPlayerRecordMovementUpdateUtc = DateTimeOffset.UtcNow;
            _lastPlayerTimeSaveUtc = DateTimeOffset.UtcNow;
            _playerStateDirty = true;
            _currentMapOwnerServerName = mapAvailability.OwnerServerName;
            ResetMapServiceMovementRoute();
            StartPlayerSaveTimer();
            await _playerEnteredWorldAsync(player, _currentMapOwnerServerName, cancellationToken);
            await SendWorldEntryPacketsAsync(player, cancellationToken);

            _activePlayerCountChanged(_playerSessionRegistry.ActivePlayerCount);
            Logger.Write(LogType.SUCCESS, $"Player '{player.Name}' ({player.Guid}) entered world map={player.Map}, zone={player.Zone} through {mapAvailability.OwnerServerName}.", "WorldClientSession");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await StopPlayerSaveTimerAsync();
            CurrentPlayer = null;
            CurrentMovement = null;
            _currentMapOwnerServerName = string.Empty;
            ResetMapServiceMovementRoute();
            _playerSessionRegistry.Unregister(player, this);
            await _characterRepository.SetCharacterOnlineAsync(player.Guid, false, CancellationToken.None);

            await SendMapUnavailableLoginFailedAsync(player, mapAvailability, $"Player login failed while entering world for '{player.Name}' ({player.Guid}) on map={player.Map} through {mapAvailability.OwnerServerName}: {exception.Message}", cancellationToken);
        }
    }

    /**
      * Sends send character login failed data to the connected session or internal peer.
      * The send path keeps packet construction and delivery together so opcode handling remains easy to trace during protocol debugging.
      * Inputs used by this operation: failureCode, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task SendCharacterLoginFailedAsync(CharacterLoginFailureCode failureCode, CancellationToken cancellationToken)
    {
        // Do not follow this with SMSG_CHAR_ENUM. During CMSG_PLAYER_LOGIN the
        // 1.12 client is in the character-login transition state and expects a
        // single login failure result. Sending a character list immediately after
        // the failure can make the client treat the world socket as invalid.
        await SendAsync(WorldOpcode.SMSG_CHARACTER_LOGIN_FAILED, WorldPacketBuilders.BuildCharacterLoginFailed(failureCode), _crypt, cancellationToken);
        MarkCharacterEnumDelayWindow();
        await AllowCharacterLoginFailureDeliveryAsync(cancellationToken);
    }

    /**
      * Sends a character login failure and records the server-side reason that maps to the client-visible failure text.
      */
    private async Task SendCharacterLoginFailedWithReasonAsync(CharacterLoginFailureCode failureCode, string reason, CancellationToken cancellationToken)
    {
        Logger.Write(LogType.WARNING, $"{reason} Client failure code: {failureCode}.", "WorldClientSession");
        await SendCharacterLoginFailedAsync(failureCode, cancellationToken);
    }

    /**
      * Sends every client-visible rejection packet available during the character-login transition for missing or unavailable map ownership.
      */
    private async Task SendMapUnavailableLoginFailedAsync(PlayerLoginRecord player, MapAvailabilityResult mapAvailability, string reason, CancellationToken cancellationToken)
    {
        CharacterLoginFailureCode failureCode = ResolveMapAvailabilityFailureCode(mapAvailability);
        TransferAbortReason transferAbortReason = ResolveMapAvailabilityTransferAbortReason(mapAvailability);
        string clientMessage = BuildMapUnavailableClientMessage(player, mapAvailability);

        Logger.Write(LogType.WARNING, $"{reason} Client failure code: {failureCode}; transfer abort reason: {transferAbortReason}.", "WorldClientSession");

        await SendAsync(WorldOpcode.SMSG_TRANSFER_ABORTED, WorldPacketBuilders.BuildTransferAborted(player.Map, transferAbortReason), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_CHARACTER_LOGIN_FAILED, WorldPacketBuilders.BuildCharacterLoginFailed(failureCode), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_NOTIFICATION, WorldPacketBuilders.BuildNotification(clientMessage), _crypt, cancellationToken);

        MarkCharacterEnumDelayWindow();
        await AllowCharacterLoginFailureDeliveryAsync(cancellationToken);
    }

    /**
      * Marks the short window where automatic character-list refreshes should wait behind a login failure popup.
      */
    private void MarkCharacterEnumDelayWindow()
    {
        _delayCharacterEnumUntilUtc = DateTimeOffset.UtcNow.Add(CharacterLoginFailureDeliveryDelay);
    }

    /**
      * Gives character-login failure packets a brief delivery window while keeping the authenticated world socket alive.
      */
    private static async Task AllowCharacterLoginFailureDeliveryAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(CharacterLoginFailureDeliveryDelay, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutdown cancellation should not turn an already-sent character-login failure into a noisy session error.
        }
    }

    /**
      * Chooses the vanilla character-login failure code that gives the client the closest built-in reason text for map availability failures.
      */
    private static CharacterLoginFailureCode ResolveMapAvailabilityFailureCode(MapAvailabilityResult mapAvailability)
    {
        return mapAvailability.RequiresInstanceServer ? CharacterLoginFailureCode.NoInstances : CharacterLoginFailureCode.NoWorld;
    }

    /**
      * Chooses the closest built-in transfer-abort reason for the unavailable map owner case.
      */
    private static TransferAbortReason ResolveMapAvailabilityTransferAbortReason(MapAvailabilityResult mapAvailability)
    {
        return mapAvailability.RequiresInstanceServer ? TransferAbortReason.InstanceNotFound : TransferAbortReason.MapNotAllowed;
    }

    /**
      * Builds the fallback client notification used when the character-login failure packet is visually swallowed by the character-list refresh.
      */
    private static string BuildMapUnavailableClientMessage(PlayerLoginRecord player, MapAvailabilityResult mapAvailability)
    {
        string serviceName = mapAvailability.RequiresInstanceServer ? "instance server" : "world server";
        return $"Unable to enter world: no {serviceName} is currently available for map {player.Map}.";
    }

    /**
      * Sends send world entry packets data to the connected session or internal peer.
      * The send path keeps packet construction and delivery together so opcode handling remains easy to trace during protocol debugging.
      * Inputs used by this operation: player, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task SendWorldEntryPacketsAsync(PlayerLoginRecord player, CancellationToken cancellationToken)
    {
        DateTimeOffset localTime = DateTimeOffset.Now;
        // Follow the vanilla login bootstrap order more closely:
        // login position/account cache first, then pre-map UI state, then the
        // object create packet that makes the player appear in the world.
        await SendAsync(WorldOpcode.SMSG_LOGIN_VERIFY_WORLD, WorldPacketBuilders.BuildLoginVerifyWorld(player), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_ACCOUNT_DATA_TIMES, WorldPacketBuilders.BuildAccountDataTimes(), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_SET_REST_START, WorldPacketBuilders.BuildSetRestStart(localTime), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_BINDPOINTUPDATE, WorldPacketBuilders.BuildBindPointUpdate(player), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_TUTORIAL_FLAGS, WorldPacketBuilders.BuildTutorialFlags(player), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_INITIAL_SPELLS, WorldPacketBuilders.BuildInitialSpells(player), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_ACTION_BUTTONS, WorldPacketBuilders.BuildActionButtons(player), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_INITIALIZE_FACTIONS, WorldPacketBuilders.BuildInitializeFactions(), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_LOGIN_SETTIMESPEED, WorldPacketBuilders.BuildLoginSetTimeSpeed(localTime), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_UPDATE_OBJECT, WorldPacketBuilders.BuildPlayerCreateUpdate(player), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_NAME_QUERY_RESPONSE, WorldPacketBuilders.BuildNameQueryResponse(new CharacterNameQueryResult(player.Guid, player.Name, player.Race, player.Gender, player.Class)), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_MOTD, WorldPacketBuilders.BuildMessageOfTheDay(_messageOfTheDay), _crypt, cancellationToken);
        await JoinDefaultChatChannelsAsync(cancellationToken);
    }

    /**
      * Performs the forward packet to map service operation for the connected world client session lifecycle and packet dispatch workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: packet, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task<bool> ForwardPacketToMapServiceAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        PlayerLoginRecord? player = CurrentPlayer;
        string ownerServerName = _currentMapOwnerServerName;
        if (player is null || string.IsNullOrWhiteSpace(ownerServerName))
        {
            return false;
        }

        try
        {
            await _playerClientPacketAsync(player, ownerServerName, packet, cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Logger.Write(LogType.WARNING, $"Failed to forward {packet.Opcode} from player '{player.Name}' ({player.Guid}) to {ownerServerName}: {exception.Message}", "WorldClientSession");
            await NotifyMapServiceFailureAsync($"The map service for map {player.Map} is not available right now. Some actions may not work until it returns.", cancellationToken);
            return false;
        }
    }

    /**
      * Handles the handle movement packet event for the connected world client session lifecycle and packet dispatch workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: packet, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private void HandleMovementPacket(WorldPacket packet)
    {
        PlayerLoginRecord? player = CurrentPlayer;
        string ownerServerName = _currentMapOwnerServerName;
        if (player is null || string.IsNullOrWhiteSpace(ownerServerName))
        {
            return;
        }

        if (!WorldMovementPacketParser.TryReadMovementState(player, packet.Opcode, packet.Payload, out PlayerMovementState? movement))
        {
            if (_reportedUnhandledOpcodes.Add(packet.Opcode))
            {
                Logger.Write(LogType.TRACE, $"Accepted movement opcode {packet.Opcode} from {RemoteEndPoint}, but no position state could be parsed from payload={packet.Payload.Length} byte(s). Future packets with this opcode will be accepted silently.", "WorldClientSession");
            }

            // Do not forward unparsed movement as generic client-packet traffic. Some movement opcodes are
            // high-frequency, and forwarding the raw payload through the internal text stream can create
            // visible client latency without giving Map/Instance services useful authoritative state.
            return;
        }

        PlayerMovementState? previousMovement = CurrentMovement;
        WorldMovementDiagnostics.LogIncomingMovement(packet.Opcode, packet.Payload.Length, player, movement, previousMovement, RemoteEndPoint);

        ApplyMovementState(movement);
        PlayerLoginRecord updatedPlayer = RequireCurrentPlayer();

        QueueMovementBroadcastToNearbyPlayers(packet, movement);

        if (!ShouldRouteMovementToMapService(movement))
        {
            return;
        }

        QueueMapServiceMovement(updatedPlayer, ownerServerName, movement);
    }

    /**
      * Enqueues the latest coalesced movement telemetry for Map/Instance services without starting one task per movement sample.
      */
    private void QueueMapServiceMovement(PlayerLoginRecord player, string ownerServerName, PlayerMovementState movement)
    {
        if (_disconnect.IsCancellationRequested || string.IsNullOrWhiteSpace(ownerServerName))
        {
            return;
        }

        _mapServiceMovementQueue.Writer.TryWrite(new QueuedMapServiceMovement(player, ownerServerName, movement));
    }

    /**
      * Starts the serialized World -> Map/Instance movement telemetry route loop.
      */
    private void StartMapServiceMovementRouteLoop(CancellationToken cancellationToken)
    {
        _mapServiceMovementRouteLoop ??= Task.Run(() => ProcessMapServiceMovementRouteQueueAsync(cancellationToken), CancellationToken.None);
    }

    /**
      * Routes coalesced movement telemetry on one background path so slow internal networking cannot pile up movement tasks.
      */
    private async Task ProcessMapServiceMovementRouteQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _mapServiceMovementQueue.Reader.WaitToReadAsync(cancellationToken))
            {
                QueuedMapServiceMovement latest = default;
                bool hasLatest = false;

                while (_mapServiceMovementQueue.Reader.TryRead(out QueuedMapServiceMovement queued))
                {
                    latest = queued;
                    hasLatest = true;
                }

                if (!hasLatest)
                {
                    continue;
                }

                DateTimeOffset routeStartedUtc = DateTimeOffset.UtcNow;
                await _playerMovementAsync(latest.Player, latest.OwnerServerName, latest.Movement, cancellationToken);
                WorldMovementDiagnostics.LogMapServiceMovementRoute(
                    latest.Player,
                    latest.OwnerServerName,
                    latest.Movement,
                    routeStartedUtc,
                    DateTimeOffset.UtcNow - routeStartedUtc,
                    RemoteEndPoint);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown or session disconnect.
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.WARNING, $"Map-service movement route writer stopped for {RemoteEndPoint}: {exception.Message}", "WorldClientSession");
        }
    }

    /**
      * Clears movement telemetry throttle state when the player enters or leaves a routed map owner.
      */
    private void ResetMapServiceMovementRoute()
    {
        _lastMapServiceMovementRouteUtc = DateTimeOffset.MinValue;
        _lastMapServiceMovementRouteMap = 0;
        _lastMapServiceMovementRouteZone = 0;
        _hasLastMapServiceMovementRoute = false;
    }

    /**
      * Returns true when the latest movement should be sent to Map/Instance services.
      * Normal movement packets are coalesced because WorldServer remains authoritative for the client socket and nearby-player broadcast.
      */
    private bool ShouldRouteMovementToMapService(PlayerMovementState movement)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        bool mapOrZoneChanged = !_hasLastMapServiceMovementRoute ||
            _lastMapServiceMovementRouteMap != movement.Map ||
            _lastMapServiceMovementRouteZone != movement.Zone;

        if (!mapOrZoneChanged && now - _lastMapServiceMovementRouteUtc < MapServiceMovementRouteInterval)
        {
            return false;
        }

        _lastMapServiceMovementRouteUtc = now;
        _lastMapServiceMovementRouteMap = movement.Map;
        _lastMapServiceMovementRouteZone = movement.Zone;
        _hasLastMapServiceMovementRoute = true;
        return true;
    }

    /**
      * Performs the apply movement state operation for the connected world client session lifecycle and packet dispatch workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: movement.
      */
    private void ApplyMovementState(PlayerMovementState movement)
    {
        PlayerLoginRecord? player = CurrentPlayer;
        if (player is null)
        {
            return;
        }

        CurrentMovement = movement;
        _playerStateDirty = true;

        bool mapOrZoneChanged = player.Map != movement.Map || player.Zone != movement.Zone;
        if (!mapOrZoneChanged && movement.LastUpdatedUtc - _lastPlayerRecordMovementUpdateUtc < PlayerRecordMovementUpdateInterval)
        {
            return;
        }

        CurrentPlayer = ApplyMovementToPlayerRecord(player, movement);
        _lastPlayerRecordMovementUpdateUtc = movement.LastUpdatedUtc;
    }

    /**
      * Applies the latest movement coordinates to the heavier player record only when persistence or slower systems need it.
      */
    private static PlayerLoginRecord ApplyMovementToPlayerRecord(PlayerLoginRecord player, PlayerMovementState movement)
    {
        return player with
        {
            Map = movement.Map,
            Zone = movement.Zone,
            PositionX = movement.PositionX,
            PositionY = movement.PositionY,
            PositionZ = movement.PositionZ,
            Orientation = movement.Orientation,
        };
    }

    /**
      * Copies CurrentMovement into CurrentPlayer before persistence-sensitive operations.
      */
    private void SynchronizeCurrentPlayerRecordFromMovement()
    {
        PlayerLoginRecord? player = CurrentPlayer;
        PlayerMovementState? movement = CurrentMovement;
        if (player is null || movement is null)
        {
            return;
        }

        CurrentPlayer = ApplyMovementToPlayerRecord(player, movement);
        _lastPlayerRecordMovementUpdateUtc = movement.LastUpdatedUtc;
    }

    /**
      * Performs the broadcast movement to nearby players operation for the connected world client session lifecycle and packet dispatch workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: packet, movement, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private void QueueMovementBroadcastToNearbyPlayers(WorldPacket packet, PlayerMovementState movement)
    {
        if (!WorldMovementOpcode.HasMovementInfoAtPayloadStart(packet.Opcode))
        {
            return;
        }

        PlayerLoginRecord? player = CurrentPlayer;
        if (player is null)
        {
            return;
        }

        byte[]? payload = null;
        foreach (WorldClientSession recipient in _playerSessionRegistry.EnumerateSessions())
        {
            PlayerLoginRecord? recipientPlayer = recipient.CurrentPlayer;
            if (ReferenceEquals(recipient, this) || recipientPlayer is null || recipientPlayer.Map != movement.Map)
            {
                continue;
            }

            if (recipientPlayer.Guid == player.Guid || recipientPlayer.ClientGuid == movement.ClientGuid)
            {
                WorldMovementDiagnostics.LogSkippedSelfMovementBroadcast(player, recipientPlayer, movement, RemoteEndPoint, recipient.RemoteEndPoint);
                continue;
            }

            if (!IsWithinMovementBroadcastRange(movement, recipient.CurrentMovement, recipientPlayer))
            {
                continue;
            }

            payload ??= WorldPacketBuilders.BuildMovementBroadcast(movement.ClientGuid, packet.Payload);
            recipient.TryQueueMovementPacket(packet.Opcode, payload);
        }
    }

    /**
      * Enqueues a high-frequency movement update for this session without blocking the source player's packet receive loop.
      * Old movement is intentionally dropped when a recipient falls behind because the newest position supersedes it.
      */
    private bool TryQueueMovementPacket(WorldOpcode opcode, byte[] payload)
    {
        if (_crypt is null || _disconnect.IsCancellationRequested)
        {
            return false;
        }

        return _movementBroadcastQueue.Writer.TryWrite(new QueuedMovementPacket(opcode, payload));
    }

    /**
      * Starts the per-session movement broadcast writer once the client network stream is available.
      */
    private void StartMovementBroadcastLoop(CancellationToken cancellationToken)
    {
        _movementBroadcastLoop ??= Task.Run(() => ProcessMovementBroadcastQueueAsync(cancellationToken), CancellationToken.None);
    }

    /**
      * Drains queued movement packets for this session on one writer path.
      * This mirrors the server-side buffering pattern for outbound packets instead of writing them inline from gameplay packet handling.
      */
    private async Task ProcessMovementBroadcastQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _movementBroadcastQueue.Reader.WaitToReadAsync(cancellationToken))
            {
                while (_movementBroadcastQueue.Reader.TryRead(out QueuedMovementPacket packet))
                {
                    if (_crypt is null)
                    {
                        continue;
                    }

                    await SendAsync(packet.Opcode, packet.Payload, _crypt, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during disconnect/shutdown.
        }
        catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException or InvalidOperationException)
        {
            Logger.Write(LogType.TRACE, $"Movement broadcast writer stopped for {RemoteEndPoint}: {exception.Message}", "WorldClientSession");
        }
    }

    /**
      * Handles the handle zone update event for the connected world client session lifecycle and packet dispatch workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: packet, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task HandleZoneUpdateAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        PlayerLoginRecord? player = CurrentPlayer;
        if (player is null)
        {
            return;
        }

        if (packet.Payload.Length >= sizeof(uint))
        {
            WorldPacketReader reader = new(packet.Payload);
            uint zone = reader.ReadUInt32();
            CurrentPlayer = player with { Zone = zone };
            if (CurrentMovement is not null)
            {
                CurrentMovement = CurrentMovement with { Zone = zone, LastUpdatedUtc = DateTimeOffset.UtcNow };
            }

            _playerStateDirty = true;
        }

        await ForwardPacketToMapServiceAsync(packet, cancellationToken);
    }

    /**
      * Starts the start player save timer workflow and prepares the component to accept runtime work.
      * Startup is ordered so validation and dependency setup finish before services are announced as available.
      */
    private void StartPlayerSaveTimer()
    {
        _playerSaveCancellation?.Cancel();
        _playerSaveCancellation?.Dispose();

        _playerSaveCancellation = CancellationTokenSource.CreateLinkedTokenSource(_disconnect.Token);
        _playerSaveLoop = RunPlayerSaveTimerAsync(_playerSaveCancellation.Token);
    }

    /**
      * Performs the run player save timer operation for the connected world client session lifecycle and packet dispatch workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task RunPlayerSaveTimerAsync(CancellationToken cancellationToken)
    {
        try
        {
            using PeriodicTimer timer = new(_playerSaveInterval);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await SaveCurrentPlayerAsync(force: false, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.WARNING, $"Player save timer stopped for {RemoteEndPoint}: {exception.Message}", "WorldClientSession");
        }
    }

    /**
      * Stops the stop player save timer workflow and releases owned runtime resources in a controlled order.
      * Shutdown logic is centralized to avoid dangling connections, incomplete saves, or partially registered services.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task StopPlayerSaveTimerAsync()
    {
        CancellationTokenSource? saveCancellation = _playerSaveCancellation;
        Task? saveLoop = _playerSaveLoop;
        _playerSaveCancellation = null;
        _playerSaveLoop = null;

        if (saveCancellation is null)
        {
            return;
        }

        await saveCancellation.CancelAsync();
        if (saveLoop is not null)
        {
            try
            {
                await saveLoop;
            }
            catch (OperationCanceledException)
            {
            }
        }

        saveCancellation.Dispose();
    }

    /**
      * Updates save current player state in memory or persistent storage.
      * The method keeps mutation rules centralized so player/account data changes remain auditable and safe to call from packet handlers.
      * Inputs used by this operation: force, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task SaveCurrentPlayerAsync(bool force, CancellationToken cancellationToken)
    {
        SynchronizeCurrentPlayerRecordFromMovement();

        PlayerLoginRecord? player = CurrentPlayer;
        if (player is null || (!force && !_playerStateDirty))
        {
            return;
        }

        await _playerSaveLock.WaitAsync(cancellationToken);
        try
        {
            player = CurrentPlayer;
            if (player is null || (!force && !_playerStateDirty))
            {
                return;
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            uint elapsedSeconds = SaturatingSeconds(now - _lastPlayerTimeSaveUtc);
            PlayerLoginRecord snapshot = player with
            {
                TotalTime = AddClamped(player.TotalTime, elapsedSeconds),
                LevelTime = AddClamped(player.LevelTime, elapsedSeconds),
            };

            if (force)
            {
                await _characterRepository.SavePlayerAsync(snapshot, cancellationToken);
            }
            else
            {
                await _characterRepository.SavePlayerPositionAsync(snapshot, cancellationToken);
            }

            CurrentPlayer = snapshot;
            _lastPlayerTimeSaveUtc = now;
            _playerStateDirty = false;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Logger.Write(LogType.WARNING, $"Failed to save player state for {player?.Name ?? RemoteEndPoint}: {exception.Message}", "WorldClientSession");
        }
        finally
        {
            _playerSaveLock.Release();
        }
    }

    /**
      * Determines whether within movement broadcast range for the connected world client session lifecycle and packet dispatch workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: source, target.
      */
    private static bool IsWithinMovementBroadcastRange(PlayerMovementState source, PlayerMovementState? targetMovement, PlayerLoginRecord targetPlayer)
    {
        float targetX = targetMovement?.PositionX ?? targetPlayer.PositionX;
        float targetY = targetMovement?.PositionY ?? targetPlayer.PositionY;
        float targetZ = targetMovement?.PositionZ ?? targetPlayer.PositionZ;

        float deltaX = source.PositionX - targetX;
        float deltaY = source.PositionY - targetY;
        float deltaZ = source.PositionZ - targetZ;
        float distanceSquared = (deltaX * deltaX) + (deltaY * deltaY) + (deltaZ * deltaZ);
        return distanceSquared <= MaximumMovementBroadcastDistanceSquared;
    }

    /**
      * Performs the saturating seconds operation for the connected world client session lifecycle and packet dispatch workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: elapsed.
      */
    private static uint SaturatingSeconds(TimeSpan elapsed)
    {
        if (elapsed <= TimeSpan.Zero)
        {
            return 0;
        }

        return elapsed.TotalSeconds >= uint.MaxValue ? uint.MaxValue : (uint)elapsed.TotalSeconds;
    }

    /**
      * Adds clamped for the connected world client session lifecycle and packet dispatch workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: value, addition.
      */
    private static uint AddClamped(uint value, uint addition)
    {
        ulong result = (ulong)value + addition;
        return result > uint.MaxValue ? uint.MaxValue : (uint)result;
    }

    /**
      * Handles swapping two top-level inventory/equipment/bank slots.
      */
    private async Task HandleSwapInvItemAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        if (packet.Payload.Length < 2)
        {
            await SendInventoryFailureAsync(InventoryChangeFailureItemNotFound, 0, 0, cancellationToken);
            return;
        }

        WorldPacketReader reader = new(packet.Payload);
        byte firstSlot = reader.ReadUInt8();
        byte secondSlot = reader.ReadUInt8();

        await SwapInventoryLocationsAsync(
            new InventoryClientPosition(ClientBackpackBag, firstSlot),
            new InventoryClientPosition(ClientBackpackBag, secondSlot),
            cancellationToken);
    }

    /**
      * Handles swapping items between player inventory, equipped bags, bank, and bank bags.
      */
    private async Task HandleSwapItemAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        if (packet.Payload.Length < 4)
        {
            await SendInventoryFailureAsync(InventoryChangeFailureItemNotFound, 0, 0, cancellationToken);
            return;
        }

        WorldPacketReader reader = new(packet.Payload);
        byte firstBag = reader.ReadUInt8();
        byte firstSlot = reader.ReadUInt8();
        byte secondBag = reader.ReadUInt8();
        byte secondSlot = reader.ReadUInt8();

        await SwapInventoryLocationsAsync(
            new InventoryClientPosition(firstBag, firstSlot),
            new InventoryClientPosition(secondBag, secondSlot),
            cancellationToken);
    }

    /**
      * Handles right-click auto-equip and right-click unequip.
      */
    private async Task HandleAutoEquipItemAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        if (packet.Payload.Length < 2)
        {
            await SendInventoryFailureAsync(InventoryChangeFailureItemNotFound, 0, 0, cancellationToken);
            return;
        }

        WorldPacketReader reader = new(packet.Payload);
        byte sourceBag = reader.ReadUInt8();
        byte sourceSlot = reader.ReadUInt8();

        PlayerLoginRecord player = RequireCurrentPlayer();
        IReadOnlyList<PlayerInventoryItem> inventory = player.Inventory;
        if (!TryResolveClientInventoryLocation(new InventoryClientPosition(sourceBag, sourceSlot), inventory, out InventoryStorageLocation sourceLocation))
        {
            await SendInventoryFailureAsync(InventoryChangeFailureItemNotFound, 0, 0, cancellationToken);
            return;
        }

        PlayerInventoryItem? sourceItem = FindItemAtLocation(inventory, sourceLocation);
        if (sourceItem is null)
        {
            await SendInventoryFailureAsync(InventoryChangeFailureItemNotFound, 0, 0, cancellationToken);
            return;
        }

        InventoryStorageLocation destinationLocation;
        if (sourceItem.BagGuid == 0 && sourceItem.Slot < 19)
        {
            if (!TryFindFirstFreeBackpackLocation(inventory, out destinationLocation))
            {
                await SendInventoryFailureAsync(InventoryChangeFailureBagFull, CharacterGuid.ToItemGuid(sourceItem.ItemGuid), 0, cancellationToken);
                return;
            }
        }
        else if (!TryResolveAutoEquipLocation(sourceItem, inventory, out destinationLocation))
        {
            await SendInventoryFailureAsync(InventoryChangeFailureItemDoesntGoToSlot, CharacterGuid.ToItemGuid(sourceItem.ItemGuid), 0, cancellationToken);
            return;
        }

        await MoveOrSwapItemAsync(sourceItem, sourceLocation, destinationLocation, cancellationToken);
    }

    /**
      * Handles client requests that equip a known item GUID into an explicit equipment slot.
      */
    private async Task HandleAutoEquipItemSlotAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        if (packet.Payload.Length < 9)
        {
            await SendInventoryFailureAsync(InventoryChangeFailureItemNotFound, 0, 0, cancellationToken);
            return;
        }

        WorldPacketReader reader = new(packet.Payload);
        byte destinationSlot = reader.ReadUInt8();
        ulong itemClientGuid = reader.ReadUInt64();
        uint itemGuid = CharacterGuid.FromClientGuid(itemClientGuid);

        PlayerLoginRecord player = RequireCurrentPlayer();
        PlayerInventoryItem? sourceItem = player.Inventory.FirstOrDefault(item => item.ItemGuid == itemGuid);
        if (sourceItem is null)
        {
            WorldPacketReader alternateReader = new(packet.Payload);
            ulong alternateItemClientGuid = alternateReader.ReadUInt64();
            byte alternateDestinationSlot = alternateReader.ReadUInt8();
            uint alternateItemGuid = CharacterGuid.FromClientGuid(alternateItemClientGuid);
            PlayerInventoryItem? alternateSourceItem = player.Inventory.FirstOrDefault(item => item.ItemGuid == alternateItemGuid);
            if (alternateSourceItem is null)
            {
                await SendInventoryFailureAsync(InventoryChangeFailureItemNotFound, itemClientGuid, 0, cancellationToken);
                return;
            }

            sourceItem = alternateSourceItem;
            itemClientGuid = alternateItemClientGuid;
            destinationSlot = alternateDestinationSlot;
        }

        InventoryStorageLocation sourceLocation = new(sourceItem.BagGuid, sourceItem.Slot);
        InventoryStorageLocation destinationLocation = new(0, destinationSlot);
        if (!CanPlaceItemAtLocation(sourceItem, destinationLocation, player.Inventory))
        {
            await SendInventoryFailureAsync(InventoryChangeFailureItemDoesntGoToSlot, itemClientGuid, 0, cancellationToken);
            return;
        }

        await MoveOrSwapItemAsync(sourceItem, sourceLocation, destinationLocation, cancellationToken);
    }

    /**
      * Handles client auto-store requests by moving the source item to the first free backpack slot.
      */
    private async Task HandleAutoStoreBagItemAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        if (packet.Payload.Length < 2)
        {
            await SendInventoryFailureAsync(InventoryChangeFailureItemNotFound, 0, 0, cancellationToken);
            return;
        }

        WorldPacketReader reader = new(packet.Payload);
        byte sourceBag = reader.ReadUInt8();
        byte sourceSlot = reader.ReadUInt8();

        PlayerLoginRecord player = RequireCurrentPlayer();
        IReadOnlyList<PlayerInventoryItem> inventory = player.Inventory;
        if (!TryResolveClientInventoryLocation(new InventoryClientPosition(sourceBag, sourceSlot), inventory, out InventoryStorageLocation sourceLocation))
        {
            await SendInventoryFailureAsync(InventoryChangeFailureItemNotFound, 0, 0, cancellationToken);
            return;
        }

        PlayerInventoryItem? sourceItem = FindItemAtLocation(inventory, sourceLocation);
        if (sourceItem is null)
        {
            await SendInventoryFailureAsync(InventoryChangeFailureItemNotFound, 0, 0, cancellationToken);
            return;
        }

        if (!TryFindFirstFreeBackpackLocation(inventory, out InventoryStorageLocation destinationLocation))
        {
            await SendInventoryFailureAsync(InventoryChangeFailureBagFull, CharacterGuid.ToItemGuid(sourceItem.ItemGuid), 0, cancellationToken);
            return;
        }

        await MoveOrSwapItemAsync(sourceItem, sourceLocation, destinationLocation, cancellationToken);
    }

    /**
      * Handles splitting part of a stack into an empty slot or merging the split count into a compatible destination stack.
      */
    private async Task HandleSplitItemAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        if (packet.Payload.Length < 5)
        {
            await SendInventoryFailureAsync(InventoryChangeFailureItemNotFound, 0, 0, cancellationToken);
            return;
        }

        WorldPacketReader reader = new(packet.Payload);
        byte sourceBag = reader.ReadUInt8();
        byte sourceSlot = reader.ReadUInt8();
        byte destinationBag = reader.ReadUInt8();
        byte destinationSlot = reader.ReadUInt8();
        byte splitCount = reader.ReadUInt8();

        if (splitCount == 0)
        {
            return;
        }

        PlayerLoginRecord player = RequireCurrentPlayer();
        IReadOnlyList<PlayerInventoryItem> inventory = player.Inventory;
        if (!TryResolveClientInventoryLocation(new InventoryClientPosition(sourceBag, sourceSlot), inventory, out InventoryStorageLocation sourceLocation))
        {
            await SendInventoryFailureAsync(InventoryChangeFailureItemNotFound, 0, 0, cancellationToken);
            return;
        }

        PlayerInventoryItem? sourceItem = FindItemAtLocation(inventory, sourceLocation);
        if (sourceItem is null || sourceItem.StackCount <= splitCount)
        {
            await SendInventoryFailureAsync(InventoryChangeFailureItemNotFound, 0, 0, cancellationToken);
            return;
        }

        InventoryStorageLocation destinationLocation;
        if (!TryResolveClientInventoryLocation(new InventoryClientPosition(destinationBag, destinationSlot), inventory, out destinationLocation))
        {
            if (destinationBag == ClientBackpackBag && destinationSlot == ClientBackpackBag && TryFindFirstFreeBackpackLocation(inventory, out destinationLocation))
            {
                // Client requested an automatic destination. Use the first free backpack/bag slot.
            }
            else
            {
                await SendInventoryFailureAsync(InventoryChangeFailureItemDoesntGoToSlot, CharacterGuid.ToItemGuid(sourceItem.ItemGuid), 0, cancellationToken);
                return;
            }
        }

        if (sourceLocation.Equals(destinationLocation))
        {
            return;
        }

        PlayerInventoryItem? destinationItem = FindItemAtLocation(inventory, destinationLocation);
        if (destinationItem is null && !CanPlaceItemAtLocation(sourceItem, destinationLocation, inventory))
        {
            await SendInventoryFailureAsync(InventoryChangeFailureItemDoesntGoToSlot, CharacterGuid.ToItemGuid(sourceItem.ItemGuid), 0, cancellationToken);
            return;
        }

        if (destinationItem is not null && destinationItem.TemplateEntry != sourceItem.TemplateEntry)
        {
            await SendInventoryFailureAsync(InventoryChangeFailureItemDoesntGoToSlot, CharacterGuid.ToItemGuid(sourceItem.ItemGuid), CharacterGuid.ToItemGuid(destinationItem.ItemGuid), cancellationToken);
            return;
        }

        await ApplyInventoryStackSplitAsync(sourceItem, destinationLocation, splitCount, cancellationToken);
    }

    private async Task SwapInventoryLocationsAsync(InventoryClientPosition firstClientPosition, InventoryClientPosition secondClientPosition, CancellationToken cancellationToken)
    {
        PlayerLoginRecord player = RequireCurrentPlayer();
        IReadOnlyList<PlayerInventoryItem> inventory = player.Inventory;

        if (!TryResolveClientInventoryLocation(firstClientPosition, inventory, out InventoryStorageLocation firstLocation) ||
            !TryResolveClientInventoryLocation(secondClientPosition, inventory, out InventoryStorageLocation secondLocation))
        {
            await SendInventoryFailureAsync(InventoryChangeFailureItemDoesntGoToSlot, 0, 0, cancellationToken);
            return;
        }

        if (firstLocation.Equals(secondLocation))
        {
            return;
        }

        PlayerInventoryItem? firstItem = FindItemAtLocation(inventory, firstLocation);
        PlayerInventoryItem? secondItem = FindItemAtLocation(inventory, secondLocation);
        if (firstItem is null && secondItem is null)
        {
            await SendInventoryFailureAsync(InventoryChangeFailureItemNotFound, 0, 0, cancellationToken);
            return;
        }

        if (firstItem is not null && !CanPlaceItemAtLocation(firstItem, secondLocation, inventory))
        {
            await SendInventoryFailureAsync(InventoryChangeFailureItemDoesntGoToSlot, CharacterGuid.ToItemGuid(firstItem.ItemGuid), secondItem is null ? 0 : CharacterGuid.ToItemGuid(secondItem.ItemGuid), cancellationToken);
            return;
        }

        if (secondItem is not null && !CanPlaceItemAtLocation(secondItem, firstLocation, inventory))
        {
            await SendInventoryFailureAsync(InventoryChangeFailureItemDoesntGoToSlot, CharacterGuid.ToItemGuid(secondItem.ItemGuid), firstItem is null ? 0 : CharacterGuid.ToItemGuid(firstItem.ItemGuid), cancellationToken);
            return;
        }

        List<PlayerInventoryPlacementUpdate> placements = [];
        if (firstItem is not null)
        {
            placements.Add(new PlayerInventoryPlacementUpdate(firstItem.ItemGuid, secondLocation.BagGuid, secondLocation.Slot));
        }

        if (secondItem is not null)
        {
            placements.Add(new PlayerInventoryPlacementUpdate(secondItem.ItemGuid, firstLocation.BagGuid, firstLocation.Slot));
        }

        await ApplyInventoryPlacementsAsync(placements, cancellationToken);
    }

    private async Task MoveOrSwapItemAsync(PlayerInventoryItem sourceItem, InventoryStorageLocation sourceLocation, InventoryStorageLocation destinationLocation, CancellationToken cancellationToken)
    {
        if (sourceLocation.Equals(destinationLocation))
        {
            return;
        }

        PlayerLoginRecord player = RequireCurrentPlayer();
        IReadOnlyList<PlayerInventoryItem> inventory = player.Inventory;
        PlayerInventoryItem? destinationItem = FindItemAtLocation(inventory, destinationLocation);

        if (!CanPlaceItemAtLocation(sourceItem, destinationLocation, inventory))
        {
            await SendInventoryFailureAsync(InventoryChangeFailureItemDoesntGoToSlot, CharacterGuid.ToItemGuid(sourceItem.ItemGuid), destinationItem is null ? 0 : CharacterGuid.ToItemGuid(destinationItem.ItemGuid), cancellationToken);
            return;
        }

        if (destinationItem is not null && !CanPlaceItemAtLocation(destinationItem, sourceLocation, inventory))
        {
            await SendInventoryFailureAsync(InventoryChangeFailureItemDoesntGoToSlot, CharacterGuid.ToItemGuid(destinationItem.ItemGuid), CharacterGuid.ToItemGuid(sourceItem.ItemGuid), cancellationToken);
            return;
        }

        List<PlayerInventoryPlacementUpdate> placements =
        [
            new PlayerInventoryPlacementUpdate(sourceItem.ItemGuid, destinationLocation.BagGuid, destinationLocation.Slot),
        ];

        if (destinationItem is not null)
        {
            placements.Add(new PlayerInventoryPlacementUpdate(destinationItem.ItemGuid, sourceLocation.BagGuid, sourceLocation.Slot));
        }

        await ApplyInventoryPlacementsAsync(placements, cancellationToken);
    }

    private async Task ApplyInventoryPlacementsAsync(IReadOnlyList<PlayerInventoryPlacementUpdate> placements, CancellationToken cancellationToken)
    {
        PlayerLoginRecord player = RequireCurrentPlayer();
        IReadOnlyList<PlayerInventoryItem> refreshedInventory = await _characterRepository.UpdateInventoryPlacementsAsync(player.Guid, placements, cancellationToken);
        if (refreshedInventory.Count == 0)
        {
            await SendInventoryFailureAsync(InventoryChangeFailureItemNotFound, 0, 0, cancellationToken);
            return;
        }

        PlayerLoginRecord updatedPlayer = player with { Inventory = refreshedInventory };
        CurrentPlayer = updatedPlayer;
        _playerStateDirty = true;
        await SendAsync(WorldOpcode.SMSG_UPDATE_OBJECT, WorldPacketBuilders.BuildInventoryStateUpdate(updatedPlayer), _crypt, cancellationToken);
    }

    private async Task ApplyInventoryStackSplitAsync(PlayerInventoryItem sourceItem, InventoryStorageLocation destinationLocation, uint splitCount, CancellationToken cancellationToken)
    {
        PlayerLoginRecord player = RequireCurrentPlayer();
        HashSet<uint> knownItemGuids = player.Inventory.Select(item => item.ItemGuid).ToHashSet();
        IReadOnlyList<PlayerInventoryItem> refreshedInventory = await _characterRepository.SplitInventoryStackAsync(
            player.Guid,
            sourceItem.ItemGuid,
            destinationLocation.BagGuid,
            destinationLocation.Slot,
            splitCount,
            cancellationToken);

        if (refreshedInventory.Count == 0)
        {
            await SendInventoryFailureAsync(InventoryChangeFailureItemDoesntGoToSlot, CharacterGuid.ToItemGuid(sourceItem.ItemGuid), 0, cancellationToken);
            return;
        }

        HashSet<uint> createdItemGuids = refreshedInventory
            .Where(item => !knownItemGuids.Contains(item.ItemGuid))
            .Select(item => item.ItemGuid)
            .ToHashSet();

        PlayerLoginRecord updatedPlayer = player with { Inventory = refreshedInventory };
        CurrentPlayer = updatedPlayer;
        _playerStateDirty = true;
        await SendAsync(WorldOpcode.SMSG_UPDATE_OBJECT, WorldPacketBuilders.BuildInventoryStateUpdate(updatedPlayer, createdItemGuids), _crypt, cancellationToken);
    }

    private async Task SendInventoryFailureAsync(byte failureCode, ulong itemGuid, ulong itemGuid2, CancellationToken cancellationToken)
    {
        await SendAsync(WorldOpcode.SMSG_INVENTORY_CHANGE_FAILURE, WorldPacketBuilders.BuildInventoryChangeFailure(failureCode, itemGuid, itemGuid2), _crypt, cancellationToken);
    }

    private static bool TryResolveClientInventoryLocation(InventoryClientPosition position, IReadOnlyList<PlayerInventoryItem> inventory, out InventoryStorageLocation location)
    {
        if (position.Bag == ClientBackpackBag)
        {
            location = new InventoryStorageLocation(0, position.Slot);
            return IsValidTopLevelSlot(position.Slot);
        }

        if (position.Bag == 0)
        {
            byte normalizedBackpackSlot = position.Slot < 16
                ? (byte)(23 + position.Slot)
                : position.Slot;
            location = new InventoryStorageLocation(0, normalizedBackpackSlot);
            return IsValidTopLevelSlot(normalizedBackpackSlot);
        }

        byte containerSlot = position.Bag is >= 1 and <= 4
            ? (byte)(18 + position.Bag)
            : position.Bag;

        PlayerInventoryItem? bagItem = inventory.FirstOrDefault(item => item.BagGuid == 0 && item.Slot == containerSlot && item.IsContainer);
        if (bagItem is null || position.Slot >= bagItem.ContainerSlots)
        {
            location = default;
            return false;
        }

        location = new InventoryStorageLocation(bagItem.ItemGuid, position.Slot);
        return true;
    }

    private static PlayerInventoryItem? FindItemAtLocation(IReadOnlyList<PlayerInventoryItem> inventory, InventoryStorageLocation location)
    {
        return inventory.FirstOrDefault(item => item.BagGuid == location.BagGuid && item.Slot == location.Slot);
    }

    private static bool CanPlaceItemAtLocation(PlayerInventoryItem item, InventoryStorageLocation location, IReadOnlyList<PlayerInventoryItem> inventory)
    {
        if (location.BagGuid != 0)
        {
            PlayerInventoryItem? bagItem = inventory.FirstOrDefault(candidate => candidate.ItemGuid == location.BagGuid && candidate.IsContainer);
            return bagItem is not null && location.Slot < bagItem.ContainerSlots && !item.IsContainer;
        }

        byte slot = location.Slot;
        if (slot < 19)
        {
            return IsItemAllowedInEquipmentSlot(item, slot);
        }

        if (slot is >= 19 and < 23)
        {
            return item.IsContainer;
        }

        if (slot is >= 23 and < 39)
        {
            return true;
        }

        if (slot is >= 39 and < 63)
        {
            return true;
        }

        if (slot is >= 63 and < 69)
        {
            return item.IsContainer;
        }

        if (slot is >= 81 and < 113)
        {
            return true;
        }

        return false;
    }

    private static bool TryResolveAutoEquipLocation(PlayerInventoryItem item, IReadOnlyList<PlayerInventoryItem> inventory, out InventoryStorageLocation location)
    {
        if (item.IsContainer)
        {
            for (byte bagSlot = 19; bagSlot < 23; bagSlot++)
            {
                InventoryStorageLocation candidate = new(0, bagSlot);
                if (FindItemAtLocation(inventory, candidate) is null)
                {
                    location = candidate;
                    return true;
                }
            }

            location = new InventoryStorageLocation(0, 19);
            return true;
        }

        byte[] allowedSlots = ResolveAllowedEquipmentSlots(item);
        foreach (byte slot in allowedSlots)
        {
            if (FindItemAtLocation(inventory, new InventoryStorageLocation(0, slot)) is null)
            {
                location = new InventoryStorageLocation(0, slot);
                return true;
            }
        }

        if (allowedSlots.Length > 0)
        {
            location = new InventoryStorageLocation(0, allowedSlots[0]);
            return true;
        }

        location = default;
        return false;
    }

    private static bool TryFindFirstFreeBackpackLocation(IReadOnlyList<PlayerInventoryItem> inventory, out InventoryStorageLocation location)
    {
        for (byte slot = 23; slot < 39; slot++)
        {
            InventoryStorageLocation candidate = new(0, slot);
            if (FindItemAtLocation(inventory, candidate) is null)
            {
                location = candidate;
                return true;
            }
        }

        foreach (PlayerInventoryItem bagItem in inventory.Where(item => item.BagGuid == 0 && item.Slot is >= 19 and < 23 && item.IsContainer).OrderBy(item => item.Slot))
        {
            for (byte slot = 0; slot < bagItem.ContainerSlots; slot++)
            {
                InventoryStorageLocation candidate = new(bagItem.ItemGuid, slot);
                if (FindItemAtLocation(inventory, candidate) is null)
                {
                    location = candidate;
                    return true;
                }
            }
        }

        location = default;
        return false;
    }

    private static bool IsValidTopLevelSlot(byte slot)
    {
        return slot < 69 || slot is >= 81 and < 113;
    }

    private static bool IsItemAllowedInEquipmentSlot(PlayerInventoryItem item, byte slot)
    {
        return ResolveAllowedEquipmentSlots(item).Contains(slot);
    }

    private static byte[] ResolveAllowedEquipmentSlots(PlayerInventoryItem item)
    {
        return item.InventoryType switch
        {
            1 => [0],
            2 => [1],
            3 => [2],
            4 => [3],
            5 => [4],
            6 => [5],
            7 => [6],
            8 => [7],
            9 => [8],
            10 => [9],
            11 => [10, 11],
            12 => [12, 13],
            13 => [15],
            14 => [16],
            15 => [17],
            16 => [14],
            17 => [15],
            19 => [18],
            20 => [4],
            21 => [15],
            22 => [16],
            23 => [16],
            25 => [17],
            26 => [17],
            28 => [17],
            _ => [],
        };
    }

    /**
      * Handles the handle item query single event for the connected world client session lifecycle and packet dispatch workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: packet, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task HandleItemQuerySingleAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        WorldPacketReader reader = new(packet.Payload);
        uint itemEntry = reader.ReadUInt32();

        byte[] payload = _itemSystem.TryGetItemTemplate(itemEntry, out ItemTemplateRecord itemTemplate)
            ? WorldPacketBuilders.BuildItemQuerySingleResponse(itemTemplate)
            : WorldPacketBuilders.BuildItemQuerySingleNotFound(itemEntry);

        await SendAsync(WorldOpcode.SMSG_ITEM_QUERY_SINGLE_RESPONSE, payload, _crypt, cancellationToken);
    }

    /**
      * Handles the handle item name query event for the connected world client session lifecycle and packet dispatch workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: packet, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task HandleItemNameQueryAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        WorldPacketReader reader = new(packet.Payload);
        uint itemEntry = reader.ReadUInt32();

        byte[] payload = _itemSystem.TryGetItemTemplate(itemEntry, out ItemTemplateRecord itemTemplate)
            ? WorldPacketBuilders.BuildItemNameQueryResponse(itemTemplate)
            : WorldPacketBuilders.BuildItemNameQueryNotFound(itemEntry);

        await SendAsync(WorldOpcode.SMSG_ITEM_NAME_QUERY_RESPONSE, payload, _crypt, cancellationToken);
    }

    /**
      * Handles the handle name query event for the connected world client session lifecycle and packet dispatch workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: packet, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task HandleNameQueryAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        uint characterGuid = CharacterGuid.FromClientGuid(ReadClientGuid(packet.Payload));
        if (characterGuid == 0)
        {
            return;
        }

        CharacterNameQueryResult? character = CurrentPlayer is not null && CurrentPlayer.Guid == characterGuid
            ? new CharacterNameQueryResult(CurrentPlayer.Guid, CurrentPlayer.Name, CurrentPlayer.Race, CurrentPlayer.Gender, CurrentPlayer.Class)
            : await _characterRepository.GetCharacterNameQueryAsync(characterGuid, cancellationToken);

        if (character is null)
        {
            return;
        }

        await SendAsync(WorldOpcode.SMSG_NAME_QUERY_RESPONSE, WorldPacketBuilders.BuildNameQueryResponse(character), _crypt, cancellationToken);
    }

    /**
      * Handles the handle who event for the connected world client session lifecycle and packet dispatch workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task HandleWhoAsync(CancellationToken cancellationToken)
    {
        PlayerLoginRecord player = RequireCurrentPlayer();
        IReadOnlyList<PlayerLoginRecord> players = _playerSessionRegistry.SnapshotSessions()
            .Select(session => session.CurrentPlayer)
            .Where(other => other is not null && other.Faction == player.Faction)
            .Cast<PlayerLoginRecord>()
            .ToArray();

        await SendAsync(WorldOpcode.SMSG_WHO, WorldPacketBuilders.BuildWhoResponse(players), _crypt, cancellationToken);
    }

    /**
      * Handles the handle logout request event for the connected world client session lifecycle and packet dispatch workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task HandleLogoutRequestAsync(CancellationToken cancellationToken)
    {
        if (CurrentPlayer is null)
        {
            await SendAsync(WorldOpcode.SMSG_LOGOUT_RESPONSE, WorldPacketBuilders.BuildLogoutResponse(), _crypt, cancellationToken);
            await SendAsync(WorldOpcode.SMSG_LOGOUT_COMPLETE, WorldPacketBuilders.BuildLogoutComplete(), _crypt, cancellationToken);
            return;
        }

        await SendAsync(WorldOpcode.SMSG_LOGOUT_RESPONSE, WorldPacketBuilders.BuildLogoutResponse(), _crypt, cancellationToken);
        await CleanupCurrentPlayerAsync(cancellationToken);
        await SendAsync(WorldOpcode.SMSG_LOGOUT_COMPLETE, WorldPacketBuilders.BuildLogoutComplete(), _crypt, cancellationToken);
    }

    /**
      * Handles the handle played time event for the connected world client session lifecycle and packet dispatch workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task HandlePlayedTimeAsync(CancellationToken cancellationToken)
    {
        await SendAsync(WorldOpcode.SMSG_PLAYED_TIME, WorldPacketBuilders.BuildPlayedTime(RequireCurrentPlayer()), _crypt, cancellationToken);
    }

    /**
      * Handles the handle message chat event for the connected world client session lifecycle and packet dispatch workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: packet, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task HandleMessageChatAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        if (CurrentPlayer is null)
        {
            Logger.Write(LogType.WARNING, $"Ignoring CMSG_MESSAGECHAT from {RemoteEndPoint}: player is not in world.", "WorldClientSession");
            return;
        }

        ChatIncomingMessage message;
        try
        {
            message = _chatSystem.NormalizeIncomingMessage(CurrentPlayer, ReadChatMessage(packet.Payload));
        }
        catch (InvalidDataException exception)
        {
            Logger.Write(LogType.WARNING, $"Ignoring malformed chat packet from '{CurrentPlayer.Name}': {exception.Message}", "WorldClientSession");
            return;
        }

        if (string.IsNullOrWhiteSpace(message.Text))
        {
            return;
        }

        if (ChatSystem.IsCommandMessage(message))
        {
            string response = await _commandService.ExecuteAsync(this, message.Text, cancellationToken);
            await SendSystemMessageAsync(response, cancellationToken);
            return;
        }

        IReadOnlyList<IChatSession> recipients = _chatSystem.GetRecipients(this, message, _playerSessionRegistry.SnapshotSessions().Cast<IChatSession>());
        string channelName = message.Type == ChatMessageType.Channel ? _chatSystem.ResolveChannelName(CurrentPlayer, message.Target) : string.Empty;
        uint channelRank = message.Type == ChatMessageType.Channel ? _chatSystem.ResolveChannelPlayerRank(CurrentPlayer) : 0;
        byte[] payload = WorldPacketBuilders.BuildChatMessage(
            message.Type,
            message.Language,
            CurrentPlayer.ClientGuid,
            CurrentPlayer.Name,
            message.Text,
            channelName,
            0,
            channelRank);

        WorldClientSession[] worldRecipients = recipients.OfType<WorldClientSession>().ToArray();
        foreach (WorldClientSession recipient in worldRecipients)
        {
            await recipient.SendAsync(WorldOpcode.SMSG_MESSAGECHAT, payload, recipient._crypt, cancellationToken);
        }

        if (message.Type == ChatMessageType.Whisper && worldRecipients.Length > 0)
        {
            PlayerLoginRecord whisperTarget = worldRecipients[0].RequireCurrentPlayer();
            byte[] informPayload = WorldPacketBuilders.BuildChatMessage(
                ChatMessageType.WhisperInform,
                message.Language,
                whisperTarget.ClientGuid,
                whisperTarget.Name,
                message.Text);

            await SendAsync(WorldOpcode.SMSG_MESSAGECHAT, informPayload, _crypt, cancellationToken);
        }

        Logger.Write(LogType.NETWORK, $"Relayed {message.Type} chat from '{CurrentPlayer.Name}' to {worldRecipients.Length} faction-scoped recipient(s).", "WorldClientSession");
    }

    /**
      * Sends a throttled in-game system message when the world session is alive but the map service cannot receive player packets.
      */
    private async Task NotifyMapServiceFailureAsync(string message, CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (now - _lastMapServiceFailureNotificationUtc < MapServiceFailureNotificationCooldown)
        {
            return;
        }

        _lastMapServiceFailureNotificationUtc = now;

        try
        {
            await SendSystemMessageAsync(message, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Logger.Write(LogType.TRACE, $"Unable to send map-service failure notification to {RemoteEndPoint}: {exception.Message}", "WorldClientSession");
        }
    }

    /**
      * Sends send system message data to the connected session or internal peer.
      * The send path keeps packet construction and delivery together so opcode handling remains easy to trace during protocol debugging.
      * Inputs used by this operation: message, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task SendSystemMessageAsync(string message, CancellationToken cancellationToken)
    {
        PlayerLoginRecord? player = CurrentPlayer;
        ulong senderGuid = player?.ClientGuid ?? 0;
        string senderName = player?.Name ?? "Server";

        foreach (string line in SplitSystemMessageLines(message))
        {
            byte[] payload = WorldPacketBuilders.BuildChatMessage(ChatMessageType.System, ChatLanguage.Universal, senderGuid, senderName, line);
            await SendAsync(WorldOpcode.SMSG_MESSAGECHAT, payload, _crypt, cancellationToken);
        }
    }

    /**
      * Splits multiline command output into short chat-frame-safe system messages.
      */
    private static IEnumerable<string> SplitSystemMessageLines(string message)
    {
        string normalized = string.IsNullOrWhiteSpace(message)
            ? string.Empty
            : message.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

        foreach (string rawLine in normalized.Split('\n', StringSplitOptions.None))
        {
            string line = rawLine.TrimEnd();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            foreach (string chunk in SplitSystemMessageLine(line))
            {
                yield return chunk;
            }
        }
    }

    /**
      * Wraps long generated system-chat lines without splitting words when possible.
      */
    private static IEnumerable<string> SplitSystemMessageLine(string line)
    {
        string remaining = line;
        while (remaining.Length > SystemChatLineLength)
        {
            int splitIndex = remaining.LastIndexOf(' ', SystemChatLineLength);
            if (splitIndex <= 0)
            {
                splitIndex = SystemChatLineLength;
            }

            yield return remaining[..splitIndex].TrimEnd();
            remaining = remaining[splitIndex..].TrimStart();
        }

        if (!string.IsNullOrWhiteSpace(remaining))
        {
            yield return remaining;
        }
    }

    /**
      * Applies the join default chat channels state transition to the current runtime session.
      * State changes are routed through one method so logging, validation, and side effects stay aligned with the server lifecycle.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task JoinDefaultChatChannelsAsync(CancellationToken cancellationToken)
    {
        if (CurrentPlayer is null)
        {
            return;
        }

        foreach (string channelName in _chatSystem.GetDefaultChannelNames(CurrentPlayer))
        {
            await JoinChatChannelAsync(channelName, cancellationToken);
        }
    }

    /**
      * Handles the handle join channel event for the connected world client session lifecycle and packet dispatch workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: packet, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task HandleJoinChannelAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        if (CurrentPlayer is null)
        {
            return;
        }

        WorldPacketReader reader = new(packet.Payload);
        string channelName = reader.Remaining > 0 ? reader.ReadCString() : string.Empty;
        await JoinChatChannelAsync(channelName, cancellationToken);
    }

    /**
      * Applies the join chat channel state transition to the current runtime session.
      * State changes are routed through one method so logging, validation, and side effects stay aligned with the server lifecycle.
      * Inputs used by this operation: channelName, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task JoinChatChannelAsync(string channelName, CancellationToken cancellationToken)
    {
        if (CurrentPlayer is null)
        {
            return;
        }

        string normalized = _chatSystem.ResolveChannelName(CurrentPlayer, channelName);
        uint channelFlags = _chatSystem.ResolveChannelFlags(CurrentPlayer, normalized);
        _chatSystem.JoinChannel(this, normalized);
        await SendAsync(WorldOpcode.SMSG_CHANNEL_NOTIFY, WorldPacketBuilders.BuildChannelNotify(0x02, normalized, channelFlags), _crypt, cancellationToken);
    }

    /**
      * Handles the handle leave channel event for the connected world client session lifecycle and packet dispatch workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: packet, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task HandleLeaveChannelAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        if (CurrentPlayer is null)
        {
            return;
        }

        WorldPacketReader reader = new(packet.Payload);
        string channelName = reader.Remaining > 0 ? reader.ReadCString() : string.Empty;
        string normalized = _chatSystem.ResolveChannelName(CurrentPlayer, channelName);
        _chatSystem.LeaveChannel(this, normalized);
        await SendAsync(WorldOpcode.SMSG_CHANNEL_NOTIFY, WorldPacketBuilders.BuildChannelNotify(0x03, normalized), _crypt, cancellationToken);
    }

    /**
      * Handles the handle channel list event for the connected world client session lifecycle and packet dispatch workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: packet, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task HandleChannelListAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        if (CurrentPlayer is null)
        {
            return;
        }

        WorldPacketReader reader = new(packet.Payload);
        string channelName = reader.Remaining > 0 ? reader.ReadCString() : string.Empty;
        string normalized = _chatSystem.ResolveChannelName(CurrentPlayer, channelName);
        PlayerFaction faction = CurrentPlayer.Faction;
        IReadOnlyList<PlayerLoginRecord> members = _playerSessionRegistry.SnapshotSessions()
            .Where(session => session.CurrentPlayer?.Faction == faction && session.IsInChatChannel(normalized))
            .Select(session => session.RequireCurrentPlayer())
            .ToArray();

        uint channelFlags = _chatSystem.ResolveChannelFlags(CurrentPlayer, normalized);
        await SendAsync(WorldOpcode.SMSG_CHANNEL_LIST, WorldPacketBuilders.BuildChannelList(normalized, members, channelFlags), _crypt, cancellationToken);
    }

    /**
      * Handles the handle request account data event for the connected world client session lifecycle and packet dispatch workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: packet, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task HandleRequestAccountDataAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        uint accountDataType = 0;
        if (packet.Payload.Length >= sizeof(uint))
        {
            WorldPacketReader reader = new(packet.Payload);
            accountDataType = reader.ReadUInt32();
        }

        await SendAsync(WorldOpcode.SMSG_UPDATE_ACCOUNT_DATA, WorldPacketBuilders.BuildUpdateAccountData(accountDataType), _crypt, cancellationToken);
    }

    /**
      * Handles the handle character create event for the connected world client session lifecycle and packet dispatch workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: packet, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task HandleCharacterCreateAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        WorldAccountSessionRecord account = RequireAccount();
        CharacterCreateRequest request = ReadCharacterCreateRequest(packet.Payload);
        CharacterCreateResult result = await _characterService.CreateCharacterAsync(account.Id, request, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_CHAR_CREATE, WorldPacketBuilders.BuildCharacterCreate(result), _crypt, cancellationToken);

        if (result == CharacterCreateResult.Success)
        {
            await _characterCountChangedAsync(cancellationToken);
        }

        Logger.Write(
            result == CharacterCreateResult.Success ? LogType.SUCCESS : LogType.WARNING,
            $"Character create result for account '{account.Username}', name='{request.Name}': {result}.",
            "WorldClientSession");
    }

    /**
      * Handles the handle character delete event for the connected world client session lifecycle and packet dispatch workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: packet, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task HandleCharacterDeleteAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        WorldAccountSessionRecord account = RequireAccount();
        ulong clientGuid = ReadCharacterDeleteGuid(packet.Payload);
        CharacterDeleteServiceResult result = await _characterService.DeleteCharacterAsync(account.Id, clientGuid, cancellationToken);

        if (result == CharacterDeleteServiceResult.SecurityMismatch)
        {
            return;
        }

        CharacterDeleteResult clientResult = result == CharacterDeleteServiceResult.Success
            ? CharacterDeleteResult.Success
            : CharacterDeleteResult.Failed;

        await SendAsync(WorldOpcode.SMSG_CHAR_DELETE, WorldPacketBuilders.BuildCharacterDelete(clientResult), _crypt, cancellationToken);

        if (result == CharacterDeleteServiceResult.Success)
        {
            await _characterCountChangedAsync(cancellationToken);
        }

        Logger.Write(
            result == CharacterDeleteServiceResult.Success ? LogType.SUCCESS : LogType.WARNING,
            $"Character delete result for account '{account.Username}', guid=0x{clientGuid:X16}: {result}.",
            "WorldClientSession");
    }

    /**
      * Performs the cleanup current player operation for the connected world client session lifecycle and packet dispatch workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task CleanupCurrentPlayerAsync(CancellationToken cancellationToken, bool notifyMapService = true)
    {
        PlayerLoginRecord? player = CurrentPlayer;
        if (player is null)
        {
            return;
        }

        await StopPlayerSaveTimerAsync();
        await SaveCurrentPlayerAsync(force: true, cancellationToken);

        player = CurrentPlayer ?? player;
        string ownerServerName = _currentMapOwnerServerName;
        CurrentPlayer = null;
        CurrentMovement = null;
        _currentMapOwnerServerName = string.Empty;
        ResetMapServiceMovementRoute();
        _chatChannels.Clear();

        if (notifyMapService && !string.IsNullOrWhiteSpace(ownerServerName))
        {
            try
            {
                await _playerLeftWorldAsync(player, ownerServerName, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Logger.Write(LogType.WARNING, $"Failed to notify map service that player '{player.Name}' ({player.Guid}) left world: {exception.Message}", "WorldClientSession");
            }
        }

        _playerSessionRegistry.Unregister(player, this);
        _activePlayerCountChanged(_playerSessionRegistry.ActivePlayerCount);

        try
        {
            await _characterRepository.SetCharacterOnlineAsync(player.Guid, false, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Logger.Write(LogType.FAILED, $"Failed to mark player '{player.Name}' ({player.Guid}) offline: {exception.Message}", "WorldClientSession");
        }
    }

    /**
      * Parses read character delete guid input into the strongly typed server representation.
      * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
      * Inputs used by this operation: payload.
      */
    private static ulong ReadCharacterDeleteGuid(byte[] payload)
    {
        return ReadClientGuid(payload);
    }

    /**
      * Parses read client guid input into the strongly typed server representation.
      * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
      * Inputs used by this operation: payload.
      */
    private static ulong ReadClientGuid(byte[] payload)
    {
        WorldPacketReader reader = new(payload);
        return reader.ReadUInt64();
    }

    /**
      * Parses read character create request input into the strongly typed server representation.
      * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
      * Inputs used by this operation: payload.
      */
    private static CharacterCreateRequest ReadCharacterCreateRequest(byte[] payload)
    {
        WorldPacketReader reader = new(payload);
        string name = reader.ReadCString();
        return new CharacterCreateRequest(
            name,
            reader.ReadUInt8(),
            reader.ReadUInt8(),
            reader.ReadUInt8(),
            reader.ReadUInt8(),
            reader.ReadUInt8(),
            reader.ReadUInt8(),
            reader.ReadUInt8(),
            reader.ReadUInt8(),
            reader.ReadUInt8());
    }

    /**
      * Parses read chat message input into the strongly typed server representation.
      * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
      * Inputs used by this operation: payload.
      */
    private static ChatIncomingMessage ReadChatMessage(byte[] payload)
    {
        WorldPacketReader reader = new(payload);
        ChatMessageType messageType = (ChatMessageType)reader.ReadUInt32();
        ChatLanguage language = (ChatLanguage)reader.ReadUInt32();
        string target = string.Empty;

        if (messageType is ChatMessageType.Whisper or ChatMessageType.Channel)
        {
            target = reader.ReadCString();
        }

        string text = reader.Remaining > 0 ? reader.ReadCString() : string.Empty;
        return new ChatIncomingMessage(messageType, language, target, text);
    }

    /**
      * Resolves the faction for race value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: race.
      */
    private static PlayerFaction ResolveFactionForRace(byte race)
    {
        return race switch
        {
            1 or 3 or 4 or 7 => PlayerFaction.Alliance,
            2 or 5 or 6 or 8 => PlayerFaction.Horde,
            _ => PlayerFaction.Neutral,
        };
    }

    /**
      * Sends send data to the connected session or internal peer.
      * The send path keeps packet construction and delivery together so opcode handling remains easy to trace during protocol debugging.
      * Inputs used by this operation: opcode, payload, crypt, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task SendAsync(WorldOpcode opcode, byte[] payload, WorldHeaderCrypt? crypt, CancellationToken cancellationToken)
    {
        WorldMovementDiagnostics.LogOutgoingPositionPacket(opcode, payload, CurrentPlayer, CurrentMovement, RemoteEndPoint);

        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            await WorldPacketIO.WriteServerPacketAsync(GetStream(), opcode, payload, crypt, cancellationToken);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /**
      * Resolves the stream value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      */
    private NetworkStream GetStream()
    {
        return _stream ?? throw new InvalidOperationException("World client stream is not initialized.");
    }

    /**
      * Requires account for the connected world client session lifecycle and packet dispatch workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      */
    private WorldAccountSessionRecord RequireAccount()
    {
        return _account ?? throw new InvalidOperationException("World client is not authenticated.");
    }

    /**
      * Stops the dispose workflow and releases owned runtime resources in a controlled order.
      * Shutdown logic is centralized to avoid dangling connections, incomplete saves, or partially registered services.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await CleanupCurrentPlayerAsync(CancellationToken.None);
        await DisconnectAsync();
        await StopBanMonitorAsync();
        await StopPlayerSaveTimerAsync();
        _playerSaveLock.Dispose();
        _sendLock.Dispose();
        _disconnect.Dispose();
        _client.Dispose();
    }
}
