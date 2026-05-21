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

using EmulationServer.Database.Configuration;
using EmulationServer.Database.Interfaces;
using EmulationServer.Database.Services;
using EmulationServer.Network.Configuration;
using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Peers;
using EmulationServer.Network.Networking.Socket;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

/**
  * File overview: src/EmulationServer.Core/Servers/EmulationServerHost.cs
  * This file belongs to the project runtime logic and supporting data models portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Core.Servers;

/**
  * Represents the emulation server host component in the project runtime logic and supporting data models area.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public sealed class EmulationServerHost : IAsyncDisposable
{
    /**
      * Stores the server name dependency or runtime value for EmulationServerHost.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly string _serverName;
    /**
      * Stores the database settings dependency or runtime value for EmulationServerHost.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly DatabaseSettings? _databaseSettings;
    /**
      * Stores the internal network settings dependency or runtime value for EmulationServerHost.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly InternalNetworkSettings _internalNetworkSettings;
    /**
      * Stores the database service dependency or runtime value for EmulationServerHost.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly IDatabaseService? _databaseService;
    /**
      * Stores the internal socket listener dependency or runtime value for EmulationServerHost.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly InternalSocketListener _internalSocketListener;
    /**
      * Stores the internal peer connector dependency or runtime value for EmulationServerHost.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly InternalPeerConnector _internalPeerConnector;
    /**
      * Stores the shutdown cancellation dependency or runtime value for EmulationServerHost.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly CancellationTokenSource _shutdownCancellation = new();
    /**
      * Stores the startup completed dependency or runtime value for EmulationServerHost.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly TaskCompletionSource<bool> _startupCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /**
      * Stores the shutdown requested dependency or runtime value for EmulationServerHost.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private int _shutdownRequested;

    /**
      * Creates a new EmulationServerHost instance and stores the dependencies required by the component.
      * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
      */
    public EmulationServerHost(
        string serverName,
        InternalNetworkSettings internalNetworkSettings,
        InternalNetworkCallbacks? callbacks = null)
        : this(serverName, null, internalNetworkSettings, callbacks)
    {
    }

    /**
      * Creates a new EmulationServerHost instance and stores the dependencies required by the component.
      * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
      */
    public EmulationServerHost(
        string serverName,
        DatabaseSettings? databaseSettings,
        InternalNetworkSettings internalNetworkSettings,
        InternalNetworkCallbacks? callbacks = null)
    {
        if (string.IsNullOrWhiteSpace(serverName))
        {
            throw new ArgumentException("Server name is required.", nameof(serverName));
        }

        ArgumentNullException.ThrowIfNull(internalNetworkSettings);

        databaseSettings?.Validate();
        internalNetworkSettings.Validate();

        _serverName = serverName;
        _databaseSettings = databaseSettings;
        _internalNetworkSettings = internalNetworkSettings;
        _databaseService = databaseSettings is null ? null : new MySqlDatabaseService(databaseSettings);

        InternalNetworkCallbacks hostCallbacks = CreateHostCallbacks(callbacks ?? InternalNetworkCallbacks.Empty);

        _internalSocketListener = new InternalSocketListener(internalNetworkSettings, hostCallbacks);
        _internalPeerConnector = new InternalPeerConnector(
            serverName,
            internalNetworkSettings.Peers,
            internalNetworkSettings.RegistrationKey,
            internalNetworkSettings.LatencyReportInterval,
            internalNetworkSettings.PingTimeout,
            hostCallbacks);
    }

    /**
      * Gets or stores the startup completed value used by EmulationServerHost.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public Task StartupCompleted => _startupCompleted.Task;

    /**
      * Starts the component and prepares the runtime state required before it can accept work.
      * The method is part of EmulationServerHost and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using CancellationTokenSource linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, _shutdownCancellation.Token);

        try
        {
            Logger.Write(LogType.NOTICE, $"Starting {_serverName}...", nameof(EmulationServerHost));
            await ValidateStartupAsync(linkedCancellation.Token);

            await _internalPeerConnector.StartAsync(linkedCancellation.Token);

            if (_internalNetworkSettings.Peers.Count == 0)
            {
                Logger.Write(LogType.NETWORK, $"{_serverName} has no outgoing internal peers configured. Waiting for incoming internal server registrations...", nameof(EmulationServerHost));
            }

            Logger.Write(LogType.NETWORK, $"{_serverName} started successfully. Listening for internal server connections...", nameof(EmulationServerHost));

            _startupCompleted.TrySetResult(true);

            await _internalSocketListener.StartAsync(linkedCancellation.Token);

            Logger.Write(LogType.TRACE, $"{_serverName} stopped.", nameof(EmulationServerHost));
        }
        catch (Exception exception)
        {
            _startupCompleted.TrySetException(exception);
            throw;
        }
    }

    /**
      * Stops the component and releases runtime resources in a controlled order.
      * The method is part of EmulationServerHost and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _internalPeerConnector.StopAsync(cancellationToken);
        await _internalSocketListener.StopAsync(cancellationToken);
    }

    /**
      * Releases owned resources and ensures background work is stopped safely.
      * The method is part of EmulationServerHost and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        _shutdownCancellation.Dispose();

        if (_databaseService is not null)
        {
            await _databaseService.DisposeAsync();
        }
    }

    /**
      * Creates a new object with validated defaults so callers receive a ready-to-use instance.
      * The method is part of EmulationServerHost and keeps this workflow isolated from the caller.
      */
    private InternalNetworkCallbacks CreateHostCallbacks(InternalNetworkCallbacks callbacks)
    {
        return new InternalNetworkCallbacks
        {
            ServerAuthenticatedAsync = callbacks.ServerAuthenticatedAsync,
            PacketReceivedAsync = callbacks.PacketReceivedAsync,
            ServerDisconnectedAsync = callbacks.ServerDisconnectedAsync,
            PeerAuthenticatedAsync = callbacks.PeerAuthenticatedAsync,
            PeerPacketReceivedAsync = callbacks.PeerPacketReceivedAsync,
            PeerDisconnectedAsync = callbacks.PeerDisconnectedAsync,
            PeerReconnectTimedOutAsync = callbacks.PeerReconnectTimedOutAsync,
            ShutdownRequestedAsync = async (sourceServerName, reason, cancellationToken) =>
            {
                await callbacks.NotifyShutdownRequestedAsync(sourceServerName, reason, cancellationToken);
                await RequestShutdownAsync(sourceServerName, reason);
            },
        };
    }

    /**
      * Performs the request shutdown async operation for EmulationServerHost.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    private async Task RequestShutdownAsync(string sourceServerName, string reason)
    {
        if (Interlocked.Exchange(ref _shutdownRequested, 1) == 1)
        {
            return;
        }

        Logger.Write(LogType.WARNING, $"{_serverName} received internal shutdown request from {sourceServerName}: {reason}. Stopping server...", nameof(EmulationServerHost));
        await _shutdownCancellation.CancelAsync();
    }

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of EmulationServerHost and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task ValidateStartupAsync(CancellationToken cancellationToken)
    {
        Logger.Write(LogType.TRACE, $"Validating {_serverName} settings...", nameof(EmulationServerHost));

        _internalNetworkSettings.Validate();

        if (_databaseSettings is not null && _databaseService is not null)
        {
            _databaseSettings.Validate();

            Logger.Write(LogType.DATABASE, $"Validating {_serverName} database connection...", nameof(EmulationServerHost));
            await _databaseService.ValidateConnectionAsync(cancellationToken);

            Logger.Write(LogType.NETWORK, $"{_serverName} settings, database connection, and internal networking validated successfully.", nameof(EmulationServerHost));
            return;
        }

        Logger.Write(LogType.NETWORK, $"{_serverName} settings and internal networking validated successfully. No direct database connection is configured.", nameof(EmulationServerHost));
    }
}
