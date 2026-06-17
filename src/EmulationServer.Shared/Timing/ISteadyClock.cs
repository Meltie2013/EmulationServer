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
// File: src/EmulationServer.Shared/Timing/ISteadyClock.cs
// Purpose: Contains I steady clock code for the shared infrastructure, logging, timing, and cross-service utility layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Shared.Timing;

// Type: ISteadyClock
// Purpose: Defines the I steady clock contract used by the shared infrastructure, logging, timing, and cross-service utility layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public interface ISteadyClock
{

    long Timestamp { get; }

    DateTimeOffset UtcNow { get; }

    long Add(long timestamp, TimeSpan duration);

    TimeSpan GetElapsedTime(long startingTimestamp);

    TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp);

    ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken);

    ValueTask DelayUntilAsync(long deadlineTimestamp, CancellationToken cancellationToken);
}
