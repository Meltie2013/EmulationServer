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
// File: src/RealmServer/Core/RealmServer.cs
// Purpose: Contains realm server code for the realm server authentication, realm-list, and account connection layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Database.Accounts;
using EmulationServer.Database.Interfaces;
using EmulationServer.Database.Services;
using EmulationServer.Network.Networking.Peers;
using EmulationServer.Network.Networking.Socket;
using EmulationServer.RealmServer.Auth;
using EmulationServer.RealmServer.Commands;
using EmulationServer.RealmServer.Configuration;
using EmulationServer.RealmServer.Internal;
using EmulationServer.RealmServer.Realms;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.RealmServer.Core;

// Type: RealmServer
// Purpose: Provides realm server behavior for the realm server authentication, realm-list, and account connection layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class RealmServer : IAsyncDisposable
{

    // Field: Stores the settings state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current settings backing value maintained by the owning type.
    private readonly RealmServerSettings _settings;

    // Field: Stores the database service state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current database service backing value maintained by the owning type.
    private readonly IDatabaseService _databaseService;

    // Field: Stores the account repository state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current account repository backing value maintained by the owning type.
    private readonly AccountRepository _accountRepository;

    // Field: Stores the realm store state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current realm store backing value maintained by the owning type.
    private readonly ConfiguredRealmStore _realmStore;

    // Field: Stores the socket listener state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current socket listener backing value maintained by the owning type.
    private readonly RealmSocketListener _socketListener;

    // Field: Stores the internal socket listener state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current internal socket listener backing value maintained by the owning type.
    private readonly InternalSocketListener _internalSocketListener;

    // Field: Stores the internal peer connector state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current internal peer connector backing value maintained by the owning type.
    private readonly InternalPeerConnector _internalPeerConnector;

    // Field: Stores the command service state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current command service backing value maintained by the owning type.
    private readonly RealmConsoleCommandService _commandService;

    // Constructor: RealmServer
    // Purpose: Initializes a new RealmServer instance with dependencies and values required by the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - settings: Settings values that control how this operation should run.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmServer so callers do not duplicate validation, protocol, or persistence rules.
    public RealmServer(RealmServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _settings = settings;
        _databaseService = new MySqlDatabaseService(settings.Database);
        _accountRepository = new AccountRepository(_databaseService);
        _realmStore = new ConfiguredRealmStore(settings.Realms, settings.RealmList);

        RealmListPacketBuilder realmListPacketBuilder = new(_realmStore);
        _socketListener = new RealmSocketListener(
            settings.Socket,
            () => new RealmAuthSessionProcessor(_accountRepository, realmListPacketBuilder));

        RealmInternalPacketHandler internalPacketHandler = new(_realmStore);
        _internalSocketListener = new InternalSocketListener(settings.InternalNetwork, internalPacketHandler.CreateCallbacks());
        _internalPeerConnector = new InternalPeerConnector(
            "RealmServer",
            settings.InternalNetwork.Peers,
            settings.InternalNetwork.RegistrationKey,
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
            internalPacketHandler.CreateCallbacks());

        _commandService = new RealmConsoleCommandService(_accountRepository);
    }

    // Method: StartAsync
    // Purpose: Controls the start lifecycle step for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.Write(LogType.NOTICE, "Starting RealmServer...", "RealmServer");
        await ValidateStartupAsync(cancellationToken);

        _commandService.Start(cancellationToken);
        await _internalPeerConnector.StartAsync(cancellationToken);

        _ = Task.Run(() => _internalSocketListener.StartAsync(cancellationToken), CancellationToken.None);

        if (_settings.InternalNetwork.Peers.Count == 0)
        {
            Logger.Write(LogType.NETWORK, "RealmServer has no outgoing internal peers configured. Waiting for incoming realm status packets.", "RealmServer");
        }

        Logger.Write(LogType.NETWORK, "RealmServer started successfully. Listening for authentication connections...", "RealmServer");
        await _socketListener.StartAsync(cancellationToken);

        Logger.Write(LogType.TRACE, "RealmServer stopped.", "RealmServer");
    }

    // Method: StopAsync
    // Purpose: Controls the stop lifecycle step for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _internalPeerConnector.StopAsync(cancellationToken);
        await _internalSocketListener.StopAsync(cancellationToken);
        await _socketListener.StopAsync(cancellationToken);
    }

    // Method: DisposeAsync
    // Purpose: Controls the dispose lifecycle step for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        await _internalPeerConnector.DisposeAsync();
        await _databaseService.DisposeAsync();
    }

    // Method: ValidateStartupAsync
    // Purpose: Validates or evaluates validate startup rules for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task ValidateStartupAsync(CancellationToken cancellationToken)
    {
        Logger.Write(LogType.SYSTEM, "Validating RealmServer settings...", "RealmServer");
        _settings.Validate();

        Logger.Write(LogType.SYSTEM, "Validating RealmServer critical authentication opcodes...", "RealmServer");
        RealmAuthOpcodeVerifier.VerifyCriticalOpCodes();
        Logger.Write(LogType.SYSTEM, $"Validated RealmServer critical authentication opcodes...", "RealmServer");

        Logger.Write(LogType.SYSTEM, "Validating account database connection...", "RealmServer");
        await _databaseService.ValidateConnectionAsync(cancellationToken);

        Logger.Write(LogType.SYSTEM, $"Loaded {_settings.Realms.Count} configured realm(s).", "RealmServer");
        Logger.Write(LogType.SYSTEM, "RealmServer settings, authentication opcodes, account database connection, and internal networking validated successfully.", "RealmServer");
    }
}
