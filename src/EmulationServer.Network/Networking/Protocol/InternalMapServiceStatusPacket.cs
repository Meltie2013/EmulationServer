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

namespace EmulationServer.Network.Networking.Protocol;

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
    double LoadPercent)
{
    public string ToPacketLine()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{InternalProtocol.MapServiceStatus} {OwnerServerName} {Kind} {MapId} {InstanceId} {State} {Tick} {ActivePlayers} {ActiveGrids} {LastTickMilliseconds:0.###} {AverageTickMilliseconds:0.###} {LoadPercent:0.##}");
    }

    public static bool TryParse(string packet, out InternalMapServiceStatusPacket status)
    {
        status = Empty;

        if (string.IsNullOrWhiteSpace(packet))
        {
            return false;
        }

        string[] parts = packet.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 12 || !string.Equals(parts[0], InternalProtocol.MapServiceStatus, StringComparison.OrdinalIgnoreCase))
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
            loadPercent);

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
        0);
}
