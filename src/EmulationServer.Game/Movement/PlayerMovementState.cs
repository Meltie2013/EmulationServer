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

namespace EmulationServer.Game.Movement;

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
    public float PositionX => Position.X;

    public float PositionY => Position.Y;

    public float PositionZ => Position.Z;

    public float Orientation => Position.Orientation;

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
