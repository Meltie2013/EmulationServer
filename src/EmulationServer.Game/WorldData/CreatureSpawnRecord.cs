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

namespace EmulationServer.Game.WorldData;

/**
  * Carries immutable creature spawn data from the world database.
  * zoneId and areaId are EmulationServer extensions used to index and diagnose where each NPC is spawned.
  */
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
