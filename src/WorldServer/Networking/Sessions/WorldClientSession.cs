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

namespace EmulationServer.WorldServer.Networking.Sessions;

public sealed class WorldClientSession : IChatSession, IInGameCommandSession, IAsyncDisposable
{
    private const float MaximumMovementBroadcastDistanceSquared = 200.0f * 200.0f;
    private readonly TcpClient _client;
    private readonly uint _realmId;
    private readonly int _maximumPacketSize;
    private readonly WorldAccountRepository _accountRepository;
    private readonly CharacterRepository _characterRepository;
    private readonly CharacterCreationService _characterService;
    private readonly GameItemSystem _itemSystem;
    private readonly GameChatSystem _chatSystem;
    private readonly GameInGameCommandService _commandService;
    private readonly WorldPlayerSessionRegistry _playerSessionRegistry;
    private readonly Func<PlayerLoginRecord, MapAvailabilityResult> _mapAvailabilityResolver;
    private readonly Func<PlayerLoginRecord, string, CancellationToken, Task> _playerEnteredWorldAsync;
    private readonly Func<PlayerLoginRecord, string, CancellationToken, Task> _playerLeftWorldAsync;
    private readonly Func<PlayerLoginRecord, string, PlayerMovementState, CancellationToken, Task> _playerMovementAsync;
    private readonly Func<PlayerLoginRecord, string, WorldPacket, CancellationToken, Task> _playerClientPacketAsync;
    private readonly TimeSpan _playerSaveInterval;
    private readonly SemaphoreSlim _playerSaveLock = new(1, 1);
    private readonly Action<int> _activePlayerCountChanged;
    private readonly Func<CancellationToken, Task> _characterCountChangedAsync;
    private readonly CancellationTokenSource _disconnect = new();
    private readonly HashSet<string> _chatChannels = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<WorldOpcode> _reportedUnhandledOpcodes = [];
    private readonly uint _serverSeed;
    private readonly string _messageOfTheDay;

    private NetworkStream? _stream;
    private WorldHeaderCrypt? _crypt;
    private WorldAccountSessionRecord? _account;
    private string _currentMapOwnerServerName = string.Empty;
    private CancellationTokenSource? _playerSaveCancellation;
    private Task? _playerSaveLoop;
    private bool _playerStateDirty;
    private DateTimeOffset _lastPlayerTimeSaveUtc;
    private bool _disposed;

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

    public Guid Id { get; }

    public PlayerLoginRecord? CurrentPlayer { get; private set; }

    public PlayerMovementState? CurrentMovement { get; private set; }

    public byte AccountGmLevel => _account?.GmLevel ?? 0;

    public int ActivePlayerCount => _playerSessionRegistry.ActivePlayerCount;

    public string MessageOfTheDay => _messageOfTheDay;

    public string RemoteEndPoint => _client.Client.RemoteEndPoint?.ToString() ?? "unknown";

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

    public PlayerLoginRecord RequireCurrentPlayer()
    {
        return CurrentPlayer ?? throw new InvalidOperationException("World client has not entered the game world.");
    }

    public bool IsInChatChannel(string channelName)
    {
        return _chatChannels.Contains(ChatSystem.NormalizeChannelName(channelName));
    }

    public void JoinChatChannel(string channelName)
    {
        _chatChannels.Add(ChatSystem.NormalizeChannelName(channelName));
    }

    public void LeaveChatChannel(string channelName)
    {
        _chatChannels.Remove(ChatSystem.NormalizeChannelName(channelName));
    }

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

    private async Task HandlePingAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        WorldPacketReader reader = new(packet.Payload);
        uint sequence = reader.ReadUInt32();
        _ = packet.Payload.Length >= 8 ? reader.ReadUInt32() : 0;

        await SendAsync(WorldOpcode.SMSG_PONG, WorldPacketBuilders.BuildPong(sequence), _crypt, cancellationToken);
    }

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

    private async Task SendCharacterLoginFailedAsync(CharacterLoginFailureCode failureCode, CancellationToken cancellationToken)
    {
        // Do not follow this with SMSG_CHAR_ENUM. During CMSG_PLAYER_LOGIN the
        // 1.12 client is in the character-login transition state and expects a
        // single login failure result. Sending a character list immediately after
        // the failure can make the client treat the world socket as invalid.
        await SendAsync(WorldOpcode.SMSG_CHARACTER_LOGIN_FAILED, WorldPacketBuilders.BuildCharacterLoginFailed(failureCode), _crypt, cancellationToken);
    }

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

    private async Task SendMovementPacketAsync(WorldOpcode opcode, byte[] payload, CancellationToken cancellationToken)
    {
        if (_crypt is null)
        {
            return;
        }

        await SendAsync(opcode, payload, _crypt, cancellationToken);
    }

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

    private void StartPlayerSaveTimer()
    {
        _playerSaveCancellation?.Cancel();
        _playerSaveCancellation?.Dispose();

        _playerSaveCancellation = CancellationTokenSource.CreateLinkedTokenSource(_disconnect.Token);
        _playerSaveLoop = RunPlayerSaveTimerAsync(_playerSaveCancellation.Token);
    }

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

    private static bool IsWithinMovementBroadcastRange(PlayerLoginRecord source, PlayerLoginRecord target)
    {
        float deltaX = source.PositionX - target.PositionX;
        float deltaY = source.PositionY - target.PositionY;
        float deltaZ = source.PositionZ - target.PositionZ;
        float distanceSquared = (deltaX * deltaX) + (deltaY * deltaY) + (deltaZ * deltaZ);
        return distanceSquared <= MaximumMovementBroadcastDistanceSquared;
    }

    private static uint SaturatingSeconds(TimeSpan elapsed)
    {
        if (elapsed <= TimeSpan.Zero)
        {
            return 0;
        }

        return elapsed.TotalSeconds >= uint.MaxValue ? uint.MaxValue : (uint)elapsed.TotalSeconds;
    }

    private static uint AddClamped(uint value, uint addition)
    {
        ulong result = (ulong)value + addition;
        return result > uint.MaxValue ? uint.MaxValue : (uint)result;
    }

    private async Task HandleItemQuerySingleAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        WorldPacketReader reader = new(packet.Payload);
        uint itemEntry = reader.ReadUInt32();

        byte[] payload = _itemSystem.TryGetItemTemplate(itemEntry, out ItemTemplateRecord itemTemplate)
            ? WorldPacketBuilders.BuildItemQuerySingleResponse(itemTemplate)
            : WorldPacketBuilders.BuildItemQuerySingleNotFound(itemEntry);

        await SendAsync(WorldOpcode.SMSG_ITEM_QUERY_SINGLE_RESPONSE, payload, _crypt, cancellationToken);
    }

    private async Task HandleItemNameQueryAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        WorldPacketReader reader = new(packet.Payload);
        uint itemEntry = reader.ReadUInt32();

        byte[] payload = _itemSystem.TryGetItemTemplate(itemEntry, out ItemTemplateRecord itemTemplate)
            ? WorldPacketBuilders.BuildItemNameQueryResponse(itemTemplate)
            : WorldPacketBuilders.BuildItemNameQueryNotFound(itemEntry);

        await SendAsync(WorldOpcode.SMSG_ITEM_NAME_QUERY_RESPONSE, payload, _crypt, cancellationToken);
    }

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

    private async Task HandlePlayedTimeAsync(CancellationToken cancellationToken)
    {
        await SendAsync(WorldOpcode.SMSG_PLAYED_TIME, WorldPacketBuilders.BuildPlayedTime(RequireCurrentPlayer()), _crypt, cancellationToken);
    }

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

    private async Task SendSystemMessageAsync(string message, CancellationToken cancellationToken)
    {
        PlayerLoginRecord? player = CurrentPlayer;
        ulong senderGuid = player?.ClientGuid ?? 0;
        string senderName = player?.Name ?? "Server";
        byte[] payload = WorldPacketBuilders.BuildChatMessage(ChatMessageType.System, ChatLanguage.Universal, senderGuid, senderName, message);
        await SendAsync(WorldOpcode.SMSG_MESSAGECHAT, payload, _crypt, cancellationToken);
    }

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

    private static ulong ReadCharacterDeleteGuid(byte[] payload)
    {
        return ReadClientGuid(payload);
    }

    private static ulong ReadClientGuid(byte[] payload)
    {
        WorldPacketReader reader = new(payload);
        return reader.ReadUInt64();
    }

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

    private static PlayerFaction ResolveFactionForRace(byte race)
    {
        return race switch
        {
            1 or 3 or 4 or 7 => PlayerFaction.Alliance,
            2 or 5 or 6 or 8 => PlayerFaction.Horde,
            _ => PlayerFaction.Neutral,
        };
    }

    private async Task SendAsync(WorldOpcode opcode, byte[] payload, WorldHeaderCrypt? crypt, CancellationToken cancellationToken)
    {
        await WorldPacketIO.WriteServerPacketAsync(GetStream(), opcode, payload, crypt, cancellationToken);
    }

    private NetworkStream GetStream()
    {
        return _stream ?? throw new InvalidOperationException("World client stream is not initialized.");
    }

    private WorldAccountSessionRecord RequireAccount()
    {
        return _account ?? throw new InvalidOperationException("World client is not authenticated.");
    }

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
