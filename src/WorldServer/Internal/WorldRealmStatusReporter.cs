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
// File: src/WorldServer/Internal/WorldRealmStatusReporter.cs
// Purpose: Contains world realm status reporter code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Globalization;
using System.Net.Sockets;
using EmulationServer.Network.Networking.Health;
using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Network.Networking.Socket;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.WorldServer.Configuration;

namespace EmulationServer.WorldServer.Internal;

// Type: WorldRealmStatusReporter
// Purpose: Provides world realm status reporter behavior for the world server gameplay, session, and character runtime layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class WorldRealmStatusReporter : IAsyncDisposable
{

    // Field: Stores the settings state used by the world server gameplay, session, and character runtime layer.
    // Value: current settings backing value maintained by the owning type.
    private readonly RealmStatusSettings _settings;

    // Field: Stores the registration key state used by the world server gameplay, session, and character runtime layer.
    // Value: current registration key backing value maintained by the owning type.
    private readonly string _registrationKey;

    private readonly SemaphoreSlim _sendLock = new(1, 1);

    // Field: Stores the max connections state used by the world server gameplay, session, and character runtime layer.
    // Value: current max connections backing value maintained by the owning type.
    private readonly int _maxConnections;

    // Field: Stores the latency report interval state used by the world server gameplay, session, and character runtime layer.
    // Value: current latency report interval backing value maintained by the owning type.
    private readonly TimeSpan _latencyReportInterval;

    // Field: Stores the latency logging enabled state used by the world server gameplay, session, and character runtime layer.
    // Value: current latency logging enabled backing value maintained by the owning type.
    private readonly bool _latencyLoggingEnabled;

    // Field: Stores the latency log interval state used by the world server gameplay, session, and character runtime layer.
    // Value: current latency log interval backing value maintained by the owning type.
    private readonly TimeSpan _latencyLogInterval;

    // Field: Stores the ping timeout state used by the world server gameplay, session, and character runtime layer.
    // Value: current ping timeout backing value maintained by the owning type.
    private readonly TimeSpan _pingTimeout;

    // Field: Stores the receive buffer size state used by the world server gameplay, session, and character runtime layer.
    // Value: current receive buffer size backing value maintained by the owning type.
    private readonly int _receiveBufferSize;

    // Field: Stores the send buffer size state used by the world server gameplay, session, and character runtime layer.
    // Value: current send buffer size backing value maintained by the owning type.
    private readonly int _sendBufferSize;

    // Field: Stores the keep alive state used by the world server gameplay, session, and character runtime layer.
    // Value: current keep alive backing value maintained by the owning type.
    private readonly bool _keepAlive;

    // Field: Stores the keep alive time seconds state used by the world server gameplay, session, and character runtime layer.
    // Value: current keep alive time seconds backing value maintained by the owning type.
    private readonly int _keepAliveTimeSeconds;

    // Field: Stores the keep alive interval seconds state used by the world server gameplay, session, and character runtime layer.
    // Value: current keep alive interval seconds backing value maintained by the owning type.
    private readonly int _keepAliveIntervalSeconds;

    // Field: Stores the authentication timeout state used by the world server gameplay, session, and character runtime layer.
    // Value: current authentication timeout backing value maintained by the owning type.
    private readonly TimeSpan _authenticationTimeout;
    // Field: Stores the cancellation token state used by the world server gameplay, session, and character runtime layer.
    // Value: current cancellation token backing value maintained by the owning type.
    private readonly Func<CancellationToken, Task<IReadOnlyDictionary<uint, byte>>> _characterCountSnapshotLoader;

    // Field: Stores the internal latency callback used by movement timing telemetry.
    // Value: optional callback invoked when RealmServer latency is measured.
    private readonly Action<string, TimeSpan>? _latencyMeasured;

    // Field: Stores the stop cancellation state used by the world server gameplay, session, and character runtime layer.
    // Value: current stop cancellation backing value maintained by the owning type.
    private CancellationTokenSource? _stopCancellation;

    // Field: Stores the report task state used by the world server gameplay, session, and character runtime layer.
    // Value: current report task backing value maintained by the owning type.
    private Task? _reportTask;

    // Field: Stores the client state used by the world server gameplay, session, and character runtime layer.
    // Value: current client backing value maintained by the owning type.
    private TcpClient? _client;

    // Field: Stores the stream state used by the world server gameplay, session, and character runtime layer.
    // Value: current stream backing value maintained by the owning type.
    private NetworkStream? _stream;

    // Field: Stores the reader state used by the world server gameplay, session, and character runtime layer.
    // Value: current reader backing value maintained by the owning type.
    private InternalProtocolReader? _reader;

    // Field: Stores the started state used by the world server gameplay, session, and character runtime layer.
    // Value: current started backing value maintained by the owning type.
    private int _started;

    // Field: Stores the authenticated state used by the world server gameplay, session, and character runtime layer.
    // Value: current authenticated backing value maintained by the owning type.
    private int _authenticated;

    // Field: Stores the active connections state used by the world server gameplay, session, and character runtime layer.
    // Value: current active connections backing value maintained by the owning type.
    private int _activeConnections;

    // Constructor: WorldRealmStatusReporter
    // Purpose: Initializes a new WorldRealmStatusReporter instance with dependencies and values required by the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - settings: Settings values that control how this operation should run.
    // - registrationKey: Registration key value supplied by the caller for this operation.
    // - maxConnections: Max connections value supplied by the caller for this operation.
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
    // - characterCountSnapshotLoader: Character count snapshot loader value supplied by the caller for this operation.
    // - latencyMeasured: Optional callback invoked when latency to RealmServer is measured.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldRealmStatusReporter so callers do not duplicate validation, protocol, or persistence rules.
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
        Func<CancellationToken, Task<IReadOnlyDictionary<uint, byte>>> characterCountSnapshotLoader,
        Action<string, TimeSpan>? latencyMeasured = null)
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
        _latencyMeasured = latencyMeasured;
    }

    // Method: StartAsync
    // Purpose: Controls the start lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldRealmStatusReporter so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: Read
    // Purpose: Retrieves read data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - _started: Started value supplied by the caller for this operation.
    // Returns: Returns the bool is connected => volatile. value produced by this operation.
    // Notes: This keeps the operation scoped to WorldRealmStatusReporter so callers do not duplicate validation, protocol, or persistence rules.
    public bool IsConnected => Volatile.Read(ref _started) == 1 && _stream is not null && Volatile.Read(ref _authenticated) == 1;

    // Method: WaitForConnectionAsync
    // Purpose: Handles wait for connection work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - reason: Reason value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldRealmStatusReporter so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task WaitForConnectionAsync(string reason, CancellationToken cancellationToken)
    {
        if (!_settings.Enabled)
        {
            return;
        }

        if (IsConnected)
        {
            return;
        }

        Logger.Write(LogType.NETWORK, $"WorldServer waiting for RealmServer before opening public client connections. {reason}", "WorldRealmStatusReporter");
        DateTimeOffset nextStatusUtc = DateTimeOffset.UtcNow.AddSeconds(15);

        while (!cancellationToken.IsCancellationRequested)
        {
            if (IsConnected)
            {
                Logger.Write(LogType.SUCCESS, "RealmServer is online; WorldServer may accept public client connections.", "WorldRealmStatusReporter");
                return;
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (now >= nextStatusUtc)
            {
                Logger.Write(LogType.NETWORK, "WorldServer is still waiting for RealmServer before opening public client connections.", "WorldRealmStatusReporter");
                nextStatusUtc = now.AddSeconds(15);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }

    // Method: SetActiveConnections
    // Purpose: Applies set active connections changes for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - activeConnections: Active connections value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldRealmStatusReporter so callers do not duplicate validation, protocol, or persistence rules.
    public void SetActiveConnections(int activeConnections)
    {
        Interlocked.Exchange(ref _activeConnections, Math.Max(0, activeConnections));
    }

    // Method: SendRealmStatusNowAsync
    // Purpose: Handles send realm status now work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldRealmStatusReporter so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: SendCharacterCountSnapshotNowAsync
    // Purpose: Handles send character count snapshot now work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldRealmStatusReporter so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: StopAsync
    // Purpose: Controls the stop lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldRealmStatusReporter so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

            }
        }

        CleanupConnection();

        _stopCancellation?.Dispose();
        _stopCancellation = null;
        _reportTask = null;

        Logger.Write(LogType.NETWORK, "WorldServer realm status reporter stopped.", "WorldRealmStatusReporter");
    }

    // Method: DisposeAsync
    // Purpose: Controls the dispose lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldRealmStatusReporter so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        _sendLock.Dispose();
    }

    // Method: RunAsync
    // Purpose: Controls the run lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldRealmStatusReporter so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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
                    _pingTimeout,
                    _latencyMeasured);

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

    // Method: SendShutdownRequestAsync
    // Purpose: Handles send shutdown request work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - reason: Reason value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous Boolean result that is true when send shutdown request async succeeds or the requested condition is met.
    // Notes: This keeps the operation scoped to WorldRealmStatusReporter so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: SendRealmStatusLoopAsync
    // Purpose: Handles send realm status loop work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldRealmStatusReporter so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: SendCharacterCountSnapshotAsync
    // Purpose: Handles send character count snapshot work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldRealmStatusReporter so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: SendCharacterCountSnapshotDataAsync
    // Purpose: Handles send character count snapshot data work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - realmId: Realm ID identifier used to select the exact record, object, or runtime owner.
    // - pairs: Pairs value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldRealmStatusReporter so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task SendCharacterCountSnapshotDataAsync(uint realmId, List<string> pairs, CancellationToken cancellationToken)
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

    // Method: ProcessRealmServerPacketsAsync
    // Purpose: Executes the process realm server packets operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - latencyMonitor: Latency monitor value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldRealmStatusReporter so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task ProcessRealmServerPacketsAsync(InternalLatencyMonitor latencyMonitor, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            InternalProtocolReader reader = _reader ?? throw new IOException("RealmServer connection reader is not available.");

            string? line = await reader.ReadLineAsync(
                InternalProtocol.MaximumPacketLineLength,
                cancellationToken) ?? throw new IOException("RealmServer disconnected from WorldServer realm status reporter.");

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            await ProcessRealmServerPacketAsync(line, latencyMonitor, cancellationToken);
        }
    }

    // Method: ProcessRealmServerPacketAsync
    // Purpose: Executes the process realm server packet operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - line: Line value supplied by the caller for this operation.
    // - latencyMonitor: Latency monitor value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldRealmStatusReporter so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private static async Task ProcessRealmServerPacketAsync(string line, InternalLatencyMonitor latencyMonitor, CancellationToken cancellationToken)
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

    // Method: ConnectAndAuthenticateAsync
    // Purpose: Handles connect and authenticate work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldRealmStatusReporter so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task ConnectAndAuthenticateAsync(CancellationToken cancellationToken)
    {
        CleanupConnection();

        _client = new TcpClient();
        TcpSocketOptions.ConfigureClient(
            _client,
            _receiveBufferSize,
            _sendBufferSize,
            _keepAlive,
            _keepAliveTimeSeconds,
            _keepAliveIntervalSeconds);

        Logger.Write(LogType.NETWORK, $"WorldServer connecting to RealmServer internal listener at {_settings.RealmServerHost}:{_settings.RealmServerPort}...", "WorldRealmStatusReporter");

        await _client.ConnectAsync(_settings.RealmServerHost, _settings.RealmServerPort, cancellationToken);
        _stream = _client.GetStream();
        _reader = new InternalProtocolReader(_stream);

        using CancellationTokenSource authenticationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        authenticationCancellation.CancelAfter(_authenticationTimeout);

        string? challenge = await _reader.ReadLineAsync(
            InternalProtocol.MaximumAuthenticationLineLength,
            authenticationCancellation.Token) ?? throw new InvalidOperationException("RealmServer disconnected before authentication challenge.");
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
            authenticationCancellation.Token) ?? throw new InvalidOperationException("RealmServer disconnected before accepting authentication.");
        string[] responseParts = response.Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (responseParts.Length != 2 || !string.Equals(responseParts[0], InternalProtocol.AuthenticationAccepted, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("RealmServer rejected WorldServer authentication.");
        }

        if (!string.Equals(responseParts[1], "RealmServer", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"RealmServer accepted WorldServer authentication as unexpected server '{responseParts[1]}'.");
        }

        Volatile.Write(ref _authenticated, 1);

        Logger.Write(LogType.NETWORK, "WorldServer authenticated with RealmServer internal listener.", "WorldRealmStatusReporter");
    }

    // Method: SendRealmStatusAsync
    // Purpose: Handles send realm status work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - online: Online value supplied by the caller for this operation.
    // - activeConnections: Active connections value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldRealmStatusReporter so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: CleanupConnection
    // Purpose: Executes the cleanup connection operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldRealmStatusReporter so callers do not duplicate validation, protocol, or persistence rules.
    private void CleanupConnection()
    {
        try
        {
            _reader?.Dispose();
        }
        catch
        {

        }

        try
        {
            _stream?.Dispose();
        }
        catch
        {

        }

        try
        {
            _client?.Dispose();
        }
        catch
        {

        }

        Volatile.Write(ref _authenticated, 0);
        _reader = null;
        _stream = null;
        _client = null;
    }
}
