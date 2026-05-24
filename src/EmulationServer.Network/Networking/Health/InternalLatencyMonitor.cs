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

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;

using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;


/**
 * File overview: src/EmulationServer.Network/Networking/Health/InternalLatencyMonitor.cs
 * Documents the InternalLatencyMonitor source file in the internal server networking, packet framing, and peer/session lifecycle area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Network.Networking.Health;

/**
  * Sends ping packets, tracks matching pong responses, and reports server-to-server latency.
  * It watches ongoing runtime state and reports changes or health information to the logs.
  */
public sealed class InternalLatencyMonitor : IAsyncDisposable
{
    /**
     * Holds the private local server name state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly string _localServerName;
    /**
     * Holds the private remote server name state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly string _remoteServerName;
    /**
     * Holds the private stream state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly NetworkStream _stream;
    /**
     * Holds the private send lock state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly SemaphoreSlim _sendLock;
    /**
     * Holds the private report interval state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly TimeSpan _reportInterval;
    /**
     * Holds the private ping timeout state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly TimeSpan _pingTimeout;
    private readonly ConcurrentDictionary<long, PendingPing> _pendingPings = new();

    /**
     * Holds the private stop cancellation state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private CancellationTokenSource? _stopCancellation;
    /**
     * Holds the private monitor task state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private Task? _monitorTask;
    /**
     * Holds the private next ping id state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private long _nextPingId;
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
     * Initializes a new InternalLatencyMonitor instance with the dependencies required by the internal server networking, packet framing, and peer/session lifecycle workflow.
     * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
     * Inputs used by this operation: localServerName, remoteServerName, stream, sendLock, reportInterval, pingTimeout.
     */
    public InternalLatencyMonitor(
        string localServerName,
        string remoteServerName,
        NetworkStream stream,
        SemaphoreSlim sendLock,
        TimeSpan reportInterval,
        TimeSpan pingTimeout)
    {
        if (string.IsNullOrWhiteSpace(localServerName))
        {
            throw new ArgumentException("Local server name is required.", nameof(localServerName));
        }

        if (string.IsNullOrWhiteSpace(remoteServerName))
        {
            throw new ArgumentException("Remote server name is required.", nameof(remoteServerName));
        }

        if (reportInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(reportInterval), "Latency report interval must be greater than zero.");
        }

        if (pingTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(pingTimeout), "Ping timeout must be greater than zero.");
        }

        _localServerName = localServerName;
        _remoteServerName = remoteServerName;
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _sendLock = sendLock ?? throw new ArgumentNullException(nameof(sendLock));
        _reportInterval = reportInterval;
        _pingTimeout = pingTimeout;
    }

    /**
     * Starts the start workflow and prepares the component to accept runtime work.
     * Startup is ordered so validation and dependency setup finish before services are announced as available.
     * Inputs used by this operation: cancellationToken.
     */
    public void Start(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _started, 1) == 1)
        {
            throw new InvalidOperationException($"Latency monitor for {_localServerName} -> {_remoteServerName} has already been started.");
        }

        _stopCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _monitorTask = Task.Run(() => RunAsync(_stopCancellation.Token), CancellationToken.None);
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
                // Expected during shutdown.
            }
        }

        stopCancellation?.Dispose();
        _stopCancellation = null;
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
     * Performs the respond to ping operation for the internal server networking, packet framing, and peer/session lifecycle workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: pingId, cancellationToken.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
     */
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

        Logger.Write(LogType.TRACE, $"{_localServerName} sent PONG packet to {_remoteServerName}.", nameof(InternalLatencyMonitor));
    }

    /**
     * Performs the record pong operation for the internal server networking, packet framing, and peer/session lifecycle workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: pingId.
     */
    public void RecordPong(string pingId)
    {
        if (!long.TryParse(pingId, out long id))
        {
            Logger.Write(LogType.WARNING, $"{_localServerName} received invalid latency pong id from {_remoteServerName}: '{pingId}'.", nameof(InternalLatencyMonitor));
            return;
        }

        if (!_pendingPings.TryRemove(id, out PendingPing? pendingPing))
        {
            Logger.Write(LogType.DEBUG, $"{_localServerName} received unmatched latency pong {id} from {_remoteServerName}.", nameof(InternalLatencyMonitor));
            return;
        }

        TimeSpan latency = GetElapsedTime(pendingPing.StartTimestamp);
        Logger.Write(LogType.TRACE, $"{_localServerName} latency to {_remoteServerName}: {latency.TotalMilliseconds:0.##} ms.", nameof(InternalLatencyMonitor));
    }

    /**
      * Runs the main loop for this component until cancellation or shutdown is requested.
      * The method is part of InternalLatencyMonitor and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
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
            // Expected during shutdown.
        }
        catch (IOException exception)
        {
            Logger.Write(LogType.NETWORK, $"{_localServerName} latency monitor stopped for {_remoteServerName}: {exception.Message}", nameof(InternalLatencyMonitor));
        }
        catch (SocketException exception)
        {
            Logger.Write(LogType.NETWORK, $"{_localServerName} latency monitor socket stopped for {_remoteServerName}: {exception.SocketErrorCode}", nameof(InternalLatencyMonitor));
        }
        catch (ObjectDisposedException)
        {
            // Expected when the connection closes.
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.CRITICAL, exception.ToString(), nameof(InternalLatencyMonitor));
        }
    }

    /**
      * Sends a protocol message or status update to a connected peer.
      * The method is part of InternalLatencyMonitor and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
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

        Logger.Write(LogType.TRACE, $"{_localServerName} sent PING packet to {_remoteServerName}.", nameof(InternalLatencyMonitor));
    }

    /**
      * Removes an item from the managed collection and cleans up related state.
      * The method is part of InternalLatencyMonitor and keeps this workflow isolated from the caller.
      */
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
                Logger.Write(LogType.WARNING, $"{_localServerName} did not receive latency pong {pendingPing.Key} from {_remoteServerName} within {_pingTimeout.TotalSeconds:0.##} second(s).", nameof(InternalLatencyMonitor));
            }
        }
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of InternalLatencyMonitor and keeps this workflow isolated from the caller.
      */
    private static TimeSpan GetElapsedTime(long startTimestamp)
    {
        long elapsedTicks = Stopwatch.GetTimestamp() - startTimestamp;
        double elapsedSeconds = elapsedTicks / (double)Stopwatch.Frequency;

        return TimeSpan.FromSeconds(elapsedSeconds);
    }

    /**
      * Represents immutable pending ping data passed between parts of the server.
      * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
     * Positional fields carried by this record: StartTimestamp.
      */
    private sealed record PendingPing(long StartTimestamp);
}
