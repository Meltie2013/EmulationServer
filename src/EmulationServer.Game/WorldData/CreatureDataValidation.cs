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
  * Centralizes creature/NPC data gates used by the world cache, internal snapshots, and map runtime.
  * The loader must be permissive enough to cache real Mangos Zero rows. Some valid creature templates rely on
  * spawn-level model ids, UnitClass defaults, or runtime health defaults, so those fields are normalized later
  * instead of causing the whole row to be dropped during startup.
  */
public static class CreatureDataValidation
{
    private const float MinimumCreatureScale = 0.0001f;
    private const float MaximumCreatureScale = 100.0f;

    /**
      * Returns whether a creature_template row is safe to cache and forward to MapServer/InstanceServer.
      * Keep this gate intentionally permissive: Mangos Zero data contains valid helper/trigger/templates with
      * zero levels, zero models, zero UnitClass, or incomplete stat pools. Runtime/client visibility decides
      * which rows are renderable; the world cache should not drop valid template entries before spawns can
      * resolve their references.
      */
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

    /**
      * Returns whether a creature spawn row is safe to cache and hand to map runtime.
      * zoneId/areaId remain required because the project uses those resolved fields to prevent unresolved rows
      * from entering runtime map ownership. curhealth can be zero in imported data and is normalized at runtime.
      */
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

    /**
      * Resolves the display model that should be used for a creature spawn.
      * The spawn override wins because Mangos creature rows can set modelid even when template ModelId1-4 are zero.
      */
    public static uint ResolveDisplayModelId(CreatureSpawnRecord spawn, CreatureTemplateRecord template)
    {
        ArgumentNullException.ThrowIfNull(spawn);
        ArgumentNullException.ThrowIfNull(template);

        return spawn.ModelId != 0 ? spawn.ModelId : template.GetPreferredModelId();
    }

    /**
      * Returns whether a creature spawn/template pair has enough display data for future client create packets.
      */
    public static bool HasClientVisibleDisplay(CreatureSpawnRecord spawn, CreatureTemplateRecord template)
    {
        return ResolveDisplayModelId(spawn, template) != 0;
    }

    /**
      * Returns whether a creature spawn/template pair is safe to serialize into a first-pass client UNIT create block.
      */
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

    private static bool IsFiniteRecoverableScale(float value)
    {
        return float.IsFinite(value) && value >= 0.0f && value <= MaximumCreatureScale;
    }

    private static bool IsFinitePositiveScale(float value)
    {
        return float.IsFinite(value) && value >= MinimumCreatureScale && value <= MaximumCreatureScale;
    }

    private static bool IsFiniteNonNegative(float value)
    {
        return float.IsFinite(value) && value >= 0.0f;
    }

    private static bool IsFiniteWorldPosition(float x, float y, float z)
    {
        return float.IsFinite(x) && float.IsFinite(y) && float.IsFinite(z);
    }
}
