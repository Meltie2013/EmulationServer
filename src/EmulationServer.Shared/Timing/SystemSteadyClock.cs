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
// File: src/EmulationServer.Shared/Timing/SystemSteadyClock.cs
// Purpose: Contains system steady clock code for the shared infrastructure, logging, timing, and cross-service utility layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Diagnostics;

namespace EmulationServer.Shared.Timing;

// Type: SystemSteadyClock
// Purpose: Provides system steady clock behavior for the shared infrastructure, logging, timing, and cross-service utility layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class SystemSteadyClock : ISteadyClock
{
    // Method: FromSeconds
    // Purpose: Executes the from seconds operation for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters: none.
    // Returns: Returns the time span maximum delay slice = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to SystemSteadyClock so callers do not duplicate validation, protocol, or persistence rules.
    private static readonly TimeSpan MaximumDelaySlice = TimeSpan.FromSeconds(5);

    public static SystemSteadyClock Instance { get; } = new();

    // Constructor: SystemSteadyClock
    // Purpose: Initializes a new SystemSteadyClock instance with dependencies and values required by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to SystemSteadyClock so callers do not duplicate validation, protocol, or persistence rules.
    private SystemSteadyClock()
    {
    }

    // Method: GetTimestamp
    // Purpose: Retrieves get timestamp data for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters: none.
    // Returns: Returns the long timestamp => stopwatch. value produced by this operation.
    // Notes: This keeps the operation scoped to SystemSteadyClock so callers do not duplicate validation, protocol, or persistence rules.
    public long Timestamp => Stopwatch.GetTimestamp();

    // Property: Gets or sets the utc now value used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: utc now value exposed by the owning type.
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    // Method: Add
    // Purpose: Applies add changes for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - timestamp: Timestamp value supplied by the caller for this operation.
    // - duration: Duration value supplied by the caller for this operation.
    // Returns: Returns the long value produced by this operation.
    // Notes: This keeps the operation scoped to SystemSteadyClock so callers do not duplicate validation, protocol, or persistence rules.
    public long Add(long timestamp, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return timestamp;
        }

        double stopwatchTicks = duration.TotalSeconds * Stopwatch.Frequency;
        if (stopwatchTicks >= long.MaxValue)
        {
            return long.MaxValue;
        }

        long roundedTicks = (long)Math.Ceiling(stopwatchTicks);
        if (long.MaxValue - timestamp < roundedTicks)
        {
            return long.MaxValue;
        }

        return timestamp + roundedTicks;
    }

    // Method: GetElapsedTime
    // Purpose: Retrieves get elapsed time data for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - startingTimestamp: Starting timestamp value supplied by the caller for this operation.
    // Returns: Returns the time span value produced by this operation.
    // Notes: This keeps the operation scoped to SystemSteadyClock so callers do not duplicate validation, protocol, or persistence rules.
    public TimeSpan GetElapsedTime(long startingTimestamp)
    {
        return Stopwatch.GetElapsedTime(startingTimestamp);
    }

    // Method: GetElapsedTime
    // Purpose: Retrieves get elapsed time data for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - startingTimestamp: Starting timestamp value supplied by the caller for this operation.
    // - endingTimestamp: Ending timestamp value supplied by the caller for this operation.
    // Returns: Returns the time span value produced by this operation.
    // Notes: This keeps the operation scoped to SystemSteadyClock so callers do not duplicate validation, protocol, or persistence rules.
    public TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp)
    {
        return Stopwatch.GetElapsedTime(startingTimestamp, endingTimestamp);
    }

    // Method: DelayAsync
    // Purpose: Executes the delay operation for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - delay: Delay value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to SystemSteadyClock so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        if (delay <= TimeSpan.Zero)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }

        return DelayUntilAsync(Add(Timestamp, delay), cancellationToken);
    }

    // Method: DelayUntilAsync
    // Purpose: Executes the delay until operation for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - deadlineTimestamp: Deadline timestamp value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to SystemSteadyClock so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async ValueTask DelayUntilAsync(long deadlineTimestamp, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TimeSpan remaining = GetElapsedTime(Timestamp, deadlineTimestamp);
            if (remaining <= TimeSpan.Zero)
            {
                return;
            }

            TimeSpan delay = remaining <= MaximumDelaySlice
                ? remaining
                : MaximumDelaySlice;

            await Task.Delay(delay, cancellationToken);
        }
    }
}
