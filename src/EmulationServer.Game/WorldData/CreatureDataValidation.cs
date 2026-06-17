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
// File: src/EmulationServer.Game/WorldData/CreatureDataValidation.cs
// Purpose: Contains creature data validation code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.WorldData;

// Type: CreatureDataValidation
// Purpose: Provides creature data validation behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class CreatureDataValidation
{
    // Constant: Defines the minimum creature scale constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed minimum creature scale value used anywhere this rule or protocol value is needed.
    private const float MinimumCreatureScale = 0.0001f;
    // Constant: Defines the maximum creature scale constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed maximum creature scale value used anywhere this rule or protocol value is needed.
    private const float MaximumCreatureScale = 100.0f;

    // Method: IsLoadableTemplate
    // Purpose: Validates or evaluates is loadable template rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - template: Template value supplied by the caller for this operation.
    // Returns: Returns true when is loadable template succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CreatureDataValidation so callers do not duplicate validation, protocol, or persistence rules.
    public static bool IsLoadableTemplate(CreatureTemplateRecord template)
    {
        ArgumentNullException.ThrowIfNull(template);

        return template.Entry != 0 &&
            !string.IsNullOrWhiteSpace(template.Name) &&
            IsFiniteRecoverableScale(template.Scale) &&
            IsFiniteNonNegative(template.SpeedWalk) &&
            IsFiniteNonNegative(template.SpeedRun) &&
            IsFiniteNonNegative(template.HealthMultiplier) &&
            IsFiniteNonNegative(template.PowerMultiplier) &&
            IsFiniteNonNegative(template.DamageMultiplier) &&
            IsFiniteNonNegative(template.DamageVariance) &&
            IsFiniteNonNegative(template.ArmorMultiplier) &&
            IsFiniteNonNegative(template.ExperienceMultiplier);
    }

    // Method: IsLoadableSpawn
    // Purpose: Validates or evaluates is loadable spawn rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - spawn: Spawn value supplied by the caller for this operation.
    // Returns: Returns true when is loadable spawn succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CreatureDataValidation so callers do not duplicate validation, protocol, or persistence rules.
    public static bool IsLoadableSpawn(CreatureSpawnRecord spawn)
    {
        ArgumentNullException.ThrowIfNull(spawn);

        return spawn.Guid != 0 &&
            spawn.Entry != 0 &&
            spawn.ZoneId != 0 &&
            spawn.AreaId != 0 &&
            IsFiniteWorldPosition(spawn.PositionX, spawn.PositionY, spawn.PositionZ) &&
            float.IsFinite(spawn.Orientation) &&
            IsFiniteNonNegative(spawn.SpawnDistance);
    }

    // Method: ResolveDisplayModelId
    // Purpose: Retrieves resolve display model ID data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - spawn: Spawn value supplied by the caller for this operation.
    // - template: Template value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureDataValidation so callers do not duplicate validation, protocol, or persistence rules.
    public static uint ResolveDisplayModelId(CreatureSpawnRecord spawn, CreatureTemplateRecord template)
    {
        ArgumentNullException.ThrowIfNull(spawn);
        ArgumentNullException.ThrowIfNull(template);

        return spawn.ModelId != 0 ? spawn.ModelId : template.GetPreferredModelId();
    }

    // Method: HasClientVisibleDisplay
    // Purpose: Validates or evaluates has client visible display rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - spawn: Spawn value supplied by the caller for this operation.
    // - template: Template value supplied by the caller for this operation.
    // Returns: Returns true when has client visible display succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CreatureDataValidation so callers do not duplicate validation, protocol, or persistence rules.
    public static bool HasClientVisibleDisplay(CreatureSpawnRecord spawn, CreatureTemplateRecord template)
    {
        return ResolveDisplayModelId(spawn, template) != 0;
    }

    // Method: IsClientVisibleCreature
    // Purpose: Validates or evaluates is client visible creature rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - spawn: Spawn value supplied by the caller for this operation.
    // - template: Template value supplied by the caller for this operation.
    // Returns: Returns true when is client visible creature succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CreatureDataValidation so callers do not duplicate validation, protocol, or persistence rules.
    public static bool IsClientVisibleCreature(CreatureSpawnRecord spawn, CreatureTemplateRecord template)
    {
        ArgumentNullException.ThrowIfNull(spawn);
        ArgumentNullException.ThrowIfNull(template);

        return IsLoadableSpawn(spawn) &&
            IsLoadableTemplate(template) &&
            HasClientVisibleDisplay(spawn, template) &&
            spawn.DeathState == 0 &&
            template.GetEffectiveMinLevel() > 0 &&
            template.GetEffectiveMaxLevel() >= template.GetEffectiveMinLevel();
    }

    // Method: IsFiniteRecoverableScale
    // Purpose: Validates or evaluates is finite recoverable scale rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns true when is finite recoverable scale succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CreatureDataValidation so callers do not duplicate validation, protocol, or persistence rules.
    private static bool IsFiniteRecoverableScale(float value)
    {
        return float.IsFinite(value) && value >= 0.0f && value <= MaximumCreatureScale;
    }

    // Method: IsFinitePositiveScale
    // Purpose: Validates or evaluates is finite positive scale rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns true when is finite positive scale succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CreatureDataValidation so callers do not duplicate validation, protocol, or persistence rules.
    private static bool IsFinitePositiveScale(float value)
    {
        return float.IsFinite(value) && value >= MinimumCreatureScale && value <= MaximumCreatureScale;
    }

    // Method: IsFiniteNonNegative
    // Purpose: Validates or evaluates is finite non negative rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns true when is finite non negative succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CreatureDataValidation so callers do not duplicate validation, protocol, or persistence rules.
    private static bool IsFiniteNonNegative(float value)
    {
        return float.IsFinite(value) && value >= 0.0f;
    }

    // Method: IsFiniteWorldPosition
    // Purpose: Validates or evaluates is finite world position rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - x: X value supplied by the caller for this operation.
    // - y: Y value supplied by the caller for this operation.
    // - z: Z value supplied by the caller for this operation.
    // Returns: Returns true when is finite world position succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CreatureDataValidation so callers do not duplicate validation, protocol, or persistence rules.
    private static bool IsFiniteWorldPosition(float x, float y, float z)
    {
        return float.IsFinite(x) && float.IsFinite(y) && float.IsFinite(z);
    }
}
