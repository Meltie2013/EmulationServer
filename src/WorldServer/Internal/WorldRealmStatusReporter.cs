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

using System.Globalization;
using System.Net.Sockets;
using EmulationServer.Network.Networking.Health;
using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.WorldServer.Configuration;

/**
  * File overview: src/WorldServer/Internal/WorldRealmStatusReporter.cs
  * Documents the WorldRealmStatusReporter source file in the world server startup, client networking, gameplay routing, and persistence area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.WorldServer.Internal;

/**
  * Owns the world realm status reporter behavior for the world server startup, client networking, gameplay routing, and persistence layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class WorldRealmStatusReporter : IAsyncDisposable
{
    /**
      * Holds the private settings state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly RealmStatusSettings _settings;
    /**
      * Holds the private registration key state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly string _registrationKey;
    /**
      * Holds the private send lock state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    /**
      * Holds the private max connections state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly int _maxConnections;
    /**
      * Holds the private latency report interval state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly TimeSpan _latencyReportInterval;
    /**
      * Holds whether successful RealmServer latency values should be logged during normal runtime.
      */
    private readonly bool _latencyLoggingEnabled;
    /**
      * Holds the minimum delay between visible RealmServer latency log lines.
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
    private readonly Func<CancellationToken, Task<IReadOnlyDictionary<uint, byte>>> _characterCountSnapshotLoader;

    /**
      * Holds the private stop cancellation state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private CancellationTokenSource? _stopCancellation;
    /**
      * Holds the private report task state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private Task? _reportTask;
    /**
      * Holds the private client state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private TcpClient? _client;
    /**
      * Holds the private stream state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private NetworkStream? _stream;
    /**
      * Holds the private reader state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private InternalProtocolReader? _reader;
    /**
      * Holds the private started state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private int _started;
    /**
      * Holds the private active connections state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private int _activeConnections;

    /**
      * Initializes a new WorldRealmStatusReporter instance with the dependencies required by the world server startup, client networking, gameplay routing, and persistence workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: settings, registrationKey, maxConnections, latencyReportInterval, pingTimeout, receiveBufferSize....
      */
    public WorldRealmStatusReporter(
        RealmStatusSettings settings,
        string registrationKey,
        int maxConnections,
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
        Func<CancellationToken, Task<IReadOnlyDictionary<uint, byte>>> characterCountSnapshotLoader)
    {
        _settings = settings ?? throw new ArgumentNullException();

        if (string.IsNullOrWhiteSpace(registrationKey))
        {
            throw new ArgumentException("Registration key is required.");
        }

        if (maxConnections <= 0)
        {
            throw new ArgumentOutOfRangeException(null, "WorldServer max connections must be greater than zero.");
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

        _settings.Validate();
        _registrationKey = registrationKey;
        _maxConnections = maxConnections;
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
        _characterCountSnapshotLoader = characterCountSnapshotLoader ?? throw new ArgumentNullException();
    }

    /**
      * Starts the start workflow and prepares the component to accept runtime work.
      * Startup is ordered so validation and dependency setup finish before services are announced as available.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_settings.Enabled)
        {
            Logger.Write(LogType.NETWORK, "WorldServer realm status reporting is disabled.", "WorldRealmStatusReporter");
            return Task.CompletedTask;
        }

        if (Interlocked.Exchange(ref _started, 1) == 1)
        {
            throw new InvalidOperationException("WorldServer realm status reporter has already been started.");
        }

        _stopCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _reportTask = Task.Run(() => RunAsync(_stopCancellation.Token), CancellationToken.None);

        Logger.Write(LogType.NETWORK, $"WorldServer realm status reporter started for realm {_settings.RealmId}.", "WorldRealmStatusReporter");

        return Task.CompletedTask;
    }

    /**
      * Updates the active player count used by the next realm-status packet.
      */
    public void SetActiveConnections(int activeConnections)
    {
        Interlocked.Exchange(ref _activeConnections, Math.Max(0, activeConnections));
    }

    /**
      * Sends the current realm-status snapshot immediately instead of waiting for the next periodic update.
      */
    public async Task SendRealmStatusNowAsync(CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _started) == 0 || _stream is null)
        {
            return;
        }

        try
        {
            await SendRealmStatusAsync(true, Volatile.Read(ref _activeConnections), cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
        {
            Logger.Write(LogType.WARNING, $"Unable to send immediate realm status: {exception.Message}", "WorldRealmStatusReporter");
        }
    }

    /**
      * Sends a character-count snapshot immediately when character storage changes.
      */
    public async Task SendCharacterCountSnapshotNowAsync(CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _started) == 0 || _stream is null)
        {
            return;
        }

        try
        {
            await SendCharacterCountSnapshotAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
        {
            Logger.Write(LogType.WARNING, $"Unable to send immediate character-count snapshot: {exception.Message}", "WorldRealmStatusReporter");
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
        if (Interlocked.Exchange(ref _started, 0) == 0)
        {
            return;
        }

        try
        {
            if (_stream is not null)
            {
                await SendRealmStatusAsync(false, 0, cancellationToken);
            }
        }
        catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
        {
            Logger.Write(LogType.WARNING, $"Unable to send offline realm status before shutdown: {exception.Message}", "WorldRealmStatusReporter");
        }

        if (_stopCancellation is not null)
        {
            await _stopCancellation.CancelAsync();
        }

        if (_reportTask is not null)
        {
            try
            {
                await _reportTask;
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
        }

        CleanupConnection();

        _stopCancellation?.Dispose();
        _stopCancellation = null;
        _reportTask = null;

        Logger.Write(LogType.NETWORK, "WorldServer realm status reporter stopped.", "WorldRealmStatusReporter");
    }

    /**
      * Stops the dispose workflow and releases owned runtime resources in a controlled order.
      * Shutdown logic is centralized to avoid dangling connections, incomplete saves, or partially registered services.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        _sendLock.Dispose();
    }

    /**
      * Runs the main loop for this component until cancellation or shutdown is requested.
      * The method is part of WorldRealmStatusReporter and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndAuthenticateAsync(cancellationToken);

                NetworkStream stream = _stream ?? throw new IOException("RealmServer connection is not available.");
                await using InternalLatencyMonitor latencyMonitor = new(
                    "WorldServer",
                    "RealmServer",
                    stream,
                    _sendLock,
                    _latencyReportInterval,
                    _latencyLoggingEnabled,
                    _latencyLogInterval,
                    _pingTimeout);

                latencyMonitor.Start(cancellationToken);

                Task receiveTask = ProcessRealmServerPacketsAsync(latencyMonitor, cancellationToken);
                Task statusTask = SendRealmStatusLoopAsync(cancellationToken);

                Task completedTask = await Task.WhenAny(receiveTask, statusTask);
                await completedTask;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                Logger.Write(LogType.WARNING, $"WorldServer could not update RealmServer status: {exception.Message}", "WorldRealmStatusReporter");
                CleanupConnection();

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    /**
      * Sends a shutdown request to RealmServer over the realm-status internal connection.
      */
    public async Task<bool> SendShutdownRequestAsync(string reason, CancellationToken cancellationToken = default)
    {
        if (_stream is null)
        {
            return false;
        }

        string safeReason = string.IsNullOrWhiteSpace(reason) ? "No reason provided." : reason.Trim();
        string packet = $"{InternalProtocol.ShutdownRequest} WorldServer {safeReason}";

        await InternalProtocol.WriteLineAsync(
            _stream,
            _sendLock,
            packet,
            cancellationToken);

        return true;
    }

    /**
      * Sends a protocol message or status update to a connected peer.
      * The method is part of WorldRealmStatusReporter and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task SendRealmStatusLoopAsync(CancellationToken cancellationToken)
    {
        await SendRealmStatusAsync(true, Volatile.Read(ref _activeConnections), cancellationToken);
        await SendCharacterCountSnapshotAsync(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(_settings.UpdateInterval, cancellationToken);
            await SendRealmStatusAsync(true, Volatile.Read(ref _activeConnections), cancellationToken);
        }
    }

    /**
      * Sends send character count snapshot data to the connected session or internal peer.
      * The send path keeps packet construction and delivery together so opcode handling remains easy to trace during protocol debugging.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private async Task SendCharacterCountSnapshotAsync(CancellationToken cancellationToken)
    {
        if (_stream is null)
        {
            return;
        }

        IReadOnlyDictionary<uint, byte> characterCounts;
        try
        {
            characterCounts = await _characterCountSnapshotLoader(cancellationToken);
        }
        catch (Exception exception) when (exception is MySqlConnector.MySqlException or InvalidOperationException or IOException)
        {
            Logger.Write(LogType.WARNING, $"WorldServer could not load character-count snapshot for RealmServer: {exception.Message}", "WorldRealmStatusReporter");
            return;
        }

        uint realmId = _settings.RealmId;
        await InternalProtocol.WriteLineAsync(
            _stream,
            _sendLock,
            $"{InternalProtocol.RealmCharacterCountSnapshotBegin} {realmId}",
            cancellationToken);

        const int MaxPairsPerPacket = 64;
        List<string> pairs = [];
        foreach ((uint accountId, byte count) in characterCounts.OrderBy(pair => pair.Key))
        {
            pairs.Add(string.Create(CultureInfo.InvariantCulture, $"{accountId}:{count}"));

            if (pairs.Count >= MaxPairsPerPacket)
            {
                await SendCharacterCountSnapshotDataAsync(realmId, pairs, cancellationToken);
                pairs.Clear();
            }
        }

        if (pairs.Count > 0)
        {
            await SendCharacterCountSnapshotDataAsync(realmId, pairs, cancellationToken);
        }

        await InternalProtocol.WriteLineAsync(
            _stream,
            _sendLock,
            $"{InternalProtocol.RealmCharacterCountSnapshotEnd} {realmId}",
            cancellationToken);

        Logger.Write(LogType.TRACE, $"WorldServer sent realm {realmId} character-count snapshot: {characterCounts.Count} account(s).", "WorldRealmStatusReporter");
    }

    /**
      * Sends one character-count data chunk.
      */
    private async Task SendCharacterCountSnapshotDataAsync(uint realmId, IReadOnlyList<string> pairs, CancellationToken cancellationToken)
    {
        if (_stream is null || pairs.Count == 0)
        {
            return;
        }

        string packet = $"{InternalProtocol.RealmCharacterCountSnapshotData} {realmId} {string.Join(' ', pairs)}";
        await InternalProtocol.WriteLineAsync(
            _stream,
            _sendLock,
            packet,
            cancellationToken);
    }

    /**
      * Processes incoming data and dispatches it to the correct subsystem handler.
      * The method is part of WorldRealmStatusReporter and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task ProcessRealmServerPacketsAsync(InternalLatencyMonitor latencyMonitor, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            InternalProtocolReader reader = _reader ?? throw new IOException("RealmServer connection reader is not available.");

            string? line = await reader.ReadLineAsync(
                InternalProtocol.MaximumPacketLineLength,
                cancellationToken);

            if (line is null)
            {
                throw new IOException("RealmServer disconnected from WorldServer realm status reporter.");
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            await ProcessRealmServerPacketAsync(line, latencyMonitor, cancellationToken);
        }
    }

    /**
      * Processes incoming data and dispatches it to the correct subsystem handler.
      * The method is part of WorldRealmStatusReporter and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task ProcessRealmServerPacketAsync(string line, InternalLatencyMonitor latencyMonitor, CancellationToken cancellationToken)
    {
        string[] parts = line.Split(' ', 3, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            return;
        }

        if (parts.Length >= 2 && string.Equals(parts[0], InternalProtocol.Ping, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.TRACE, "WorldServer received PING packet from RealmServer.", "WorldRealmStatusReporter");
            await latencyMonitor.RespondToPingAsync(parts[1], cancellationToken);
            return;
        }

        if (parts.Length >= 2 && string.Equals(parts[0], InternalProtocol.Pong, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.TRACE, "WorldServer received PONG packet from RealmServer.", "WorldRealmStatusReporter");
            latencyMonitor.RecordPong(parts[1]);
            return;
        }

        if (parts.Length >= 2 && string.Equals(parts[0], InternalProtocol.ShutdownRequest, StringComparison.OrdinalIgnoreCase))
        {
            string reason = parts.Length == 3 ? parts[2] : "No reason provided.";
            Logger.Write(LogType.WARNING, $"WorldServer received shutdown request from {parts[1]}: {reason}", "WorldRealmStatusReporter");
            return;
        }

        Logger.Write(LogType.DEBUG, $"WorldServer received RealmServer internal packet: {line}", "WorldRealmStatusReporter");
    }

    /**
      * Creates or restores an internal network connection to the target server.
      * The method is part of WorldRealmStatusReporter and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task ConnectAndAuthenticateAsync(CancellationToken cancellationToken)
    {
        CleanupConnection();

        _client = new TcpClient
        {
            NoDelay = true,
            ReceiveBufferSize = _receiveBufferSize,
            SendBufferSize = _sendBufferSize,
        };

        if (_keepAlive)
        {
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            TrySetTcpKeepAliveOption(_client, SocketOptionName.TcpKeepAliveTime, _keepAliveTimeSeconds);
            TrySetTcpKeepAliveOption(_client, SocketOptionName.TcpKeepAliveInterval, _keepAliveIntervalSeconds);
        }

        Logger.Write(LogType.NETWORK, $"WorldServer connecting to RealmServer internal listener at {_settings.RealmServerHost}:{_settings.RealmServerPort}...", "WorldRealmStatusReporter");

        await _client.ConnectAsync(_settings.RealmServerHost, _settings.RealmServerPort, cancellationToken);
        _stream = _client.GetStream();
        _reader = new InternalProtocolReader(_stream);

        using CancellationTokenSource authenticationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        authenticationCancellation.CancelAfter(_authenticationTimeout);

        string? challenge = await _reader.ReadLineAsync(
            InternalProtocol.MaximumAuthenticationLineLength,
            authenticationCancellation.Token);

        if (challenge is null)
        {
            throw new InvalidOperationException("RealmServer disconnected before authentication challenge.");
        }

        string[] challengeParts = challenge.Split(' ', 3, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (challengeParts.Length != 3 || !string.Equals(challengeParts[0], InternalProtocol.AuthenticationChallenge, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("RealmServer sent an invalid authentication challenge.");
        }

        string challengedServerName = challengeParts[1];
        string challengeNonce = challengeParts[2];

        if (!string.Equals(challengedServerName, "RealmServer", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"RealmServer internal listener identified as unexpected server '{challengedServerName}'.");
        }

        string authenticationProof = InternalProtocol.CreateAuthenticationProof(
            _registrationKey,
            "WorldServer",
            challengedServerName,
            challengeNonce);

        await InternalProtocol.WriteLineAsync(
            _stream,
            _sendLock,
            $"{InternalProtocol.AuthenticationResponse} WorldServer {authenticationProof}",
            authenticationCancellation.Token);

        string? response = await _reader.ReadLineAsync(
            InternalProtocol.MaximumAuthenticationLineLength,
            authenticationCancellation.Token);

        if (response is null)
        {
            throw new InvalidOperationException("RealmServer disconnected before accepting authentication.");
        }

        string[] responseParts = response.Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (responseParts.Length != 2 || !string.Equals(responseParts[0], InternalProtocol.AuthenticationAccepted, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("RealmServer rejected WorldServer authentication.");
        }

        if (!string.Equals(responseParts[1], "RealmServer", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"RealmServer accepted WorldServer authentication as unexpected server '{responseParts[1]}'.");
        }

        Logger.Write(LogType.NETWORK, "WorldServer authenticated with RealmServer internal listener.", "WorldRealmStatusReporter");
    }

    /**
      * Sends a protocol message or status update to a connected peer.
      * The method is part of WorldRealmStatusReporter and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task SendRealmStatusAsync(bool online, int activeConnections, CancellationToken cancellationToken)
    {
        if (_stream is null)
        {
            return;
        }

        int safeActiveConnections = Math.Max(0, activeConnections);
        int safePopulationCapacityLimit = Math.Max(1, _settings.PopulationCapacityLimit > 0 ? _settings.PopulationCapacityLimit : _maxConnections);
        string state = online ? "online" : "offline";

        string packet = $"REALM_STATUS {_settings.RealmId} {state} {safeActiveConnections} {safePopulationCapacityLimit}";

        await InternalProtocol.WriteLineAsync(
            _stream,
            _sendLock,
            packet,
            cancellationToken);

        Logger.Write(LogType.TRACE, $"WorldServer sent realm status: {packet}", "WorldRealmStatusReporter");
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
      * Performs the cleanup connection operation for the world server startup, client networking, gameplay routing, and persistence workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      */
    private void CleanupConnection()
    {
        try
        {
            _reader?.Dispose();
        }
        catch
        {
            // Ignore cleanup exceptions.
        }

        try
        {
            _stream?.Dispose();
        }
        catch
        {
            // Ignore cleanup exceptions.
        }

        try
        {
            _client?.Dispose();
        }
        catch
        {
            // Ignore cleanup exceptions.
        }

        _reader = null;
        _stream = null;
        _client = null;
    }
}
