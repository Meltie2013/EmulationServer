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

/**
  * File overview: src/RealmServer/Core/RealmServer.cs
  * This file belongs to the server startup, shutdown, and dependency orchestration portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.RealmServer.Core;

/**
  * Represents the realm server component in the server startup, shutdown, and dependency orchestration area.
  * It owns the server startup, shutdown, and dependency wiring for this process.
  */
public sealed class RealmServer : IAsyncDisposable
{
    /**
      * Stores the settings dependency or runtime value for RealmServer.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly RealmServerSettings _settings;
    /**
      * Stores the database service dependency or runtime value for RealmServer.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly IDatabaseService _databaseService;
    /**
      * Stores the account repository dependency or runtime value for RealmServer.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly AccountRepository _accountRepository;
    /**
      * Stores the realm store dependency or runtime value for RealmServer.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly ConfiguredRealmStore _realmStore;
    /**
      * Stores the socket listener dependency or runtime value for RealmServer.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly RealmSocketListener _socketListener;
    /**
      * Stores the internal socket listener dependency or runtime value for RealmServer.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly InternalSocketListener _internalSocketListener;
    /**
      * Stores the internal peer connector dependency or runtime value for RealmServer.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly InternalPeerConnector _internalPeerConnector;
    /**
      * Stores the command service dependency or runtime value for RealmServer.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly RealmConsoleCommandService _commandService;

    /**
      * Creates a new RealmServer instance and stores the dependencies required by the component.
      * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
      */
    public RealmServer(RealmServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _settings = settings;
        _databaseService = new MySqlDatabaseService(settings.Database);
        _accountRepository = new AccountRepository(_databaseService);
        _realmStore = new ConfiguredRealmStore(settings.Realms);

        RealmListPacketBuilder realmListPacketBuilder = new(_realmStore);
        _socketListener = new RealmSocketListener(
            settings.Socket,
            () => new RealmAuthSessionProcessor(_accountRepository, realmListPacketBuilder));

        RealmInternalPacketHandler internalPacketHandler = new(_realmStore);
        _internalSocketListener = new InternalSocketListener(settings.InternalNetwork, internalPacketHandler.CreateCallbacks());
        _internalPeerConnector = new InternalPeerConnector(
            nameof(RealmServer),
            settings.InternalNetwork.Peers,
            settings.InternalNetwork.RegistrationKey,
            settings.InternalNetwork.LatencyReportInterval,
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

    /**
      * Starts the component and prepares the runtime state required before it can accept work.
      * The method is part of RealmServer and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.Write(LogType.NOTICE, "Starting RealmServer...", nameof(RealmServer));
        await ValidateStartupAsync(cancellationToken);

        _commandService.Start(cancellationToken);
        await _internalPeerConnector.StartAsync(cancellationToken);

        _ = Task.Run(() => _internalSocketListener.StartAsync(cancellationToken), CancellationToken.None);

        if (_settings.InternalNetwork.Peers.Count == 0)
        {
            Logger.Write(LogType.NETWORK, "RealmServer has no outgoing internal peers configured. Waiting for incoming realm status packets.", nameof(RealmServer));
        }

        Logger.Write(LogType.NETWORK, "RealmServer started successfully. Listening for authentication connections...", nameof(RealmServer));
        await _socketListener.StartAsync(cancellationToken);

        Logger.Write(LogType.TRACE, "RealmServer stopped.", nameof(RealmServer));
    }

    /**
      * Stops the component and releases runtime resources in a controlled order.
      * The method is part of RealmServer and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _internalPeerConnector.StopAsync(cancellationToken);
        await _internalSocketListener.StopAsync(cancellationToken);
        await _socketListener.StopAsync(cancellationToken);
    }

    /**
      * Releases owned resources and ensures background work is stopped safely.
      * The method is part of RealmServer and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        await _internalPeerConnector.DisposeAsync();
        await _databaseService.DisposeAsync();
    }

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of RealmServer and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task ValidateStartupAsync(CancellationToken cancellationToken)
    {
        Logger.Write(LogType.TRACE, "Validating RealmServer settings...", nameof(RealmServer));
        _settings.Validate();

        Logger.Write(LogType.TRACE, "Validating RealmServer critical authentication opcodes...", nameof(RealmServer));
        RealmAuthOpcodeVerifier.VerifyCriticalOpCodes();
        Logger.Write(LogType.TRACE, $"Validated RealmServer critical authentication opcodes...", nameof(RealmServer));

        Logger.Write(LogType.NETWORK, "Validating account database connection...", nameof(RealmServer));
        await _databaseService.ValidateConnectionAsync(cancellationToken);

        Logger.Write(LogType.NETWORK, $"Loaded {_settings.Realms.Count} configured realm(s).", nameof(RealmServer));
        Logger.Write(LogType.NETWORK, "RealmServer settings, authentication opcodes, account database connection, and internal networking validated successfully.", nameof(RealmServer));
    }
}
