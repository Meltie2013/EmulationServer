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
// File: src/EmulationServer.Network/Networking/Protocol/InternalMapServiceStatusPacket.cs
// Purpose: Contains internal map service status packet code for the packet serialization, socket transport, and protocol framing layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Globalization;

namespace EmulationServer.Network.Networking.Protocol;

// Type: InternalMapServiceStatusPacket
// Purpose: Represents internal map service status packet data passed through the packet serialization, socket transport, and protocol framing layer.
// Constructor values:
// - OwnerServerName: Owner server name value supplied by the caller for this operation.
// - Kind: Kind value supplied by the caller for this operation.
// - MapId: Map ID identifier used to select the exact record, object, or runtime owner.
// - InstanceId: Instance ID identifier used to select the exact record, object, or runtime owner.
// - State: State value supplied by the caller for this operation.
// - Tick: Tick value supplied by the caller for this operation.
// - ActivePlayers: Active players value supplied by the caller for this operation.
// - ActiveGrids: Active grids value supplied by the caller for this operation.
// - LastTickMilliseconds: Last tick milliseconds value supplied by the caller for this operation.
// - AverageTickMilliseconds: Average tick milliseconds value supplied by the caller for this operation.
// - LoadPercent: Load percent value supplied by the caller for this operation.
// - StartedUtc: Started utc value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record InternalMapServiceStatusPacket(
    string OwnerServerName,
    string Kind,
    int MapId,
    long InstanceId,
    string State,
    long Tick,
    int ActivePlayers,
    int ActiveGrids,
    double LastTickMilliseconds,
    double AverageTickMilliseconds,
    double LoadPercent,
    DateTimeOffset StartedUtc)
{

    // Method: ToPacketLine
    // Purpose: Executes the to packet line operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters: none.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to InternalMapServiceStatusPacket so callers do not duplicate validation, protocol, or persistence rules.
    public string ToPacketLine()
    {
        long startedUnixTimeSeconds = StartedUtc <= DateTimeOffset.UnixEpoch
            ? 0
            : StartedUtc.ToUnixTimeSeconds();

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{InternalProtocol.MapServiceStatus} {OwnerServerName} {Kind} {MapId} {InstanceId} {State} {Tick} {ActivePlayers} {ActiveGrids} {LastTickMilliseconds:0.###} {AverageTickMilliseconds:0.###} {LoadPercent:0.##} {startedUnixTimeSeconds}");
    }

    // Method: TryParse
    // Purpose: Attempts to retrieve or parse try parse data without treating normal misses as failures.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - status: Status value supplied by the caller for this operation.
    // Returns: Returns true when try parse succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to InternalMapServiceStatusPacket so callers do not duplicate validation, protocol, or persistence rules.
    public static bool TryParse(string packet, out InternalMapServiceStatusPacket status)
    {
        status = Empty;

        if (string.IsNullOrWhiteSpace(packet))
        {
            return false;
        }

        string[] parts = packet.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if ((parts.Length != 12 && parts.Length != 13) || !string.Equals(parts[0], InternalProtocol.MapServiceStatus, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int mapId) || mapId < 0)
        {
            return false;
        }

        if (!long.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out long instanceId) || instanceId < 0)
        {
            return false;
        }

        if (!long.TryParse(parts[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out long tick) || tick < 0)
        {
            return false;
        }

        if (!int.TryParse(parts[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out int activePlayers) || activePlayers < 0)
        {
            return false;
        }

        if (!int.TryParse(parts[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out int activeGrids) || activeGrids < 0)
        {
            return false;
        }

        if (!double.TryParse(parts[9], NumberStyles.Float, CultureInfo.InvariantCulture, out double lastTickMilliseconds) || lastTickMilliseconds < 0)
        {
            return false;
        }

        if (!double.TryParse(parts[10], NumberStyles.Float, CultureInfo.InvariantCulture, out double averageTickMilliseconds) || averageTickMilliseconds < 0)
        {
            return false;
        }

        if (!double.TryParse(parts[11], NumberStyles.Float, CultureInfo.InvariantCulture, out double loadPercent) || loadPercent < 0)
        {
            return false;
        }

        DateTimeOffset startedUtc = DateTimeOffset.UnixEpoch;
        if (parts.Length == 13)
        {
            if (!long.TryParse(parts[12], NumberStyles.Integer, CultureInfo.InvariantCulture, out long startedUnixTimeSeconds) || startedUnixTimeSeconds < 0)
            {
                return false;
            }

            try
            {
                startedUtc = startedUnixTimeSeconds == 0
                    ? DateTimeOffset.UnixEpoch
                    : DateTimeOffset.FromUnixTimeSeconds(startedUnixTimeSeconds);
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        status = new InternalMapServiceStatusPacket(
            parts[1],
            parts[2],
            mapId,
            instanceId,
            parts[5],
            tick,
            activePlayers,
            activeGrids,
            lastTickMilliseconds,
            averageTickMilliseconds,
            loadPercent,
            startedUtc);

        return true;
    }

    private static InternalMapServiceStatusPacket Empty { get; } = new(
        string.Empty,
        string.Empty,
        0,
        0,
        string.Empty,
        0,
        0,
        0,
        0,
        0,
        0,
        DateTimeOffset.UnixEpoch);
}
