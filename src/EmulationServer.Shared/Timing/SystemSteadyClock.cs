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

using System.Diagnostics;

namespace EmulationServer.Shared.Timing;

/**
  * Steady clock backed by Stopwatch so runtime timers are not affected by wall-clock changes.
  */
public sealed class SystemSteadyClock : ISteadyClock
{
    private static readonly TimeSpan MaximumDelaySlice = TimeSpan.FromSeconds(5);

    /**
      * Gets the shared clock instance used by production server code.
      */
    public static SystemSteadyClock Instance { get; } = new();

    private SystemSteadyClock()
    {
    }

    public long Timestamp => Stopwatch.GetTimestamp();

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

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

    public TimeSpan GetElapsedTime(long startingTimestamp)
    {
        return Stopwatch.GetElapsedTime(startingTimestamp);
    }

    public TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp)
    {
        return Stopwatch.GetElapsedTime(startingTimestamp, endingTimestamp);
    }

    public ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        if (delay <= TimeSpan.Zero)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }

        return DelayUntilAsync(Add(Timestamp, delay), cancellationToken);
    }

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
