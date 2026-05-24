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

using System.Diagnostics.CodeAnalysis;

using EmulationServer.Game.Movement;
using EmulationServer.Game.Players;
using EmulationServer.WorldServer.Networking.Packets;

/**
  * File overview: src/WorldServer/Networking/Movement/WorldMovementPacketParser.cs
  * Documents the WorldMovementPacketParser source file in the world movement opcode parsing and server-side movement state updates area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.WorldServer.Networking.Movement;

/**
  * Owns the world movement packet parser behavior for the world movement opcode parsing and server-side movement state updates layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public static class WorldMovementPacketParser
{
    /**
      * Defines the constant value for ack movement info offset.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const int AckMovementInfoOffset = 12;
    /**
      * Defines the constant value for maximum map coordinate.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const float MaximumMapCoordinate = 100000.0f;
    /**
      * Defines the constant value for minimum map height.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const float MinimumMapHeight = -5000.0f;
    /**
      * Defines the constant value for maximum map height.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const float MaximumMapHeight = 10000.0f;
    /**
      * Defines the constant value for maximum transport offset.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const float MaximumTransportOffset = 500.0f;

    /**
      * Defines the constant value for pitch flags.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const MovementFlags PitchFlags =
        MovementFlags.Swimming |
        MovementFlags.Flying |
        MovementFlags.Ascending |
        MovementFlags.Descending |
        MovementFlags.PitchUp |
        MovementFlags.PitchDown;

    /**
      * Tries to resolve the read movement state value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: player, opcode, payload, state.
      */
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
            WorldPacketReader reader = new(payload[movementInfoOffset..]);
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

    /**
      * Resolves the movement info offset value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: opcode, payloadLength.
      */
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

    /**
      * Determines whether valid position for the world movement opcode parsing and server-side movement state updates workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: position.
      */
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

    /**
      * Determines whether valid transport for the world movement opcode parsing and server-side movement state updates workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: transport.
      */
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
