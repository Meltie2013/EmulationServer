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
// File: src/WorldServer/Networking/Movement/WorldMovementPacketParser.cs
// Purpose: Contains world movement packet parser code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Diagnostics.CodeAnalysis;

using EmulationServer.Game.Movement;
using EmulationServer.Game.Players;
using EmulationServer.WorldServer.Networking.Packets;

namespace EmulationServer.WorldServer.Networking.Movement;

// Type: WorldMovementPacketParser
// Purpose: Provides world movement packet parser behavior for the world server gameplay, session, and character runtime layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class WorldMovementPacketParser
{

    // Constant: Defines the ack movement info offset constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed ack movement info offset value used anywhere this rule or protocol value is needed.
    private const int AckMovementInfoOffset = 12;

    // Constant: Defines the maximum map coordinate constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed maximum map coordinate value used anywhere this rule or protocol value is needed.
    private const float MaximumMapCoordinate = 100000.0f;

    // Constant: Defines the minimum map height constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed minimum map height value used anywhere this rule or protocol value is needed.
    private const float MinimumMapHeight = -5000.0f;

    // Constant: Defines the maximum map height constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed maximum map height value used anywhere this rule or protocol value is needed.
    private const float MaximumMapHeight = 10000.0f;

    // Constant: Defines the maximum transport offset constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed maximum transport offset value used anywhere this rule or protocol value is needed.
    private const float MaximumTransportOffset = 500.0f;

    private const MovementFlags PitchFlags =
        MovementFlags.Swimming |
        MovementFlags.Flying |
        MovementFlags.Ascending |
        MovementFlags.Descending |
        MovementFlags.PitchUp |
        MovementFlags.PitchDown;

    // Method: TryReadMovementState
    // Purpose: Attempts to retrieve or parse try read movement state data without treating normal misses as failures.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - opcode: Opcode value supplied by the caller for this operation.
    // - bytepayload: Bytepayload value supplied by the caller for this operation.
    // - state: State value supplied by the caller for this operation.
    // Returns: Returns true when try read movement state succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldMovementPacketParser so callers do not duplicate validation, protocol, or persistence rules.
    public static bool TryReadMovementState(
        PlayerLoginRecord player,
        WorldOpcode opcode,
        byte[] payload,
        [NotNullWhen(true)] out PlayerMovementState? state)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(payload);

        state = null;
        int movementInfoOffset = ResolveMovementInfoOffset(opcode, payload.Length);
        if (movementInfoOffset < 0)
        {
            return false;
        }

        try
        {
            WorldPacketReader reader = new(payload.AsMemory(movementInfoOffset));
            MovementFlags flags = (MovementFlags)reader.ReadUInt32();
            uint clientTime = reader.ReadUInt32();
            MovementPosition position = new(
                reader.ReadFloat(),
                reader.ReadFloat(),
                reader.ReadFloat(),
                reader.ReadFloat());

            TransportMovementInfo? transport = null;
            if ((flags & MovementFlags.OnTransport) != 0 && reader.Remaining >= 28)
            {
                transport = new TransportMovementInfo(
                    reader.ReadUInt64(),
                    reader.ReadFloat(),
                    reader.ReadFloat(),
                    reader.ReadFloat(),
                    reader.ReadFloat(),
                    reader.ReadUInt32());
            }

            float? pitch = null;
            if ((flags & PitchFlags) != 0 && reader.Remaining >= sizeof(float))
            {
                pitch = reader.ReadFloat();
            }

            uint fallTime = 0;
            if (reader.Remaining >= sizeof(uint))
            {
                fallTime = reader.ReadUInt32();
            }

            JumpMovementInfo? jump = null;
            if ((flags & MovementFlags.Falling) != 0 && reader.Remaining >= 16)
            {
                jump = new JumpMovementInfo(
                    fallTime,
                    reader.ReadFloat(),
                    reader.ReadFloat(),
                    reader.ReadFloat(),
                    reader.ReadFloat());
            }

            if (!IsValidPosition(position) || !IsValidTransport(transport))
            {
                return false;
            }

            state = new PlayerMovementState(
                player.Guid,
                player.AccountId,
                player.ClientGuid,
                player.Map,
                player.Zone,
                (ushort)opcode,
                flags,
                clientTime,
                position,
                transport,
                pitch,
                fallTime,
                jump,
                DateTimeOffset.UtcNow);

            return true;
        }
        catch (InvalidDataException)
        {
            return false;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    // Method: ResolveMovementInfoOffset
    // Purpose: Retrieves resolve movement info offset data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - opcode: Opcode value supplied by the caller for this operation.
    // - payloadLength: Payload length value supplied by the caller for this operation.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to WorldMovementPacketParser so callers do not duplicate validation, protocol, or persistence rules.
    private static int ResolveMovementInfoOffset(WorldOpcode opcode, int payloadLength)
    {
        if (WorldMovementOpcode.HasMovementInfoAtPayloadStart(opcode))
        {
            return 0;
        }

        if (WorldMovementOpcode.HasAckHeaderBeforeMovementInfo(opcode) && payloadLength >= AckMovementInfoOffset + 20)
        {
            return AckMovementInfoOffset;
        }

        return -1;
    }

    // Method: IsValidPosition
    // Purpose: Validates or evaluates is valid position rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - position: Position value supplied by the caller for this operation.
    // Returns: Returns true when is valid position succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldMovementPacketParser so callers do not duplicate validation, protocol, or persistence rules.
    private static bool IsValidPosition(MovementPosition position)
    {
        return position.IsFinite &&
            MathF.Abs(position.X) <= MaximumMapCoordinate &&
            MathF.Abs(position.Y) <= MaximumMapCoordinate &&
            position.Z >= MinimumMapHeight &&
            position.Z <= MaximumMapHeight &&
            position.Orientation >= -100.0f &&
            position.Orientation <= 100.0f;
    }

    // Method: IsValidTransport
    // Purpose: Validates or evaluates is valid transport rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - transport: Transport value supplied by the caller for this operation.
    // Returns: Returns true when is valid transport succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldMovementPacketParser so callers do not duplicate validation, protocol, or persistence rules.
    private static bool IsValidTransport(TransportMovementInfo? transport)
    {
        if (transport is null)
        {
            return true;
        }

        return transport.IsFinite &&
            MathF.Abs(transport.X) <= MaximumTransportOffset &&
            MathF.Abs(transport.Y) <= MaximumTransportOffset &&
            MathF.Abs(transport.Z) <= MaximumTransportOffset &&
            transport.Orientation >= -100.0f &&
            transport.Orientation <= 100.0f;
    }
}
