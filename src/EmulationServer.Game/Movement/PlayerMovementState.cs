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
// File: src/EmulationServer.Game/Movement/PlayerMovementState.cs
// Purpose: Contains player movement state code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Game.Players;

namespace EmulationServer.Game.Movement;

// Type: PlayerMovementState
// Purpose: Represents player movement state data passed through the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - PlayerGuid: Player GUID identifier used to select the exact record, object, or runtime owner.
// - AccountId: Account ID identifier used to select the exact record, object, or runtime owner.
// - ClientGuid: Client GUID identifier used to select the exact record, object, or runtime owner.
// - Map: Map value supplied by the caller for this operation.
// - Zone: Zone value supplied by the caller for this operation.
// - Opcode: Opcode value supplied by the caller for this operation.
// - Flags: Flags value supplied by the caller for this operation.
// - ClientTime: Client time value supplied by the caller for this operation.
// - Position: Position value supplied by the caller for this operation.
// - Transport: Transport value supplied by the caller for this operation.
// - Pitch: Pitch value supplied by the caller for this operation.
// - FallTime: Fall time value supplied by the caller for this operation.
// - Jump: Jump value supplied by the caller for this operation.
// - LastUpdatedUtc: Last updated utc value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
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

    // Property: Gets or sets the position X value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: position X value exposed by the owning type.
    public float PositionX => Position.X;

    // Property: Gets or sets the position Y value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: position Y value exposed by the owning type.
    public float PositionY => Position.Y;

    // Property: Gets or sets the position Z value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: position Z value exposed by the owning type.
    public float PositionZ => Position.Z;

    // Property: Gets or sets the orientation value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: orientation value exposed by the owning type.
    public float Orientation => Position.Orientation;

    // Method: FromPlayer
    // Purpose: Executes the from player operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - opcode: Opcode value supplied by the caller for this operation.
    // Returns: Returns the player movement state value produced by this operation.
    // Notes: This keeps the operation scoped to PlayerMovementState so callers do not duplicate validation, protocol, or persistence rules.
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
