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
 * Documents the EmulationServerHost source file in the shared startup, configuration, and host orchestration area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Core.Servers;

/**
 * Owns the emulation server host behavior for the shared startup, configuration, and host orchestration layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public sealed class EmulationServerHost : IAsyncDisposable
{
    /**
     * Holds the private server name state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly string _serverName;
    /**
     * Holds the private database settings state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly DatabaseSettings? _databaseSettings;
    /**
     * Holds the private internal network settings state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly InternalNetworkSettings _internalNetworkSettings;
    /**
     * Holds the private database service state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly IDatabaseService? _databaseService;
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
     * Holds the private shutdown cancellation state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly CancellationTokenSource _shutdownCancellation = new();
    /**
     * Holds the private startup completed state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly TaskCompletionSource<bool> _startupCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /**
     * Holds the private shutdown requested state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private int _shutdownRequested;

    /**
     * Initializes a new EmulationServerHost instance with the dependencies required by the shared startup, configuration, and host orchestration workflow.
     * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
     * Inputs used by this operation: serverName, internalNetworkSettings, callbacks.
     */
    public EmulationServerHost(
        string serverName,
        InternalNetworkSettings internalNetworkSettings,
        InternalNetworkCallbacks? callbacks = null)
        : this(serverName, null, internalNetworkSettings, callbacks)
    {
    }

    /**
     * Initializes a new EmulationServerHost instance with the dependencies required by the shared startup, configuration, and host orchestration workflow.
     * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
     * Inputs used by this operation: serverName, databaseSettings, internalNetworkSettings, callbacks.
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
            internalNetworkSettings.ReceiveBufferSize,
            internalNetworkSettings.SendBufferSize,
            internalNetworkSettings.KeepAlive,
            internalNetworkSettings.KeepAliveTimeSeconds,
            internalNetworkSettings.KeepAliveIntervalSeconds,
            internalNetworkSettings.AuthenticationTimeout,
            hostCallbacks);
    }

    /**
      * Gets or stores the startup completed value used by EmulationServerHost.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public Task StartupCompleted => _startupCompleted.Task;

    /**
     * Starts the start workflow and prepares the component to accept runtime work.
     * Startup is ordered so validation and dependency setup finish before services are announced as available.
     * Inputs used by this operation: cancellationToken.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
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
     * Stops the stop workflow and releases owned runtime resources in a controlled order.
     * Shutdown logic is centralized to avoid dangling connections, incomplete saves, or partially registered services.
     * Inputs used by this operation: cancellationToken.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
     */
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _internalPeerConnector.StopAsync(cancellationToken);
        await _internalSocketListener.StopAsync(cancellationToken);
    }

    /**
     * Stops the dispose workflow and releases owned runtime resources in a controlled order.
     * Shutdown logic is centralized to avoid dangling connections, incomplete saves, or partially registered services.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
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
     * Creates the host callbacks result needed by the caller.
     * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
     * Inputs used by this operation: callbacks.
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
     * Performs the request shutdown operation for the shared startup, configuration, and host orchestration workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: sourceServerName, reason.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
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
