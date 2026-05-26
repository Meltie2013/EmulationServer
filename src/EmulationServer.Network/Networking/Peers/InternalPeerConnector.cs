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
using System.Threading.Channels;

using EmulationServer.Network.Configuration;
using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Health;
using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

/**
  * File overview: src/EmulationServer.Network/Networking/Peers/InternalPeerConnector.cs
  * Documents the InternalPeerConnector source file in the internal server networking, packet framing, and peer/session lifecycle area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Network.Networking.Peers;

/**
  * Owns the internal peer connector behavior for the internal server networking, packet framing, and peer/session lifecycle layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class InternalPeerConnector : IAsyncDisposable
{
    /**
      * Keeps peer packet dispatch bounded so internal routing can use worker scheduling without unbounded memory growth.
      */
    private const int InternalPeerPacketDispatchQueueCapacity = 4096;

    /**
      * Holds the private server name state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly string _serverName;
    /**
      * Holds the private peers state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly IReadOnlyList<InternalPeerSettings> _peers;
    /**
      * Holds the private registration key state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly string _registrationKey;
    /**
      * Holds the private latency report interval state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly TimeSpan _latencyReportInterval;
    /**
      * Holds whether successful latency values should be logged during normal runtime.
      */
    private readonly bool _latencyLoggingEnabled;
    /**
      * Holds the minimum delay between visible latency log lines for active peer connections.
      */
    private readonly TimeSpan _latencyLogInterval;
    /**
      * Holds the private ping timeout state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly TimeSpan _pingTimeout;
    /**
      * Holds the private receive buffer size state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly int _receiveBufferSize;
    /**
      * Holds the private send buffer size state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly int _sendBufferSize;
    /**
      * Holds the private keep alive state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly bool _keepAlive;
    /**
      * Holds the private keep alive time seconds state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly int _keepAliveTimeSeconds;
    /**
      * Holds the private keep alive interval seconds state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly int _keepAliveIntervalSeconds;
    /**
      * Holds the private authentication timeout state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly TimeSpan _authenticationTimeout;
    /**
      * Holds the private callbacks state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly InternalNetworkCallbacks _callbacks;
    /**
      * Holds the private connection tasks state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly List<Task> _connectionTasks = [];
    /**
      * Holds the private sync root state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly object _syncRoot = new();

    /**
      * Holds the private stop cancellation state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private CancellationTokenSource? _stopCancellation;
    /**
      * Holds the private started state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private int _started;
    /**
      * Holds the private stopping state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private int _stopping;

    /**
      * Initializes a new InternalPeerConnector instance with the dependencies required by the internal server networking, packet framing, and peer/session lifecycle workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: serverName, peers, registrationKey, latencyReportInterval, pingTimeout, receiveBufferSize....
      */
    public InternalPeerConnector(
        string serverName,
        IReadOnlyList<InternalPeerSettings> peers,
        string registrationKey,
        TimeSpan latencyReportInterval,
        bool latencyLoggingEnabled,
        TimeSpan latencyLogInterval,
        TimeSpan pingTimeout,
        int receiveBufferSize,
        int sendBufferSize,
        bool keepAlive,
        int keepAliveTimeSeconds,
        int keepAliveIntervalSeconds,
        TimeSpan authenticationTimeout,
        InternalNetworkCallbacks? callbacks = null)
    {
        if (string.IsNullOrWhiteSpace(serverName))
        {
            throw new ArgumentException("Server name is required.");
        }

        if (string.IsNullOrWhiteSpace(registrationKey))
        {
            throw new ArgumentException("Registration key is required.");
        }

        if (latencyReportInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(null, "Latency report interval must be greater than zero.");
        }

        if (latencyLoggingEnabled && latencyLogInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(null, "Latency log interval must be greater than zero when latency logging is enabled.");
        }

        if (pingTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(null, "Ping timeout must be greater than zero.");
        }

        if (receiveBufferSize <= 0)
        {
            throw new ArgumentOutOfRangeException(null, "Receive buffer size must be greater than zero.");
        }

        if (sendBufferSize <= 0)
        {
            throw new ArgumentOutOfRangeException(null, "Send buffer size must be greater than zero.");
        }

        if (keepAliveTimeSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(null, "Keep-alive time cannot be negative.");
        }

        if (keepAliveIntervalSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(null, "Keep-alive interval cannot be negative.");
        }

        if (authenticationTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(null, "Authentication timeout must be greater than zero.");
        }

        _serverName = serverName;
        _peers = peers ?? throw new ArgumentNullException();
        _registrationKey = registrationKey;
        _latencyReportInterval = latencyReportInterval;
        _latencyLoggingEnabled = latencyLoggingEnabled;
        _latencyLogInterval = latencyLogInterval;
        _pingTimeout = pingTimeout;
        _receiveBufferSize = receiveBufferSize;
        _sendBufferSize = sendBufferSize;
        _keepAlive = keepAlive;
        _keepAliveTimeSeconds = keepAliveTimeSeconds;
        _keepAliveIntervalSeconds = keepAliveIntervalSeconds;
        _authenticationTimeout = authenticationTimeout;
        _callbacks = callbacks ?? InternalNetworkCallbacks.Empty;
    }

    /**
      * Starts the start workflow and prepares the component to accept runtime work.
      * Startup is ordered so validation and dependency setup finish before services are announced as available.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
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
            Logger.Write(LogType.TRACE, $"{_serverName} has no configured outgoing internal peers.", "InternalPeerConnector");
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

        Logger.Write(LogType.NETWORK, $"{_serverName} internal peer connector started with {enabledPeers.Count} peer(s).", "InternalPeerConnector");
        return Task.CompletedTask;
    }

    /**
      * Stops the stop workflow and releases owned runtime resources in a controlled order.
      * Shutdown logic is centralized to avoid dangling connections, incomplete saves, or partially registered services.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
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
                Logger.Write(LogType.WARNING, $"Stopped waiting for {_serverName} peer connector because shutdown wait timed out.", "InternalPeerConnector");
            }
        }

        stopCancellation?.Dispose();
        _stopCancellation = null;

        Logger.Write(LogType.NETWORK, $"{_serverName} internal peer connector stopped.", "InternalPeerConnector");
    }

    /**
      * Stops the dispose workflow and releases owned runtime resources in a controlled order.
      * Shutdown logic is centralized to avoid dangling connections, incomplete saves, or partially registered services.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
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
                        "InternalPeerConnector");
                }
                else if (!loggedInitialWait)
                {
                    Logger.Write(LogType.NETWORK, $"{_serverName} waiting for internal peer {peer.Name} at {peer.Host}:{peer.Port} to become available...", "InternalPeerConnector");
                    loggedInitialWait = true;
                }

                using TcpClient client = new();
                ConfigureClient(client);

                await client.ConnectAsync(peer.Host, peer.Port, cancellationToken);

                await using NetworkStream stream = client.GetStream();
                using InternalProtocolReader reader = new(stream);
                using SemaphoreSlim sendLock = new(1, 1);

                await AuthenticateWithPeerAsync(peer, reader, stream, sendLock, cancellationToken);

                connection = new InternalPeerConnection(_serverName, peer, stream, sendLock);
                everAuthenticated = true;
                reconnectWindowStartedUtc = null;

                Logger.Write(LogType.NETWORK, $"{_serverName} authenticated with internal peer {peer.Name}.", "InternalPeerConnector");
                await _callbacks.NotifyPeerAuthenticatedAsync(connection, peer.Name, cancellationToken);

                await ProcessAuthenticatedPeerAsync(connection, reader, stream, sendLock, cancellationToken);

                Logger.Write(LogType.NETWORK, $"{_serverName} disconnected from internal peer {peer.Name}.", "InternalPeerConnector");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                if (everAuthenticated)
                {
                    Logger.Write(LogType.WARNING, $"{_serverName} lost or could not reconnect to internal peer {peer.Name} at {peer.Host}:{peer.Port}: {exception.Message}", "InternalPeerConnector");
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
                        Logger.Write(LogType.CRITICAL, exception.ToString(), "InternalPeerConnector");
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
      * Applies low-latency socket options to outgoing internal peer connections.
      */
    private void ConfigureClient(TcpClient client)
    {
        client.NoDelay = true;
        client.ReceiveBufferSize = _receiveBufferSize;
        client.SendBufferSize = _sendBufferSize;

        if (!_keepAlive)
        {
            return;
        }

        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        TrySetTcpKeepAliveOption(client, SocketOptionName.TcpKeepAliveTime, _keepAliveTimeSeconds);
        TrySetTcpKeepAliveOption(client, SocketOptionName.TcpKeepAliveInterval, _keepAliveIntervalSeconds);
    }

    /**
      * Tries to resolve the set tcp keep alive option value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: client, optionName, valueSeconds.
      */
    private static void TrySetTcpKeepAliveOption(TcpClient client, SocketOptionName optionName, int valueSeconds)
    {
        if (valueSeconds <= 0)
        {
            return;
        }

        try
        {
            client.Client.SetSocketOption(SocketOptionLevel.Tcp, optionName, valueSeconds);
        }
        catch (SocketException)
        {
            // Some platforms do not expose per-socket TCP keep-alive tuning. KeepAlive itself is still enabled.
        }
        catch (ObjectDisposedException)
        {
            // The socket is already closed.
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
            "InternalPeerConnector");

        try
        {
            await _callbacks.NotifyPeerReconnectTimedOutAsync(peer.Name, peer.ReconnectTimeout, cancellationToken);
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.CRITICAL, exception.ToString(), "InternalPeerConnector");
        }
    }

    /**
      * Completes the internal authentication flow before normal packets are exchanged.
      * The method is part of InternalPeerConnector and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    private async Task AuthenticateWithPeerAsync(
        InternalPeerSettings peer,
        InternalProtocolReader reader,
        NetworkStream stream,
        SemaphoreSlim sendLock,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource authenticationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        authenticationCancellation.CancelAfter(_authenticationTimeout);

        string? challenge = await reader.ReadLineAsync(
            InternalProtocol.MaximumAuthenticationLineLength,
            authenticationCancellation.Token);

        if (challenge is null)
        {
            throw new InvalidOperationException($"Internal peer {peer.Name} disconnected before requesting authentication.");
        }

        string[] challengeParts = challenge.Split(' ', 3, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (challengeParts.Length != 3 || !string.Equals(challengeParts[0], InternalProtocol.AuthenticationChallenge, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Internal peer {peer.Name} sent an invalid authentication challenge.");
        }

        string challengedServerName = challengeParts[1];
        string challengeNonce = challengeParts[2];

        if (!string.Equals(challengedServerName, peer.Name, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Internal peer {peer.Name} identified as unexpected server '{challengedServerName}'.");
        }

        string authenticationProof = InternalProtocol.CreateAuthenticationProof(
            _registrationKey,
            _serverName,
            challengedServerName,
            challengeNonce);

        await InternalProtocol.WriteLineAsync(
            stream,
            sendLock,
            $"{InternalProtocol.AuthenticationResponse} {_serverName} {authenticationProof}",
            authenticationCancellation.Token);

        string? response = await reader.ReadLineAsync(
            InternalProtocol.MaximumAuthenticationLineLength,
            authenticationCancellation.Token);

        if (response is null)
        {
            throw new InvalidOperationException($"Internal peer {peer.Name} disconnected before accepting authentication.");
        }

        string[] responseParts = response.Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (responseParts.Length != 2 || !string.Equals(responseParts[0], InternalProtocol.AuthenticationAccepted, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Internal peer {peer.Name} rejected authentication.");
        }

        if (!string.Equals(responseParts[1], peer.Name, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Internal peer {peer.Name} accepted authentication as unexpected server '{responseParts[1]}'.");
        }
    }

    /**
      * Processes incoming data and dispatches it to the correct subsystem handler.
      * The method is part of InternalPeerConnector and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    private async Task ProcessAuthenticatedPeerAsync(
        InternalPeerConnection connection,
        InternalProtocolReader reader,
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
            _latencyLoggingEnabled,
            _latencyLogInterval,
            _pingTimeout,
            (serverName, latency) => _callbacks.NotifyLatencyMeasured(serverName, latency),
            (serverName, elapsed) => _callbacks.NotifyPingTimedOut(serverName, elapsed));

        latencyMonitor.Start(cancellationToken);

        Channel<string> packetDispatchQueue = Channel.CreateBounded<string>(new BoundedChannelOptions(InternalPeerPacketDispatchQueueCapacity)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = false,
        });

        Task packetDispatchLoop = Task.Run(
            () => ProcessQueuedPeerPacketsAsync(connection, packetDispatchQueue.Reader, cancellationToken),
            CancellationToken.None);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                string? line = await reader.ReadLineAsync(
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

                if (await TryProcessPeerControlPacketAsync(connection, line, latencyMonitor, cancellationToken))
                {
                    continue;
                }

                LogPeerPacket(connection, line);
                await packetDispatchQueue.Writer.WriteAsync(line, cancellationToken);
            }
        }
        finally
        {
            packetDispatchQueue.Writer.TryComplete();
            await WaitForPeerPacketDispatchLoopAsync(packetDispatchLoop);
        }
    }


    /**
      * Processes incoming data and dispatches it to the correct subsystem handler.
      * The method is part of InternalPeerConnector and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    private async Task<bool> TryProcessPeerControlPacketAsync(
        InternalPeerConnection connection,
        string line,
        InternalLatencyMonitor latencyMonitor,
        CancellationToken cancellationToken)
    {
        string[] parts = line.Split(' ', 3, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        if (parts.Length >= 2 && string.Equals(parts[0], InternalProtocol.Ping, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.TRACE, $"{_serverName} received PING packet from {connection.RemoteServerName}.", "InternalPeerConnector");
            await latencyMonitor.RespondToPingAsync(parts[1], cancellationToken);
            return true;
        }

        if (parts.Length >= 2 && string.Equals(parts[0], InternalProtocol.Pong, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.TRACE, $"{_serverName} received PONG packet from {connection.RemoteServerName}.", "InternalPeerConnector");
            latencyMonitor.RecordPong(parts[1]);
            return true;
        }

        if (parts.Length >= 2 && string.Equals(parts[0], InternalProtocol.ShutdownRequest, StringComparison.OrdinalIgnoreCase))
        {
            string reason = parts.Length == 3 ? parts[2] : "No reason provided.";
            await _callbacks.NotifyShutdownRequestedAsync(parts[1], reason, cancellationToken);
            return true;
        }

        return false;
    }

    /**
      * Dispatches non-control packets from an authenticated peer on a worker path.
      */
    private async Task ProcessQueuedPeerPacketsAsync(
        InternalPeerConnection connection,
        ChannelReader<string> reader,
        CancellationToken cancellationToken)
    {
        try
        {
            while (await reader.WaitToReadAsync(cancellationToken))
            {
                while (reader.TryRead(out string? line))
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    await _callbacks.NotifyPeerPacketReceivedAsync(connection, connection.RemoteServerName, line, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown/reconnect.
        }
        catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException or InvalidOperationException)
        {
            Logger.Write(LogType.NETWORK, $"{_serverName} peer packet dispatcher stopped for {connection.RemoteServerName}: {exception.Message}", "InternalPeerConnector");
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.CRITICAL, exception.ToString(), "InternalPeerConnector");
        }
    }

    /**
      * Waits briefly for queued peer dispatch to stop without delaying reconnects for a long time.
      */
    private static async Task WaitForPeerPacketDispatchLoopAsync(Task packetDispatchLoop)
    {
        if (packetDispatchLoop.IsCompleted)
        {
            await packetDispatchLoop;
            return;
        }

        try
        {
            Task completedTask = await Task.WhenAny(packetDispatchLoop, Task.Delay(TimeSpan.FromSeconds(1)));
            if (completedTask == packetDispatchLoop)
            {
                await packetDispatchLoop;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    /**
      * Keeps normal peer packet logging out of the socket reader loop body.
      */
    private void LogPeerPacket(InternalPeerConnection connection, string line)
    {
        string[] parts = line.Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return;
        }

        if (string.Equals(parts[0], InternalProtocol.WorldCapacity, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.NETWORK, $"{_serverName} received world capacity packet from {connection.RemoteServerName}: {line}", "InternalPeerConnector");
        }
        else if (!IsQuietMapServicePacket(parts[0]))
        {
            Logger.Write(LogType.DEBUG, $"{_serverName} received internal packet from peer {connection.RemoteServerName}: {line}", "InternalPeerConnector");
        }
    }


    /**
      * Returns true for high-volume map-service packets that should be dispatched without per-packet connector logging.
      */
    private static bool IsQuietMapServicePacket(string opcode)
    {
        return string.Equals(opcode, InternalProtocol.MapServiceStatus, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(opcode, InternalProtocol.MapServiceCommand, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(opcode, InternalProtocol.MapServiceCommandResult, StringComparison.OrdinalIgnoreCase);
    }
}
