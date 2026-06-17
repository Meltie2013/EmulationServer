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
// File: src/EmulationServer.Network/Networking/Health/InternalLatencyMonitor.cs
// Purpose: Contains internal latency monitor code for the packet serialization, socket transport, and protocol framing layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;

using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Network.Networking.Health;

// Type: InternalLatencyMonitor
// Purpose: Provides internal latency monitor behavior for the packet serialization, socket transport, and protocol framing layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class InternalLatencyMonitor : IAsyncDisposable
{

    // Field: Stores the local server name state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current local server name backing value maintained by the owning type.
    private readonly string _localServerName;

    // Field: Stores the remote server name state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current remote server name backing value maintained by the owning type.
    private readonly string _remoteServerName;

    // Field: Stores the stream state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current stream backing value maintained by the owning type.
    private readonly NetworkStream _stream;

    // Field: Stores the send lock state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current send lock backing value maintained by the owning type.
    private readonly SemaphoreSlim _sendLock;

    // Field: Stores the report interval state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current report interval backing value maintained by the owning type.
    private readonly TimeSpan _reportInterval;

    // Field: Stores the latency logging enabled state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current latency logging enabled backing value maintained by the owning type.
    private readonly bool _latencyLoggingEnabled;

    // Field: Stores the latency log interval state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current latency log interval backing value maintained by the owning type.
    private readonly TimeSpan _latencyLogInterval;

    // Field: Stores the last latency log utc ticks state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current last latency log utc ticks backing value maintained by the owning type.
    private long _lastLatencyLogUtcTicks;

    // Field: Stores the ping timeout state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current ping timeout backing value maintained by the owning type.
    private readonly TimeSpan _pingTimeout;

    // Field: Stores the string state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current string backing value maintained by the owning type.
    private readonly Action<string, TimeSpan>? _latencyMeasured;

    // Field: Stores the string state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current string backing value maintained by the owning type.
    private readonly Action<string, TimeSpan>? _pingTimedOut;
    private readonly ConcurrentDictionary<long, PendingPing> _pendingPings = new();

    // Field: Stores the stop cancellation state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current stop cancellation backing value maintained by the owning type.
    private CancellationTokenSource? _stopCancellation;

    // Field: Stores the monitor task state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current monitor task backing value maintained by the owning type.
    private Task? _monitorTask;

    // Field: Stores the next ping ID state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current next ping ID backing value maintained by the owning type.
    private long _nextPingId;

    // Field: Stores the started state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current started backing value maintained by the owning type.
    private int _started;

    // Field: Stores the stopping state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current stopping backing value maintained by the owning type.
    private int _stopping;

    // Constructor: InternalLatencyMonitor
    // Purpose: Initializes a new InternalLatencyMonitor instance with dependencies and values required by the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - localServerName: Local server name value supplied by the caller for this operation.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - stream: Stream value supplied by the caller for this operation.
    // - sendLock: Send lock value supplied by the caller for this operation.
    // - reportInterval: Report interval value supplied by the caller for this operation.
    // - latencyLoggingEnabled: Latency logging enabled value supplied by the caller for this operation.
    // - latencyLogInterval: Latency log interval value supplied by the caller for this operation.
    // - pingTimeout: Ping timeout value supplied by the caller for this operation.
    // - latencyMeasured: Latency measured value supplied by the caller for this operation.
    // - pingTimedOut: Ping timed out value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to InternalLatencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    public InternalLatencyMonitor(
        string localServerName,
        string remoteServerName,
        NetworkStream stream,
        SemaphoreSlim sendLock,
        TimeSpan reportInterval,
        bool latencyLoggingEnabled,
        TimeSpan latencyLogInterval,
        TimeSpan pingTimeout,
        Action<string, TimeSpan>? latencyMeasured = null,
        Action<string, TimeSpan>? pingTimedOut = null)
    {
        if (string.IsNullOrWhiteSpace(localServerName))
        {
            throw new ArgumentException("Local server name is required.");
        }

        if (string.IsNullOrWhiteSpace(remoteServerName))
        {
            throw new ArgumentException("Remote server name is required.");
        }

        if (reportInterval <= TimeSpan.Zero)
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

        _localServerName = localServerName;
        _remoteServerName = remoteServerName;
        _stream = stream ?? throw new ArgumentNullException();
        _sendLock = sendLock ?? throw new ArgumentNullException();
        _reportInterval = reportInterval;
        _latencyLoggingEnabled = latencyLoggingEnabled;
        _latencyLogInterval = latencyLogInterval;
        _pingTimeout = pingTimeout;
        _latencyMeasured = latencyMeasured;
        _pingTimedOut = pingTimedOut;
    }

    // Method: Start
    // Purpose: Controls the start lifecycle step for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: none.
    // Notes: This keeps the operation scoped to InternalLatencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    public void Start(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _started, 1) == 1)
        {
            throw new InvalidOperationException($"Latency monitor for {_localServerName} -> {_remoteServerName} has already been started.");
        }

        _stopCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _monitorTask = Task.Run(() => RunAsync(_stopCancellation.Token), CancellationToken.None);
    }

    // Method: StopAsync
    // Purpose: Controls the stop lifecycle step for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalLatencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
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

        if (_monitorTask is not null)
        {
            try
            {
                Task completedTask = await Task.WhenAny(_monitorTask, Task.Delay(TimeSpan.FromSeconds(2), cancellationToken));
                if (completedTask == _monitorTask)
                {
                    await _monitorTask;
                }
            }
            catch (OperationCanceledException)
            {

            }
        }

        stopCancellation?.Dispose();
        _stopCancellation = null;
    }

    // Method: DisposeAsync
    // Purpose: Controls the dispose lifecycle step for the packet serialization, socket transport, and protocol framing layer.
    // Parameters: none.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalLatencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
    }

    // Method: RespondToPingAsync
    // Purpose: Executes the respond to ping operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - pingId: Ping ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalLatencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task RespondToPingAsync(string pingId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(pingId))
        {
            return;
        }

        await InternalProtocol.WriteLineAsync(
            _stream,
            _sendLock,
            $"{InternalProtocol.Pong} {pingId}",
            cancellationToken);

        Logger.Write(LogType.TRACE, $"{_localServerName} sent PONG packet to {_remoteServerName}.", "InternalLatencyMonitor");
    }

    // Method: RecordPong
    // Purpose: Executes the record pong operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - pingId: Ping ID identifier used to select the exact record, object, or runtime owner.
    // Returns: none.
    // Notes: This keeps the operation scoped to InternalLatencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    public void RecordPong(string pingId)
    {
        if (!long.TryParse(pingId, out long id))
        {
            Logger.Write(LogType.WARNING, $"{_localServerName} received invalid latency pong id from {_remoteServerName}: '{pingId}'.", "InternalLatencyMonitor");
            return;
        }

        if (!_pendingPings.TryRemove(id, out PendingPing? pendingPing))
        {
            Logger.Write(LogType.DEBUG, $"{_localServerName} received unmatched latency pong {id} from {_remoteServerName}.", "InternalLatencyMonitor");
            return;
        }

        TimeSpan latency = GetElapsedTime(pendingPing.StartTimestamp);
        Logger.Write(LogType.TRACE, $"{_localServerName} latency to {_remoteServerName}: {latency.TotalMilliseconds:0.##} ms.", "InternalLatencyMonitor");
        NotifyLatencyMeasured(latency);

        if (ShouldLogLatency())
        {
            Logger.Write(LogType.SYSTEM, $"{_localServerName} latency to {_remoteServerName}: {latency.TotalMilliseconds:0.##} ms.", "InternalLatencyMonitor");
        }
    }

    // Method: RunAsync
    // Purpose: Controls the run lifecycle step for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalLatencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                RemoveTimedOutPings();
                await SendPingAsync(cancellationToken);

                await Task.Delay(_reportInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {

        }
        catch (IOException exception)
        {
            Logger.Write(LogType.NETWORK, $"{_localServerName} latency monitor stopped for {_remoteServerName}: {exception.Message}", "InternalLatencyMonitor");
        }
        catch (SocketException exception)
        {
            Logger.Write(LogType.NETWORK, $"{_localServerName} latency monitor socket stopped for {_remoteServerName}: {exception.SocketErrorCode}", "InternalLatencyMonitor");
        }
        catch (ObjectDisposedException)
        {

        }
        catch (Exception exception)
        {
            Logger.Write(LogType.CRITICAL, exception.ToString(), "InternalLatencyMonitor");
        }
    }

    // Method: SendPingAsync
    // Purpose: Handles send ping work for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalLatencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task SendPingAsync(CancellationToken cancellationToken)
    {
        long id = Interlocked.Increment(ref _nextPingId);
        PendingPing pendingPing = new(Stopwatch.GetTimestamp());

        _pendingPings[id] = pendingPing;

        await InternalProtocol.WriteLineAsync(
            _stream,
            _sendLock,
            $"{InternalProtocol.Ping} {id}",
            cancellationToken);

        Logger.Write(LogType.TRACE, $"{_localServerName} sent PING packet to {_remoteServerName}.", "InternalLatencyMonitor");
    }

    // Method: RemoveTimedOutPings
    // Purpose: Applies remove timed out pings changes for the packet serialization, socket transport, and protocol framing layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to InternalLatencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    private void RemoveTimedOutPings()
    {
        foreach (KeyValuePair<long, PendingPing> pendingPing in _pendingPings)
        {
            TimeSpan elapsed = GetElapsedTime(pendingPing.Value.StartTimestamp);
            if (elapsed <= _pingTimeout)
            {
                continue;
            }

            if (_pendingPings.TryRemove(pendingPing.Key, out _))
            {
                Logger.Write(LogType.WARNING, $"{_localServerName} did not receive latency pong {pendingPing.Key} from {_remoteServerName} within {_pingTimeout.TotalSeconds:0.##} second(s).", "InternalLatencyMonitor");
                NotifyPingTimedOut(elapsed);
            }
        }
    }

    // Method: ShouldLogLatency
    // Purpose: Validates or evaluates should log latency rules for the packet serialization, socket transport, and protocol framing layer.
    // Parameters: none.
    // Returns: Returns true when should log latency succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to InternalLatencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    private bool ShouldLogLatency()
    {
        if (!_latencyLoggingEnabled)
        {
            return false;
        }

        long nowTicks = DateTime.UtcNow.Ticks;
        long previousTicks = Interlocked.Read(ref _lastLatencyLogUtcTicks);

        if (previousTicks != 0 && TimeSpan.FromTicks(nowTicks - previousTicks) < _latencyLogInterval)
        {
            return false;
        }

        return Interlocked.CompareExchange(ref _lastLatencyLogUtcTicks, nowTicks, previousTicks) == previousTicks;
    }

    // Method: NotifyLatencyMeasured
    // Purpose: Executes the notify latency measured operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - latency: Latency value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to InternalLatencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    private void NotifyLatencyMeasured(TimeSpan latency)
    {
        try
        {
            _latencyMeasured?.Invoke(_remoteServerName, latency);
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.WARNING, $"{_localServerName} latency measurement callback for {_remoteServerName} failed: {exception.Message}", "InternalLatencyMonitor");
        }
    }

    // Method: NotifyPingTimedOut
    // Purpose: Executes the notify ping timed out operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - elapsed: Elapsed value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to InternalLatencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    private void NotifyPingTimedOut(TimeSpan elapsed)
    {
        try
        {
            _pingTimedOut?.Invoke(_remoteServerName, elapsed);
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.WARNING, $"{_localServerName} ping timeout callback for {_remoteServerName} failed: {exception.Message}", "InternalLatencyMonitor");
        }
    }

    // Method: GetElapsedTime
    // Purpose: Retrieves get elapsed time data for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - startTimestamp: Start timestamp value supplied by the caller for this operation.
    // Returns: Returns the time span value produced by this operation.
    // Notes: This keeps the operation scoped to InternalLatencyMonitor so callers do not duplicate validation, protocol, or persistence rules.
    private static TimeSpan GetElapsedTime(long startTimestamp)
    {
        long elapsedTicks = Stopwatch.GetTimestamp() - startTimestamp;
        double elapsedSeconds = elapsedTicks / (double)Stopwatch.Frequency;

        return TimeSpan.FromSeconds(elapsedSeconds);
    }

    // Type: PendingPing
    // Purpose: Represents pending ping data passed through the packet serialization, socket transport, and protocol framing layer.
    // Constructor values:
    // - StartTimestamp: Start timestamp value supplied by the caller for this operation.
    // Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
    private sealed record PendingPing(long StartTimestamp);
}
