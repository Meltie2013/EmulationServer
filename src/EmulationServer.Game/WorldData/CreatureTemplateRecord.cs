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
// File: src/EmulationServer.Game/WorldData/CreatureTemplateRecord.cs
// Purpose: Contains creature template record code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.WorldData;

// Type: CreatureTemplateRecord
// Purpose: Represents creature template record data passed through the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
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
    // Property: Gets or sets the model ids value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: model ids value exposed by the owning type.
    public IReadOnlyList<uint> ModelIds => [ModelId1, ModelId2, ModelId3, ModelId4];

    // Method: GetPreferredModelId
    // Purpose: Retrieves get preferred model ID data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureTemplateRecord so callers do not duplicate validation, protocol, or persistence rules.
    public uint GetPreferredModelId()
    {
        return ModelIds.FirstOrDefault(modelId => modelId != 0);
    }

    // Method: GetEffectiveMinLevel
    // Purpose: Retrieves get effective min level data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the byte value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureTemplateRecord so callers do not duplicate validation, protocol, or persistence rules.
    public byte GetEffectiveMinLevel()
    {
        return MinLevel == 0 ? (byte)1 : MinLevel;
    }

    // Method: GetEffectiveMaxLevel
    // Purpose: Retrieves get effective max level data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the byte value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureTemplateRecord so callers do not duplicate validation, protocol, or persistence rules.
    public byte GetEffectiveMaxLevel()
    {
        byte effectiveMinLevel = GetEffectiveMinLevel();
        return MaxLevel == 0 || MaxLevel < effectiveMinLevel ? effectiveMinLevel : MaxLevel;
    }

    // Method: GetEffectiveUnitClass
    // Purpose: Retrieves get effective unit class data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the byte value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureTemplateRecord so callers do not duplicate validation, protocol, or persistence rules.
    public byte GetEffectiveUnitClass()
    {
        return UnitClass == 0 ? (byte)1 : UnitClass;
    }

    // Method: GetEffectiveInhabitType
    // Purpose: Retrieves get effective inhabit type data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the byte value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureTemplateRecord so callers do not duplicate validation, protocol, or persistence rules.
    public byte GetEffectiveInhabitType()
    {
        return InhabitType == 0 ? (byte)3 : InhabitType;
    }

    // Method: GetEffectiveWalkSpeed
    // Purpose: Retrieves get effective walk speed data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the float value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureTemplateRecord so callers do not duplicate validation, protocol, or persistence rules.
    public float GetEffectiveWalkSpeed()
    {
        return SpeedWalk <= 0 || !float.IsFinite(SpeedWalk) ? 1.0f : SpeedWalk;
    }

    // Method: GetEffectiveRunSpeed
    // Purpose: Retrieves get effective run speed data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the float value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureTemplateRecord so callers do not duplicate validation, protocol, or persistence rules.
    public float GetEffectiveRunSpeed()
    {
        return SpeedRun <= 0 || !float.IsFinite(SpeedRun) ? 1.14286f : SpeedRun;
    }

    // Method: GetEffectiveHealth
    // Purpose: Retrieves get effective health data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - spawnHealth: Spawn health value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureTemplateRecord so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: GetEffectiveMana
    // Purpose: Retrieves get effective mana data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - spawnMana: Spawn mana value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureTemplateRecord so callers do not duplicate validation, protocol, or persistence rules.
    public uint GetEffectiveMana(uint spawnMana)
    {
        if (spawnMana != 0)
        {
            return spawnMana;
        }

        return MaxLevelMana != 0 ? MaxLevelMana : MinLevelMana;
    }
}
