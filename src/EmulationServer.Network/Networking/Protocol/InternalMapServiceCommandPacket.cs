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
  * File overview: src/EmulationServer.Network/Networking/Protocol/InternalMapServiceCommandPacket.cs
  * Documents the InternalMapServiceCommandPacket source file in the internal server networking, packet framing, and peer/session lifecycle area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Network.Networking.Protocol;

/**
  * Represents immutable internal map service command packet data passed between parts of the server.
  * It represents an internal protocol payload exchanged between server processes.
  * Positional fields carried by this record: CommandId, Action, MapId.
  */
public sealed record InternalMapServiceCommandPacket(
    string CommandId,
    string Action,
    int MapId)
{
    /**
      * Performs the to packet line operation for the internal server networking, packet framing, and peer/session lifecycle workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      */
    public string ToPacketLine()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{InternalProtocol.MapServiceCommand} {CommandId} {Action} {MapId}");
    }

    /**
      * Attempts the operation without treating a normal failure as an exceptional condition.
      * The method is part of InternalMapServiceCommandPacket and keeps this workflow isolated from the caller.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    public static bool TryParse(string packet, out InternalMapServiceCommandPacket command)
    {
        command = Empty;

        if (string.IsNullOrWhiteSpace(packet))
        {
            return false;
        }

        string[] parts = packet.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4 || !string.Equals(parts[0], InternalProtocol.MapServiceCommand, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(parts[1]) || string.IsNullOrWhiteSpace(parts[2]))
        {
            return false;
        }

        if (!int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int mapId) || mapId < 0)
        {
            return false;
        }

        command = new InternalMapServiceCommandPacket(parts[1], parts[2], mapId);
        return true;
    }

    /**
      * Gets or stores the empty value used by InternalMapServiceCommandPacket.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    private static InternalMapServiceCommandPacket Empty { get; } = new(string.Empty, string.Empty, 0);
}
