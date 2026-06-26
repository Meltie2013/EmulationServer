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
// File: src/WorldServer/Networking/Movement/WorldMovementTimingTelemetry.cs
// Purpose: Stores shared latency measurements used by automatic movement timing.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.WorldServer.Networking.Movement;

// Type: WorldMovementTimingTelemetry
// Purpose: Captures internal server latency so movement sessions can self-adjust without configuration values.
// Notes: This is intentionally small and thread-safe because latency callbacks run outside the client packet loop.
public sealed class WorldMovementTimingTelemetry
{
    private const double MaximumInternalLatencyMilliseconds = 2000.0d;
    private const double InternalLatencySmoothWeight = 0.20d;

    private readonly object _sync = new();
    private double? _internalServerLatencyMilliseconds;

    // Method: RecordInternalServerLatency
    // Purpose: Records the latest measured latency to an internal server such as RealmServer.
    // Parameters:
    // - serverName: Name of the internal server that produced the measurement.
    // - latency: Measured round-trip latency.
    // Returns: none.
    public void RecordInternalServerLatency(string serverName, TimeSpan latency)
    {
        if (string.IsNullOrWhiteSpace(serverName))
        {
            return;
        }

        double sample = Math.Clamp(latency.TotalMilliseconds, 0.0d, MaximumInternalLatencyMilliseconds);

        lock (_sync)
        {
            _internalServerLatencyMilliseconds = _internalServerLatencyMilliseconds is double current
                ? current + ((sample - current) * InternalLatencySmoothWeight)
                : sample;
        }
    }

    // Method: GetInternalServerLatency
    // Purpose: Retrieves the current smoothed internal server latency measurement.
    // Parameters: none.
    // Returns: Returns null when no internal latency has been measured yet; otherwise returns the smoothed latency.
    public TimeSpan? GetInternalServerLatency()
    {
        lock (_sync)
        {
            return _internalServerLatencyMilliseconds is double latencyMilliseconds
                ? TimeSpan.FromMilliseconds(latencyMilliseconds)
                : null;
        }
    }
}
