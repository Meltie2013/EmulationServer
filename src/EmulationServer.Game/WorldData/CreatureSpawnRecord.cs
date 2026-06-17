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
// File: src/EmulationServer.Game/WorldData/CreatureSpawnRecord.cs
// Purpose: Contains creature spawn record code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.WorldData;

// Type: CreatureSpawnRecord
// Purpose: Represents creature spawn record data passed through the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - Guid: GUID identifier used to select the exact record, object, or runtime owner.
// - Entry: Entry value supplied by the caller for this operation.
// - Map: Map value supplied by the caller for this operation.
// - ZoneId: Zone ID identifier used to select the exact record, object, or runtime owner.
// - AreaId: Area ID identifier used to select the exact record, object, or runtime owner.
// - ModelId: Model ID identifier used to select the exact record, object, or runtime owner.
// - EquipmentId: Equipment ID identifier used to select the exact record, object, or runtime owner.
// - PositionX: Position X value supplied by the caller for this operation.
// - PositionY: Position Y value supplied by the caller for this operation.
// - PositionZ: Position Z value supplied by the caller for this operation.
// - Orientation: Orientation value supplied by the caller for this operation.
// - SpawnTimeSeconds: Spawn time seconds value supplied by the caller for this operation.
// - SpawnDistance: Spawn distance value supplied by the caller for this operation.
// - CurrentWaypoint: Current waypoint value supplied by the caller for this operation.
// - CurrentHealth: Current health value supplied by the caller for this operation.
// - CurrentMana: Current mana value supplied by the caller for this operation.
// - DeathState: Death state value supplied by the caller for this operation.
// - MovementType: Movement type value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record CreatureSpawnRecord(
    uint Guid,
    uint Entry,
    ushort Map,
    uint ZoneId,
    uint AreaId,
    uint ModelId,
    int EquipmentId,
    float PositionX,
    float PositionY,
    float PositionZ,
    float Orientation,
    uint SpawnTimeSeconds,
    float SpawnDistance,
    uint CurrentWaypoint,
    uint CurrentHealth,
    uint CurrentMana,
    byte DeathState,
    byte MovementType);
