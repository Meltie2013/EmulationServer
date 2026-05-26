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
  * Documents the RealmServer source file in the realm authentication, realm-list handling, and external client login services area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.RealmServer.Core;

/**
  * Owns the realm server behavior for the realm authentication, realm-list handling, and external client login services layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class RealmServer : IAsyncDisposable
{
    /**
      * Holds the private settings state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly RealmServerSettings _settings;
    /**
      * Holds the private database service state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly IDatabaseService _databaseService;
    /**
      * Holds the private account repository state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly AccountRepository _accountRepository;
    /**
      * Holds the private realm store state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly ConfiguredRealmStore _realmStore;
    /**
      * Holds the private socket listener state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly RealmSocketListener _socketListener;
    /**
      * Holds the private internal socket listener state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly InternalSocketListener _internalSocketListener;
    /**
      * Holds the private internal peer connector state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly InternalPeerConnector _internalPeerConnector;
    /**
      * Holds the private command service state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly RealmConsoleCommandService _commandService;

    /**
      * Initializes a new RealmServer instance with the dependencies required by the realm authentication, realm-list handling, and external client login services workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: settings.
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

    /**
      * Starts the start workflow and prepares the component to accept runtime work.
      * Startup is ordered so validation and dependency setup finish before services are announced as available.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Stops the stop workflow and releases owned runtime resources in a controlled order.
      * Shutdown logic is centralized to avoid dangling connections, incomplete saves, or partially registered services.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _internalPeerConnector.StopAsync(cancellationToken);
        await _internalSocketListener.StopAsync(cancellationToken);
        await _socketListener.StopAsync(cancellationToken);
    }

    /**
      * Stops the dispose workflow and releases owned runtime resources in a controlled order.
      * Shutdown logic is centralized to avoid dangling connections, incomplete saves, or partially registered services.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
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
