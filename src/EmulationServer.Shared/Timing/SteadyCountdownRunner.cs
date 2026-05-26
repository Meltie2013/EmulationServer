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

namespace EmulationServer.Shared.Timing;

/**
  * Runs a countdown against a monotonic deadline and emits warning notices at selected remaining times.
  */
public static class SteadyCountdownRunner
{
    /**
      * Default administrator-visible countdown warning points used by restart and shutdown workflows.
      */
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

    /**
      * Runs a countdown and calls onElapsedAsync exactly once after the steady deadline has elapsed.
      */
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

    private static TimeSpan GetRemaining(ISteadyClock clock, long deadlineTimestamp)
    {
        TimeSpan remaining = clock.GetElapsedTime(clock.Timestamp, deadlineTimestamp);
        return remaining <= TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }

    private static TimeSpan RoundRemaining(TimeSpan remaining)
    {
        if (remaining.TotalSeconds <= 1)
        {
            return TimeSpan.FromSeconds(1);
        }

        return TimeSpan.FromSeconds(Math.Ceiling(remaining.TotalSeconds));
    }
}
