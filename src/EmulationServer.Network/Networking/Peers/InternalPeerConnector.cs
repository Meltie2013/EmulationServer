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
// File: src/EmulationServer.Network/Networking/Peers/InternalPeerConnector.cs
// Purpose: Contains internal peer connector code for the packet serialization, socket transport, and protocol framing layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Net.Sockets;
using System.Threading.Channels;

using EmulationServer.Network.Configuration;
using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Health;
using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Network.Networking.Socket;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Network.Networking.Peers;

// Type: InternalPeerConnector
// Purpose: Provides internal peer connector behavior for the packet serialization, socket transport, and protocol framing layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class InternalPeerConnector : IAsyncDisposable
{

    // Constant: Defines the internal peer packet dispatch queue capacity constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed internal peer packet dispatch queue capacity value used anywhere this rule or protocol value is needed.
    private const int InternalPeerPacketDispatchQueueCapacity = 4096;

    // Field: Stores the server name state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current server name backing value maintained by the owning type.
    private readonly string _serverName;

    // Field: Stores the peers state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current peers backing value maintained by the owning type.
    private readonly IReadOnlyList<InternalPeerSettings> _peers;

    // Field: Stores the registration key state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current registration key backing value maintained by the owning type.
    private readonly string _registrationKey;

    // Field: Stores the latency report interval state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current latency report interval backing value maintained by the owning type.
    private readonly TimeSpan _latencyReportInterval;

    // Field: Stores the latency logging enabled state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current latency logging enabled backing value maintained by the owning type.
    private readonly bool _latencyLoggingEnabled;

    // Field: Stores the latency log interval state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current latency log interval backing value maintained by the owning type.
    private readonly TimeSpan _latencyLogInterval;

    // Field: Stores the ping timeout state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current ping timeout backing value maintained by the owning type.
    private readonly TimeSpan _pingTimeout;

    // Field: Stores the receive buffer size state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current receive buffer size backing value maintained by the owning type.
    private readonly int _receiveBufferSize;

    // Field: Stores the send buffer size state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current send buffer size backing value maintained by the owning type.
    private readonly int _sendBufferSize;

    // Field: Stores the keep alive state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current keep alive backing value maintained by the owning type.
    private readonly bool _keepAlive;

    // Field: Stores the keep alive time seconds state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current keep alive time seconds backing value maintained by the owning type.
    private readonly int _keepAliveTimeSeconds;

    // Field: Stores the keep alive interval seconds state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current keep alive interval seconds backing value maintained by the owning type.
    private readonly int _keepAliveIntervalSeconds;

    // Field: Stores the authentication timeout state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current authentication timeout backing value maintained by the owning type.
    private readonly TimeSpan _authenticationTimeout;

    // Field: Stores the callbacks state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current callbacks backing value maintained by the owning type.
    private readonly InternalNetworkCallbacks _callbacks;

    // Field: Stores the connection tasks state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current connection tasks backing value maintained by the owning type.
    private readonly List<Task> _connectionTasks = [];

    private readonly object _syncRoot = new();

    // Field: Stores the stop cancellation state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current stop cancellation backing value maintained by the owning type.
    private CancellationTokenSource? _stopCancellation;

    // Field: Stores the started state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current started backing value maintained by the owning type.
    private int _started;

    // Field: Stores the stopping state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current stopping backing value maintained by the owning type.
    private int _stopping;

    // Constructor: InternalPeerConnector
    // Purpose: Initializes a new InternalPeerConnector instance with dependencies and values required by the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - serverName: Server name value supplied by the caller for this operation.
    // - peers: Peers value supplied by the caller for this operation.
    // - registrationKey: Registration key value supplied by the caller for this operation.
    // - latencyReportInterval: Latency report interval value supplied by the caller for this operation.
    // - latencyLoggingEnabled: Latency logging enabled value supplied by the caller for this operation.
    // - latencyLogInterval: Latency log interval value supplied by the caller for this operation.
    // - pingTimeout: Ping timeout value supplied by the caller for this operation.
    // - receiveBufferSize: Receive buffer size value supplied by the caller for this operation.
    // - sendBufferSize: Send buffer size value supplied by the caller for this operation.
    // - keepAlive: Keep alive value supplied by the caller for this operation.
    // - keepAliveTimeSeconds: Keep alive time seconds value supplied by the caller for this operation.
    // - keepAliveIntervalSeconds: Keep alive interval seconds value supplied by the caller for this operation.
    // - authenticationTimeout: Authentication timeout value supplied by the caller for this operation.
    // - callbacks: Callbacks value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to InternalPeerConnector so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: StartAsync
    // Purpose: Controls the start lifecycle step for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalPeerConnector so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: StopAsync
    // Purpose: Controls the stop lifecycle step for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalPeerConnector so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: DisposeAsync
    // Purpose: Controls the dispose lifecycle step for the packet serialization, socket transport, and protocol framing layer.
    // Parameters: none.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalPeerConnector so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
    }

    // Method: RunPeerLoopAsync
    // Purpose: Controls the run peer loop lifecycle step for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - peer: Peer value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalPeerConnector so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: ConfigureClient
    // Purpose: Executes the configure client operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - client: Client value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to InternalPeerConnector so callers do not duplicate validation, protocol, or persistence rules.
    private void ConfigureClient(TcpClient client)
    {
        TcpSocketOptions.ConfigureClient(
            client,
            _receiveBufferSize,
            _sendBufferSize,
            _keepAlive,
            _keepAliveTimeSeconds,
            _keepAliveIntervalSeconds);
    }

    // Method: GetRemainingReconnectWindow
    // Purpose: Retrieves get remaining reconnect window data for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - peer: Peer value supplied by the caller for this operation.
    // - reconnectWindowStartedUtc: Reconnect window started utc value supplied by the caller for this operation.
    // - nowUtc: Now utc value supplied by the caller for this operation.
    // Returns: Returns the time span value produced by this operation.
    // Notes: This keeps the operation scoped to InternalPeerConnector so callers do not duplicate validation, protocol, or persistence rules.
    private static TimeSpan GetRemainingReconnectWindow(
        InternalPeerSettings peer,
        DateTimeOffset reconnectWindowStartedUtc,
        DateTimeOffset nowUtc)
    {
        TimeSpan elapsed = nowUtc - reconnectWindowStartedUtc;
        TimeSpan remaining = peer.ReconnectTimeout - elapsed;

        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    // Method: StopReconnectAttemptsAsync
    // Purpose: Controls the stop reconnect attempts lifecycle step for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - peer: Peer value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalPeerConnector so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: AuthenticateWithPeerAsync
    // Purpose: Executes the authenticate with peer operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - peer: Peer value supplied by the caller for this operation.
    // - reader: Database reader used to execute this operation without opening unnecessary additional state.
    // - stream: Stream value supplied by the caller for this operation.
    // - sendLock: Send lock value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalPeerConnector so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: ProcessAuthenticatedPeerAsync
    // Purpose: Executes the process authenticated peer operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - reader: Database reader used to execute this operation without opening unnecessary additional state.
    // - stream: Stream value supplied by the caller for this operation.
    // - sendLock: Send lock value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalPeerConnector so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: TryProcessPeerControlPacketAsync
    // Purpose: Executes the try process peer control packet operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - line: Line value supplied by the caller for this operation.
    // - latencyMonitor: Latency monitor value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous Boolean result that is true when try process peer control packet async succeeds or the requested condition is met.
    // Notes: This keeps the operation scoped to InternalPeerConnector so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: ProcessQueuedPeerPacketsAsync
    // Purpose: Executes the process queued peer packets operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - reader: Database reader used to execute this operation without opening unnecessary additional state.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalPeerConnector so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: WaitForPeerPacketDispatchLoopAsync
    // Purpose: Handles wait for peer packet dispatch loop work for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - packetDispatchLoop: Packet dispatch loop value supplied by the caller for this operation.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalPeerConnector so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: LogPeerPacket
    // Purpose: Executes the log peer packet operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - line: Line value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to InternalPeerConnector so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: IsQuietMapServicePacket
    // Purpose: Validates or evaluates is quiet map service packet rules for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - opcode: Opcode value supplied by the caller for this operation.
    // Returns: Returns true when is quiet map service packet succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to InternalPeerConnector so callers do not duplicate validation, protocol, or persistence rules.
    private static bool IsQuietMapServicePacket(string opcode)
    {
        return string.Equals(opcode, InternalProtocol.MapServiceStatus, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(opcode, InternalProtocol.MapServiceCommand, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(opcode, InternalProtocol.MapServiceCommandResult, StringComparison.OrdinalIgnoreCase);
    }
}
