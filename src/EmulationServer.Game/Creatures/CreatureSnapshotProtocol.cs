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
// File: src/EmulationServer.Game/Creatures/CreatureSnapshotProtocol.cs
// Purpose: Contains creature snapshot protocol code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Globalization;
using System.Text;
using EmulationServer.Game.WorldData;
using EmulationServer.Network.Networking.Protocol;

namespace EmulationServer.Game.Creatures;

// Type: CreatureSnapshotProtocol
// Purpose: Provides creature snapshot protocol behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class CreatureSnapshotProtocol
{
    // Constant: Defines the encoded empty string constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed encoded empty string value used anywhere this rule or protocol value is needed.
    private const string EncodedEmptyString = "AA==";
    // Constant: Defines the template numeric field count constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed template numeric field count value used anywhere this rule or protocol value is needed.
    private const int TemplateNumericFieldCount = 70;

    private static readonly CreatureTemplateRecord EmptyTemplate = new(
        Entry: 0,
        Name: string.Empty,
        SubName: string.Empty,
        MinLevel: 0,
        MaxLevel: 0,
        ModelId1: 0,
        ModelId2: 0,
        ModelId3: 0,
        ModelId4: 0,
        FactionAlliance: 0,
        FactionHorde: 0,
        Scale: 1,
        Family: 0,
        CreatureType: 0,
        InhabitType: 0,
        RegenerateStats: 0,
        RacialLeader: 0,
        NpcFlags: 0,
        UnitFlags: 0,
        DynamicFlags: 0,
        ExtraFlags: 0,
        CreatureTypeFlags: 0,
        SpeedWalk: 1,
        SpeedRun: 1,
        UnitClass: 0,
        Rank: 0,
        HealthMultiplier: 1,
        PowerMultiplier: 1,
        DamageMultiplier: 1,
        DamageVariance: 1,
        ArmorMultiplier: 1,
        ExperienceMultiplier: 1,
        MinLevelHealth: 0,
        MaxLevelHealth: 0,
        MinLevelMana: 0,
        MaxLevelMana: 0,
        MinMeleeDamage: 0,
        MaxMeleeDamage: 0,
        MinRangedDamage: 0,
        MaxRangedDamage: 0,
        Armor: 0,
        MeleeAttackPower: 0,
        RangedAttackPower: 0,
        MeleeBaseAttackTime: 2000,
        RangedBaseAttackTime: 2000,
        DamageSchool: 0,
        MinLootGold: 0,
        MaxLootGold: 0,
        LootId: 0,
        PickpocketLootId: 0,
        SkinningLootId: 0,
        KillCredit1: 0,
        KillCredit2: 0,
        MechanicImmuneMask: 0,
        SchoolImmuneMask: 0,
        ResistanceHoly: 0,
        ResistanceFire: 0,
        ResistanceNature: 0,
        ResistanceFrost: 0,
        ResistanceShadow: 0,
        ResistanceArcane: 0,
        SpellListId: 0,
        PetSpellDataId: 0,
        MovementType: 0,
        TrainerType: 0,
        TrainerSpell: 0,
        TrainerClass: 0,
        TrainerRace: 0,
        TrainerTemplateId: 0,
        VendorTemplateId: 0,
        GossipMenuId: 0,
        EquipmentTemplateId: 0,
        Civilian: 0,
        AIName: string.Empty);

    private static readonly CreatureSpawnRecord EmptySpawn = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    // Method: CreateBeginPacket
    // Purpose: Applies create begin packet changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - snapshotId: Snapshot ID identifier used to select the exact record, object, or runtime owner.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - templateCount: Template count value supplied by the caller for this operation.
    // - spawnCount: Spawn count value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    public static string CreateBeginPacket(string snapshotId, int mapId, int templateCount, int spawnCount)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{InternalProtocol.CreatureSnapshotBegin} {snapshotId} {mapId} {templateCount} {spawnCount}");
    }

    // Method: CreateTemplatePacket
    // Purpose: Applies create template packet changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - snapshotId: Snapshot ID identifier used to select the exact record, object, or runtime owner.
    // - template: Template value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    public static string CreateTemplatePacket(string snapshotId, CreatureTemplateRecord template)
    {
        ArgumentNullException.ThrowIfNull(template);

        string numericFields = string.Join(',', CreateTemplateNumericFields(template));
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{InternalProtocol.CreatureTemplateSnapshot} {snapshotId} {template.Entry} {Encode(numericFields)} {Encode(template.Name)} {Encode(template.SubName)} {Encode(template.AIName)}");
    }

    // Method: CreateSpawnPacket
    // Purpose: Applies create spawn packet changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - snapshotId: Snapshot ID identifier used to select the exact record, object, or runtime owner.
    // - spawn: Spawn value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    public static string CreateSpawnPacket(string snapshotId, CreatureSpawnRecord spawn)
    {
        ArgumentNullException.ThrowIfNull(spawn);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{InternalProtocol.CreatureSpawnSnapshot} {snapshotId} {spawn.Guid} {spawn.Entry} {spawn.Map} {spawn.ZoneId} {spawn.AreaId} {spawn.ModelId} {spawn.EquipmentId} {spawn.PositionX:0.######} {spawn.PositionY:0.######} {spawn.PositionZ:0.######} {spawn.Orientation:0.######} {spawn.SpawnTimeSeconds} {spawn.SpawnDistance:0.######} {spawn.CurrentWaypoint} {spawn.CurrentHealth} {spawn.CurrentMana} {spawn.DeathState} {spawn.MovementType}");
    }

    // Method: CreateEndPacket
    // Purpose: Applies create end packet changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - snapshotId: Snapshot ID identifier used to select the exact record, object, or runtime owner.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    public static string CreateEndPacket(string snapshotId, int mapId)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{InternalProtocol.CreatureSnapshotEnd} {snapshotId} {mapId}");
    }

    // Method: TryParseBegin
    // Purpose: Attempts to retrieve or parse try parse begin data without treating normal misses as failures.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - snapshotId: Snapshot ID identifier used to select the exact record, object, or runtime owner.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - templateCount: Template count value supplied by the caller for this operation.
    // - spawnCount: Spawn count value supplied by the caller for this operation.
    // Returns: Returns true when try parse begin succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    public static bool TryParseBegin(string packet, out string snapshotId, out int mapId, out int templateCount, out int spawnCount)
    {
        snapshotId = string.Empty;
        mapId = 0;
        templateCount = 0;
        spawnCount = 0;

        string[] parts = Split(packet);
        if (parts.Length != 5 || !IsOpcode(parts[0], InternalProtocol.CreatureSnapshotBegin))
        {
            return false;
        }

        if (!TryParseNonNegativeInt(parts[2], out mapId) ||
            !TryParseNonNegativeInt(parts[3], out templateCount) ||
            !TryParseNonNegativeInt(parts[4], out spawnCount))
        {
            return false;
        }

        snapshotId = parts[1];
        return !string.IsNullOrWhiteSpace(snapshotId);
    }

    // Method: TryParseTemplate
    // Purpose: Attempts to retrieve or parse try parse template data without treating normal misses as failures.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - snapshotId: Snapshot ID identifier used to select the exact record, object, or runtime owner.
    // - template: Template value supplied by the caller for this operation.
    // Returns: Returns true when try parse template succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    public static bool TryParseTemplate(string packet, out string snapshotId, out CreatureTemplateRecord template)
    {
        snapshotId = string.Empty;
        template = EmptyTemplate;

        string[] parts = Split(packet);
        if (parts.Length != 7 || !IsOpcode(parts[0], InternalProtocol.CreatureTemplateSnapshot))
        {
            return false;
        }

        if (!uint.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint entry) ||
            !TryDecode(parts[3], out string numericFieldText) ||
            !TryDecode(parts[4], out string name) ||
            !TryDecode(parts[5], out string subName) ||
            !TryDecode(parts[6], out string aiName))
        {
            return false;
        }

        string[] fields = numericFieldText.Split(',', StringSplitOptions.TrimEntries);
        if (fields.Length != TemplateNumericFieldCount)
        {
            return false;
        }

        if (!TryBuildTemplate(entry, fields, name, subName, aiName, out template))
        {
            return false;
        }

        snapshotId = parts[1];
        return !string.IsNullOrWhiteSpace(snapshotId);
    }

    // Method: TryParseSpawn
    // Purpose: Attempts to retrieve or parse try parse spawn data without treating normal misses as failures.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - snapshotId: Snapshot ID identifier used to select the exact record, object, or runtime owner.
    // - spawn: Spawn value supplied by the caller for this operation.
    // Returns: Returns true when try parse spawn succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    public static bool TryParseSpawn(string packet, out string snapshotId, out CreatureSpawnRecord spawn)
    {
        snapshotId = string.Empty;
        spawn = EmptySpawn;

        string[] parts = Split(packet);
        if (parts.Length != 20 || !IsOpcode(parts[0], InternalProtocol.CreatureSpawnSnapshot))
        {
            return false;
        }

        if (!uint.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint guid) ||
            !uint.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint entry) ||
            !ushort.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort map) ||
            !uint.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint zoneId) ||
            !uint.TryParse(parts[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint areaId) ||
            !uint.TryParse(parts[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint modelId) ||
            !int.TryParse(parts[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out int equipmentId) ||
            !float.TryParse(parts[9], NumberStyles.Float, CultureInfo.InvariantCulture, out float positionX) ||
            !float.TryParse(parts[10], NumberStyles.Float, CultureInfo.InvariantCulture, out float positionY) ||
            !float.TryParse(parts[11], NumberStyles.Float, CultureInfo.InvariantCulture, out float positionZ) ||
            !float.TryParse(parts[12], NumberStyles.Float, CultureInfo.InvariantCulture, out float orientation) ||
            !uint.TryParse(parts[13], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint spawnTimeSeconds) ||
            !float.TryParse(parts[14], NumberStyles.Float, CultureInfo.InvariantCulture, out float spawnDistance) ||
            !uint.TryParse(parts[15], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint currentWaypoint) ||
            !uint.TryParse(parts[16], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint currentHealth) ||
            !uint.TryParse(parts[17], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint currentMana) ||
            !byte.TryParse(parts[18], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte deathState) ||
            !byte.TryParse(parts[19], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte movementType))
        {
            return false;
        }

        snapshotId = parts[1];
        spawn = new CreatureSpawnRecord(
            guid,
            entry,
            map,
            zoneId,
            areaId,
            modelId,
            equipmentId,
            positionX,
            positionY,
            positionZ,
            orientation,
            spawnTimeSeconds,
            spawnDistance,
            currentWaypoint,
            currentHealth,
            currentMana,
            deathState,
            movementType);

        return !string.IsNullOrWhiteSpace(snapshotId);
    }

    // Method: TryParseEnd
    // Purpose: Attempts to retrieve or parse try parse end data without treating normal misses as failures.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - snapshotId: Snapshot ID identifier used to select the exact record, object, or runtime owner.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns true when try parse end succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    public static bool TryParseEnd(string packet, out string snapshotId, out int mapId)
    {
        snapshotId = string.Empty;
        mapId = 0;

        string[] parts = Split(packet);
        if (parts.Length != 3 || !IsOpcode(parts[0], InternalProtocol.CreatureSnapshotEnd))
        {
            return false;
        }

        if (!TryParseNonNegativeInt(parts[2], out mapId))
        {
            return false;
        }

        snapshotId = parts[1];
        return !string.IsNullOrWhiteSpace(snapshotId);
    }

    // Method: IsSnapshotPacket
    // Purpose: Validates or evaluates is snapshot packet rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // Returns: Returns true when is snapshot packet succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    public static bool IsSnapshotPacket(string packet)
    {
        string[] parts = Split(packet);
        return parts.Length > 0 &&
            (IsOpcode(parts[0], InternalProtocol.CreatureSnapshotBegin) ||
             IsOpcode(parts[0], InternalProtocol.CreatureTemplateSnapshot) ||
             IsOpcode(parts[0], InternalProtocol.CreatureSpawnSnapshot) ||
             IsOpcode(parts[0], InternalProtocol.CreatureSnapshotEnd));
    }

    // Method: CreateTemplateNumericFields
    // Purpose: Applies create template numeric fields changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - template: Template value supplied by the caller for this operation.
    // Returns: Returns the string[] value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    private static string[] CreateTemplateNumericFields(CreatureTemplateRecord template)
    {
        return
        [
            Format(template.MinLevel), Format(template.MaxLevel), Format(template.ModelId1), Format(template.ModelId2), Format(template.ModelId3), Format(template.ModelId4),
            Format(template.FactionAlliance), Format(template.FactionHorde), Format(template.Scale), Format(template.Family), Format(template.CreatureType), Format(template.InhabitType),
            Format(template.RegenerateStats), Format(template.RacialLeader), Format(template.NpcFlags), Format(template.UnitFlags), Format(template.DynamicFlags), Format(template.ExtraFlags),
            Format(template.CreatureTypeFlags), Format(template.SpeedWalk), Format(template.SpeedRun), Format(template.UnitClass), Format(template.Rank), Format(template.HealthMultiplier),
            Format(template.PowerMultiplier), Format(template.DamageMultiplier), Format(template.DamageVariance), Format(template.ArmorMultiplier), Format(template.ExperienceMultiplier),
            Format(template.MinLevelHealth), Format(template.MaxLevelHealth), Format(template.MinLevelMana), Format(template.MaxLevelMana), Format(template.MinMeleeDamage),
            Format(template.MaxMeleeDamage), Format(template.MinRangedDamage), Format(template.MaxRangedDamage), Format(template.Armor), Format(template.MeleeAttackPower),
            Format(template.RangedAttackPower), Format(template.MeleeBaseAttackTime), Format(template.RangedBaseAttackTime), Format(template.DamageSchool), Format(template.MinLootGold),
            Format(template.MaxLootGold), Format(template.LootId), Format(template.PickpocketLootId), Format(template.SkinningLootId), Format(template.KillCredit1), Format(template.KillCredit2),
            Format(template.MechanicImmuneMask), Format(template.SchoolImmuneMask), Format(template.ResistanceHoly), Format(template.ResistanceFire), Format(template.ResistanceNature),
            Format(template.ResistanceFrost), Format(template.ResistanceShadow), Format(template.ResistanceArcane), Format(template.SpellListId), Format(template.PetSpellDataId),
            Format(template.MovementType), Format(template.TrainerType), Format(template.TrainerSpell), Format(template.TrainerClass), Format(template.TrainerRace), Format(template.TrainerTemplateId),
            Format(template.VendorTemplateId), Format(template.GossipMenuId), Format(template.EquipmentTemplateId), Format(template.Civilian),
        ];
    }

    // Method: TryBuildTemplate
    // Purpose: Executes the try build template operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - entry: Entry value supplied by the caller for this operation.
    // - fields: Fields value supplied by the caller for this operation.
    // - name: Name value supplied by the caller for this operation.
    // - subName: Sub name value supplied by the caller for this operation.
    // - aiName: Ai name value supplied by the caller for this operation.
    // - template: Template value supplied by the caller for this operation.
    // Returns: Returns true when try build template succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    private static bool TryBuildTemplate(uint entry, IReadOnlyList<string> fields, string name, string subName, string aiName, out CreatureTemplateRecord template)
    {
        template = EmptyTemplate;
        int index = 0;

        if (!TryReadByte(fields, ref index, out byte minLevel) ||
            !TryReadByte(fields, ref index, out byte maxLevel) ||
            !TryReadUInt32(fields, ref index, out uint modelId1) ||
            !TryReadUInt32(fields, ref index, out uint modelId2) ||
            !TryReadUInt32(fields, ref index, out uint modelId3) ||
            !TryReadUInt32(fields, ref index, out uint modelId4) ||
            !TryReadUInt16(fields, ref index, out ushort factionAlliance) ||
            !TryReadUInt16(fields, ref index, out ushort factionHorde) ||
            !TryReadSingle(fields, ref index, out float scale) ||
            !TryReadSByte(fields, ref index, out sbyte family) ||
            !TryReadByte(fields, ref index, out byte creatureType) ||
            !TryReadByte(fields, ref index, out byte inhabitType) ||
            !TryReadByte(fields, ref index, out byte regenerateStats) ||
            !TryReadByte(fields, ref index, out byte racialLeader) ||
            !TryReadUInt32(fields, ref index, out uint npcFlags) ||
            !TryReadUInt32(fields, ref index, out uint unitFlags) ||
            !TryReadUInt32(fields, ref index, out uint dynamicFlags) ||
            !TryReadUInt32(fields, ref index, out uint extraFlags) ||
            !TryReadUInt32(fields, ref index, out uint creatureTypeFlags) ||
            !TryReadSingle(fields, ref index, out float speedWalk) ||
            !TryReadSingle(fields, ref index, out float speedRun) ||
            !TryReadByte(fields, ref index, out byte unitClass) ||
            !TryReadByte(fields, ref index, out byte rank) ||
            !TryReadSingle(fields, ref index, out float healthMultiplier) ||
            !TryReadSingle(fields, ref index, out float powerMultiplier) ||
            !TryReadSingle(fields, ref index, out float damageMultiplier) ||
            !TryReadSingle(fields, ref index, out float damageVariance) ||
            !TryReadSingle(fields, ref index, out float armorMultiplier) ||
            !TryReadSingle(fields, ref index, out float experienceMultiplier) ||
            !TryReadUInt32(fields, ref index, out uint minLevelHealth) ||
            !TryReadUInt32(fields, ref index, out uint maxLevelHealth) ||
            !TryReadUInt32(fields, ref index, out uint minLevelMana) ||
            !TryReadUInt32(fields, ref index, out uint maxLevelMana) ||
            !TryReadSingle(fields, ref index, out float minMeleeDamage) ||
            !TryReadSingle(fields, ref index, out float maxMeleeDamage) ||
            !TryReadSingle(fields, ref index, out float minRangedDamage) ||
            !TryReadSingle(fields, ref index, out float maxRangedDamage) ||
            !TryReadUInt32(fields, ref index, out uint armor) ||
            !TryReadUInt32(fields, ref index, out uint meleeAttackPower) ||
            !TryReadUInt16(fields, ref index, out ushort rangedAttackPower) ||
            !TryReadUInt32(fields, ref index, out uint meleeBaseAttackTime) ||
            !TryReadUInt32(fields, ref index, out uint rangedBaseAttackTime) ||
            !TryReadSByte(fields, ref index, out sbyte damageSchool) ||
            !TryReadUInt32(fields, ref index, out uint minLootGold) ||
            !TryReadUInt32(fields, ref index, out uint maxLootGold) ||
            !TryReadUInt32(fields, ref index, out uint lootId) ||
            !TryReadUInt32(fields, ref index, out uint pickpocketLootId) ||
            !TryReadUInt32(fields, ref index, out uint skinningLootId) ||
            !TryReadUInt32(fields, ref index, out uint killCredit1) ||
            !TryReadUInt32(fields, ref index, out uint killCredit2) ||
            !TryReadUInt32(fields, ref index, out uint mechanicImmuneMask) ||
            !TryReadUInt32(fields, ref index, out uint schoolImmuneMask) ||
            !TryReadInt16(fields, ref index, out short resistanceHoly) ||
            !TryReadInt16(fields, ref index, out short resistanceFire) ||
            !TryReadInt16(fields, ref index, out short resistanceNature) ||
            !TryReadInt16(fields, ref index, out short resistanceFrost) ||
            !TryReadInt16(fields, ref index, out short resistanceShadow) ||
            !TryReadInt16(fields, ref index, out short resistanceArcane) ||
            !TryReadUInt32(fields, ref index, out uint spellListId) ||
            !TryReadUInt32(fields, ref index, out uint petSpellDataId) ||
            !TryReadByte(fields, ref index, out byte movementType) ||
            !TryReadSByte(fields, ref index, out sbyte trainerType) ||
            !TryReadUInt32(fields, ref index, out uint trainerSpell) ||
            !TryReadByte(fields, ref index, out byte trainerClass) ||
            !TryReadByte(fields, ref index, out byte trainerRace) ||
            !TryReadUInt32(fields, ref index, out uint trainerTemplateId) ||
            !TryReadUInt32(fields, ref index, out uint vendorTemplateId) ||
            !TryReadUInt32(fields, ref index, out uint gossipMenuId) ||
            !TryReadUInt32(fields, ref index, out uint equipmentTemplateId) ||
            !TryReadByte(fields, ref index, out byte civilian))
        {
            return false;
        }

        template = new CreatureTemplateRecord(
            entry,
            name,
            subName,
            minLevel,
            maxLevel,
            modelId1,
            modelId2,
            modelId3,
            modelId4,
            factionAlliance,
            factionHorde,
            scale,
            family,
            creatureType,
            inhabitType,
            regenerateStats,
            racialLeader,
            npcFlags,
            unitFlags,
            dynamicFlags,
            extraFlags,
            creatureTypeFlags,
            speedWalk,
            speedRun,
            unitClass,
            rank,
            healthMultiplier,
            powerMultiplier,
            damageMultiplier,
            damageVariance,
            armorMultiplier,
            experienceMultiplier,
            minLevelHealth,
            maxLevelHealth,
            minLevelMana,
            maxLevelMana,
            minMeleeDamage,
            maxMeleeDamage,
            minRangedDamage,
            maxRangedDamage,
            armor,
            meleeAttackPower,
            rangedAttackPower,
            meleeBaseAttackTime,
            rangedBaseAttackTime,
            damageSchool,
            minLootGold,
            maxLootGold,
            lootId,
            pickpocketLootId,
            skinningLootId,
            killCredit1,
            killCredit2,
            mechanicImmuneMask,
            schoolImmuneMask,
            resistanceHoly,
            resistanceFire,
            resistanceNature,
            resistanceFrost,
            resistanceShadow,
            resistanceArcane,
            spellListId,
            petSpellDataId,
            movementType,
            trainerType,
            trainerSpell,
            trainerClass,
            trainerRace,
            trainerTemplateId,
            vendorTemplateId,
            gossipMenuId,
            equipmentTemplateId,
            civilian,
            aiName);

        return true;
    }

    // Method: TryReadByte
    // Purpose: Attempts to retrieve or parse try read byte data without treating normal misses as failures.
    // Parameters:
    // - fields: Fields value supplied by the caller for this operation.
    // - index: Index value supplied by the caller for this operation.
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns true when try read byte succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    private static bool TryReadByte(IReadOnlyList<string> fields, ref int index, out byte value) => byte.TryParse(fields[index++], NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    // Method: TryReadSByte
    // Purpose: Attempts to retrieve or parse try read S byte data without treating normal misses as failures.
    // Parameters:
    // - fields: Fields value supplied by the caller for this operation.
    // - index: Index value supplied by the caller for this operation.
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns true when try read S byte succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    private static bool TryReadSByte(IReadOnlyList<string> fields, ref int index, out sbyte value) => sbyte.TryParse(fields[index++], NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    // Method: TryReadInt16
    // Purpose: Attempts to retrieve or parse try read int16 data without treating normal misses as failures.
    // Parameters:
    // - fields: Fields value supplied by the caller for this operation.
    // - index: Index value supplied by the caller for this operation.
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns true when try read int16 succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    private static bool TryReadInt16(IReadOnlyList<string> fields, ref int index, out short value) => short.TryParse(fields[index++], NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    // Method: TryReadUInt16
    // Purpose: Attempts to retrieve or parse try read U int16 data without treating normal misses as failures.
    // Parameters:
    // - fields: Fields value supplied by the caller for this operation.
    // - index: Index value supplied by the caller for this operation.
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns true when try read U int16 succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    private static bool TryReadUInt16(IReadOnlyList<string> fields, ref int index, out ushort value) => ushort.TryParse(fields[index++], NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    // Method: TryReadUInt32
    // Purpose: Attempts to retrieve or parse try read U int32 data without treating normal misses as failures.
    // Parameters:
    // - fields: Fields value supplied by the caller for this operation.
    // - index: Index value supplied by the caller for this operation.
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns true when try read U int32 succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    private static bool TryReadUInt32(IReadOnlyList<string> fields, ref int index, out uint value) => uint.TryParse(fields[index++], NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    // Method: TryReadSingle
    // Purpose: Attempts to retrieve or parse try read single data without treating normal misses as failures.
    // Parameters:
    // - fields: Fields value supplied by the caller for this operation.
    // - index: Index value supplied by the caller for this operation.
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns true when try read single succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    private static bool TryReadSingle(IReadOnlyList<string> fields, ref int index, out float value) => float.TryParse(fields[index++], NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    // Method: Format
    // Purpose: Executes the format operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    private static string Format(byte value) => value.ToString(CultureInfo.InvariantCulture);
    // Method: Format
    // Purpose: Executes the format operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    private static string Format(sbyte value) => value.ToString(CultureInfo.InvariantCulture);
    // Method: Format
    // Purpose: Executes the format operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    private static string Format(short value) => value.ToString(CultureInfo.InvariantCulture);
    // Method: Format
    // Purpose: Executes the format operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    private static string Format(ushort value) => value.ToString(CultureInfo.InvariantCulture);
    // Method: Format
    // Purpose: Executes the format operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    private static string Format(uint value) => value.ToString(CultureInfo.InvariantCulture);
    // Method: Format
    // Purpose: Executes the format operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    private static string Format(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);

    // Method: Encode
    // Purpose: Builds or writes encode output for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    private static string Encode(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return EncodedEmptyString;
        }

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    // Method: TryDecode
    // Purpose: Executes the try decode operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // - decoded: Decoded value supplied by the caller for this operation.
    // Returns: Returns true when try decode succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    private static bool TryDecode(string value, out string decoded)
    {
        decoded = string.Empty;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(value));
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    // Method: Split
    // Purpose: Executes the split operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // Returns: Returns the string[] value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    private static string[] Split(string packet)
    {
        return packet.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    // Method: IsOpcode
    // Purpose: Validates or evaluates is opcode rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - candidate: Candidate value supplied by the caller for this operation.
    // - opcode: Opcode value supplied by the caller for this operation.
    // Returns: Returns true when is opcode succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    private static bool IsOpcode(string candidate, string opcode)
    {
        return string.Equals(candidate, opcode, StringComparison.OrdinalIgnoreCase);
    }

    // Method: TryParseNonNegativeInt
    // Purpose: Attempts to retrieve or parse try parse non negative int data without treating normal misses as failures.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // - result: Result value supplied by the caller for this operation.
    // Returns: Returns true when try parse non negative int succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CreatureSnapshotProtocol so callers do not duplicate validation, protocol, or persistence rules.
    private static bool TryParseNonNegativeInt(string value, out int result)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) && result >= 0;
    }
}
