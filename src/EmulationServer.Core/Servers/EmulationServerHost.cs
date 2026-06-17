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
// File: src/EmulationServer.Core/Servers/EmulationServerHost.cs
// Purpose: Contains emulation server host code for the host orchestration, configuration loading, and service lifecycle layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Collections.Concurrent;

using EmulationServer.Database.Configuration;
using EmulationServer.Database.Interfaces;
using EmulationServer.Database.Services;
using EmulationServer.Network.Configuration;
using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Peers;
using EmulationServer.Network.Networking.Socket;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Core.Servers;

// Type: EmulationServerHost
// Purpose: Provides emulation server host behavior for the host orchestration, configuration loading, and service lifecycle layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class EmulationServerHost : IAsyncDisposable
{

    // Field: Stores the server name state used by the host orchestration, configuration loading, and service lifecycle layer.
    // Value: current server name backing value maintained by the owning type.
    private readonly string _serverName;

    // Field: Stores the database settings state used by the host orchestration, configuration loading, and service lifecycle layer.
    // Value: current database settings backing value maintained by the owning type.
    private readonly DatabaseSettings? _databaseSettings;

    // Field: Stores the internal network settings state used by the host orchestration, configuration loading, and service lifecycle layer.
    // Value: current internal network settings backing value maintained by the owning type.
    private readonly InternalNetworkSettings _internalNetworkSettings;

    // Field: Stores the database service state used by the host orchestration, configuration loading, and service lifecycle layer.
    // Value: current database service backing value maintained by the owning type.
    private readonly MySqlDatabaseService? _databaseService;

    // Field: Stores the internal socket listener state used by the host orchestration, configuration loading, and service lifecycle layer.
    // Value: current internal socket listener backing value maintained by the owning type.
    private readonly InternalSocketListener _internalSocketListener;

    // Field: Stores the internal peer connector state used by the host orchestration, configuration loading, and service lifecycle layer.
    // Value: current internal peer connector backing value maintained by the owning type.
    private readonly InternalPeerConnector _internalPeerConnector;
    private readonly ConcurrentDictionary<string, byte> _authenticatedIncomingServers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _authenticatedOutgoingPeers = new(StringComparer.OrdinalIgnoreCase);

    private readonly CancellationTokenSource _shutdownCancellation = new();

    private readonly TaskCompletionSource<bool> _startupCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Field: Stores the shutdown requested state used by the host orchestration, configuration loading, and service lifecycle layer.
    // Value: current shutdown requested backing value maintained by the owning type.
    private int _shutdownRequested;

    // Constructor: EmulationServerHost
    // Purpose: Initializes a new EmulationServerHost instance with dependencies and values required by the host orchestration, configuration loading, and service lifecycle layer.
    // Parameters:
    // - serverName: Server name value supplied by the caller for this operation.
    // - internalNetworkSettings: Internal network settings value supplied by the caller for this operation.
    // - callbacks: Callbacks value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to EmulationServerHost so callers do not duplicate validation, protocol, or persistence rules.
    public EmulationServerHost(
        string serverName,
        InternalNetworkSettings internalNetworkSettings,
        InternalNetworkCallbacks? callbacks = null)
        : this(serverName, null, internalNetworkSettings, callbacks)
    {
    }

    // Constructor: EmulationServerHost
    // Purpose: Initializes a new EmulationServerHost instance with dependencies and values required by the host orchestration, configuration loading, and service lifecycle layer.
    // Parameters:
    // - serverName: Server name value supplied by the caller for this operation.
    // - databaseSettings: Database settings value supplied by the caller for this operation.
    // - internalNetworkSettings: Internal network settings value supplied by the caller for this operation.
    // - callbacks: Callbacks value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to EmulationServerHost so callers do not duplicate validation, protocol, or persistence rules.
    public EmulationServerHost(
        string serverName,
        DatabaseSettings? databaseSettings,
        InternalNetworkSettings internalNetworkSettings,
        InternalNetworkCallbacks? callbacks = null)
    {
        if (string.IsNullOrWhiteSpace(serverName))
        {
            throw new ArgumentException("Server name is required.");
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
            internalNetworkSettings.LatencyLoggingEnabled,
            internalNetworkSettings.LatencyLogInterval,
            internalNetworkSettings.PingTimeout,
            internalNetworkSettings.ReceiveBufferSize,
            internalNetworkSettings.SendBufferSize,
            internalNetworkSettings.KeepAlive,
            internalNetworkSettings.KeepAliveTimeSeconds,
            internalNetworkSettings.KeepAliveIntervalSeconds,
            internalNetworkSettings.AuthenticationTimeout,
            hostCallbacks);
    }

    // Property: Gets or sets the startup completed value used by the host orchestration, configuration loading, and service lifecycle layer.
    // Value: startup completed value exposed by the owning type.
    public Task StartupCompleted => _startupCompleted.Task;

    // Method: StartAsync
    // Purpose: Controls the start lifecycle step for the host orchestration, configuration loading, and service lifecycle layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to EmulationServerHost so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using CancellationTokenSource linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, _shutdownCancellation.Token);

        try
        {
            Logger.Write(LogType.NOTICE, $"Starting {_serverName}...", "EmulationServerHost");
            await ValidateStartupAsync(linkedCancellation.Token);

            await _internalPeerConnector.StartAsync(linkedCancellation.Token);

            if (_internalNetworkSettings.Peers.Count == 0)
            {
                Logger.Write(LogType.NETWORK, $"{_serverName} has no outgoing internal peers configured. Waiting for incoming internal server registrations...", "EmulationServerHost");
            }

            Logger.Write(LogType.NETWORK, $"{_serverName} started successfully. Listening for internal server connections...", "EmulationServerHost");

            _startupCompleted.TrySetResult(true);

            await _internalSocketListener.StartAsync(linkedCancellation.Token);

            Logger.Write(LogType.TRACE, $"{_serverName} stopped.", "EmulationServerHost");
        }
        catch (Exception exception)
        {
            _startupCompleted.TrySetException(exception);
            throw;
        }
    }

    // Method: StopAsync
    // Purpose: Controls the stop lifecycle step for the host orchestration, configuration loading, and service lifecycle layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to EmulationServerHost so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _internalPeerConnector.StopAsync(cancellationToken);
        await _internalSocketListener.StopAsync(cancellationToken);
    }

    // Method: DisposeAsync
    // Purpose: Controls the dispose lifecycle step for the host orchestration, configuration loading, and service lifecycle layer.
    // Parameters: none.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to EmulationServerHost so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        _shutdownCancellation.Dispose();

        if (_databaseService is not null)
        {
            await _databaseService.DisposeAsync();
        }
    }

    // Method: IsInternalServerConnected
    // Purpose: Validates or evaluates is internal server connected rules for the host orchestration, configuration loading, and service lifecycle layer.
    // Parameters:
    // - serverName: Server name value supplied by the caller for this operation.
    // Returns: Returns true when is internal server connected succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to EmulationServerHost so callers do not duplicate validation, protocol, or persistence rules.
    public bool IsInternalServerConnected(string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
        {
            return false;
        }

        string normalizedServerName = serverName.Trim();
        return _authenticatedIncomingServers.ContainsKey(normalizedServerName) || _authenticatedOutgoingPeers.ContainsKey(normalizedServerName);
    }

    // Method: WaitForInternalServersAsync
    // Purpose: Handles wait for internal servers work for the host orchestration, configuration loading, and service lifecycle layer.
    // Parameters:
    // - requiredServerNames: Required server names value supplied by the caller for this operation.
    // - reason: Reason value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to EmulationServerHost so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task WaitForInternalServersAsync(
        IEnumerable<string> requiredServerNames,
        string reason,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requiredServerNames);

        string[] requiredServers = [.. requiredServerNames
            .Where(serverName => !string.IsNullOrWhiteSpace(serverName))
            .Select(serverName => serverName.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)];

        if (requiredServers.Length == 0)
        {
            return;
        }

        string[] missingServers = GetMissingInternalServers(requiredServers);
        if (missingServers.Length == 0)
        {
            return;
        }

        Logger.Write(
            LogType.NETWORK,
            $"{_serverName} waiting for required internal server(s): {string.Join(", ", missingServers)}. {reason}",
            "EmulationServerHost");

        DateTimeOffset nextStatusUtc = DateTimeOffset.UtcNow.AddSeconds(15);

        while (!cancellationToken.IsCancellationRequested)
        {
            missingServers = GetMissingInternalServers(requiredServers);
            if (missingServers.Length == 0)
            {
                Logger.Write(
                    LogType.SUCCESS,
                    $"{_serverName} required internal server(s) are online: {string.Join(", ", requiredServers)}.",
                    "EmulationServerHost");
                return;
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (now >= nextStatusUtc)
            {
                Logger.Write(
                    LogType.NETWORK,
                    $"{_serverName} is still waiting for required internal server(s): {string.Join(", ", missingServers)}.",
                    "EmulationServerHost");
                nextStatusUtc = now.AddSeconds(15);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }

    // Method: GetMissingInternalServers
    // Purpose: Retrieves get missing internal servers data for the host orchestration, configuration loading, and service lifecycle layer.
    // Parameters:
    // - requiredServerNames: Required server names value supplied by the caller for this operation.
    // Returns: Returns the string[] value produced by this operation.
    // Notes: This keeps the operation scoped to EmulationServerHost so callers do not duplicate validation, protocol, or persistence rules.
    private string[] GetMissingInternalServers(IReadOnlyCollection<string> requiredServerNames)
    {
        return [.. requiredServerNames.Where(serverName => !IsInternalServerConnected(serverName))];
    }

    // Method: CreateHostCallbacks
    // Purpose: Applies create host callbacks changes for the host orchestration, configuration loading, and service lifecycle layer.
    // Parameters:
    // - callbacks: Callbacks value supplied by the caller for this operation.
    // Returns: Returns the internal network callbacks value produced by this operation.
    // Notes: This keeps the operation scoped to EmulationServerHost so callers do not duplicate validation, protocol, or persistence rules.
    private InternalNetworkCallbacks CreateHostCallbacks(InternalNetworkCallbacks callbacks)
    {
        return new InternalNetworkCallbacks
        {
            ServerAuthenticatedAsync = async (session, remoteServerName, cancellationToken) =>
            {
                _authenticatedIncomingServers[remoteServerName] = 0;
                await callbacks.NotifyServerAuthenticatedAsync(session, remoteServerName, cancellationToken);
            },

            PacketReceivedAsync = callbacks.PacketReceivedAsync,
            ServerDisconnectedAsync = async (session, remoteServerName, cancellationToken) =>
            {
                _authenticatedIncomingServers.TryRemove(remoteServerName, out _);
                await callbacks.NotifyServerDisconnectedAsync(session, remoteServerName, cancellationToken);
            },

            PeerAuthenticatedAsync = async (connection, remoteServerName, cancellationToken) =>
            {
                _authenticatedOutgoingPeers[remoteServerName] = 0;
                await callbacks.NotifyPeerAuthenticatedAsync(connection, remoteServerName, cancellationToken);
            },

            PeerPacketReceivedAsync = callbacks.PeerPacketReceivedAsync,
            PeerDisconnectedAsync = async (connection, remoteServerName, cancellationToken) =>
            {
                _authenticatedOutgoingPeers.TryRemove(remoteServerName, out _);
                await callbacks.NotifyPeerDisconnectedAsync(connection, remoteServerName, cancellationToken);
            },

            PeerReconnectTimedOutAsync = callbacks.PeerReconnectTimedOutAsync,
            LatencyMeasured = callbacks.LatencyMeasured,
            PingTimedOut = callbacks.PingTimedOut,
            ShutdownRequestedAsync = async (sourceServerName, reason, cancellationToken) =>
            {
                await callbacks.NotifyShutdownRequestedAsync(sourceServerName, reason, cancellationToken);
                await RequestShutdownAsync(sourceServerName, reason);
            },
        };
    }

    // Method: RequestShutdownAsync
    // Purpose: Executes the request shutdown operation for the host orchestration, configuration loading, and service lifecycle layer.
    // Parameters:
    // - sourceServerName: Source server name value supplied by the caller for this operation.
    // - reason: Reason value supplied by the caller for this operation.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to EmulationServerHost so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task RequestShutdownAsync(string sourceServerName, string reason)
    {
        if (Interlocked.Exchange(ref _shutdownRequested, 1) == 1)
        {
            return;
        }

        Logger.Write(LogType.WARNING, $"{_serverName} received internal shutdown request from {sourceServerName}: {reason}. Stopping server...", "EmulationServerHost");
        await _shutdownCancellation.CancelAsync();
    }

    // Method: ValidateStartupAsync
    // Purpose: Validates or evaluates validate startup rules for the host orchestration, configuration loading, and service lifecycle layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to EmulationServerHost so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task ValidateStartupAsync(CancellationToken cancellationToken)
    {
        Logger.Write(LogType.TRACE, $"Validating {_serverName} settings...", "EmulationServerHost");

        _internalNetworkSettings.Validate();

        if (_databaseSettings is not null && _databaseService is not null)
        {
            _databaseSettings.Validate();

            Logger.Write(LogType.DATABASE, $"Validating {_serverName} database connection...", "EmulationServerHost");
            await _databaseService.ValidateConnectionAsync(cancellationToken);

            Logger.Write(LogType.SUCCESS, $"{_serverName} settings, database connection, and internal networking validated successfully.", "EmulationServerHost");
            return;
        }

        Logger.Write(LogType.SUCCESS, $"{_serverName} settings and internal networking validated successfully. No direct database connection is configured.", "EmulationServerHost");
    }
}
