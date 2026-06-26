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
// File: src/WorldServer/Networking/Movement/WorldMovementTimingController.cs
// Purpose: Automatically tunes movement-related background timing from live client and server conditions.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.WorldServer.Networking.Movement;

// Type: WorldMovementTimingController
// Purpose: Tracks live movement cadence, client latency, backend route cost, and nearby-player pressure for one world session.
// Notes: Player-to-player movement packets are still sent immediately. This controller only tunes non-critical background work.
public sealed class WorldMovementTimingController
{
    // These values are guardrails for the automatic algorithm, not administrator-facing configuration.
    private const double InitialClientLatencyMilliseconds = 100.0d;
    private const double InitialMovementCadenceMilliseconds = 50.0d;
    private const double InitialMapRouteCostMilliseconds = 5.0d;
    private const double MinimumMapServiceRouteMilliseconds = 20.0d;
    private const double MaximumMapServiceRouteMilliseconds = 250.0d;
    private const double MinimumPlayerVisibilityMilliseconds = 40.0d;
    private const double MaximumPlayerVisibilityMilliseconds = 500.0d;
    private const double MaximumClientLatencyMilliseconds = 1500.0d;
    private const double MaximumMovementCadenceMilliseconds = 1000.0d;
    private const double MaximumMapRouteCostMilliseconds = 1000.0d;
    private const double MaximumVisibleRecipientPressure = 75.0d;
    private const double ClientLatencySmoothWeight = 0.20d;
    private const double MovementCadenceSmoothWeight = 0.25d;
    private const double MapRouteSmoothWeight = 0.25d;
    private const double VisibleRecipientSmoothWeight = 0.30d;

    private readonly Func<TimeSpan?> _internalServerLatencyResolver;
    private readonly object _sync = new();
    private double _clientLatencyMilliseconds = InitialClientLatencyMilliseconds;
    private double _movementCadenceMilliseconds = InitialMovementCadenceMilliseconds;
    private double _mapRouteCostMilliseconds = InitialMapRouteCostMilliseconds;
    private double _visibleRecipientPressure;
    private DateTimeOffset _lastIncomingMovementUtc = DateTimeOffset.MinValue;

    // Constructor: WorldMovementTimingController
    // Purpose: Initializes automatic movement timing with a resolver for shared internal server latency.
    // Parameters:
    // - internalServerLatencyResolver: Optional callback that returns current RealmServer or backend latency.
    // Returns: none.
    public WorldMovementTimingController(Func<TimeSpan?>? internalServerLatencyResolver = null)
    {
        _internalServerLatencyResolver = internalServerLatencyResolver ?? (() => null);
    }

    // Method: RecordClientLatency
    // Purpose: Records the latency value reported by CMSG_PING so timing can react to the connected player's network conditions.
    // Parameters:
    // - latencyMilliseconds: Client-reported latency from the ping payload.
    // Returns: none.
    public void RecordClientLatency(uint latencyMilliseconds)
    {
        double sample = Math.Clamp((double)latencyMilliseconds, 0.0d, MaximumClientLatencyMilliseconds);

        lock (_sync)
        {
            _clientLatencyMilliseconds = Smooth(_clientLatencyMilliseconds, sample, ClientLatencySmoothWeight);
        }
    }

    // Method: RecordIncomingMovement
    // Purpose: Records live movement packet spacing so background work follows the client's real movement cadence.
    // Parameters:
    // - movementUpdatedUtc: Server time when the movement packet was parsed.
    // Returns: none.
    public void RecordIncomingMovement(DateTimeOffset movementUpdatedUtc)
    {
        lock (_sync)
        {
            if (_lastIncomingMovementUtc != DateTimeOffset.MinValue)
            {
                double sample = Math.Clamp((movementUpdatedUtc - _lastIncomingMovementUtc).TotalMilliseconds, 1.0d, MaximumMovementCadenceMilliseconds);
                _movementCadenceMilliseconds = Smooth(_movementCadenceMilliseconds, sample, MovementCadenceSmoothWeight);
            }

            _lastIncomingMovementUtc = movementUpdatedUtc;
        }
    }

    // Method: RecordMapServiceRouteDuration
    // Purpose: Records how long the backend map-service route took so movement routing backs off when the backend is under pressure.
    // Parameters:
    // - routeDuration: Elapsed time spent forwarding the latest movement state to the owning map service.
    // Returns: none.
    public void RecordMapServiceRouteDuration(TimeSpan routeDuration)
    {
        double sample = Math.Clamp(routeDuration.TotalMilliseconds, 0.0d, MaximumMapRouteCostMilliseconds);

        lock (_sync)
        {
            _mapRouteCostMilliseconds = Smooth(_mapRouteCostMilliseconds, sample, MapRouteSmoothWeight);
        }
    }

    // Method: RecordVisibleRecipientCount
    // Purpose: Records how many nearby players received the latest movement packet to estimate per-session fan-out pressure.
    // Parameters:
    // - recipientCount: Number of recipient sessions that accepted the movement broadcast.
    // Returns: none.
    public void RecordVisibleRecipientCount(int recipientCount)
    {
        double sample = Math.Clamp(recipientCount * 3.0d, 0.0d, MaximumVisibleRecipientPressure);

        lock (_sync)
        {
            _visibleRecipientPressure = Smooth(_visibleRecipientPressure, sample, VisibleRecipientSmoothWeight);
        }
    }

    // Method: GetMapServiceRouteInterval
    // Purpose: Calculates the current backend movement route interval from live session timing data.
    // Parameters: none.
    // Returns: Returns the automatically calculated map-service movement route interval.
    public TimeSpan GetMapServiceRouteInterval()
    {
        lock (_sync)
        {
            double internalServerLatencyMilliseconds = GetInternalServerLatencyMilliseconds();
            double combinedBackendMilliseconds = Math.Max(_mapRouteCostMilliseconds, internalServerLatencyMilliseconds);
            double clientPressure = Math.Max(0.0d, _clientLatencyMilliseconds - 50.0d) * 0.12d;
            double backendPressure = Math.Max(0.0d, combinedBackendMilliseconds - 8.0d) * 1.50d;
            double cadenceTarget = Math.Clamp(_movementCadenceMilliseconds * 0.75d, MinimumMapServiceRouteMilliseconds, MaximumMapServiceRouteMilliseconds);
            double calculated = Math.Max(cadenceTarget, MinimumMapServiceRouteMilliseconds + clientPressure + backendPressure + (_visibleRecipientPressure * 0.25d));
            return TimeSpan.FromMilliseconds(Math.Clamp(calculated, MinimumMapServiceRouteMilliseconds, MaximumMapServiceRouteMilliseconds));
        }
    }

    // Method: GetPlayerVisibilityRefreshInterval
    // Purpose: Calculates the current player visibility refresh interval from live session timing data.
    // Parameters: none.
    // Returns: Returns the automatically calculated movement-driven player visibility refresh interval.
    public TimeSpan GetPlayerVisibilityRefreshInterval()
    {
        lock (_sync)
        {
            double internalServerLatencyMilliseconds = GetInternalServerLatencyMilliseconds();
            double combinedBackendMilliseconds = Math.Max(_mapRouteCostMilliseconds, internalServerLatencyMilliseconds);
            double clientPressure = Math.Max(0.0d, _clientLatencyMilliseconds - 50.0d) * 0.25d;
            double backendPressure = Math.Max(0.0d, combinedBackendMilliseconds - 10.0d) * 1.25d;
            double cadenceTarget = Math.Clamp(_movementCadenceMilliseconds, MinimumPlayerVisibilityMilliseconds, MaximumPlayerVisibilityMilliseconds);
            double calculated = Math.Max(MinimumPlayerVisibilityMilliseconds, cadenceTarget + clientPressure + backendPressure + _visibleRecipientPressure);
            return TimeSpan.FromMilliseconds(Math.Clamp(calculated, MinimumPlayerVisibilityMilliseconds, MaximumPlayerVisibilityMilliseconds));
        }
    }

    // Method: GetInternalServerLatencyMilliseconds
    // Purpose: Retrieves the shared internal server latency measurement for the timing calculation.
    // Parameters: none.
    // Returns: Returns latency in milliseconds, or zero when no shared latency has been measured yet.
    private double GetInternalServerLatencyMilliseconds()
    {
        TimeSpan? latency = _internalServerLatencyResolver();
        return latency is null
            ? 0.0d
            : Math.Clamp(latency.Value.TotalMilliseconds, 0.0d, MaximumMapRouteCostMilliseconds);
    }

    // Method: Smooth
    // Purpose: Applies exponential smoothing to avoid sudden timing jumps from a single network or server spike.
    // Parameters:
    // - current: Current smoothed value.
    // - sample: Latest observed value.
    // - weight: Weight to apply to the latest observation.
    // Returns: Returns the smoothed value.
    private static double Smooth(double current, double sample, double weight)
    {
        return current + ((sample - current) * weight);
    }
}
