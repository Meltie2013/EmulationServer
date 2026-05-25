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

/**
  * File overview: src/EmulationServer.Game/WorldData/ItemTemplateRecord.cs
  * Documents the ItemTemplateRecord source file in the world database template loading and cache construction area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.WorldData;

/**
  * Carries immutable item_template data for item instance creation, equipment display, and vanilla item query responses.
  * The query packet needs the complete template row so the client can build the in-game item tooltip with stats, damage, armor, resistances, spells, durability, set, and bag data.
  */
public sealed record ItemTemplateRecord
{
    public ItemTemplateRecord(
        uint entry,
        byte itemClass,
        byte subClass,
        string name,
        uint displayId,
        byte quality,
        uint flags,
        byte buyCount,
        uint buyPrice,
        uint sellPrice,
        byte inventoryType,
        int allowableClass,
        int allowableRace,
        byte itemLevel,
        byte requiredLevel,
        ushort requiredSkill,
        ushort requiredSkillRank,
        uint requiredSpell,
        uint requiredHonorRank,
        uint requiredCityRank,
        ushort requiredReputationFaction,
        ushort requiredReputationRank,
        ushort maxCount,
        ushort stackable,
        byte containerSlots,
        IReadOnlyList<ItemTemplateStatRecord> stats,
        IReadOnlyList<ItemTemplateDamageRecord> damages,
        ushort armor,
        byte holyResistance,
        byte fireResistance,
        byte natureResistance,
        byte frostResistance,
        byte shadowResistance,
        byte arcaneResistance,
        ushort delay,
        byte ammoType,
        float rangedModRange,
        IReadOnlyList<ItemTemplateSpellRecord> spells,
        byte bonding,
        string description,
        uint pageText,
        byte languageId,
        byte pageMaterial,
        uint startQuest,
        uint lockId,
        sbyte material,
        byte sheath,
        uint randomProperty,
        uint block,
        uint itemSet,
        uint maxDurability,
        uint area,
        int map,
        int bagFamily,
        uint disenchantId,
        byte foodType,
        uint minimumMoneyLoot,
        uint maximumMoneyLoot,
        uint duration,
        byte extraFlags)
    {
        Entry = entry;
        Class = itemClass;
        SubClass = subClass;
        Name = name;
        DisplayId = displayId;
        Quality = quality;
        Flags = flags;
        BuyCount = buyCount;
        BuyPrice = buyPrice;
        SellPrice = sellPrice;
        InventoryType = inventoryType;
        AllowableClass = allowableClass;
        AllowableRace = allowableRace;
        ItemLevel = itemLevel;
        RequiredLevel = requiredLevel;
        RequiredSkill = requiredSkill;
        RequiredSkillRank = requiredSkillRank;
        RequiredSpell = requiredSpell;
        RequiredHonorRank = requiredHonorRank;
        RequiredCityRank = requiredCityRank;
        RequiredReputationFaction = requiredReputationFaction;
        RequiredReputationRank = requiredReputationRank;
        MaxCount = maxCount;
        Stackable = stackable;
        ContainerSlots = containerSlots;
        Stats = Normalize(stats, 10, new ItemTemplateStatRecord(0, 0));
        Damages = Normalize(damages, 5, new ItemTemplateDamageRecord(0, 0, 0));
        Armor = armor;
        HolyResistance = holyResistance;
        FireResistance = fireResistance;
        NatureResistance = natureResistance;
        FrostResistance = frostResistance;
        ShadowResistance = shadowResistance;
        ArcaneResistance = arcaneResistance;
        Delay = delay;
        AmmoType = ammoType;
        RangedModRange = rangedModRange;
        Spells = Normalize(spells, 5, new ItemTemplateSpellRecord(0, 0, 0, 0, -1, 0, -1));
        Bonding = bonding;
        Description = description;
        PageText = pageText;
        LanguageId = languageId;
        PageMaterial = pageMaterial;
        StartQuest = startQuest;
        LockId = lockId;
        Material = material;
        Sheath = sheath;
        RandomProperty = randomProperty;
        Block = block;
        ItemSet = itemSet;
        MaxDurability = maxDurability;
        Area = area;
        Map = map;
        BagFamily = bagFamily;
        DisenchantId = disenchantId;
        FoodType = foodType;
        MinimumMoneyLoot = minimumMoneyLoot;
        MaximumMoneyLoot = maximumMoneyLoot;
        Duration = duration;
        ExtraFlags = extraFlags;
    }

    public uint Entry { get; }

    public byte Class { get; }

    public byte SubClass { get; }

    public string Name { get; }

    public uint DisplayId { get; }

    public byte Quality { get; }

    public uint Flags { get; }

    public byte BuyCount { get; }

    public uint BuyPrice { get; }

    public uint SellPrice { get; }

    public byte InventoryType { get; }

    public int AllowableClass { get; }

    public int AllowableRace { get; }

    public byte ItemLevel { get; }

    public byte RequiredLevel { get; }

    public ushort RequiredSkill { get; }

    public ushort RequiredSkillRank { get; }

    public uint RequiredSpell { get; }

    public uint RequiredHonorRank { get; }

    public uint RequiredCityRank { get; }

    public ushort RequiredReputationFaction { get; }

    public ushort RequiredReputationRank { get; }

    public ushort MaxCount { get; }

    public ushort Stackable { get; }

    public byte ContainerSlots { get; }

    public IReadOnlyList<ItemTemplateStatRecord> Stats { get; }

    public IReadOnlyList<ItemTemplateDamageRecord> Damages { get; }

    public ushort Armor { get; }

    public byte HolyResistance { get; }

    public byte FireResistance { get; }

    public byte NatureResistance { get; }

    public byte FrostResistance { get; }

    public byte ShadowResistance { get; }

    public byte ArcaneResistance { get; }

    public ushort Delay { get; }

    public byte AmmoType { get; }

    public float RangedModRange { get; }

    public IReadOnlyList<ItemTemplateSpellRecord> Spells { get; }

    public byte Bonding { get; }

    public string Description { get; }

    public uint PageText { get; }

    public byte LanguageId { get; }

    public byte PageMaterial { get; }

    public uint StartQuest { get; }

    public uint LockId { get; }

    public sbyte Material { get; }

    public byte Sheath { get; }

    public uint RandomProperty { get; }

    public uint Block { get; }

    public uint ItemSet { get; }

    public uint MaxDurability { get; }

    public uint Area { get; }

    public int Map { get; }

    public int BagFamily { get; }

    public uint DisenchantId { get; }

    public byte FoodType { get; }

    public uint MinimumMoneyLoot { get; }

    public uint MaximumMoneyLoot { get; }

    public uint Duration { get; }

    public byte ExtraFlags { get; }

    private static IReadOnlyList<T> Normalize<T>(IReadOnlyList<T> values, int fixedCount, T emptyValue)
    {
        ArgumentNullException.ThrowIfNull(values);

        T[] normalized = new T[fixedCount];
        for (int index = 0; index < fixedCount; index++)
        {
            normalized[index] = index < values.Count ? values[index] : emptyValue;
        }

        return normalized;
    }
}
