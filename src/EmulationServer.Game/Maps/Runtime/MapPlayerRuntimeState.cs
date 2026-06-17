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
// File: src/EmulationServer.Game/Maps/Runtime/MapPlayerRuntimeState.cs
// Purpose: Contains map player runtime state code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Maps.Runtime;

// Type: MapPlayerRuntimeState
// Purpose: Represents map player runtime state data passed through the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - AccountId: Account ID identifier used to select the exact record, object, or runtime owner.
// - Guid: GUID identifier used to select the exact record, object, or runtime owner.
// - Name: Name value supplied by the caller for this operation.
// - Map: Map value supplied by the caller for this operation.
// - Zone: Zone value supplied by the caller for this operation.
// - PositionX: Position X value supplied by the caller for this operation.
// - PositionY: Position Y value supplied by the caller for this operation.
// - PositionZ: Position Z value supplied by the caller for this operation.
// - Orientation: Orientation value supplied by the caller for this operation.
// - LastMovementOpcode: Last movement opcode value supplied by the caller for this operation.
// - MovementFlags: Movement flags value supplied by the caller for this operation.
// - ClientMovementTime: Client movement time value supplied by the caller for this operation.
// - LastUpdatedUtc: Last updated utc value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record MapPlayerRuntimeState(
    uint AccountId,
    uint Guid,
    string Name,
    uint Map,
    uint Zone,
    float PositionX,
    float PositionY,
    float PositionZ,
    float Orientation,
    ushort LastMovementOpcode,
    uint MovementFlags,
    uint ClientMovementTime,
    DateTimeOffset LastUpdatedUtc);
