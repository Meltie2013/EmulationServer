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

using EmulationServer.Game.Players;

/**
  * File overview: src/EmulationServer.Game/Movement/PlayerMovementState.cs
  * Documents the PlayerMovementState source file in the movement packet state and client coordinate tracking area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Movement;

/**
  * Carries immutable player movement state data for the movement packet state and client coordinate tracking layer.
  * Records in this project are used as explicit transfer models so packet parsing, database repositories, and runtime systems can pass strongly typed values without mutating shared state.
  * Positional fields carried by this record: PlayerGuid, AccountId, ClientGuid, Map, Zone, Opcode, Flags, ClientTime, Position, Transport, Pitch, FallTime, Jump, LastUpdatedUtc.
  */
public sealed record PlayerMovementState(
    uint PlayerGuid,
    uint AccountId,
    ulong ClientGuid,
    uint Map,
    uint Zone,
    ushort Opcode,
    MovementFlags Flags,
    uint ClientTime,
    MovementPosition Position,
    TransportMovementInfo? Transport,
    float? Pitch,
    uint FallTime,
    JumpMovementInfo? Jump,
    DateTimeOffset LastUpdatedUtc)
{
    /**
      * Stores the default position x value used when the caller does not supply an override.
      * Centralizing the default keeps configuration and packet behavior consistent across the server process.
      */
    public float PositionX => Position.X;

    /**
      * Stores the default position y value used when the caller does not supply an override.
      * Centralizing the default keeps configuration and packet behavior consistent across the server process.
      */
    public float PositionY => Position.Y;

    /**
      * Stores the default position z value used when the caller does not supply an override.
      * Centralizing the default keeps configuration and packet behavior consistent across the server process.
      */
    public float PositionZ => Position.Z;

    /**
      * Stores the default orientation value used when the caller does not supply an override.
      * Centralizing the default keeps configuration and packet behavior consistent across the server process.
      */
    public float Orientation => Position.Orientation;

    /**
      * Performs the from player operation for the movement packet state and client coordinate tracking workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: player, opcode.
      */
    public static PlayerMovementState FromPlayer(PlayerLoginRecord player, ushort opcode = 0)
    {
        ArgumentNullException.ThrowIfNull(player);

        return new PlayerMovementState(
            player.Guid,
            player.AccountId,
            player.ClientGuid,
            player.Map,
            player.Zone,
            opcode,
            MovementFlags.None,
            0,
            new MovementPosition(player.PositionX, player.PositionY, player.PositionZ, player.Orientation),
            null,
            null,
            0,
            null,
            DateTimeOffset.UtcNow);
    }
}
