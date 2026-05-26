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

using System.Globalization;

/**
  * File overview: src/EmulationServer.Network/Networking/Protocol/InternalWorldHealthStatusPacket.cs
  * Documents the InternalWorldHealthStatusPacket source file in the internal server networking, packet framing, and peer/session lifecycle area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Network.Networking.Protocol;

/**
  * Carries the WorldServer runtime load snapshot used by ProxyServer health aggregation.
  * ProxyServer owns the health state; WorldServer only reports the current values it already owns.
  * Positional fields carried by this record: OwnerServerName, ActivePlayers, MaxConnections, ReportedUtc.
  */
public sealed record InternalWorldHealthStatusPacket(
    string OwnerServerName,
    int ActivePlayers,
    int MaxConnections,
    DateTimeOffset ReportedUtc)
{
    /**
      * Converts this snapshot into the internal protocol line used between servers.
      */
    public string ToPacketLine()
    {
        long reportedUnixTimeSeconds = ReportedUtc.ToUnixTimeSeconds();

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{InternalProtocol.WorldHealthStatus} {OwnerServerName} {ActivePlayers} {MaxConnections} {reportedUnixTimeSeconds}");
    }

    /**
      * Attempts to parse a protocol line into a world health snapshot.
      */
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
