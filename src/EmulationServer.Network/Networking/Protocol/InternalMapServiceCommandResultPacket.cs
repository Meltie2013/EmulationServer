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
using System.Text;

/**
  * File overview: src/EmulationServer.Network/Networking/Protocol/InternalMapServiceCommandResultPacket.cs
  * This file belongs to the internal server-to-server protocol packet parsing and formatting portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Network.Networking.Protocol;

/**
  * Represents immutable internal map service command result packet data passed between parts of the server.
  * It represents an internal protocol payload exchanged between server processes.
  */
public sealed record InternalMapServiceCommandResultPacket(
    string CommandId,
    string OwnerServerName,
    string Kind,
    int MapId,
    long InstanceId,
    string ResultCode,
    string State,
    string Message)
{
    /**
      * Performs the to packet line operation for InternalMapServiceCommandResultPacket.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
    public string ToPacketLine()
    {
        string encodedMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(Message ?? string.Empty));

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{InternalProtocol.MapServiceCommandResult} {CommandId} {OwnerServerName} {Kind} {MapId} {InstanceId} {ResultCode} {State} {encodedMessage}");
    }

    /**
      * Attempts the operation without treating a normal failure as an exceptional condition.
      * The method is part of InternalMapServiceCommandResultPacket and keeps this workflow isolated from the caller.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    public static bool TryParse(string packet, out InternalMapServiceCommandResultPacket result)
    {
        result = Empty;

        if (string.IsNullOrWhiteSpace(packet))
        {
            return false;
        }

        string[] parts = packet.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 9 || !string.Equals(parts[0], InternalProtocol.MapServiceCommandResult, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int mapId) || mapId < 0)
        {
            return false;
        }

        if (!long.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out long instanceId) || instanceId < 0)
        {
            return false;
        }

        string message;
        try
        {
            message = Encoding.UTF8.GetString(Convert.FromBase64String(parts[8]));
        }
        catch (FormatException)
        {
            return false;
        }

        result = new InternalMapServiceCommandResultPacket(
            parts[1],
            parts[2],
            parts[3],
            mapId,
            instanceId,
            parts[6],
            parts[7],
            message);

        return true;
    }

    /**
      * Gets or stores the empty value used by InternalMapServiceCommandResultPacket.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    private static InternalMapServiceCommandResultPacket Empty { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        0,
        0,
        string.Empty,
        string.Empty,
        string.Empty);
}
