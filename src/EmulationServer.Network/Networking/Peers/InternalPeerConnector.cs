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

using EmulationServer.Network.Configuration;
using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Health;
using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

/**
  * File overview: src/EmulationServer.Network/Networking/Peers/InternalPeerConnector.cs
  * This file belongs to the project runtime logic and supporting data models portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Network.Networking.Peers;

/**
  * Represents the internal peer connector component in the project runtime logic and supporting data models area.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public sealed class InternalPeerConnector : IAsyncDisposable
{
    /**
      * Stores the server name dependency or runtime value for InternalPeerConnector.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly string _serverName;
    /**
      * Stores the peers dependency or runtime value for InternalPeerConnector.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly IReadOnlyList<InternalPeerSettings> _peers;
    /**
      * Stores the registration key dependency or runtime value for InternalPeerConnector.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly string _registrationKey;
    /**
      * Stores the latency report interval dependency or runtime value for InternalPeerConnector.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly TimeSpan _latencyReportInterval;
    /**
      * Stores the ping timeout dependency or runtime value for InternalPeerConnector.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly TimeSpan _pingTimeout;
    /**
      * Stores the callbacks dependency or runtime value for InternalPeerConnector.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly InternalNetworkCallbacks _callbacks;
    /**
      * Stores the connection tasks dependency or runtime value for InternalPeerConnector.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly List<Task> _connectionTasks = [];
    /**
      * Stores the sync root dependency or runtime value for InternalPeerConnector.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly object _syncRoot = new();

    /**
      * Stores the stop cancellation dependency or runtime value for InternalPeerConnector.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private CancellationTokenSource? _stopCancellation;
    /**
      * Stores the started dependency or runtime value for InternalPeerConnector.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private int _started;
    /**
      * Stores the stopping dependency or runtime value for InternalPeerConnector.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private int _stopping;

    /**
      * Creates a new InternalPeerConnector instance and stores the dependencies required by the component.
      * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
      */
    public InternalPeerConnector(
        string serverName,
        IReadOnlyList<InternalPeerSettings> peers,
        string registrationKey,
        TimeSpan latencyReportInterval,
        TimeSpan pingTimeout,
        InternalNetworkCallbacks? callbacks = null)
    {
        if (string.IsNullOrWhiteSpace(serverName))
        {
            throw new ArgumentException("Server name is required.", nameof(serverName));
        }

        if (string.IsNullOrWhiteSpace(registrationKey))
        {
            throw new ArgumentException("Registration key is required.", nameof(registrationKey));
        }

        if (latencyReportInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(latencyReportInterval), "Latency report interval must be greater than zero.");
        }

        if (pingTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(pingTimeout), "Ping timeout must be greater than zero.");
        }

        _serverName = serverName;
        _peers = peers ?? throw new ArgumentNullException(nameof(peers));
        _registrationKey = registrationKey;
        _latencyReportInterval = latencyReportInterval;
        _pingTimeout = pingTimeout;
        _callbacks = callbacks ?? InternalNetworkCallbacks.Empty;
    }

    /**
      * Starts the component and prepares the runtime state required before it can accept work.
      * The method is part of InternalPeerConnector and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _started, 1) == 1)
        {
            throw new InvalidOperationException($"{_serverName} internal peer connector has already been started.");
        }

        List<InternalPeerSettings> enabledPeers = _peers
            .Where(peer => peer.Enabled)
            .ToList();

        if (enabledPeers.Count == 0)
        {
            Logger.Write(LogType.TRACE, $"{_serverName} has no configured outgoing internal peers.", nameof(InternalPeerConnector));
            return Task.CompletedTask;
        }

        _stopCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        CancellationToken peerCancellationToken = _stopCancellation.Token;

        lock (_syncRoot)
        {
            foreach (InternalPeerSettings peer in enabledPeers)
            {
                _connectionTasks.Add(Task.Run(() => RunPeerLoopAsync(peer, peerCancellationToken), CancellationToken.None));
            }
        }

        Logger.Write(LogType.NETWORK, $"{_serverName} internal peer connector started with {enabledPeers.Count} peer(s).", nameof(InternalPeerConnector));
        return Task.CompletedTask;
    }

    /**
      * Stops the component and releases runtime resources in a controlled order.
      * The method is part of InternalPeerConnector and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _stopping, 1) == 1)
        {
            return;
        }

        CancellationTokenSource? stopCancellation = _stopCancellation;
        if (stopCancellation is not null)
        {
            await stopCancellation.CancelAsync();
        }

        Task[] connectionTasks;
        lock (_syncRoot)
        {
            connectionTasks = _connectionTasks.ToArray();
        }

        if (connectionTasks.Length > 0)
        {
            Task allConnectionsStopped = Task.WhenAll(connectionTasks);
            Task completedTask = await Task.WhenAny(allConnectionsStopped, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken));

            if (completedTask == allConnectionsStopped)
            {
                await allConnectionsStopped;
            }
            else
            {
                Logger.Write(LogType.WARNING, $"Stopped waiting for {_serverName} peer connector because shutdown wait timed out.", nameof(InternalPeerConnector));
            }
        }

        stopCancellation?.Dispose();
        _stopCancellation = null;

        Logger.Write(LogType.NETWORK, $"{_serverName} internal peer connector stopped.", nameof(InternalPeerConnector));
    }

    /**
      * Releases owned resources and ensures background work is stopped safely.
      * The method is part of InternalPeerConnector and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
    }

    /**
      * Runs the main loop for this component until cancellation or shutdown is requested.
      * The method is part of InternalPeerConnector and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task RunPeerLoopAsync(InternalPeerSettings peer, CancellationToken cancellationToken)
    {
        bool everAuthenticated = false;
        bool loggedInitialWait = false;
        DateTimeOffset? reconnectWindowStartedUtc = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            InternalPeerConnection? connection = null;

            try
            {
                if (everAuthenticated)
                {
                    reconnectWindowStartedUtc ??= DateTimeOffset.UtcNow;

                    TimeSpan remainingReconnectWindow = GetRemainingReconnectWindow(
                        peer,
                        reconnectWindowStartedUtc.Value,
                        DateTimeOffset.UtcNow);

                    if (remainingReconnectWindow <= TimeSpan.Zero)
                    {
                        await StopReconnectAttemptsAsync(peer, cancellationToken);
                        break;
                    }

                    Logger.Write(
                        LogType.NETWORK,
                        $"{_serverName} reconnecting to internal peer {peer.Name} at {peer.Host}:{peer.Port}. Reconnect window remaining: {remainingReconnectWindow.TotalSeconds:0.##} second(s).",
                        nameof(InternalPeerConnector));
                }
                else if (!loggedInitialWait)
                {
                    Logger.Write(LogType.NETWORK, $"{_serverName} waiting for internal peer {peer.Name} at {peer.Host}:{peer.Port} to become available...", nameof(InternalPeerConnector));
                    loggedInitialWait = true;
                }

                using TcpClient client = new();
                client.NoDelay = true;
                client.ReceiveBufferSize = 8192;
                client.SendBufferSize = 8192;

                await client.ConnectAsync(peer.Host, peer.Port, cancellationToken);

                await using NetworkStream stream = client.GetStream();
                using SemaphoreSlim sendLock = new(1, 1);

                await AuthenticateWithPeerAsync(peer, stream, sendLock, cancellationToken);

                connection = new InternalPeerConnection(_serverName, peer, stream, sendLock);
                everAuthenticated = true;
                reconnectWindowStartedUtc = null;

                Logger.Write(LogType.NETWORK, $"{_serverName} authenticated with internal peer {peer.Name}.", nameof(InternalPeerConnector));
                await _callbacks.NotifyPeerAuthenticatedAsync(connection, peer.Name, cancellationToken);

                await ProcessAuthenticatedPeerAsync(connection, stream, sendLock, cancellationToken);

                Logger.Write(LogType.NETWORK, $"{_serverName} disconnected from internal peer {peer.Name}.", nameof(InternalPeerConnector));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                if (everAuthenticated)
                {
                    Logger.Write(LogType.WARNING, $"{_serverName} lost or could not reconnect to internal peer {peer.Name} at {peer.Host}:{peer.Port}: {exception.Message}", nameof(InternalPeerConnector));
                }
                else
                {
                    // Keep startup clean: before the first successful authentication, the peer may simply not be online yet.
                }
            }
            finally
            {
                if (connection is not null)
                {
                    try
                    {
                        await _callbacks.NotifyPeerDisconnectedAsync(connection, peer.Name, CancellationToken.None);
                    }
                    catch (Exception exception)
                    {
                        Logger.Write(LogType.CRITICAL, exception.ToString(), nameof(InternalPeerConnector));
                    }
                }
            }

            if (everAuthenticated)
            {
                reconnectWindowStartedUtc ??= DateTimeOffset.UtcNow;

                TimeSpan remainingReconnectWindow = GetRemainingReconnectWindow(
                    peer,
                    reconnectWindowStartedUtc.Value,
                    DateTimeOffset.UtcNow);

                if (remainingReconnectWindow <= TimeSpan.Zero)
                {
                    await StopReconnectAttemptsAsync(peer, cancellationToken);
                    break;
                }

                TimeSpan reconnectDelay = peer.ReconnectDelay <= remainingReconnectWindow
                    ? peer.ReconnectDelay
                    : remainingReconnectWindow;

                try
                {
                    await Task.Delay(reconnectDelay, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                continue;
            }

            try
            {
                await Task.Delay(peer.ReconnectDelay, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    /**
      * Calculates the amount of time left before reconnect attempts must be stopped for a previously seen peer.
      * Keeping this calculation in one place prevents each reconnect path from interpreting the timeout differently.
      */
    private static TimeSpan GetRemainingReconnectWindow(
        InternalPeerSettings peer,
        DateTimeOffset reconnectWindowStartedUtc,
        DateTimeOffset nowUtc)
    {
        TimeSpan elapsed = nowUtc - reconnectWindowStartedUtc;
        TimeSpan remaining = peer.ReconnectTimeout - elapsed;

        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    /**
      * Stops active reconnect attempts for a peer after its configured reconnect window expires.
      * The listener remains online, so the remote service can still register inbound when it comes back.
      */
    private async Task StopReconnectAttemptsAsync(InternalPeerSettings peer, CancellationToken cancellationToken)
    {
        Logger.Write(
            LogType.WARNING,
            $"{_serverName} stopped reconnect attempts to internal peer {peer.Name} at {peer.Host}:{peer.Port} after {peer.ReconnectTimeout.TotalSeconds:0.##} second(s). Waiting for {peer.Name} to register again inbound.",
            nameof(InternalPeerConnector));

        try
        {
            await _callbacks.NotifyPeerReconnectTimedOutAsync(peer.Name, peer.ReconnectTimeout, cancellationToken);
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.CRITICAL, exception.ToString(), nameof(InternalPeerConnector));
        }
    }

    /**
      * Completes the internal authentication flow before normal packets are exchanged.
      * The method is part of InternalPeerConnector and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    private async Task AuthenticateWithPeerAsync(
        InternalPeerSettings peer,
        NetworkStream stream,
        SemaphoreSlim sendLock,
        CancellationToken cancellationToken)
    {
        string? challenge = await InternalProtocol.ReadLineAsync(
            stream,
            InternalProtocol.MaximumAuthenticationLineLength,
            cancellationToken);

        if (challenge is null)
        {
            throw new InvalidOperationException($"Internal peer {peer.Name} disconnected before requesting authentication.");
        }

        string[] challengeParts = challenge.Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (challengeParts.Length != 2 || !string.Equals(challengeParts[0], InternalProtocol.AuthenticationChallenge, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Internal peer {peer.Name} sent an invalid authentication challenge.");
        }

        await InternalProtocol.WriteLineAsync(
            stream,
            sendLock,
            $"{InternalProtocol.AuthenticationResponse} {_serverName} {_registrationKey}",
            cancellationToken);

        string? response = await InternalProtocol.ReadLineAsync(
            stream,
            InternalProtocol.MaximumAuthenticationLineLength,
            cancellationToken);

        if (response is null)
        {
            throw new InvalidOperationException($"Internal peer {peer.Name} disconnected before accepting authentication.");
        }

        if (!response.StartsWith($"{InternalProtocol.AuthenticationAccepted} ", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Internal peer {peer.Name} rejected authentication.");
        }
    }

    /**
      * Processes incoming data and dispatches it to the correct subsystem handler.
      * The method is part of InternalPeerConnector and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    private async Task ProcessAuthenticatedPeerAsync(
        InternalPeerConnection connection,
        NetworkStream stream,
        SemaphoreSlim sendLock,
        CancellationToken cancellationToken)
    {
        await using InternalLatencyMonitor latencyMonitor = new(
            _serverName,
            connection.RemoteServerName,
            stream,
            sendLock,
            _latencyReportInterval,
            _pingTimeout);

        latencyMonitor.Start(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            string? line = await InternalProtocol.ReadLineAsync(
                stream,
                InternalProtocol.MaximumPacketLineLength,
                cancellationToken);

            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            await ProcessPeerPacketAsync(connection, line, latencyMonitor, cancellationToken);
        }
    }

    /**
      * Processes incoming data and dispatches it to the correct subsystem handler.
      * The method is part of InternalPeerConnector and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    private async Task ProcessPeerPacketAsync(
        InternalPeerConnection connection,
        string line,
        InternalLatencyMonitor latencyMonitor,
        CancellationToken cancellationToken)
    {
        string[] parts = line.Split(' ', 3, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return;
        }

        if (parts.Length >= 2 && string.Equals(parts[0], InternalProtocol.Ping, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.TRACE, $"{_serverName} received PING packet from {connection.RemoteServerName}.", nameof(InternalPeerConnector));
            await latencyMonitor.RespondToPingAsync(parts[1], cancellationToken);
            return;
        }

        if (parts.Length >= 2 && string.Equals(parts[0], InternalProtocol.Pong, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.TRACE, $"{_serverName} received PONG packet from {connection.RemoteServerName}.", nameof(InternalPeerConnector));
            latencyMonitor.RecordPong(parts[1]);
            return;
        }

        if (parts.Length >= 2 && string.Equals(parts[0], InternalProtocol.ShutdownRequest, StringComparison.OrdinalIgnoreCase))
        {
            string reason = parts.Length == 3 ? parts[2] : "No reason provided.";
            await _callbacks.NotifyShutdownRequestedAsync(parts[1], reason, cancellationToken);
            return;
        }

        if (string.Equals(parts[0], InternalProtocol.WorldCapacity, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.NETWORK, $"{_serverName} received world capacity packet from {connection.RemoteServerName}: {line}", nameof(InternalPeerConnector));
        }
        else if (string.Equals(parts[0], InternalProtocol.MapServiceStatus, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.TRACE, $"{_serverName} received map service status packet from {connection.RemoteServerName}.", nameof(InternalPeerConnector));
        }
        else if (string.Equals(parts[0], InternalProtocol.MapServiceCommand, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.TRACE, $"{_serverName} received map service command packet from {connection.RemoteServerName}.", nameof(InternalPeerConnector));
        }
        else if (string.Equals(parts[0], InternalProtocol.MapServiceCommandResult, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.TRACE, $"{_serverName} received map service command result packet from {connection.RemoteServerName}.", nameof(InternalPeerConnector));
        }
        else
        {
            Logger.Write(LogType.DEBUG, $"{_serverName} received internal packet from peer {connection.RemoteServerName}: {line}", nameof(InternalPeerConnector));
        }

        await _callbacks.NotifyPeerPacketReceivedAsync(connection, connection.RemoteServerName, line, cancellationToken);
    }
}
