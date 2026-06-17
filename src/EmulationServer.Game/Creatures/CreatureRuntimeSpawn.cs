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
// File: src/EmulationServer.Game/Creatures/CreatureRuntimeSpawn.cs
// Purpose: Contains creature runtime spawn code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Game.WorldData;

namespace EmulationServer.Game.Creatures;

// Type: CreatureRuntimeSpawn
// Purpose: Represents creature runtime spawn data passed through the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - Spawn: Spawn value supplied by the caller for this operation.
// - Template: Template value supplied by the caller for this operation.
// - IsSpawned: Is spawned value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record CreatureRuntimeSpawn(
    CreatureSpawnRecord Spawn,
    CreatureTemplateRecord? Template,
    bool IsSpawned)
{
    // Method: ResolveDisplayModelId
    // Purpose: Retrieves resolve display model ID data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - Spawn: Spawn value supplied by the caller for this operation.
    // - Template: Template value supplied by the caller for this operation.
    // Returns: Returns the uint display model ID => template is null ? spawn.model ID : creature data validation. value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureRuntimeSpawn so callers do not duplicate validation, protocol, or persistence rules.
    public uint DisplayModelId => Template is null ? Spawn.ModelId : CreatureDataValidation.ResolveDisplayModelId(Spawn, Template);

    // Method: GetEffectiveHealth
    // Purpose: Retrieves get effective health data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - CurrentHealth: Current health value supplied by the caller for this operation.
    // Returns: Returns the uint current health => template?. value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureRuntimeSpawn so callers do not duplicate validation, protocol, or persistence rules.
    public uint CurrentHealth => Template?.GetEffectiveHealth(Spawn.CurrentHealth) ?? Spawn.CurrentHealth;

    // Method: GetEffectiveMana
    // Purpose: Retrieves get effective mana data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - CurrentMana: Current mana value supplied by the caller for this operation.
    // Returns: Returns the uint current mana => template?. value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureRuntimeSpawn so callers do not duplicate validation, protocol, or persistence rules.
    public uint CurrentMana => Template?.GetEffectiveMana(Spawn.CurrentMana) ?? Spawn.CurrentMana;

    // Method: GetEffectiveUnitClass
    // Purpose: Retrieves get effective unit class data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the byte unit class => template?. value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureRuntimeSpawn so callers do not duplicate validation, protocol, or persistence rules.
    public byte UnitClass => Template?.GetEffectiveUnitClass() ?? (byte)1;

    // Method: GetEffectiveInhabitType
    // Purpose: Retrieves get effective inhabit type data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the byte inhabit type => template?. value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureRuntimeSpawn so callers do not duplicate validation, protocol, or persistence rules.
    public byte InhabitType => Template?.GetEffectiveInhabitType() ?? (byte)3;
}
