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
  * Carries immutable creature_template data from the world database.
  * The layout mirrors the Mangos Zero core columns used to construct creature/NPC runtime state.
  */
public sealed record CreatureTemplateRecord(
    uint Entry,
    string Name,
    string SubName,
    byte MinLevel,
    byte MaxLevel,
    uint ModelId1,
    uint ModelId2,
    uint ModelId3,
    uint ModelId4,
    ushort FactionAlliance,
    ushort FactionHorde,
    float Scale,
    sbyte Family,
    byte CreatureType,
    byte InhabitType,
    byte RegenerateStats,
    byte RacialLeader,
    uint NpcFlags,
    uint UnitFlags,
    uint DynamicFlags,
    uint ExtraFlags,
    uint CreatureTypeFlags,
    float SpeedWalk,
    float SpeedRun,
    byte UnitClass,
    byte Rank,
    float HealthMultiplier,
    float PowerMultiplier,
    float DamageMultiplier,
    float DamageVariance,
    float ArmorMultiplier,
    float ExperienceMultiplier,
    uint MinLevelHealth,
    uint MaxLevelHealth,
    uint MinLevelMana,
    uint MaxLevelMana,
    float MinMeleeDamage,
    float MaxMeleeDamage,
    float MinRangedDamage,
    float MaxRangedDamage,
    uint Armor,
    uint MeleeAttackPower,
    ushort RangedAttackPower,
    uint MeleeBaseAttackTime,
    uint RangedBaseAttackTime,
    sbyte DamageSchool,
    uint MinLootGold,
    uint MaxLootGold,
    uint LootId,
    uint PickpocketLootId,
    uint SkinningLootId,
    uint KillCredit1,
    uint KillCredit2,
    uint MechanicImmuneMask,
    uint SchoolImmuneMask,
    short ResistanceHoly,
    short ResistanceFire,
    short ResistanceNature,
    short ResistanceFrost,
    short ResistanceShadow,
    short ResistanceArcane,
    uint SpellListId,
    uint PetSpellDataId,
    byte MovementType,
    sbyte TrainerType,
    uint TrainerSpell,
    byte TrainerClass,
    byte TrainerRace,
    uint TrainerTemplateId,
    uint VendorTemplateId,
    uint GossipMenuId,
    uint EquipmentTemplateId,
    byte Civilian,
    string AIName)
{
    public IReadOnlyList<uint> ModelIds => [ModelId1, ModelId2, ModelId3, ModelId4];

    public uint GetPreferredModelId()
    {
        return ModelIds.FirstOrDefault(modelId => modelId != 0);
    }

    public byte GetEffectiveMinLevel()
    {
        return MinLevel == 0 ? (byte)1 : MinLevel;
    }

    public byte GetEffectiveMaxLevel()
    {
        byte effectiveMinLevel = GetEffectiveMinLevel();
        return MaxLevel == 0 || MaxLevel < effectiveMinLevel ? effectiveMinLevel : MaxLevel;
    }

    public byte GetEffectiveUnitClass()
    {
        return UnitClass == 0 ? (byte)1 : UnitClass;
    }

    public byte GetEffectiveInhabitType()
    {
        return InhabitType == 0 ? (byte)3 : InhabitType;
    }

    public float GetEffectiveWalkSpeed()
    {
        return SpeedWalk <= 0 || !float.IsFinite(SpeedWalk) ? 1.0f : SpeedWalk;
    }

    public float GetEffectiveRunSpeed()
    {
        return SpeedRun <= 0 || !float.IsFinite(SpeedRun) ? 1.14286f : SpeedRun;
    }

    public uint GetEffectiveHealth(uint spawnHealth)
    {
        if (spawnHealth != 0)
        {
            return spawnHealth;
        }

        uint templateHealth = MaxLevelHealth != 0 ? MaxLevelHealth : MinLevelHealth;
        if (templateHealth != 0)
        {
            return templateHealth;
        }

        return Math.Max((uint)GetEffectiveMaxLevel(), 1u) * 42u;
    }

    public uint GetEffectiveMana(uint spawnMana)
    {
        if (spawnMana != 0)
        {
            return spawnMana;
        }

        return MaxLevelMana != 0 ? MaxLevelMana : MinLevelMana;
    }
}
