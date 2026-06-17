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
// File: src/EmulationServer.Shared/Timing/SteadyCountdownRunner.cs
// Purpose: Contains steady countdown runner code for the shared infrastructure, logging, timing, and cross-service utility layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Shared.Timing;

// Type: SteadyCountdownRunner
// Purpose: Provides steady countdown runner behavior for the shared infrastructure, logging, timing, and cross-service utility layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class SteadyCountdownRunner
{

    // Property: Gets or sets the default warning thresholds value used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: default warning thresholds value exposed by the owning type.
    public static IReadOnlyList<TimeSpan> DefaultWarningThresholds { get; } =
    [
        TimeSpan.FromDays(1),
        TimeSpan.FromHours(12),
        TimeSpan.FromHours(6),
        TimeSpan.FromHours(3),
        TimeSpan.FromHours(1),
        TimeSpan.FromMinutes(30),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(1),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(3),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(1),
    ];

    // Method: RunAsync
    // Purpose: Controls the run lifecycle step for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - clock: Clock value supplied by the caller for this operation.
    // - delay: Delay value supplied by the caller for this operation.
    // - warningThresholds: Warning thresholds value supplied by the caller for this operation.
    // - onWarningAsync: On warning async value supplied by the caller for this operation.
    // - onElapsedAsync: On elapsed async value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to SteadyCountdownRunner so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public static async Task RunAsync(
        ISteadyClock clock,
        TimeSpan delay,
        IEnumerable<TimeSpan> warningThresholds,
        Func<TimeSpan, CancellationToken, Task> onWarningAsync,
        Func<CancellationToken, Task> onElapsedAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(warningThresholds);
        ArgumentNullException.ThrowIfNull(onWarningAsync);
        ArgumentNullException.ThrowIfNull(onElapsedAsync);

        if (delay <= TimeSpan.Zero)
        {
            await onElapsedAsync(cancellationToken);
            return;
        }

        long deadlineTimestamp = clock.Add(clock.Timestamp, delay);
        TimeSpan[] thresholds = warningThresholds
            .Where(threshold => threshold > TimeSpan.Zero && threshold < delay)
            .Distinct()
            .OrderByDescending(threshold => threshold)
            .ToArray();

        foreach (TimeSpan threshold in thresholds)
        {
            TimeSpan remaining = GetRemaining(clock, deadlineTimestamp);
            if (remaining <= TimeSpan.Zero)
            {
                break;
            }

            if (remaining > threshold)
            {
                await clock.DelayAsync(remaining - threshold, cancellationToken);
            }

            remaining = GetRemaining(clock, deadlineTimestamp);
            if (remaining > TimeSpan.Zero)
            {
                await onWarningAsync(RoundRemaining(remaining), cancellationToken);
            }
        }

        await clock.DelayUntilAsync(deadlineTimestamp, cancellationToken);
        await onElapsedAsync(cancellationToken);
    }

    // Method: GetRemaining
    // Purpose: Retrieves get remaining data for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - clock: Clock value supplied by the caller for this operation.
    // - deadlineTimestamp: Deadline timestamp value supplied by the caller for this operation.
    // Returns: Returns the time span value produced by this operation.
    // Notes: This keeps the operation scoped to SteadyCountdownRunner so callers do not duplicate validation, protocol, or persistence rules.
    private static TimeSpan GetRemaining(ISteadyClock clock, long deadlineTimestamp)
    {
        TimeSpan remaining = clock.GetElapsedTime(clock.Timestamp, deadlineTimestamp);
        return remaining <= TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }

    // Method: RoundRemaining
    // Purpose: Executes the round remaining operation for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - remaining: Remaining value supplied by the caller for this operation.
    // Returns: Returns the time span value produced by this operation.
    // Notes: This keeps the operation scoped to SteadyCountdownRunner so callers do not duplicate validation, protocol, or persistence rules.
    private static TimeSpan RoundRemaining(TimeSpan remaining)
    {
        if (remaining.TotalSeconds <= 1)
        {
            return TimeSpan.FromSeconds(1);
        }

        return TimeSpan.FromSeconds(Math.Ceiling(remaining.TotalSeconds));
    }
}
