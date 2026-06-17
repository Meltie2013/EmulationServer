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
// File: src/EmulationServer.Network/Networking/Protocol/InternalWorldHealthStatusPacket.cs
// Purpose: Contains internal world health status packet code for the packet serialization, socket transport, and protocol framing layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Globalization;

namespace EmulationServer.Network.Networking.Protocol;

// Type: InternalWorldHealthStatusPacket
// Purpose: Represents internal world health status packet data passed through the packet serialization, socket transport, and protocol framing layer.
// Constructor values:
// - OwnerServerName: Owner server name value supplied by the caller for this operation.
// - ActivePlayers: Active players value supplied by the caller for this operation.
// - MaxConnections: Max connections value supplied by the caller for this operation.
// - ReportedUtc: Reported utc value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record InternalWorldHealthStatusPacket(
    string OwnerServerName,
    int ActivePlayers,
    int MaxConnections,
    DateTimeOffset ReportedUtc)
{

    // Method: ToPacketLine
    // Purpose: Executes the to packet line operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters: none.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to InternalWorldHealthStatusPacket so callers do not duplicate validation, protocol, or persistence rules.
    public string ToPacketLine()
    {
        long reportedUnixTimeSeconds = ReportedUtc.ToUnixTimeSeconds();

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{InternalProtocol.WorldHealthStatus} {OwnerServerName} {ActivePlayers} {MaxConnections} {reportedUnixTimeSeconds}");
    }

    // Method: TryParse
    // Purpose: Attempts to retrieve or parse try parse data without treating normal misses as failures.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - status: Status value supplied by the caller for this operation.
    // Returns: Returns true when try parse succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to InternalWorldHealthStatusPacket so callers do not duplicate validation, protocol, or persistence rules.
    public static bool TryParse(string packet, out InternalWorldHealthStatusPacket status)
    {
        status = default!;

        if (string.IsNullOrWhiteSpace(packet))
        {
            return false;
        }

        string[] parts = packet.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5 || !string.Equals(parts[0], InternalProtocol.WorldHealthStatus, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!InternalProtocol.IsValidServerName(parts[1]))
        {
            return false;
        }

        if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int activePlayers) || activePlayers < 0)
        {
            return false;
        }

        if (!int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int maxConnections) || maxConnections <= 0)
        {
            return false;
        }

        if (!long.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out long reportedUnixTimeSeconds))
        {
            return false;
        }

        status = new InternalWorldHealthStatusPacket(
            parts[1],
            activePlayers,
            maxConnections,
            DateTimeOffset.FromUnixTimeSeconds(reportedUnixTimeSeconds));

        return true;
    }
}
