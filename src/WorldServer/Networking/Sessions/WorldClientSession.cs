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

using System.Net.Sockets;
using System.Security.Cryptography;

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
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _realmId = realmId;
        _maximumPacketSize = maximumPacketSize;
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _characterRepository = characterRepository ?? throw new ArgumentNullException(nameof(characterRepository));
        _characterService = characterService ?? throw new ArgumentNullException(nameof(characterService));
        _itemSystem = itemSystem ?? throw new ArgumentNullException(nameof(itemSystem));
        _chatSystem = chatSystem ?? throw new ArgumentNullException(nameof(chatSystem));
        _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
        _playerSessionRegistry = playerSessionRegistry ?? throw new ArgumentNullException(nameof(playerSessionRegistry));
        _mapAvailabilityResolver = mapAvailabilityResolver ?? throw new ArgumentNullException(nameof(mapAvailabilityResolver));
        _playerEnteredWorldAsync = playerEnteredWorldAsync ?? throw new ArgumentNullException(nameof(playerEnteredWorldAsync));
        _playerLeftWorldAsync = playerLeftWorldAsync ?? throw new ArgumentNullException(nameof(playerLeftWorldAsync));
        _playerMovementAsync = playerMovementAsync ?? throw new ArgumentNullException(nameof(playerMovementAsync));
        _playerClientPacketAsync = playerClientPacketAsync ?? throw new ArgumentNullException(nameof(playerClientPacketAsync));
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
     * Stores the default account gm level value used when the caller does not supply an override.
     * Centralizing the default keeps configuration and packet behavior consistent across the server process.
     */
    public byte AccountGmLevel => _account?.GmLevel ?? 0;

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
            Logger.Write(LogType.NETWORK, $"World client disconnected: {RemoteEndPoint}.", nameof(WorldClientSession));
        }
        catch (IOException exception)
        {
            Logger.Write(LogType.NETWORK, $"World client socket closed: {RemoteEndPoint}. {exception.Message}", nameof(WorldClientSession));
        }
        catch (SocketException exception)
        {
            Logger.Write(LogType.NETWORK, $"World client socket failed: {RemoteEndPoint}. {exception.Message}", nameof(WorldClientSession));
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.FAILED, $"World client session failed for {RemoteEndPoint}: {exception}", nameof(WorldClientSession));
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
        if (!_disconnect.IsCancellationRequested)
        {
            await _disconnect.CancelAsync();
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
     * Requires current player for the connected world client session lifecycle and packet dispatch workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     */
    public PlayerLoginRecord RequireCurrentPlayer()
    {
        return CurrentPlayer ?? throw new InvalidOperationException("World client has not entered the game world.");
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
            Logger.Write(LogType.WARNING, $"World client {RemoteEndPoint} sent {packet.Opcode} before CMSG_AUTH_SESSION.", nameof(WorldClientSession));
            await SendAsync(WorldOpcode.SMSG_AUTH_RESPONSE, WorldPacketBuilders.BuildAuthResponse(AuthResponseCode.Failed), null, cancellationToken);
            throw new InvalidDataException("World client did not send CMSG_AUTH_SESSION first.");
        }

        WorldAuthSessionRequest request = WorldAuthSessionParser.Parse(packet.Payload);
        string username = WorldAccountRepository.NormalizeUsername(request.Username);
        WorldAccountSessionRecord? account = await _accountRepository.GetAccountSessionAsync(username, cancellationToken);
        if (account is null || account.Locked)
        {
            Logger.Write(LogType.WARNING, $"World auth rejected for '{username}' from {RemoteEndPoint}: account missing or locked.", nameof(WorldClientSession));
            await SendAsync(WorldOpcode.SMSG_AUTH_RESPONSE, WorldPacketBuilders.BuildAuthResponse(AuthResponseCode.Failed), null, cancellationToken);
            throw new UnauthorizedAccessException("World account authentication failed.");
        }

        byte[] sessionKey = WorldAuthCryptography.ParseSessionKey(account.SessionKey);
        if (!WorldAuthCryptography.ProofMatches(username, request.ClientSeed, _serverSeed, sessionKey, request.ClientProof))
        {
            Logger.Write(LogType.WARNING, $"World auth proof failed for '{username}' from {RemoteEndPoint}.", nameof(WorldClientSession));
            await SendAsync(WorldOpcode.SMSG_AUTH_RESPONSE, WorldPacketBuilders.BuildAuthResponse(AuthResponseCode.Failed), null, cancellationToken);
            throw new UnauthorizedAccessException("World account proof failed.");
        }

        _account = account;
        _crypt = new WorldHeaderCrypt(sessionKey);
        await _accountRepository.SetActiveRealmAsync(account.Id, _realmId, cancellationToken);

        await SendAsync(WorldOpcode.SMSG_ADDON_INFO, WorldPacketBuilders.BuildAddonInfo(request.AddonInfo), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_AUTH_RESPONSE, WorldPacketBuilders.BuildAuthResponse(AuthResponseCode.Ok), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_ACCOUNT_DATA_TIMES, WorldPacketBuilders.BuildAccountDataTimes(), _crypt, cancellationToken);

        Logger.Write(LogType.SUCCESS, $"World client authenticated account '{account.Username}' ({account.Id}) from {RemoteEndPoint}.", nameof(WorldClientSession));
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
                    Logger.Write(LogType.TRACE, $"Received CMSG_UPDATE_ACCOUNT_DATA from {RemoteEndPoint}; persistence is not implemented yet.", nameof(WorldClientSession));
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

                case WorldOpcode.CMSG_OPENING_CINEMATIC:
                case WorldOpcode.CMSG_NEXT_CINEMATIC_CAMERA:
                case WorldOpcode.CMSG_COMPLETE_CINEMATIC:
                case WorldOpcode.CMSG_TUTORIAL_FLAG:
                case WorldOpcode.CMSG_TUTORIAL_CLEAR:
                case WorldOpcode.CMSG_TUTORIAL_RESET:
                case WorldOpcode.CMSG_STANDSTATECHANGE:
                case WorldOpcode.CMSG_SET_ACTION_BUTTON:
                case WorldOpcode.CMSG_SET_ACTIONBAR_TOGGLES:
                    Logger.Write(LogType.TRACE, $"Accepted client interface opcode {packet.Opcode} from {RemoteEndPoint}; persistence is not implemented yet.", nameof(WorldClientSession));
                    break;

                case WorldOpcode.CMSG_AREATRIGGER:
                    await ForwardPacketToMapServiceAsync(packet, cancellationToken);
                    break;

                case WorldOpcode.CMSG_ZONEUPDATE:
                    await HandleZoneUpdateAsync(packet, cancellationToken);
                    break;

                case var movementOpcode when WorldMovementOpcode.IsMovementOpcode(movementOpcode):
                    await HandleMovementPacketAsync(packet, cancellationToken);
                    break;

                default:
                    // Do not forward every unknown client opcode to MapServer. A Vanilla
                    // client can send high-frequency UI/movement-related packets after
                    // entering the world, and routing each one through the internal text
                    // stream can make the world feel laggy. Packets should be forwarded
                    // only after a concrete Map/Instance handler exists for them.
                    if (_reportedUnhandledOpcodes.Add(packet.Opcode))
                    {
                        Logger.Write(LogType.TRACE, $"Unhandled world opcode from {RemoteEndPoint}: {packet.Opcode} (0x{(ushort)packet.Opcode:X4}), payload={packet.Payload.Length} byte(s). Future packets with this opcode will be accepted silently until a handler is implemented.", nameof(WorldClientSession));
                    }
                    break;
            }
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
     * Handles the handle character enum event for the connected world client session lifecycle and packet dispatch workflow.
     * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
     * Inputs used by this operation: cancellationToken.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
     */
    private async Task HandleCharacterEnumAsync(CancellationToken cancellationToken)
    {
        WorldAccountSessionRecord account = RequireAccount();

        try
        {
            IReadOnlyList<CharacterListEntry> characters = await _characterService.GetCharacterListAsync(account.Id, cancellationToken);
            byte[] payload = WorldPacketBuilders.BuildCharacterEnum(characters);
            await SendAsync(WorldOpcode.SMSG_CHAR_ENUM, payload, _crypt, cancellationToken);

            Logger.Write(LogType.NETWORK, $"Sent character list to account '{account.Username}': {characters.Count} character(s), payload={payload.Length} byte(s).", nameof(WorldClientSession));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Logger.Write(LogType.FAILED, $"Failed to build/send character list for account '{account.Username}' ({account.Id}): {exception}", nameof(WorldClientSession));
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
            await SendCharacterLoginFailedAsync(CharacterLoginFailureCode.NotFound, cancellationToken);
            return;
        }

        PlayerLoginRecord? player = await _characterRepository.GetPlayerForLoginAsync(account.Id, characterGuid, ResolveFactionForRace, cancellationToken);
        if (player is null)
        {
            await SendCharacterLoginFailedAsync(CharacterLoginFailureCode.NotFound, cancellationToken);
            Logger.Write(LogType.WARNING, $"Player login rejected for account '{account.Username}': guid={characterGuid} was not found or was not owned by the account.", nameof(WorldClientSession));
            return;
        }

        MapAvailabilityResult mapAvailability = _mapAvailabilityResolver(player);
        if (!mapAvailability.IsAvailable)
        {
            await SendCharacterLoginFailedAsync(mapAvailability.RequiresInstanceServer ? CharacterLoginFailureCode.NoInstances : CharacterLoginFailureCode.NoWorld, cancellationToken);
            Logger.Write(LogType.WARNING, $"Player login rejected for '{player.Name}' ({player.Guid}): map={player.Map} is unavailable. {mapAvailability.Reason}", nameof(WorldClientSession));
            return;
        }

        if (!_playerSessionRegistry.TryRegister(player, this))
        {
            await SendCharacterLoginFailedAsync(CharacterLoginFailureCode.DuplicateLogin, cancellationToken);
            Logger.Write(LogType.WARNING, $"Player login rejected for '{player.Name}' ({player.Guid}): duplicate account or character session.", nameof(WorldClientSession));
            return;
        }

        try
        {
            await _characterRepository.SetCharacterOnlineAsync(player.Guid, true, cancellationToken);
            CurrentPlayer = player;
            CurrentMovement = PlayerMovementState.FromPlayer(player);
            _lastPlayerTimeSaveUtc = DateTimeOffset.UtcNow;
            _playerStateDirty = true;
            _currentMapOwnerServerName = mapAvailability.OwnerServerName;
            StartPlayerSaveTimer();
            await _playerEnteredWorldAsync(player, _currentMapOwnerServerName, cancellationToken);
            await SendWorldEntryPacketsAsync(player, cancellationToken);

            _activePlayerCountChanged(_playerSessionRegistry.ActivePlayerCount);
            Logger.Write(LogType.SUCCESS, $"Player '{player.Name}' ({player.Guid}) entered world map={player.Map}, zone={player.Zone} through {mapAvailability.OwnerServerName}.", nameof(WorldClientSession));
        }
        catch
        {
            await StopPlayerSaveTimerAsync();
            CurrentPlayer = null;
            CurrentMovement = null;
            _currentMapOwnerServerName = string.Empty;
            _playerSessionRegistry.Unregister(player, this);
            await _characterRepository.SetCharacterOnlineAsync(player.Guid, false, CancellationToken.None);
            throw;
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
        // Follow the MaNGOS Zero login bootstrap order more closely:
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
            Logger.Write(LogType.WARNING, $"Failed to forward {packet.Opcode} from player '{player.Name}' ({player.Guid}) to {ownerServerName}: {exception.Message}", nameof(WorldClientSession));
            return false;
        }
    }

    /**
     * Handles the handle movement packet event for the connected world client session lifecycle and packet dispatch workflow.
     * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
     * Inputs used by this operation: packet, cancellationToken.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
     */
    private async Task HandleMovementPacketAsync(WorldPacket packet, CancellationToken cancellationToken)
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
                Logger.Write(LogType.TRACE, $"Accepted movement opcode {packet.Opcode} from {RemoteEndPoint}, but no position state could be parsed from payload={packet.Payload.Length} byte(s). Future packets with this opcode will be accepted silently.", nameof(WorldClientSession));
            }

            await ForwardPacketToMapServiceAsync(packet, cancellationToken);
            return;
        }

        ApplyMovementState(movement);
        PlayerLoginRecord updatedPlayer = RequireCurrentPlayer();

        try
        {
            await _playerMovementAsync(updatedPlayer, ownerServerName, movement, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Logger.Write(LogType.WARNING, $"Failed to route movement for player '{updatedPlayer.Name}' ({updatedPlayer.Guid}) to {ownerServerName}: {exception.Message}", nameof(WorldClientSession));
        }

        await BroadcastMovementToNearbyPlayersAsync(packet, movement, cancellationToken);
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
        CurrentPlayer = player with
        {
            Map = movement.Map,
            Zone = movement.Zone,
            PositionX = movement.PositionX,
            PositionY = movement.PositionY,
            PositionZ = movement.PositionZ,
            Orientation = movement.Orientation,
        };
        _playerStateDirty = true;
    }

    /**
     * Performs the broadcast movement to nearby players operation for the connected world client session lifecycle and packet dispatch workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: packet, movement, cancellationToken.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
     */
    private async Task BroadcastMovementToNearbyPlayersAsync(WorldPacket packet, PlayerMovementState movement, CancellationToken cancellationToken)
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

        byte[] payload = WorldPacketBuilders.BuildMovementBroadcast(movement.ClientGuid, packet.Payload);
        foreach (WorldClientSession recipient in _playerSessionRegistry.SnapshotSessions())
        {
            if (ReferenceEquals(recipient, this) || recipient.CurrentPlayer is null || recipient.CurrentPlayer.Map != player.Map)
            {
                continue;
            }

            if (!IsWithinMovementBroadcastRange(player, recipient.CurrentPlayer))
            {
                continue;
            }

            await recipient.SendMovementPacketAsync(packet.Opcode, payload, cancellationToken);
        }
    }

    /**
     * Sends send movement packet data to the connected session or internal peer.
     * The send path keeps packet construction and delivery together so opcode handling remains easy to trace during protocol debugging.
     * Inputs used by this operation: opcode, payload, cancellationToken.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
     */
    private async Task SendMovementPacketAsync(WorldOpcode opcode, byte[] payload, CancellationToken cancellationToken)
    {
        if (_crypt is null)
        {
            return;
        }

        await SendAsync(opcode, payload, _crypt, cancellationToken);
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
                await SaveCurrentPlayerAsync(force: true, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.WARNING, $"Player save timer stopped for {RemoteEndPoint}: {exception.Message}", nameof(WorldClientSession));
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

            await _characterRepository.SavePlayerAsync(snapshot, cancellationToken);
            CurrentPlayer = snapshot;
            _lastPlayerTimeSaveUtc = now;
            _playerStateDirty = false;

            Logger.Write(LogType.TRACE, $"Saved player '{snapshot.Name}' ({snapshot.Guid}) state: map={snapshot.Map}, zone={snapshot.Zone}, position=({snapshot.PositionX:0.##}, {snapshot.PositionY:0.##}, {snapshot.PositionZ:0.##}).", nameof(WorldClientSession));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Logger.Write(LogType.WARNING, $"Failed to save player state for {player?.Name ?? RemoteEndPoint}: {exception.Message}", nameof(WorldClientSession));
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
    private static bool IsWithinMovementBroadcastRange(PlayerLoginRecord source, PlayerLoginRecord target)
    {
        float deltaX = source.PositionX - target.PositionX;
        float deltaY = source.PositionY - target.PositionY;
        float deltaZ = source.PositionZ - target.PositionZ;
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
            Logger.Write(LogType.WARNING, $"Ignoring CMSG_MESSAGECHAT from {RemoteEndPoint}: player is not in world.", nameof(WorldClientSession));
            return;
        }

        ChatIncomingMessage message;
        try
        {
            message = _chatSystem.NormalizeIncomingMessage(CurrentPlayer, ReadChatMessage(packet.Payload));
        }
        catch (InvalidDataException exception)
        {
            Logger.Write(LogType.WARNING, $"Ignoring malformed chat packet from '{CurrentPlayer.Name}': {exception.Message}", nameof(WorldClientSession));
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

        Logger.Write(LogType.NETWORK, $"Relayed {message.Type} chat from '{CurrentPlayer.Name}' to {worldRecipients.Length} faction-scoped recipient(s).", nameof(WorldClientSession));
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
        byte[] payload = WorldPacketBuilders.BuildChatMessage(ChatMessageType.System, ChatLanguage.Universal, senderGuid, senderName, message);
        await SendAsync(WorldOpcode.SMSG_MESSAGECHAT, payload, _crypt, cancellationToken);
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
            nameof(WorldClientSession));
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
            nameof(WorldClientSession));
    }

    /**
     * Performs the cleanup current player operation for the connected world client session lifecycle and packet dispatch workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: cancellationToken.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
     */
    private async Task CleanupCurrentPlayerAsync(CancellationToken cancellationToken)
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
        _chatChannels.Clear();

        if (!string.IsNullOrWhiteSpace(ownerServerName))
        {
            try
            {
                await _playerLeftWorldAsync(player, ownerServerName, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Logger.Write(LogType.WARNING, $"Failed to notify map service that player '{player.Name}' ({player.Guid}) left world: {exception.Message}", nameof(WorldClientSession));
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
            Logger.Write(LogType.FAILED, $"Failed to mark player '{player.Name}' ({player.Guid}) offline: {exception.Message}", nameof(WorldClientSession));
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
        await WorldPacketIO.WriteServerPacketAsync(GetStream(), opcode, payload, crypt, cancellationToken);
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
        await StopPlayerSaveTimerAsync();
        _playerSaveLock.Dispose();
        _disconnect.Dispose();
        _client.Dispose();
    }
}
