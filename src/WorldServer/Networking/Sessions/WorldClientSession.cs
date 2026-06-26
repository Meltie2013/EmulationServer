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
// File: src/WorldServer/Networking/Sessions/WorldClientSession.cs
// Purpose: Contains world client session code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Channels;
using EmulationServer.Database.Accounts;
using EmulationServer.Game.Characters;
using EmulationServer.Game.Chat;
using EmulationServer.Game.Commands;
using EmulationServer.Game.Movement;
using EmulationServer.Game.Players;
using EmulationServer.Game.WorldData;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.WorldServer.Auth;
using EmulationServer.WorldServer.Characters;
using EmulationServer.WorldServer.Database.Accounts;
using EmulationServer.WorldServer.Database.Characters;
using EmulationServer.WorldServer.Networking.Movement;
using EmulationServer.WorldServer.Networking.Packets;
using GameChatSystem = EmulationServer.Game.Chat.ChatSystem;
using GameInGameCommandService = EmulationServer.Game.Commands.InGameCommandService;
using GameItemSystem = EmulationServer.Game.Items.ItemSystem;
using WorldPlayerSessionRegistry = EmulationServer.WorldServer.Players.PlayerSessionRegistry;

namespace EmulationServer.WorldServer.Networking.Sessions;

// Type: WorldClientSession
// Purpose: Provides world client session behavior for the world server gameplay, session, and character runtime layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class WorldClientSession : IChatSession, IInGameCommandSession, IAsyncDisposable
{

    // Constant: Defines the maximum movement broadcast distance squared constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed maximum movement broadcast distance squared value used anywhere this rule or protocol value is needed.
    private const float MaximumMovementBroadcastDistanceSquared = 200.0f * 200.0f;
    // Constant: Defines the player visibility distance constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed player visibility distance value used anywhere this rule or protocol value is needed.
    private const float PlayerVisibilityDistance = 120.0f;
    // Constant: Defines the player visibility distance squared constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed player visibility distance squared value used anywhere this rule or protocol value is needed.
    private const float PlayerVisibilityDistanceSquared = PlayerVisibilityDistance * PlayerVisibilityDistance;
    // Constant: Defines the player visibility unload distance constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed player visibility unload distance value used anywhere this rule or protocol value is needed.
    private const float PlayerVisibilityUnloadDistance = 150.0f;
    // Constant: Defines the player visibility unload distance squared constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed player visibility unload distance squared value used anywhere this rule or protocol value is needed.
    private const float PlayerVisibilityUnloadDistanceSquared = PlayerVisibilityUnloadDistance * PlayerVisibilityUnloadDistance;
    // Constant: Defines the maximum player create updates per refresh constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed maximum player create updates per refresh value used anywhere this rule or protocol value is needed.
    private const int MaximumPlayerCreateUpdatesPerRefresh = 32;
    // Constant: Defines the game object visibility distance constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed game object visibility distance value used anywhere this rule or protocol value is needed.
    private const float GameObjectVisibilityDistance = 90.0f;
    // Constant: Defines the game object visibility distance squared constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed game object visibility distance squared value used anywhere this rule or protocol value is needed.
    private const float GameObjectVisibilityDistanceSquared = GameObjectVisibilityDistance * GameObjectVisibilityDistance;
    // Constant: Defines the game object visibility unload distance constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed game object visibility unload distance value used anywhere this rule or protocol value is needed.
    private const float GameObjectVisibilityUnloadDistance = 120.0f;
    // Constant: Defines the game object visibility unload distance squared constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed game object visibility unload distance squared value used anywhere this rule or protocol value is needed.
    private const float GameObjectVisibilityUnloadDistanceSquared = GameObjectVisibilityUnloadDistance * GameObjectVisibilityUnloadDistance;
    // Constant: Defines the maximum game object create updates per refresh constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed maximum game object create updates per refresh value used anywhere this rule or protocol value is needed.
    private const int MaximumGameObjectCreateUpdatesPerRefresh = 96;
    // Constant: Defines the creature visibility distance constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed creature visibility distance value used anywhere this rule or protocol value is needed.
    private const float CreatureVisibilityDistance = 90.0f;
    // Constant: Defines the creature visibility distance squared constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed creature visibility distance squared value used anywhere this rule or protocol value is needed.
    private const float CreatureVisibilityDistanceSquared = CreatureVisibilityDistance * CreatureVisibilityDistance;
    // Constant: Defines the creature visibility unload distance constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed creature visibility unload distance value used anywhere this rule or protocol value is needed.
    private const float CreatureVisibilityUnloadDistance = 120.0f;
    // Constant: Defines the creature visibility unload distance squared constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed creature visibility unload distance squared value used anywhere this rule or protocol value is needed.
    private const float CreatureVisibilityUnloadDistanceSquared = CreatureVisibilityUnloadDistance * CreatureVisibilityUnloadDistance;
    // Constant: Defines the maximum creature create updates per refresh constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed maximum creature create updates per refresh value used anywhere this rule or protocol value is needed.
    private const int MaximumCreatureCreateUpdatesPerRefresh = 32;

    // Method: FromMilliseconds
    // Purpose: Executes the from milliseconds operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the time span terminal auth failure delivery delay = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private static readonly TimeSpan TerminalAuthFailureDeliveryDelay = TimeSpan.FromMilliseconds(250);

    // Method: FromMilliseconds
    // Purpose: Executes the from milliseconds operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the time span character login failure delivery delay = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private static readonly TimeSpan CharacterLoginFailureDeliveryDelay = TimeSpan.FromMilliseconds(1000);

    // Method: FromSeconds
    // Purpose: Executes the from seconds operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the time span map service failure notification cooldown = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private static readonly TimeSpan MapServiceFailureNotificationCooldown = TimeSpan.FromSeconds(5);

    // Method: FromMilliseconds
    // Purpose: Executes the from milliseconds operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the time span game object visibility refresh interval = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private static readonly TimeSpan GameObjectVisibilityRefreshInterval = TimeSpan.FromMilliseconds(750);
    // Method: FromMilliseconds
    // Purpose: Executes the from milliseconds operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the time span creature visibility refresh interval = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private static readonly TimeSpan CreatureVisibilityRefreshInterval = TimeSpan.FromMilliseconds(750);

    // Method: FromSeconds
    // Purpose: Executes the from seconds operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the time span ban recheck interval = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private static readonly TimeSpan BanRecheckInterval = TimeSpan.FromSeconds(30);

    // Method: FromMilliseconds
    // Purpose: Executes the from milliseconds operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the time span player record movement update interval = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private static readonly TimeSpan PlayerRecordMovementUpdateInterval = TimeSpan.FromMilliseconds(250);

    // Constant: Defines the movement broadcast queue capacity constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed movement broadcast queue capacity value used anywhere this rule or protocol value is needed.
    private const int MovementBroadcastQueueCapacity = 256;

    // Constant: Defines the map service movement queue capacity constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed map service movement queue capacity value used anywhere this rule or protocol value is needed.
    private const int MapServiceMovementQueueCapacity = 1;

    // Constant: Defines the system chat line length constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed system chat line length value used anywhere this rule or protocol value is needed.
    private const int SystemChatLineLength = 160;

    // Type: QueuedMovementPacket
    // Purpose: Represents queued movement packet data passed through the world server gameplay, session, and character runtime layer.
    // Constructor values:
    // - Opcode: Opcode value supplied by the caller for this operation.
    // - bytePayload: Byte payload value supplied by the caller for this operation.
    // Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
    private readonly record struct QueuedMovementPacket(WorldOpcode Opcode, byte[] Payload);

    // Type: QueuedMapServiceMovement
    // Purpose: Represents queued map service movement data passed through the world server gameplay, session, and character runtime layer.
    // Constructor values:
    // - Player: Player value supplied by the caller for this operation.
    // - OwnerServerName: Owner server name value supplied by the caller for this operation.
    // - Movement: Movement value supplied by the caller for this operation.
    // Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
    private readonly record struct QueuedMapServiceMovement(PlayerLoginRecord Player, string OwnerServerName, PlayerMovementState Movement);

    // Type: InventoryClientPosition
    // Purpose: Represents inventory client position data passed through the world server gameplay, session, and character runtime layer.
    // Constructor values:
    // - Bag: Bag value supplied by the caller for this operation.
    // - Slot: Slot value supplied by the caller for this operation.
    // Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
    private readonly record struct InventoryClientPosition(byte Bag, byte Slot);

    // Type: InventoryStorageLocation
    // Purpose: Represents inventory storage location data passed through the world server gameplay, session, and character runtime layer.
    // Constructor values:
    // - BagGuid: Bag GUID identifier used to select the exact record, object, or runtime owner.
    // - Slot: Slot value supplied by the caller for this operation.
    // Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
    private readonly record struct InventoryStorageLocation(uint BagGuid, byte Slot);

    // Constant: Defines the client backpack bag constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed client backpack bag value used anywhere this rule or protocol value is needed.
    private const byte ClientBackpackBag = 0xFF;
    // Constant: Defines the inventory change failure item doesnt go to slot constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed inventory change failure item doesnt go to slot value used anywhere this rule or protocol value is needed.
    private const byte InventoryChangeFailureItemDoesntGoToSlot = 0x0D;
    // Constant: Defines the inventory change failure item not found constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed inventory change failure item not found value used anywhere this rule or protocol value is needed.
    private const byte InventoryChangeFailureItemNotFound = 0x2A;
    // Constant: Defines the inventory change failure bag full constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed inventory change failure bag full value used anywhere this rule or protocol value is needed.
    private const byte InventoryChangeFailureBagFull = 0x04;

    // Field: Stores the client state used by the world server gameplay, session, and character runtime layer.
    // Value: current client backing value maintained by the owning type.
    private readonly TcpClient _client;

    // Field: Stores the realm ID state used by the world server gameplay, session, and character runtime layer.
    // Value: current realm ID backing value maintained by the owning type.
    private readonly uint _realmId;

    // Field: Stores the maximum packet size state used by the world server gameplay, session, and character runtime layer.
    // Value: current maximum packet size backing value maintained by the owning type.
    private readonly int _maximumPacketSize;

    // Field: Stores the account repository state used by the world server gameplay, session, and character runtime layer.
    // Value: current account repository backing value maintained by the owning type.
    private readonly WorldAccountRepository _accountRepository;

    // Field: Stores the character repository state used by the world server gameplay, session, and character runtime layer.
    // Value: current character repository backing value maintained by the owning type.
    private readonly CharacterRepository _characterRepository;

    // Field: Stores the character service state used by the world server gameplay, session, and character runtime layer.
    // Value: current character service backing value maintained by the owning type.
    private readonly CharacterCreationService _characterService;

    // Field: Stores the item system state used by the world server gameplay, session, and character runtime layer.
    // Value: current item system backing value maintained by the owning type.
    private readonly GameItemSystem _itemSystem;

    // Field: Stores the chat system state used by the world server gameplay, session, and character runtime layer.
    // Value: current chat system backing value maintained by the owning type.
    private readonly GameChatSystem _chatSystem;

    // Field: Stores the command service state used by the world server gameplay, session, and character runtime layer.
    // Value: current command service backing value maintained by the owning type.
    private readonly GameInGameCommandService _commandService;

    // Field: Stores the player session registry state used by the world server gameplay, session, and character runtime layer.
    // Value: current player session registry backing value maintained by the owning type.
    private readonly WorldPlayerSessionRegistry _playerSessionRegistry;
    // Field: Stores the player login record state used by the world server gameplay, session, and character runtime layer.
    // Value: current player login record backing value maintained by the owning type.
    private readonly Func<PlayerLoginRecord, MapAvailabilityResult> _mapAvailabilityResolver;
    // Field: Stores the player login record state used by the world server gameplay, session, and character runtime layer.
    // Value: current player login record backing value maintained by the owning type.
    private readonly Func<PlayerLoginRecord, string, CancellationToken, Task> _playerEnteredWorldAsync;
    // Field: Stores the player login record state used by the world server gameplay, session, and character runtime layer.
    // Value: current player login record backing value maintained by the owning type.
    private readonly Func<PlayerLoginRecord, string, CancellationToken, Task> _playerLeftWorldAsync;
    // Field: Stores the player login record state used by the world server gameplay, session, and character runtime layer.
    // Value: current player login record backing value maintained by the owning type.
    private readonly Func<PlayerLoginRecord, string, PlayerMovementState, CancellationToken, Task> _playerMovementAsync;
    // Field: Stores the player login record state used by the world server gameplay, session, and character runtime layer.
    // Value: current player login record backing value maintained by the owning type.
    private readonly Func<PlayerLoginRecord, string, WorldPacket, CancellationToken, Task> _playerClientPacketAsync;
    // Field: Stores the world template data resolver state used by the world server gameplay, session, and character runtime layer.
    // Value: current world template data resolver backing value maintained by the owning type.
    private readonly Func<WorldTemplateDataStore> _worldTemplateDataResolver;

    // Field: Stores the player save interval state used by the world server gameplay, session, and character runtime layer.
    // Value: current player save interval backing value maintained by the owning type.
    private readonly TimeSpan _playerSaveInterval;

    private readonly SemaphoreSlim _playerSaveLock = new(1, 1);

    private readonly SemaphoreSlim _sendLock = new(1, 1);

    // Field: Tracks live movement conditions and calculates movement-related background intervals automatically.
    // Value: per-session adaptive timing controller for backend route and visibility refresh work.
    private readonly WorldMovementTimingController _movementTiming;

    // Method: QueuedMovementPacket
    // Purpose: Executes the queued movement packet operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - SingleReader: Single reader value supplied by the caller for this operation.
    // Returns: Returns the channel movement broadcast queue = channel.create bounded< value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private readonly Channel<QueuedMovementPacket> _movementBroadcastQueue = Channel.CreateBounded<QueuedMovementPacket>(new BoundedChannelOptions(MovementBroadcastQueueCapacity)
    {
        SingleReader = true,
        SingleWriter = false,
        FullMode = BoundedChannelFullMode.DropOldest,
        AllowSynchronousContinuations = false,
    });

    // Method: QueuedMapServiceMovement
    // Purpose: Executes the queued map service movement operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - SingleReader: Single reader value supplied by the caller for this operation.
    // Returns: Returns the channel map service movement queue = channel.create bounded< value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private readonly Channel<QueuedMapServiceMovement> _mapServiceMovementQueue = Channel.CreateBounded<QueuedMapServiceMovement>(new BoundedChannelOptions(MapServiceMovementQueueCapacity)
    {
        SingleReader = true,
        SingleWriter = false,
        FullMode = BoundedChannelFullMode.DropOldest,
        AllowSynchronousContinuations = false,
    });

    // Method: WorldPacket
    // Purpose: Executes the world packet operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - SingleReader: Single reader value supplied by the caller for this operation.
    // Returns: Returns the channel gameplay packet queue = channel.create bounded< value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private readonly Channel<WorldPacket> _gameplayPacketQueue = Channel.CreateBounded<WorldPacket>(new BoundedChannelOptions(1024)
    {
        SingleReader = true,
        SingleWriter = true,
        FullMode = BoundedChannelFullMode.Wait,
        AllowSynchronousContinuations = false,
    });

    // Field: Stores the active player count changed state used by the world server gameplay, session, and character runtime layer.
    // Value: current active player count changed backing value maintained by the owning type.
    private readonly Action<int> _activePlayerCountChanged;
    // Field: Stores the cancellation token state used by the world server gameplay, session, and character runtime layer.
    // Value: current cancellation token backing value maintained by the owning type.
    private readonly Func<CancellationToken, Task> _characterCountChangedAsync;

    private readonly CancellationTokenSource _disconnect = new();

    private readonly HashSet<string> _chatChannels = new(StringComparer.OrdinalIgnoreCase);

    // Field: Stores the reported unhandled opcodes state used by the world server gameplay, session, and character runtime layer.
    // Value: current reported unhandled opcodes backing value maintained by the owning type.
    private readonly HashSet<WorldOpcode> _reportedUnhandledOpcodes = [];
    // Field: Stores the ulong state used by the world server gameplay, session, and character runtime layer.
    // Value: current ulong backing value maintained by the owning type.
    private readonly Dictionary<ulong, uint> _visibleGameObjectClientGuids = [];
    // Field: Stores the ulong state used by the world server gameplay, session, and character runtime layer.
    // Value: current ulong backing value maintained by the owning type.
    private readonly Dictionary<ulong, uint> _visibleCreatureClientGuids = [];
    // Field: Stores player GUIDs that this client has already received as visible player objects.
    // Value: current visible player backing value maintained by the owning type.
    private readonly HashSet<uint> _visiblePlayerGuids = [];
    // Field: Serializes player visibility creates and destroys sent from this session and neighboring sessions.
    // Value: current visibility lock backing value maintained by the owning type.
    private readonly SemaphoreSlim _visibilityLock = new(1, 1);

    // Field: Stores the server seed state used by the world server gameplay, session, and character runtime layer.
    // Value: current server seed backing value maintained by the owning type.
    private readonly uint _serverSeed;

    // Field: Stores the stream state used by the world server gameplay, session, and character runtime layer.
    // Value: current stream backing value maintained by the owning type.
    private NetworkStream? _stream;

    // Field: Stores the crypt state used by the world server gameplay, session, and character runtime layer.
    // Value: current crypt backing value maintained by the owning type.
    private WorldHeaderCrypt? _crypt;

    // Field: Stores the account state used by the world server gameplay, session, and character runtime layer.
    // Value: current account backing value maintained by the owning type.
    private WorldAccountSessionRecord? _account;

    // Field: Stores the current map owner server name state used by the world server gameplay, session, and character runtime layer.
    // Value: current current map owner server name backing value maintained by the owning type.
    private string _currentMapOwnerServerName = string.Empty;

    // Field: Stores the player save cancellation state used by the world server gameplay, session, and character runtime layer.
    // Value: current player save cancellation backing value maintained by the owning type.
    private CancellationTokenSource? _playerSaveCancellation;

    // Field: Stores the player save loop state used by the world server gameplay, session, and character runtime layer.
    // Value: current player save loop backing value maintained by the owning type.
    private Task? _playerSaveLoop;

    // Field: Stores the movement broadcast loop state used by the world server gameplay, session, and character runtime layer.
    // Value: current movement broadcast loop backing value maintained by the owning type.
    private Task? _movementBroadcastLoop;

    // Field: Guards deferred visibility refresh work that is triggered by movement packets.
    // Value: 1 when a movement visibility refresh task is already scheduled or running.
    private int _movementVisibilityRefreshQueued;

    // Field: Stores the map service movement route loop state used by the world server gameplay, session, and character runtime layer.
    // Value: current map service movement route loop backing value maintained by the owning type.
    private Task? _mapServiceMovementRouteLoop;

    // Field: Stores the gameplay packet loop state used by the world server gameplay, session, and character runtime layer.
    // Value: current gameplay packet loop backing value maintained by the owning type.
    private Task? _gameplayPacketLoop;

    // Field: Stores the ban monitor cancellation state used by the world server gameplay, session, and character runtime layer.
    // Value: current ban monitor cancellation backing value maintained by the owning type.
    private CancellationTokenSource? _banMonitorCancellation;

    // Field: Stores the ban monitor loop state used by the world server gameplay, session, and character runtime layer.
    // Value: current ban monitor loop backing value maintained by the owning type.
    private Task? _banMonitorLoop;

    // Field: Stores the ban disconnect started state used by the world server gameplay, session, and character runtime layer.
    // Value: current ban disconnect started backing value maintained by the owning type.
    private int _banDisconnectStarted;

    // Field: Stores the last player record movement update utc state used by the world server gameplay, session, and character runtime layer.
    // Value: current last player record movement update utc backing value maintained by the owning type.
    private DateTimeOffset _lastPlayerRecordMovementUpdateUtc = DateTimeOffset.MinValue;

    // Field: Stores the player state dirty state used by the world server gameplay, session, and character runtime layer.
    // Value: current player state dirty backing value maintained by the owning type.
    private bool _playerStateDirty;

    // Field: Stores the last player time save utc state used by the world server gameplay, session, and character runtime layer.
    // Value: current last player time save utc backing value maintained by the owning type.
    private DateTimeOffset _lastPlayerTimeSaveUtc;

    // Field: Stores the last map service failure notification utc state used by the world server gameplay, session, and character runtime layer.
    // Value: current last map service failure notification utc backing value maintained by the owning type.
    private DateTimeOffset _lastMapServiceFailureNotificationUtc = DateTimeOffset.MinValue;

    // Field: Stores the last map service movement route utc state used by the world server gameplay, session, and character runtime layer.
    // Value: current last map service movement route utc backing value maintained by the owning type.
    private DateTimeOffset _lastMapServiceMovementRouteUtc = DateTimeOffset.MinValue;
    // Field: Stores the last map service movement route map state used by the world server gameplay, session, and character runtime layer.
    // Value: current last map service movement route map backing value maintained by the owning type.
    private uint _lastMapServiceMovementRouteMap;
    // Field: Stores the last map service movement route zone state used by the world server gameplay, session, and character runtime layer.
    // Value: current last map service movement route zone backing value maintained by the owning type.
    private uint _lastMapServiceMovementRouteZone;
    // Field: Stores the has last map service movement route state used by the world server gameplay, session, and character runtime layer.
    // Value: current has last map service movement route backing value maintained by the owning type.
    private bool _hasLastMapServiceMovementRoute;
    // Field: Stores the last game object visibility refresh utc state used by the world server gameplay, session, and character runtime layer.
    // Value: current last game object visibility refresh utc backing value maintained by the owning type.
    private DateTimeOffset _lastGameObjectVisibilityRefreshUtc = DateTimeOffset.MinValue;
    // Field: Stores the last creature visibility refresh utc state used by the world server gameplay, session, and character runtime layer.
    // Value: current last creature visibility refresh utc backing value maintained by the owning type.
    private DateTimeOffset _lastCreatureVisibilityRefreshUtc = DateTimeOffset.MinValue;
    // Field: Stores the last player visibility refresh utc state used by the world server gameplay, session, and character runtime layer.
    // Value: current last player visibility refresh utc backing value maintained by the owning type.
    private DateTimeOffset _lastPlayerVisibilityRefreshUtc = DateTimeOffset.MinValue;

    // Field: Stores the delay character enum until utc state used by the world server gameplay, session, and character runtime layer.
    // Value: current delay character enum until utc backing value maintained by the owning type.
    private DateTimeOffset _delayCharacterEnumUntilUtc = DateTimeOffset.MinValue;

    // Field: Stores the service disconnect started state used by the world server gameplay, session, and character runtime layer.
    // Value: current service disconnect started backing value maintained by the owning type.
    private int _serviceDisconnectStarted;

    // Field: Stores the disposed state used by the world server gameplay, session, and character runtime layer.
    // Value: current disposed backing value maintained by the owning type.
    private bool _disposed;

    // Constructor: WorldClientSession
    // Purpose: Initializes a new WorldClientSession instance with dependencies and values required by the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - client: Client value supplied by the caller for this operation.
    // - realmId: Realm ID identifier used to select the exact record, object, or runtime owner.
    // - maximumPacketSize: Maximum packet size value supplied by the caller for this operation.
    // - accountRepository: Account repository value supplied by the caller for this operation.
    // - characterRepository: Character repository value supplied by the caller for this operation.
    // - characterService: Character service value supplied by the caller for this operation.
    // - itemSystem: Item system value supplied by the caller for this operation.
    // - chatSystem: Chat system value supplied by the caller for this operation.
    // - commandService: Command service value supplied by the caller for this operation.
    // - playerSessionRegistry: Player session registry value supplied by the caller for this operation.
    // - mapAvailabilityResolver: Map availability resolver value supplied by the caller for this operation.
    // - playerEnteredWorldAsync: Player entered world async value supplied by the caller for this operation.
    // - playerLeftWorldAsync: Player left world async value supplied by the caller for this operation.
    // - playerMovementAsync: Player movement async value supplied by the caller for this operation.
    // - playerClientPacketAsync: Player client packet async value supplied by the caller for this operation.
    // - movementTimingTelemetry: Shared movement timing telemetry supplied by the world server.
    // - worldTemplateDataResolver: World template data resolver value supplied by the caller for this operation.
    // - messageOfTheDay: Message of the day value supplied by the caller for this operation.
    // - playerSaveInterval: Player save interval value supplied by the caller for this operation.
    // - activePlayerCountChanged: Active player count changed value supplied by the caller for this operation.
    // - characterCountChangedAsync: Character count changed async value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
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
        WorldMovementTimingTelemetry movementTimingTelemetry,
        Func<WorldTemplateDataStore> worldTemplateDataResolver,
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
        _movementTiming = new WorldMovementTimingController((movementTimingTelemetry ?? throw new ArgumentNullException()).GetInternalServerLatency);
        _worldTemplateDataResolver = worldTemplateDataResolver ?? throw new ArgumentNullException();
        MessageOfTheDay = string.IsNullOrWhiteSpace(messageOfTheDay) ? "Welcome to Emulation Server." : messageOfTheDay;
        _playerSaveInterval = playerSaveInterval <= TimeSpan.Zero ? TimeSpan.FromSeconds(60) : playerSaveInterval;
        _activePlayerCountChanged = activePlayerCountChanged ?? (_ => { });
        _characterCountChangedAsync = characterCountChangedAsync ?? (_ => Task.CompletedTask);
        _serverSeed = unchecked((uint)RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue));
        Id = Guid.NewGuid();
    }

    // Property: Gets or sets the ID value used by the world server gameplay, session, and character runtime layer.
    // Value: ID value exposed by the owning type.
    public Guid Id { get; }

    // Property: Gets or sets the current player value used by the world server gameplay, session, and character runtime layer.
    // Value: current player value exposed by the owning type.
    public PlayerLoginRecord? CurrentPlayer { get; private set; }

    // Property: Gets or sets the current movement value used by the world server gameplay, session, and character runtime layer.
    // Value: current movement value exposed by the owning type.
    public PlayerMovementState? CurrentMovement { get; private set; }

    // Property: Gets or sets the current map owner server name value used by the world server gameplay, session, and character runtime layer.
    // Value: current map owner server name value exposed by the owning type.
    public string CurrentMapOwnerServerName => _currentMapOwnerServerName;

    // Property: Gets or sets the account ID value used by the world server gameplay, session, and character runtime layer.
    // Value: account ID value exposed by the owning type.
    public uint AccountId => _account?.Id ?? 0;

    // Property: Gets or sets the account name value used by the world server gameplay, session, and character runtime layer.
    // Value: account name value exposed by the owning type.
    public string AccountName => _account?.Username ?? string.Empty;

    // Property: Gets or sets the account security level value used by the world server gameplay, session, and character runtime layer.
    // Value: account security level value exposed by the owning type.
    public AccountSecurityLevel AccountSecurityLevel => _account?.SecurityLevel ?? AccountSecurityLevel.Player;

    // Method: HasPermission
    // Purpose: Validates or evaluates has permission rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - permissionId: Permission ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns true when has permission succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    public bool HasPermission(uint permissionId)
    {
        return _account?.Permissions.HasPermission(permissionId) is true;
    }

    // Method: ReloadPermissionsAsync
    // Purpose: Executes the reload permissions operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task ReloadPermissionsAsync(CancellationToken cancellationToken)
    {
        WorldAccountSessionRecord account = RequireAccount();
        WorldAccountSessionRecord? reloaded = await _accountRepository.GetAccountSessionAsync(account.Username, _realmId, cancellationToken)
            ?? throw new InvalidOperationException($"Account '{account.Username}' could not be reloaded.");
        _account = reloaded;
    }

    // Property: Gets or sets the active player count value used by the world server gameplay, session, and character runtime layer.
    // Value: active player count value exposed by the owning type.
    public int ActivePlayerCount => _playerSessionRegistry.ActivePlayerCount;

    // Property: Gets or sets the message of the day value used by the world server gameplay, session, and character runtime layer.
    // Value: message of the day value exposed by the owning type.
    public string MessageOfTheDay { get; }

    // Method: ToString
    // Purpose: Executes the to string operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the string remote end point => client.client.remote end point?. value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    public string RemoteEndPoint => _client.Client.RemoteEndPoint?.ToString() ?? "unknown";

    // Method: RemoteAddress
    // Purpose: Executes the remote address operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - IPEndPoint: IP end point value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private string RemoteAddress => (_client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? string.Empty;

    // Method: ProcessAsync
    // Purpose: Executes the process operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - serverCancellationToken: Server cancellation token value supplied by the caller for this operation.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: DisconnectAsync
    // Purpose: Executes the disconnect operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

        }

        try
        {
            _client.Client.Shutdown(SocketShutdown.Send);
        }
        catch
        {

        }

        try
        {
            _client.Close();
        }
        catch
        {

        }
    }

    // Method: WaitForNetworkBackgroundLoopsAsync
    // Purpose: Handles wait for network background loops work for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: WaitForBackgroundLoopAsync
    // Purpose: Handles wait for background loop work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - loop: Loop value supplied by the caller for this operation.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: DisconnectForMapServiceUnavailableAsync
    // Purpose: Executes the disconnect for map service unavailable operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // - reason: Reason value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: RequireCurrentPlayer
    // Purpose: Executes the require current player operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the player login record value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    public PlayerLoginRecord RequireCurrentPlayer()
    {
        return CurrentPlayer ?? throw new InvalidOperationException("World client has not entered the game world.");
    }

    // Method: OpenBankAsync
    // Purpose: Executes the open bank operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task OpenBankAsync(CancellationToken cancellationToken)
    {
        PlayerLoginRecord player = RequireCurrentPlayer();
        await SendAsync(WorldOpcode.SMSG_SHOW_BANK, WorldPacketBuilders.BuildShowBank(player.ClientGuid), _crypt, cancellationToken);
    }

    // Method: IsInChatChannel
    // Purpose: Validates or evaluates is in chat channel rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - channelName: Channel name value supplied by the caller for this operation.
    // Returns: Returns true when is in chat channel succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    public bool IsInChatChannel(string channelName)
    {
        return _chatChannels.Contains(ChatSystem.NormalizeChannelName(channelName));
    }

    // Method: JoinChatChannel
    // Purpose: Executes the join chat channel operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - channelName: Channel name value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    public void JoinChatChannel(string channelName)
    {
        _chatChannels.Add(ChatSystem.NormalizeChannelName(channelName));
    }

    // Method: LeaveChatChannel
    // Purpose: Executes the leave chat channel operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - channelName: Channel name value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    public void LeaveChatChannel(string channelName)
    {
        _chatChannels.Remove(ChatSystem.NormalizeChannelName(channelName));
    }

    // Method: AuthenticateAsync
    // Purpose: Executes the authenticate operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: RejectAuthenticationAsync
    // Purpose: Executes the reject authentication operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - responseCode: Response code value supplied by the caller for this operation.
    // - crypt: Crypt value supplied by the caller for this operation.
    // - exceptionMessage: Exception message value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task RejectAuthenticationAsync(AuthResponseCode responseCode, WorldHeaderCrypt? crypt, string exceptionMessage, CancellationToken cancellationToken)
    {
        await SendAsync(WorldOpcode.SMSG_AUTH_RESPONSE, WorldPacketBuilders.BuildAuthResponse(responseCode), crypt, cancellationToken);
        await AllowTerminalResponseDeliveryAsync(cancellationToken);
        throw new UnauthorizedAccessException(exceptionMessage);
    }

    // Method: StartBanMonitor
    // Purpose: Controls the start ban monitor lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: RunBanMonitorAsync
    // Purpose: Controls the run ban monitor lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

        }
        catch (Exception exception)
        {
            Logger.Write(LogType.WARNING, $"Ban monitor stopped for {RemoteEndPoint}: {exception.Message}", "WorldClientSession");
        }
    }

    // Method: StopBanMonitorAsync
    // Purpose: Controls the stop ban monitor lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: DisconnectIfBanBecameActiveAsync
    // Purpose: Executes the disconnect if ban became active operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous Boolean result that is true when disconnect if ban became active async succeeds or the requested condition is met.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: AllowTerminalResponseDeliveryAsync
    // Purpose: Executes the allow terminal response delivery operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task AllowTerminalResponseDeliveryAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TerminalAuthFailureDeliveryDelay, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {

        }
    }

    // Method: ProcessAuthenticatedPacketsAsync
    // Purpose: Executes the process authenticated packets operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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
                await HandleMovementPacketAsync(packet, cancellationToken);
                continue;
            }

            await QueueGameplayPacketAsync(packet, cancellationToken);
        }
    }

    // Method: QueueGameplayPacketAsync
    // Purpose: Executes the queue gameplay packet operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task QueueGameplayPacketAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        await _gameplayPacketQueue.Writer.WriteAsync(packet, cancellationToken);
    }

    // Method: StartGameplayPacketLoop
    // Purpose: Controls the start gameplay packet loop lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private void StartGameplayPacketLoop(CancellationToken cancellationToken)
    {
        _gameplayPacketLoop ??= Task.Run(() => ProcessGameplayPacketQueueAsync(cancellationToken), CancellationToken.None);
    }

    // Method: ProcessGameplayPacketQueueAsync
    // Purpose: Executes the process gameplay packet queue operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: DispatchAuthenticatedPacketAsync
    // Purpose: Executes the dispatch authenticated packet operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

                case WorldOpcode.CMSG_CREATURE_QUERY:
                    await HandleCreatureQueryAsync(packet, cancellationToken);
                    break;

                case WorldOpcode.CMSG_GAMEOBJECT_QUERY:
                    await HandleGameObjectQueryAsync(packet, cancellationToken);
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
                    await HandleMovementPacketAsync(packet, cancellationToken);
                    break;

                default:

                    if (_reportedUnhandledOpcodes.Add(packet.Opcode))
                    {
                        Logger.Write(LogType.TRACE, $"Unhandled world opcode from {RemoteEndPoint}: {packet.Opcode} (0x{(ushort)packet.Opcode:X4}), payload={packet.Payload.Length} byte(s). Future packets with this opcode will be accepted silently until a handler is implemented.", "WorldClientSession");
                    }
                    break;
            }
    }

    // Method: HandlePingAsync
    // Purpose: Handles handle ping work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task HandlePingAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        WorldPacketReader reader = new(packet.Payload);
        uint sequence = reader.ReadUInt32();
        uint clientLatencyMilliseconds = packet.Payload.Length >= 8 ? reader.ReadUInt32() : 0;
        _movementTiming.RecordClientLatency(clientLatencyMilliseconds);

        await SendAsync(WorldOpcode.SMSG_PONG, WorldPacketBuilders.BuildPong(sequence), _crypt, cancellationToken);
    }

    // Method: DelayCharacterEnumAfterLoginFailureAsync
    // Purpose: Executes the delay character enum after login failure operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

        }
        finally
        {
            _delayCharacterEnumUntilUtc = DateTimeOffset.MinValue;
        }
    }

    // Method: HandleCharacterEnumAsync
    // Purpose: Handles handle character enum work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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
            await SendAsync(WorldOpcode.SMSG_CHAR_ENUM, WorldPacketBuilders.BuildCharacterEnum([]), _crypt, cancellationToken);
        }
    }

    // Method: HandlePlayerLoginAsync
    // Purpose: Handles handle player login work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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
            ResetGameObjectVisibility();
            StartPlayerSaveTimer();
            await _playerEnteredWorldAsync(player, _currentMapOwnerServerName, cancellationToken);
            await SendWorldEntryPacketsAsync(player, cancellationToken);
            PlayerLoginRecord enteredPlayer = RequireCurrentPlayer();
            await RefreshVisiblePlayersAsync(enteredPlayer, force: true, cancellationToken);
            await RefreshOtherPlayerVisibilityForCurrentPlayerAsync(enteredPlayer, force: true, cancellationToken);

            _activePlayerCountChanged(_playerSessionRegistry.ActivePlayerCount);
            Logger.Write(LogType.SYSTEM, $"Player '{player.Name}' ({player.Guid}) entered world map={player.Map}, zone={player.Zone} through {mapAvailability.OwnerServerName}.", "WorldClientSession");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await StopPlayerSaveTimerAsync();
            CurrentPlayer = null;
            CurrentMovement = null;
            _currentMapOwnerServerName = string.Empty;
            ResetMapServiceMovementRoute();
            ResetGameObjectVisibility();
            _playerSessionRegistry.Unregister(player, this);
            await _characterRepository.SetCharacterOnlineAsync(player.Guid, false, CancellationToken.None);

            await SendMapUnavailableLoginFailedAsync(player, mapAvailability, $"Player login failed while entering world for '{player.Name}' ({player.Guid}) on map={player.Map} through {mapAvailability.OwnerServerName}: {exception.Message}", cancellationToken);
        }
    }

    // Method: SendCharacterLoginFailedAsync
    // Purpose: Handles send character login failed work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - failureCode: Failure code value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task SendCharacterLoginFailedAsync(CharacterLoginFailureCode failureCode, CancellationToken cancellationToken)
    {

        await SendAsync(WorldOpcode.SMSG_CHARACTER_LOGIN_FAILED, WorldPacketBuilders.BuildCharacterLoginFailed(failureCode), _crypt, cancellationToken);
        MarkCharacterEnumDelayWindow();
        await AllowCharacterLoginFailureDeliveryAsync(cancellationToken);
    }

    // Method: SendCharacterLoginFailedWithReasonAsync
    // Purpose: Handles send character login failed with reason work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - failureCode: Failure code value supplied by the caller for this operation.
    // - reason: Reason value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task SendCharacterLoginFailedWithReasonAsync(CharacterLoginFailureCode failureCode, string reason, CancellationToken cancellationToken)
    {
        Logger.Write(LogType.WARNING, $"{reason} Client failure code: {failureCode}.", "WorldClientSession");
        await SendCharacterLoginFailedAsync(failureCode, cancellationToken);
    }

    // Method: SendMapUnavailableLoginFailedAsync
    // Purpose: Handles send map unavailable login failed work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - mapAvailability: Map availability value supplied by the caller for this operation.
    // - reason: Reason value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: MarkCharacterEnumDelayWindow
    // Purpose: Executes the mark character enum delay window operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private void MarkCharacterEnumDelayWindow()
    {
        _delayCharacterEnumUntilUtc = DateTimeOffset.UtcNow.Add(CharacterLoginFailureDeliveryDelay);
    }

    // Method: AllowCharacterLoginFailureDeliveryAsync
    // Purpose: Executes the allow character login failure delivery operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task AllowCharacterLoginFailureDeliveryAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(CharacterLoginFailureDeliveryDelay, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {

        }
    }

    // Method: ResolveMapAvailabilityFailureCode
    // Purpose: Retrieves resolve map availability failure code data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - mapAvailability: Map availability value supplied by the caller for this operation.
    // Returns: Returns the character login failure code value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private static CharacterLoginFailureCode ResolveMapAvailabilityFailureCode(MapAvailabilityResult mapAvailability)
    {
        return mapAvailability.RequiresInstanceServer ? CharacterLoginFailureCode.NoInstances : CharacterLoginFailureCode.NoWorld;
    }

    // Method: ResolveMapAvailabilityTransferAbortReason
    // Purpose: Retrieves resolve map availability transfer abort reason data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - mapAvailability: Map availability value supplied by the caller for this operation.
    // Returns: Returns the transfer abort reason value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private static TransferAbortReason ResolveMapAvailabilityTransferAbortReason(MapAvailabilityResult mapAvailability)
    {
        return mapAvailability.RequiresInstanceServer ? TransferAbortReason.InstanceNotFound : TransferAbortReason.MapNotAllowed;
    }

    // Method: BuildMapUnavailableClientMessage
    // Purpose: Builds or writes build map unavailable client message output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - mapAvailability: Map availability value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private static string BuildMapUnavailableClientMessage(PlayerLoginRecord player, MapAvailabilityResult mapAvailability)
    {
        string serviceName = mapAvailability.RequiresInstanceServer ? "instance server" : "world server";
        return $"Unable to enter world: no {serviceName} is currently available for map {player.Map}.";
    }

    // Method: SendWorldEntryPacketsAsync
    // Purpose: Handles send world entry packets work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task SendWorldEntryPacketsAsync(PlayerLoginRecord player, CancellationToken cancellationToken)
    {
        DateTimeOffset localTime = DateTimeOffset.Now;

        await SendAsync(WorldOpcode.SMSG_LOGIN_VERIFY_WORLD, WorldPacketBuilders.BuildLoginVerifyWorld(player), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_ACCOUNT_DATA_TIMES, WorldPacketBuilders.BuildAccountDataTimes(), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_SET_REST_START, WorldPacketBuilders.BuildSetRestStart(localTime), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_BINDPOINTUPDATE, WorldPacketBuilders.BuildBindPointUpdate(player), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_TUTORIAL_FLAGS, WorldPacketBuilders.BuildTutorialFlags(player), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_INITIAL_SPELLS, WorldPacketBuilders.BuildInitialSpells(player), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_ACTION_BUTTONS, WorldPacketBuilders.BuildActionButtons(player), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_INITIALIZE_FACTIONS, WorldPacketBuilders.BuildInitializeFactions(player), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_LOGIN_SETTIMESPEED, WorldPacketBuilders.BuildLoginSetTimeSpeed(localTime), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_UPDATE_OBJECT, WorldPacketBuilders.BuildPlayerCreateUpdate(player), _crypt, cancellationToken);
        await RefreshVisibleGameObjectsAsync(player, force: true, cancellationToken);
        await RefreshVisibleCreaturesAsync(player, force: true, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_NAME_QUERY_RESPONSE, WorldPacketBuilders.BuildNameQueryResponse(new CharacterNameQueryResult(player.Guid, player.Name, player.Race, player.Gender, player.Class)), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_MOTD, WorldPacketBuilders.BuildMessageOfTheDay(MessageOfTheDay), _crypt, cancellationToken);
        await JoinDefaultChatChannelsAsync(cancellationToken);
    }

    // Method: ForwardPacketToMapServiceAsync
    // Purpose: Executes the forward packet to map service operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous Boolean result that is true when forward packet to map service async succeeds or the requested condition is met.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: HandleMovementPacketAsync
    // Purpose: Handles handle movement packet work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private Task HandleMovementPacketAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        PlayerLoginRecord? player = CurrentPlayer;
        string ownerServerName = _currentMapOwnerServerName;
        if (player is null || string.IsNullOrWhiteSpace(ownerServerName))
        {
            return Task.CompletedTask;
        }

        if (!WorldMovementPacketParser.TryReadMovementState(player, packet.Opcode, packet.Payload, out PlayerMovementState? movement))
        {
            if (_reportedUnhandledOpcodes.Add(packet.Opcode))
            {
                Logger.Write(LogType.TRACE, $"Accepted movement opcode {packet.Opcode} from {RemoteEndPoint}, but no position state could be parsed from payload={packet.Payload.Length} byte(s). Future packets with this opcode will be accepted silently.", "WorldClientSession");
            }

            return Task.CompletedTask;
        }

        PlayerMovementState? previousMovement = CurrentMovement;
        _movementTiming.RecordIncomingMovement(movement.LastUpdatedUtc);
        WorldMovementDiagnostics.LogIncomingMovement(packet.Opcode, packet.Payload.Length, player, movement, previousMovement, RemoteEndPoint);

        ApplyMovementState(movement);
        PlayerLoginRecord updatedPlayer = RequireCurrentPlayer();

        QueueMovementBroadcastToNearbyPlayers(packet, movement);

        if (ShouldRouteMovementToMapService(movement))
        {
            QueueMapServiceMovement(updatedPlayer, ownerServerName, movement);
        }

        QueueMovementVisibilityRefresh(updatedPlayer, cancellationToken);
        return Task.CompletedTask;
    }

    // Method: ShouldQueueMovementVisibilityRefresh
    // Purpose: Checks whether deferred visibility work is due before scheduling a background task.
    // Parameters: none.
    // Returns: Returns true when at least one movement-driven visibility refresh interval has elapsed.
    // Notes: This prevents every movement packet from creating a background task while still keeping player visibility responsive.
    private bool ShouldQueueMovementVisibilityRefresh()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        TimeSpan playerVisibilityRefreshInterval = _movementTiming.GetPlayerVisibilityRefreshInterval();
        return now - _lastPlayerVisibilityRefreshUtc >= playerVisibilityRefreshInterval ||
            now - _lastGameObjectVisibilityRefreshUtc >= GameObjectVisibilityRefreshInterval ||
            now - _lastCreatureVisibilityRefreshUtc >= CreatureVisibilityRefreshInterval;
    }

    // Method: QueueMovementVisibilityRefresh
    // Purpose: Schedules player, creature, and game object visibility work away from the movement hot path.
    // Parameters:
    // - player: Player snapshot that caused the refresh request.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: none.
    // Notes: Movement packets are latency-sensitive. They are broadcast before this slower visibility work runs.
    private void QueueMovementVisibilityRefresh(PlayerLoginRecord player, CancellationToken cancellationToken)
    {
        if (_disconnect.IsCancellationRequested || cancellationToken.IsCancellationRequested || !ShouldQueueMovementVisibilityRefresh())
        {
            return;
        }

        if (Interlocked.Exchange(ref _movementVisibilityRefreshQueued, 1) == 1)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await RefreshVisiblePlayersAsync(player, force: false, cancellationToken);

                PlayerLoginRecord? currentPlayer = CurrentPlayer;
                if (currentPlayer is null)
                {
                    return;
                }

                await RefreshOtherPlayerVisibilityForCurrentPlayerAsync(currentPlayer, force: false, cancellationToken);
                await RefreshVisibleGameObjectsAsync(currentPlayer, force: false, cancellationToken);
                await RefreshVisibleCreaturesAsync(currentPlayer, force: false, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || _disconnect.IsCancellationRequested)
            {

            }
            catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException or InvalidOperationException)
            {
                Logger.Write(LogType.TRACE, $"Movement visibility refresh stopped for {RemoteEndPoint}: {exception.Message}", "WorldClientSession");
            }
            catch (Exception exception)
            {
                Logger.Write(LogType.WARNING, $"Movement visibility refresh failed for {RemoteEndPoint}: {exception.Message}", "WorldClientSession");
            }
            finally
            {
                Interlocked.Exchange(ref _movementVisibilityRefreshQueued, 0);
            }
        }, CancellationToken.None);
    }

    // Method: QueueMapServiceMovement
    // Purpose: Executes the queue map service movement operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // - movement: Movement value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private void QueueMapServiceMovement(PlayerLoginRecord player, string ownerServerName, PlayerMovementState movement)
    {
        if (_disconnect.IsCancellationRequested || string.IsNullOrWhiteSpace(ownerServerName))
        {
            return;
        }

        _mapServiceMovementQueue.Writer.TryWrite(new QueuedMapServiceMovement(player, ownerServerName, movement));
    }

    // Method: StartMapServiceMovementRouteLoop
    // Purpose: Controls the start map service movement route loop lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private void StartMapServiceMovementRouteLoop(CancellationToken cancellationToken)
    {
        _mapServiceMovementRouteLoop ??= Task.Run(() => ProcessMapServiceMovementRouteQueueAsync(cancellationToken), CancellationToken.None);
    }

    // Method: ProcessMapServiceMovementRouteQueueAsync
    // Purpose: Executes the process map service movement route queue operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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
                TimeSpan routeDuration = DateTimeOffset.UtcNow - routeStartedUtc;
                _movementTiming.RecordMapServiceRouteDuration(routeDuration);
                WorldMovementDiagnostics.LogMapServiceMovementRoute(
                    latest.Player,
                    latest.OwnerServerName,
                    latest.Movement,
                    routeStartedUtc,
                    routeDuration,
                    RemoteEndPoint);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {

        }
        catch (Exception exception)
        {
            Logger.Write(LogType.WARNING, $"Map-service movement route writer stopped for {RemoteEndPoint}: {exception.Message}", "WorldClientSession");
        }
    }

    // Method: ResetMapServiceMovementRoute
    // Purpose: Executes the reset map service movement route operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private void ResetMapServiceMovementRoute()
    {
        _lastMapServiceMovementRouteUtc = DateTimeOffset.MinValue;
        _lastMapServiceMovementRouteMap = 0;
        _lastMapServiceMovementRouteZone = 0;
        _hasLastMapServiceMovementRoute = false;
    }

    // Method: ShouldRouteMovementToMapService
    // Purpose: Validates or evaluates should route movement to map service rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - movement: Movement value supplied by the caller for this operation.
    // Returns: Returns true when should route movement to map service succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private bool ShouldRouteMovementToMapService(PlayerMovementState movement)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        bool mapOrZoneChanged = !_hasLastMapServiceMovementRoute ||
            _lastMapServiceMovementRouteMap != movement.Map ||
            _lastMapServiceMovementRouteZone != movement.Zone;

        TimeSpan routeInterval = _movementTiming.GetMapServiceRouteInterval();
        if (!mapOrZoneChanged && now - _lastMapServiceMovementRouteUtc < routeInterval)
        {
            return false;
        }

        _lastMapServiceMovementRouteUtc = now;
        _lastMapServiceMovementRouteMap = movement.Map;
        _lastMapServiceMovementRouteZone = movement.Zone;
        _hasLastMapServiceMovementRoute = true;
        return true;
    }

    // Method: ApplyMovementState
    // Purpose: Applies apply movement state changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - movement: Movement value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ApplyMovementToPlayerRecord
    // Purpose: Applies apply movement to player record changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - movement: Movement value supplied by the caller for this operation.
    // Returns: Returns the player login record value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: SynchronizeCurrentPlayerRecordFromMovement
    // Purpose: Executes the synchronize current player record from movement operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: QueueMovementBroadcastToNearbyPlayers
    // Purpose: Executes the queue movement broadcast to nearby players operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - movement: Movement value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
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
        int recipientCount = 0;
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
            if (recipient.TryQueueMovementPacket(packet.Opcode, payload))
            {
                recipientCount++;
            }
        }

        _movementTiming.RecordVisibleRecipientCount(recipientCount);
    }

    // Method: TryQueueMovementPacket
    // Purpose: Executes the try queue movement packet operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - opcode: Opcode value supplied by the caller for this operation.
    // - bytepayload: Bytepayload value supplied by the caller for this operation.
    // Returns: Returns true when try queue movement packet succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private bool TryQueueMovementPacket(WorldOpcode opcode, byte[] payload)
    {
        if (_crypt is null || _disconnect.IsCancellationRequested)
        {
            return false;
        }

        return _movementBroadcastQueue.Writer.TryWrite(new QueuedMovementPacket(opcode, payload));
    }

    // Method: StartMovementBroadcastLoop
    // Purpose: Controls the start movement broadcast loop lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private void StartMovementBroadcastLoop(CancellationToken cancellationToken)
    {
        _movementBroadcastLoop ??= Task.Run(() => ProcessMovementBroadcastQueueAsync(cancellationToken), CancellationToken.None);
    }

    // Method: ProcessMovementBroadcastQueueAsync
    // Purpose: Executes the process movement broadcast queue operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

        }
        catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException or InvalidOperationException)
        {
            Logger.Write(LogType.TRACE, $"Movement broadcast writer stopped for {RemoteEndPoint}: {exception.Message}", "WorldClientSession");
        }
    }

    // Method: HandleZoneUpdateAsync
    // Purpose: Handles handle zone update work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

            PlayerMovementState? movement = CurrentMovement;
            string ownerServerName = _currentMapOwnerServerName;
            if (movement is not null && !string.IsNullOrWhiteSpace(ownerServerName) && ShouldRouteMovementToMapService(movement))
            {
                QueueMapServiceMovement(RequireCurrentPlayer(), ownerServerName, movement);
            }
        }

        PlayerLoginRecord updatedPlayer = RequireCurrentPlayer();
        await RefreshVisiblePlayersAsync(updatedPlayer, force: true, cancellationToken);
        await RefreshOtherPlayerVisibilityForCurrentPlayerAsync(updatedPlayer, force: true, cancellationToken);
        await RefreshVisibleGameObjectsAsync(updatedPlayer, force: true, cancellationToken);
        await RefreshVisibleCreaturesAsync(updatedPlayer, force: true, cancellationToken);
        await ForwardPacketToMapServiceAsync(packet, cancellationToken);
    }

    // Method: ResetGameObjectVisibility
    // Purpose: Executes the reset game object visibility operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private void ResetGameObjectVisibility()
    {
        _visibleGameObjectClientGuids.Clear();
        _visibleCreatureClientGuids.Clear();
        _visiblePlayerGuids.Clear();
        _lastGameObjectVisibilityRefreshUtc = DateTimeOffset.MinValue;
        _lastCreatureVisibilityRefreshUtc = DateTimeOffset.MinValue;
        _lastPlayerVisibilityRefreshUtc = DateTimeOffset.MinValue;
    }

    // Method: RefreshVisiblePlayersAsync
    // Purpose: Refreshes player object visibility for this client.
    // Parameters:
    // - player: Current player used as the visibility source.
    // - force: True when throttling should be bypassed.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    private async Task RefreshVisiblePlayersAsync(PlayerLoginRecord player, bool force, CancellationToken cancellationToken)
    {
        if (_crypt is null || CurrentPlayer is null)
        {
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        TimeSpan playerVisibilityRefreshInterval = _movementTiming.GetPlayerVisibilityRefreshInterval();
        if (!force && now - _lastPlayerVisibilityRefreshUtc < playerVisibilityRefreshInterval)
        {
            return;
        }

        _lastPlayerVisibilityRefreshUtc = now;
        await _visibilityLock.WaitAsync(cancellationToken);
        try
        {
            PlayerLoginRecord sourcePlayer = CurrentPlayer ?? player;
            (uint sourceMap, float sourceX, float sourceY, float sourceZ) = ResolveCurrentVisibilityPosition(sourcePlayer);
            if (!IsFiniteWorldPosition(sourceX, sourceY, sourceZ))
            {
                return;
            }

            Dictionary<uint, PlayerLoginRecord> retainedCandidates = [];
            Dictionary<uint, PlayerLoginRecord> createCandidates = [];
            Dictionary<uint, float> visibleDistances = [];

            foreach (WorldClientSession session in _playerSessionRegistry.SnapshotSessions())
            {
                if (ReferenceEquals(session, this))
                {
                    continue;
                }

                PlayerLoginRecord? targetPlayer = session.CurrentPlayer;
                if (targetPlayer is null || targetPlayer.Guid == sourcePlayer.Guid)
                {
                    continue;
                }

                PlayerLoginRecord targetSnapshot = session.CreateCurrentPlayerSnapshot(targetPlayer);
                if (!CanSeePlayer(sourceMap, sourceX, sourceY, session, targetSnapshot, PlayerVisibilityUnloadDistanceSquared, out float distanceSquared))
                {
                    continue;
                }

                retainedCandidates[targetSnapshot.Guid] = targetSnapshot;
                visibleDistances[targetSnapshot.Guid] = distanceSquared;
                if (distanceSquared <= PlayerVisibilityDistanceSquared)
                {
                    createCandidates[targetSnapshot.Guid] = targetSnapshot;
                }
            }

            uint[] removeGuids = _visiblePlayerGuids
                .Where(guid => !retainedCandidates.ContainsKey(guid))
                .ToArray();

            foreach (uint removeGuid in removeGuids)
            {
                _visiblePlayerGuids.Remove(removeGuid);
                await SendAsync(WorldOpcode.SMSG_DESTROY_OBJECT, WorldPacketBuilders.BuildDestroyObject(CharacterGuid.ToClientGuid(removeGuid)), _crypt, cancellationToken);
            }

            PlayerLoginRecord[] createPlayers = [.. createCandidates.Values
                .Where(candidate => !_visiblePlayerGuids.Contains(candidate.Guid))
                .OrderBy(candidate => visibleDistances.TryGetValue(candidate.Guid, out float distanceSquared) ? distanceSquared : float.MaxValue)
                .Take(MaximumPlayerCreateUpdatesPerRefresh)];

            foreach (PlayerLoginRecord visiblePlayer in createPlayers)
            {
                await SendVisiblePlayerCreateAsync(visiblePlayer, cancellationToken);
                _visiblePlayerGuids.Add(visiblePlayer.Guid);
            }

            if (createPlayers.Length != 0 || removeGuids.Length != 0)
            {
                Logger.Write(
                    LogType.TRACE,
                    $"Refreshed player visibility for '{sourcePlayer.Name}' ({sourcePlayer.Guid}): created={createPlayers.Length}, removed={removeGuids.Length}, visible={_visiblePlayerGuids.Count}.",
                    "WorldClientSession");
            }
        }
        finally
        {
            _visibilityLock.Release();
        }
    }

    // Method: RefreshOtherPlayerVisibilityForCurrentPlayerAsync
    // Purpose: Gives nearby sessions a chance to create or destroy this player after login, movement, or zone changes.
    // Parameters:
    // - player: Player whose visibility changed.
    // - force: True when neighboring sessions should bypass visibility throttling.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    private async Task RefreshOtherPlayerVisibilityForCurrentPlayerAsync(PlayerLoginRecord player, bool force, CancellationToken cancellationToken)
    {
        foreach (WorldClientSession session in _playerSessionRegistry.SnapshotSessions())
        {
            if (ReferenceEquals(session, this))
            {
                continue;
            }

            PlayerLoginRecord? targetPlayer = session.CurrentPlayer;
            if (targetPlayer is null || targetPlayer.Guid == player.Guid)
            {
                continue;
            }

            await session.RefreshVisiblePlayersAsync(targetPlayer, force, cancellationToken);
        }
    }

    // Method: RemovePlayerFromVisibleSessionsAsync
    // Purpose: Sends destroy updates to clients that previously had this player visible.
    // Parameters:
    // - player: Player leaving the world.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    private async Task RemovePlayerFromVisibleSessionsAsync(PlayerLoginRecord player, CancellationToken cancellationToken)
    {
        foreach (WorldClientSession session in _playerSessionRegistry.SnapshotSessions())
        {
            if (ReferenceEquals(session, this))
            {
                continue;
            }

            await session.RemoveVisiblePlayerAsync(player.Guid, cancellationToken);
        }
    }

    // Method: RemoveVisiblePlayerAsync
    // Purpose: Removes one visible player from this client if it had been created before.
    // Parameters:
    // - playerGuid: Low player GUID to remove.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    private async Task RemoveVisiblePlayerAsync(uint playerGuid, CancellationToken cancellationToken)
    {
        if (_crypt is null || playerGuid == 0)
        {
            return;
        }

        await _visibilityLock.WaitAsync(cancellationToken);
        try
        {
            if (!_visiblePlayerGuids.Remove(playerGuid))
            {
                return;
            }

            await SendAsync(WorldOpcode.SMSG_DESTROY_OBJECT, WorldPacketBuilders.BuildDestroyObject(CharacterGuid.ToClientGuid(playerGuid)), _crypt, cancellationToken);
        }
        finally
        {
            _visibilityLock.Release();
        }
    }

    // Method: SendVisiblePlayerCreateAsync
    // Purpose: Sends one visible player name cache update and object create update to this client.
    // Parameters:
    // - visiblePlayer: Player that should become visible.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    private async Task SendVisiblePlayerCreateAsync(PlayerLoginRecord visiblePlayer, CancellationToken cancellationToken)
    {
        CharacterNameQueryResult nameQuery = new(visiblePlayer.Guid, visiblePlayer.Name, visiblePlayer.Race, visiblePlayer.Gender, visiblePlayer.Class);
        await SendAsync(WorldOpcode.SMSG_NAME_QUERY_RESPONSE, WorldPacketBuilders.BuildNameQueryResponse(nameQuery), _crypt, cancellationToken);
        await SendAsync(WorldOpcode.SMSG_UPDATE_OBJECT, WorldPacketBuilders.BuildVisiblePlayerCreateUpdate(visiblePlayer), _crypt, cancellationToken);
    }

    // Method: CreateCurrentPlayerSnapshot
    // Purpose: Builds a player record snapshot that includes the latest parsed movement state.
    // Parameters:
    // - player: Player record to snapshot.
    // Returns: Returns a player record using the latest movement position when one is available.
    private PlayerLoginRecord CreateCurrentPlayerSnapshot(PlayerLoginRecord player)
    {
        PlayerMovementState? movement = CurrentMovement;
        return movement is null ? player : ApplyMovementToPlayerRecord(player, movement);
    }

    // Method: CanSeePlayer
    // Purpose: Checks map ownership and distance before one session creates another player object.
    // Parameters:
    // - sourceMap: Current map for the receiving client.
    // - sourceX: Current X position for the receiving client.
    // - sourceY: Current Y position for the receiving client.
    // - targetSession: Session containing the candidate visible player.
    // - targetPlayer: Candidate visible player.
    // - maximumDistanceSquared: Maximum allowed two-dimensional distance squared.
    // - distanceSquared: Receives the calculated distance squared when map and position checks pass.
    // Returns: Returns true when the target player should be visible to the source player.
    private bool CanSeePlayer(
        uint sourceMap,
        float sourceX,
        float sourceY,
        WorldClientSession targetSession,
        PlayerLoginRecord targetPlayer,
        float maximumDistanceSquared,
        out float distanceSquared)
    {
        distanceSquared = float.MaxValue;
        (uint targetMap, float targetX, float targetY, float targetZ) = targetSession.ResolveCurrentVisibilityPosition(targetPlayer);
        if (sourceMap != targetMap || !IsFiniteWorldPosition(targetX, targetY, targetZ))
        {
            return false;
        }

        if (!string.Equals(_currentMapOwnerServerName, targetSession.CurrentMapOwnerServerName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        distanceSquared = CalculateDistanceSquared2D(sourceX, sourceY, targetX, targetY);
        return distanceSquared <= maximumDistanceSquared;
    }

    // Method: RefreshVisibleCreaturesAsync
    // Purpose: Executes the refresh visible creatures operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - force: Force value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task RefreshVisibleCreaturesAsync(PlayerLoginRecord player, bool force, CancellationToken cancellationToken)
    {
        if (_crypt is null)
        {
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (!force && now - _lastCreatureVisibilityRefreshUtc < CreatureVisibilityRefreshInterval)
        {
            return;
        }

        _lastCreatureVisibilityRefreshUtc = now;
        WorldTemplateDataStore worldData = _worldTemplateDataResolver();
        if (worldData.CreatureSpawnCount == 0 || worldData.CreatureTemplateCount == 0)
        {
            return;
        }

        (uint map, float x, float y, float z) = ResolveCurrentVisibilityPosition(player);
        if (map > ushort.MaxValue || !IsFiniteWorldPosition(x, y, z))
        {
            return;
        }

        ushort mapId = unchecked((ushort)map);
        IReadOnlyList<CreatureSpawnRecord> mapSpawns = worldData.GetCreatureSpawnsForMap(mapId);
        if (mapSpawns.Count == 0)
        {
            return;
        }

        List<CreatureClientCreateRecord> nearbyCreates = [];
        List<ulong> removeClientGuids = [];

        foreach (KeyValuePair<ulong, uint> visible in _visibleCreatureClientGuids.ToArray())
        {
            if (!worldData.TryGetCreatureSpawn(visible.Value, out CreatureSpawnRecord visibleSpawn) ||
                visibleSpawn.Map != mapId ||
                !worldData.TryGetCreatureTemplate(visibleSpawn.Entry, out CreatureTemplateRecord visibleTemplate) ||
                !CreatureDataValidation.IsClientVisibleCreature(visibleSpawn, visibleTemplate) ||
                CalculateDistanceSquared2D(x, y, visibleSpawn.PositionX, visibleSpawn.PositionY) > CreatureVisibilityUnloadDistanceSquared)
            {
                removeClientGuids.Add(visible.Key);
            }
        }

        foreach (ulong clientGuid in removeClientGuids)
        {
            _visibleCreatureClientGuids.Remove(clientGuid);
            await SendAsync(WorldOpcode.SMSG_DESTROY_OBJECT, WorldPacketBuilders.BuildDestroyObject(clientGuid), _crypt, cancellationToken);
        }

        foreach (CreatureSpawnRecord spawn in mapSpawns)
        {
            if (CalculateDistanceSquared2D(x, y, spawn.PositionX, spawn.PositionY) > CreatureVisibilityDistanceSquared)
            {
                continue;
            }

            if (!worldData.TryGetCreatureTemplate(spawn.Entry, out CreatureTemplateRecord template) ||
                !CreatureDataValidation.IsClientVisibleCreature(spawn, template))
            {
                continue;
            }

            ulong clientGuid = CharacterGuid.ToCreatureGuid(spawn.Guid, spawn.Entry);
            if (clientGuid == 0 || _visibleCreatureClientGuids.ContainsKey(clientGuid))
            {
                continue;
            }

            nearbyCreates.Add(new CreatureClientCreateRecord(spawn, template));
        }

        if (nearbyCreates.Count == 0)
        {
            return;
        }

        CreatureClientCreateRecord[] selectedCreates = [.. nearbyCreates
            .OrderBy(record => CalculateDistanceSquared2D(x, y, record.Spawn.PositionX, record.Spawn.PositionY))
            .Take(MaximumCreatureCreateUpdatesPerRefresh)];

        byte[] payload = WorldPacketBuilders.BuildCreatureCreateUpdate(selectedCreates);
        if (payload.Length == 0)
        {
            return;
        }

        await SendAsync(WorldOpcode.SMSG_UPDATE_OBJECT, payload, _crypt, cancellationToken);
        foreach (CreatureClientCreateRecord record in selectedCreates)
        {
            ulong clientGuid = CharacterGuid.ToCreatureGuid(record.Spawn.Guid, record.Spawn.Entry);
            _visibleCreatureClientGuids[clientGuid] = record.Spawn.Guid;
        }

        Logger.Write(
            LogType.TRACE,
            $"Sent {selectedCreates.Length} creature create update(s) to player '{player.Name}' ({player.Guid}) near map={mapId}, x={x:F2}, y={y:F2}.",
            "WorldClientSession");
    }

    // Method: RefreshVisibleGameObjectsAsync
    // Purpose: Executes the refresh visible game objects operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - force: Force value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task RefreshVisibleGameObjectsAsync(PlayerLoginRecord player, bool force, CancellationToken cancellationToken)
    {
        if (_crypt is null)
        {
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (!force && now - _lastGameObjectVisibilityRefreshUtc < GameObjectVisibilityRefreshInterval)
        {
            return;
        }

        _lastGameObjectVisibilityRefreshUtc = now;
        WorldTemplateDataStore worldData = _worldTemplateDataResolver();
        if (worldData.GameObjectSpawnCount == 0 || worldData.GameObjectTemplateCount == 0)
        {
            return;
        }

        (uint map, float x, float y, float z) = ResolveCurrentVisibilityPosition(player);
        if (map > ushort.MaxValue || !IsFiniteWorldPosition(x, y, z))
        {
            return;
        }

        ushort mapId = unchecked((ushort)map);
        IReadOnlyList<GameObjectSpawnRecord> mapSpawns = worldData.GetGameObjectSpawnsForMap(mapId);
        if (mapSpawns.Count == 0)
        {
            return;
        }

        List<GameObjectClientCreateRecord> nearbyCreates = [];
        List<ulong> removeClientGuids = [];

        foreach (KeyValuePair<ulong, uint> visible in _visibleGameObjectClientGuids.ToArray())
        {
            if (!worldData.TryGetGameObjectSpawn(visible.Value, out GameObjectSpawnRecord visibleSpawn) ||
                visibleSpawn.Map != mapId ||
                !worldData.TryGetGameObjectTemplate(visibleSpawn.Entry, out GameObjectTemplateRecord visibleTemplate) ||
                !GameObjectDataValidation.IsClientVisibleStaticGameObject(visibleSpawn, visibleTemplate) ||
                CalculateDistanceSquared2D(x, y, visibleSpawn.PositionX, visibleSpawn.PositionY) > GameObjectVisibilityUnloadDistanceSquared)
            {
                removeClientGuids.Add(visible.Key);
            }
        }

        foreach (ulong clientGuid in removeClientGuids)
        {
            _visibleGameObjectClientGuids.Remove(clientGuid);
            await SendAsync(WorldOpcode.SMSG_DESTROY_OBJECT, WorldPacketBuilders.BuildDestroyObject(clientGuid), _crypt, cancellationToken);
        }

        foreach (GameObjectSpawnRecord spawn in mapSpawns)
        {
            if (CalculateDistanceSquared2D(x, y, spawn.PositionX, spawn.PositionY) > GameObjectVisibilityDistanceSquared)
            {
                continue;
            }

            if (!worldData.TryGetGameObjectTemplate(spawn.Entry, out GameObjectTemplateRecord template) ||
                !GameObjectDataValidation.IsClientVisibleStaticGameObject(spawn, template))
            {
                continue;
            }

            ulong clientGuid = CharacterGuid.ToGameObjectGuid(spawn.Guid, spawn.Entry);
            if (clientGuid == 0)
            {
                continue;
            }

            if (_visibleGameObjectClientGuids.ContainsKey(clientGuid))
            {
                continue;
            }

            nearbyCreates.Add(new GameObjectClientCreateRecord(spawn, template));
        }

        if (nearbyCreates.Count == 0)
        {
            return;
        }

        GameObjectClientCreateRecord[] selectedCreates = [.. nearbyCreates
            .OrderBy(record => CalculateDistanceSquared2D(x, y, record.Spawn.PositionX, record.Spawn.PositionY))
            .Take(MaximumGameObjectCreateUpdatesPerRefresh)];

        byte[] payload = WorldPacketBuilders.BuildGameObjectCreateUpdate(selectedCreates);
        if (payload.Length == 0)
        {
            return;
        }

        await SendAsync(WorldOpcode.SMSG_UPDATE_OBJECT, payload, _crypt, cancellationToken);
        foreach (GameObjectClientCreateRecord record in selectedCreates)
        {
            ulong clientGuid = CharacterGuid.ToGameObjectGuid(record.Spawn.Guid, record.Spawn.Entry);
            _visibleGameObjectClientGuids[clientGuid] = record.Spawn.Guid;
        }

        if (selectedCreates.Length != 0)
        {
            Logger.Write(
                LogType.TRACE,
                $"Sent {selectedCreates.Length} gameobject create update(s) to player '{player.Name}' ({player.Guid}) near map={mapId}, x={x:F2}, y={y:F2}.",
                "WorldClientSession");
        }
    }

    // Method: private
    // Purpose: Executes the private operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - Map: Map value supplied by the caller for this operation.
    // - X: X value supplied by the caller for this operation.
    // - Y: Y value supplied by the caller for this operation.
    // - Z: Z value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private (uint Map, float X, float Y, float Z) ResolveCurrentVisibilityPosition(PlayerLoginRecord player)
    {
        PlayerMovementState? movement = CurrentMovement;
        if (movement is not null)
        {
            return (movement.Map, movement.PositionX, movement.PositionY, movement.PositionZ);
        }

        return (player.Map, player.PositionX, player.PositionY, player.PositionZ);
    }

    // Method: IsFiniteWorldPosition
    // Purpose: Validates or evaluates is finite world position rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - x: X value supplied by the caller for this operation.
    // - y: Y value supplied by the caller for this operation.
    // - z: Z value supplied by the caller for this operation.
    // Returns: Returns true when is finite world position succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private static bool IsFiniteWorldPosition(float x, float y, float z)
    {
        return float.IsFinite(x) && float.IsFinite(y) && float.IsFinite(z);
    }

    // Method: CalculateDistanceSquared2D
    // Purpose: Calculates calculate distance squared2 D values for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - x1: X1 value supplied by the caller for this operation.
    // - y1: Y1 value supplied by the caller for this operation.
    // - x2: X2 value supplied by the caller for this operation.
    // - y2: Y2 value supplied by the caller for this operation.
    // Returns: Returns the float value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private static float CalculateDistanceSquared2D(float x1, float y1, float x2, float y2)
    {
        float dx = x1 - x2;
        float dy = y1 - y2;
        return (dx * dx) + (dy * dy);
    }

    // Method: StartPlayerSaveTimer
    // Purpose: Controls the start player save timer lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private void StartPlayerSaveTimer()
    {
        _playerSaveCancellation?.Cancel();
        _playerSaveCancellation?.Dispose();

        _playerSaveCancellation = CancellationTokenSource.CreateLinkedTokenSource(_disconnect.Token);
        _playerSaveLoop = RunPlayerSaveTimerAsync(_playerSaveCancellation.Token);
    }

    // Method: RunPlayerSaveTimerAsync
    // Purpose: Controls the run player save timer lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: StopPlayerSaveTimerAsync
    // Purpose: Controls the stop player save timer lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: SaveCurrentPlayerAsync
    // Purpose: Applies save current player changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - force: Force value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: IsWithinMovementBroadcastRange
    // Purpose: Validates or evaluates is within movement broadcast range rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - source: Source value supplied by the caller for this operation.
    // - targetMovement: Target movement value supplied by the caller for this operation.
    // - targetPlayer: Target player value supplied by the caller for this operation.
    // Returns: Returns true when is within movement broadcast range succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: SaturatingSeconds
    // Purpose: Executes the saturating seconds operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - elapsed: Elapsed value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private static uint SaturatingSeconds(TimeSpan elapsed)
    {
        if (elapsed <= TimeSpan.Zero)
        {
            return 0;
        }

        return elapsed.TotalSeconds >= uint.MaxValue ? uint.MaxValue : (uint)elapsed.TotalSeconds;
    }

    // Method: AddClamped
    // Purpose: Applies add clamped changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // - addition: Addition value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private static uint AddClamped(uint value, uint addition)
    {
        ulong result = (ulong)value + addition;
        return result > uint.MaxValue ? uint.MaxValue : (uint)result;
    }

    // Method: HandleSwapInvItemAsync
    // Purpose: Handles handle swap inv item work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: HandleSwapItemAsync
    // Purpose: Handles handle swap item work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: HandleAutoEquipItemAsync
    // Purpose: Handles handle auto equip item work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: HandleAutoEquipItemSlotAsync
    // Purpose: Handles handle auto equip item slot work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: HandleAutoStoreBagItemAsync
    // Purpose: Handles handle auto store bag item work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: HandleSplitItemAsync
    // Purpose: Handles handle split item work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

        if (!TryResolveClientInventoryLocation(new InventoryClientPosition(destinationBag, destinationSlot), inventory, out InventoryStorageLocation destinationLocation))
        {
            if (destinationBag != ClientBackpackBag || destinationSlot != ClientBackpackBag || !TryFindFirstFreeBackpackLocation(inventory, out destinationLocation))
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

    // Method: SwapInventoryLocationsAsync
    // Purpose: Executes the swap inventory locations operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - firstClientPosition: First client position value supplied by the caller for this operation.
    // - secondClientPosition: Second client position value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: MoveOrSwapItemAsync
    // Purpose: Executes the move or swap item operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - sourceItem: Source item value supplied by the caller for this operation.
    // - sourceLocation: Source location value supplied by the caller for this operation.
    // - destinationLocation: Destination location value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: ApplyInventoryPlacementsAsync
    // Purpose: Applies apply inventory placements changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - placements: Placements value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: ApplyInventoryStackSplitAsync
    // Purpose: Applies apply inventory stack split changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - sourceItem: Source item value supplied by the caller for this operation.
    // - destinationLocation: Destination location value supplied by the caller for this operation.
    // - splitCount: Split count value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task ApplyInventoryStackSplitAsync(PlayerInventoryItem sourceItem, InventoryStorageLocation destinationLocation, uint splitCount, CancellationToken cancellationToken)
    {
        PlayerLoginRecord player = RequireCurrentPlayer();
        HashSet<uint> knownItemGuids = [.. player.Inventory.Select(item => item.ItemGuid)];
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

        HashSet<uint> createdItemGuids = [.. refreshedInventory
            .Where(item => !knownItemGuids.Contains(item.ItemGuid))
            .Select(item => item.ItemGuid)];

        PlayerLoginRecord updatedPlayer = player with { Inventory = refreshedInventory };
        CurrentPlayer = updatedPlayer;
        _playerStateDirty = true;
        await SendAsync(WorldOpcode.SMSG_UPDATE_OBJECT, WorldPacketBuilders.BuildInventoryStateUpdate(updatedPlayer, createdItemGuids), _crypt, cancellationToken);
    }

    // Method: SendInventoryFailureAsync
    // Purpose: Handles send inventory failure work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - failureCode: Failure code value supplied by the caller for this operation.
    // - itemGuid: Item GUID identifier used to select the exact record, object, or runtime owner.
    // - itemGuid2: Item guid2 value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private Task SendInventoryFailureAsync(byte failureCode, ulong itemGuid, ulong itemGuid2, CancellationToken cancellationToken)
    {
        return SendAsync(WorldOpcode.SMSG_INVENTORY_CHANGE_FAILURE, WorldPacketBuilders.BuildInventoryChangeFailure(failureCode, itemGuid, itemGuid2), _crypt, cancellationToken);
    }

    // Method: TryResolveClientInventoryLocation
    // Purpose: Attempts to retrieve or parse try resolve client inventory location data without treating normal misses as failures.
    // Parameters:
    // - position: Position value supplied by the caller for this operation.
    // - inventory: Inventory value supplied by the caller for this operation.
    // - location: Location value supplied by the caller for this operation.
    // Returns: Returns true when try resolve client inventory location succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: FindItemAtLocation
    // Purpose: Retrieves find item at location data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - inventory: Inventory value supplied by the caller for this operation.
    // - location: Location value supplied by the caller for this operation.
    // Returns: Returns the player inventory item? value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private static PlayerInventoryItem? FindItemAtLocation(IReadOnlyList<PlayerInventoryItem> inventory, InventoryStorageLocation location)
    {
        return inventory.FirstOrDefault(item => item.BagGuid == location.BagGuid && item.Slot == location.Slot);
    }

    // Method: CanPlaceItemAtLocation
    // Purpose: Validates or evaluates can place item at location rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - item: Item value supplied by the caller for this operation.
    // - location: Location value supplied by the caller for this operation.
    // - inventory: Inventory value supplied by the caller for this operation.
    // Returns: Returns true when can place item at location succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
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

        return slot is >= 81 and < 113;
    }

    // Method: TryResolveAutoEquipLocation
    // Purpose: Attempts to retrieve or parse try resolve auto equip location data without treating normal misses as failures.
    // Parameters:
    // - item: Item value supplied by the caller for this operation.
    // - inventory: Inventory value supplied by the caller for this operation.
    // - location: Location value supplied by the caller for this operation.
    // Returns: Returns true when try resolve auto equip location succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: TryFindFirstFreeBackpackLocation
    // Purpose: Executes the try find first free backpack location operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - inventory: Inventory value supplied by the caller for this operation.
    // - location: Location value supplied by the caller for this operation.
    // Returns: Returns true when try find first free backpack location succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: IsValidTopLevelSlot
    // Purpose: Validates or evaluates is valid top level slot rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - slot: Slot value supplied by the caller for this operation.
    // Returns: Returns true when is valid top level slot succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private static bool IsValidTopLevelSlot(byte slot)
    {
        return slot < 69 || slot is >= 81 and < 113;
    }

    // Method: IsItemAllowedInEquipmentSlot
    // Purpose: Validates or evaluates is item allowed in equipment slot rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - item: Item value supplied by the caller for this operation.
    // - slot: Slot value supplied by the caller for this operation.
    // Returns: Returns true when is item allowed in equipment slot succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private static bool IsItemAllowedInEquipmentSlot(PlayerInventoryItem item, byte slot)
    {
        return ResolveAllowedEquipmentSlots(item).Contains(slot);
    }

    // Method: ResolveAllowedEquipmentSlots
    // Purpose: Retrieves resolve allowed equipment slots data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - item: Item value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: HandleItemQuerySingleAsync
    // Purpose: Handles handle item query single work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task HandleItemQuerySingleAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        WorldPacketReader reader = new(packet.Payload);
        uint itemEntry = reader.ReadUInt32();

        byte[] payload = _itemSystem.TryGetItemTemplate(itemEntry, out ItemTemplateRecord itemTemplate)
            ? WorldPacketBuilders.BuildItemQuerySingleResponse(itemTemplate)
            : WorldPacketBuilders.BuildItemQuerySingleNotFound(itemEntry);

        await SendAsync(WorldOpcode.SMSG_ITEM_QUERY_SINGLE_RESPONSE, payload, _crypt, cancellationToken);
    }

    // Method: HandleItemNameQueryAsync
    // Purpose: Handles handle item name query work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task HandleItemNameQueryAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        WorldPacketReader reader = new(packet.Payload);
        uint itemEntry = reader.ReadUInt32();

        byte[] payload = _itemSystem.TryGetItemTemplate(itemEntry, out ItemTemplateRecord itemTemplate)
            ? WorldPacketBuilders.BuildItemNameQueryResponse(itemTemplate)
            : WorldPacketBuilders.BuildItemNameQueryNotFound(itemEntry);

        await SendAsync(WorldOpcode.SMSG_ITEM_NAME_QUERY_RESPONSE, payload, _crypt, cancellationToken);
    }

    // Method: HandleNameQueryAsync
    // Purpose: Handles handle name query work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: HandleCreatureQueryAsync
    // Purpose: Handles handle creature query work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task HandleCreatureQueryAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        if (packet.Payload.Length < sizeof(uint))
        {
            return;
        }

        WorldPacketReader reader = new(packet.Payload);
        uint entry = reader.ReadUInt32();
        ulong clientGuid = reader.Remaining >= sizeof(ulong) ? reader.ReadUInt64() : 0;

        WorldTemplateDataStore worldData = _worldTemplateDataResolver();
        if (!worldData.TryGetCreatureTemplate(entry, out CreatureTemplateRecord template))
        {
            await SendAsync(WorldOpcode.SMSG_CREATURE_QUERY_RESPONSE, WorldPacketBuilders.BuildCreatureQueryNotFound(entry), _crypt, cancellationToken);
            return;
        }

        CreatureSpawnRecord? spawn = null;
        if (clientGuid != 0 &&
            _visibleCreatureClientGuids.TryGetValue(clientGuid, out uint spawnGuid) &&
            worldData.TryGetCreatureSpawn(spawnGuid, out CreatureSpawnRecord visibleSpawn))
        {
            spawn = visibleSpawn;
        }

        await SendAsync(WorldOpcode.SMSG_CREATURE_QUERY_RESPONSE, WorldPacketBuilders.BuildCreatureQueryResponse(template, spawn), _crypt, cancellationToken);
    }

    // Method: HandleGameObjectQueryAsync
    // Purpose: Handles handle game object query work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task HandleGameObjectQueryAsync(WorldPacket packet, CancellationToken cancellationToken)
    {
        if (packet.Payload.Length < sizeof(uint))
        {
            return;
        }

        WorldPacketReader reader = new(packet.Payload);
        uint entry = reader.ReadUInt32();
        _ = reader.Remaining >= sizeof(ulong) ? reader.ReadUInt64() : 0;

        WorldTemplateDataStore worldData = _worldTemplateDataResolver();
        byte[] payload = worldData.TryGetGameObjectTemplate(entry, out GameObjectTemplateRecord template)
            ? WorldPacketBuilders.BuildGameObjectQueryResponse(template)
            : WorldPacketBuilders.BuildGameObjectQueryNotFound(entry);

        await SendAsync(WorldOpcode.SMSG_GAMEOBJECT_QUERY_RESPONSE, payload, _crypt, cancellationToken);
    }

    // Method: HandleWhoAsync
    // Purpose: Handles handle who work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task HandleWhoAsync(CancellationToken cancellationToken)
    {
        PlayerLoginRecord player = RequireCurrentPlayer();
        IReadOnlyList<PlayerLoginRecord> players = [.. _playerSessionRegistry.SnapshotSessions()
            .Select(session => session.CurrentPlayer)
            .Where(other => other is not null && other.Faction == player.Faction)
            .Cast<PlayerLoginRecord>()];

        await SendAsync(WorldOpcode.SMSG_WHO, WorldPacketBuilders.BuildWhoResponse(players), _crypt, cancellationToken);
    }

    // Method: HandleLogoutRequestAsync
    // Purpose: Handles handle logout request work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: HandlePlayedTimeAsync
    // Purpose: Handles handle played time work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task HandlePlayedTimeAsync(CancellationToken cancellationToken)
    {
        await SendAsync(WorldOpcode.SMSG_PLAYED_TIME, WorldPacketBuilders.BuildPlayedTime(RequireCurrentPlayer()), _crypt, cancellationToken);
    }

    // Method: HandleMessageChatAsync
    // Purpose: Handles handle message chat work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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
        uint channelRank = message.Type == ChatMessageType.Channel ? GameChatSystem.ResolveChannelPlayerRank(CurrentPlayer) : 0;
        byte[] payload = WorldPacketBuilders.BuildChatMessage(
            message.Type,
            message.Language,
            CurrentPlayer.ClientGuid,
            CurrentPlayer.Name,
            message.Text,
            channelName,
            0,
            channelRank);

        WorldClientSession[] worldRecipients = [.. recipients.OfType<WorldClientSession>()];
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

    // Method: NotifyMapServiceFailureAsync
    // Purpose: Executes the notify map service failure operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - message: Message value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: SendSystemMessageAsync
    // Purpose: Handles send system message work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - message: Message value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task SendSystemMessageAsync(string message, CancellationToken cancellationToken)
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

    // Method: SplitSystemMessageLines
    // Purpose: Executes the split system message lines operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - message: Message value supplied by the caller for this operation.
    // Returns: Returns the I enumerable value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: SplitSystemMessageLine
    // Purpose: Executes the split system message line operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - line: Line value supplied by the caller for this operation.
    // Returns: Returns the I enumerable value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: JoinDefaultChatChannelsAsync
    // Purpose: Executes the join default chat channels operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: HandleJoinChannelAsync
    // Purpose: Handles handle join channel work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: JoinChatChannelAsync
    // Purpose: Executes the join chat channel operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - channelName: Channel name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: HandleLeaveChannelAsync
    // Purpose: Handles handle leave channel work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: HandleChannelListAsync
    // Purpose: Handles handle channel list work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: HandleRequestAccountDataAsync
    // Purpose: Handles handle request account data work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: HandleCharacterCreateAsync
    // Purpose: Handles handle character create work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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
            result == CharacterCreateResult.Success ? LogType.SYSTEM : LogType.WARNING,
            $"Character create result for account '{account.Username}', name='{request.Name}': {result}.",
            "WorldClientSession");
    }

    // Method: HandleCharacterDeleteAsync
    // Purpose: Handles handle character delete work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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
            result == CharacterDeleteServiceResult.Success ? LogType.SYSTEM : LogType.WARNING,
            $"Character delete result for account '{account.Username}', guid=0x{clientGuid:X16}: {result}.",
            "WorldClientSession");
    }

    // Method: CleanupCurrentPlayerAsync
    // Purpose: Executes the cleanup current player operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // - notifyMapService: Notify map service value supplied by the caller for this operation.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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
        await RemovePlayerFromVisibleSessionsAsync(player, cancellationToken);
        string ownerServerName = _currentMapOwnerServerName;
        CurrentPlayer = null;
        CurrentMovement = null;
        _currentMapOwnerServerName = string.Empty;
        ResetMapServiceMovementRoute();
        ResetGameObjectVisibility();
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

        string ownerSuffix = string.IsNullOrWhiteSpace(ownerServerName)
            ? string.Empty
            : $" through {ownerServerName}";

        Logger.Write(LogType.SYSTEM, $"Player '{player.Name}' ({player.Guid}) left world map={player.Map}, zone={player.Zone}{ownerSuffix}.", "WorldClientSession");
    }

    // Method: ReadCharacterDeleteGuid
    // Purpose: Retrieves read character delete GUID data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - bytepayload: Bytepayload value supplied by the caller for this operation.
    // Returns: Returns the ulong value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private static ulong ReadCharacterDeleteGuid(byte[] payload)
    {
        return ReadClientGuid(payload);
    }

    // Method: ReadClientGuid
    // Purpose: Retrieves read client GUID data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - bytepayload: Bytepayload value supplied by the caller for this operation.
    // Returns: Returns the ulong value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private static ulong ReadClientGuid(byte[] payload)
    {
        WorldPacketReader reader = new(payload);
        return reader.ReadUInt64();
    }

    // Method: ReadCharacterCreateRequest
    // Purpose: Retrieves read character create request data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - bytepayload: Bytepayload value supplied by the caller for this operation.
    // Returns: Returns the character create request value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadChatMessage
    // Purpose: Retrieves read chat message data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - bytepayload: Bytepayload value supplied by the caller for this operation.
    // Returns: Returns the chat incoming message value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ResolveFactionForRace
    // Purpose: Retrieves resolve faction for race data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - race: Race value supplied by the caller for this operation.
    // Returns: Returns the player faction value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private static PlayerFaction ResolveFactionForRace(byte race)
    {
        return race switch
        {
            1 or 3 or 4 or 7 => PlayerFaction.Alliance,
            2 or 5 or 6 or 8 => PlayerFaction.Horde,
            _ => PlayerFaction.Neutral,
        };
    }

    // Method: SendAsync
    // Purpose: Handles send work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - opcode: Opcode value supplied by the caller for this operation.
    // - bytepayload: Bytepayload value supplied by the caller for this operation.
    // - crypt: Crypt value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: GetStream
    // Purpose: Retrieves get stream data for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the network stream value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private NetworkStream GetStream()
    {
        return _stream ?? throw new InvalidOperationException("World client stream is not initialized.");
    }

    // Method: RequireAccount
    // Purpose: Executes the require account operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the world account session record value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    private WorldAccountSessionRecord RequireAccount()
    {
        return _account ?? throw new InvalidOperationException("World client is not authenticated.");
    }

    // Method: DisposeAsync
    // Purpose: Controls the dispose lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSession so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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
        _visibilityLock.Dispose();
        _disconnect.Dispose();
        _client.Dispose();
    }
}
