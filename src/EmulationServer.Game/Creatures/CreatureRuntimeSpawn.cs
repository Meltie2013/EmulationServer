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

using EmulationServer.Game.WorldData;

namespace EmulationServer.Game.Creatures;

/**
  * Represents one live creature/NPC spawn owned by a map runtime.
  */
public sealed record CreatureRuntimeSpawn(
    CreatureSpawnRecord Spawn,
    CreatureTemplateRecord? Template,
    bool IsSpawned)
{
    public uint DisplayModelId => Template is null ? Spawn.ModelId : CreatureDataValidation.ResolveDisplayModelId(Spawn, Template);

    public uint CurrentHealth => Template?.GetEffectiveHealth(Spawn.CurrentHealth) ?? Spawn.CurrentHealth;

    public uint CurrentMana => Template?.GetEffectiveMana(Spawn.CurrentMana) ?? Spawn.CurrentMana;

    public byte UnitClass => Template?.GetEffectiveUnitClass() ?? (byte)1;

    public byte InhabitType => Template?.GetEffectiveInhabitType() ?? (byte)3;
}
