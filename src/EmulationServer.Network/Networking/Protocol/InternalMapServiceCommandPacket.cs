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
// File: src/EmulationServer.Network/Networking/Protocol/InternalMapServiceCommandPacket.cs
// Purpose: Contains internal map service command packet code for the packet serialization, socket transport, and protocol framing layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Globalization;

namespace EmulationServer.Network.Networking.Protocol;

// Type: InternalMapServiceCommandPacket
// Purpose: Represents internal map service command packet data passed through the packet serialization, socket transport, and protocol framing layer.
// Constructor values:
// - CommandId: Command ID identifier used to select the exact record, object, or runtime owner.
// - Action: Action value supplied by the caller for this operation.
// - MapId: Map ID identifier used to select the exact record, object, or runtime owner.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record InternalMapServiceCommandPacket(
    string CommandId,
    string Action,
    int MapId)
{

    // Method: ToPacketLine
    // Purpose: Executes the to packet line operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters: none.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to InternalMapServiceCommandPacket so callers do not duplicate validation, protocol, or persistence rules.
    public string ToPacketLine()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{InternalProtocol.MapServiceCommand} {CommandId} {Action} {MapId}");
    }

    // Method: TryParse
    // Purpose: Attempts to retrieve or parse try parse data without treating normal misses as failures.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - command: Database command used to execute this operation without opening unnecessary additional state.
    // Returns: Returns true when try parse succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to InternalMapServiceCommandPacket so callers do not duplicate validation, protocol, or persistence rules.
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

    private static InternalMapServiceCommandPacket Empty { get; } = new(string.Empty, string.Empty, 0);
}
