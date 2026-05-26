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
  * Provides monotonic time for runtime delays, countdowns, and server tick loops.
  * Use UtcNow only for protocol timestamps and logs; use Timestamp and elapsed-time helpers for timers.
  */
public interface ISteadyClock
{
    /**
      * Gets the current monotonic timestamp.
      */
    long Timestamp { get; }

    /**
      * Gets the current wall-clock UTC time for logs and protocol snapshots.
      */
    DateTimeOffset UtcNow { get; }

    /**
      * Adds a duration to a monotonic timestamp and returns the resulting deadline timestamp.
      */
    long Add(long timestamp, TimeSpan duration);

    /**
      * Returns elapsed monotonic time from the supplied start timestamp to now.
      */
    TimeSpan GetElapsedTime(long startingTimestamp);

    /**
      * Returns elapsed monotonic time between two timestamps.
      */
    TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp);

    /**
      * Waits for the supplied duration using the steady-clock deadline path.
      */
    ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken);

    /**
      * Waits until the supplied monotonic deadline is reached.
      */
    ValueTask DelayUntilAsync(long deadlineTimestamp, CancellationToken cancellationToken);
}
