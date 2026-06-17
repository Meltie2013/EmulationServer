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
// File: src/EmulationServer.Game/WorldData/ItemTemplateRecord.cs
// Purpose: Contains item template record code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.WorldData;

// Type: ItemTemplateRecord
// Purpose: Represents item template record data passed through the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record ItemTemplateRecord
{
    // Constructor: ItemTemplateRecord
    // Purpose: Initializes a new ItemTemplateRecord instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to ItemTemplateRecord so callers do not duplicate validation, protocol, or persistence rules.
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

    // Property: Gets or sets the entry value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: entry value exposed by the owning type.
    public uint Entry { get; }

    // Property: Gets or sets the class value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: class value exposed by the owning type.
    public byte Class { get; }

    // Property: Gets or sets the sub class value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: sub class value exposed by the owning type.
    public byte SubClass { get; }

    // Property: Gets or sets the name value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: name value exposed by the owning type.
    public string Name { get; }

    // Property: Gets or sets the display ID value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: display ID value exposed by the owning type.
    public uint DisplayId { get; }

    // Property: Gets or sets the quality value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: quality value exposed by the owning type.
    public byte Quality { get; }

    // Property: Gets or sets the flags value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: flags value exposed by the owning type.
    public uint Flags { get; }

    // Property: Gets or sets the buy count value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: buy count value exposed by the owning type.
    public byte BuyCount { get; }

    // Property: Gets or sets the buy price value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: buy price value exposed by the owning type.
    public uint BuyPrice { get; }

    // Property: Gets or sets the sell price value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: sell price value exposed by the owning type.
    public uint SellPrice { get; }

    // Property: Gets or sets the inventory type value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: inventory type value exposed by the owning type.
    public byte InventoryType { get; }

    // Property: Gets or sets the allowable class value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: allowable class value exposed by the owning type.
    public int AllowableClass { get; }

    // Property: Gets or sets the allowable race value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: allowable race value exposed by the owning type.
    public int AllowableRace { get; }

    // Property: Gets or sets the item level value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: item level value exposed by the owning type.
    public byte ItemLevel { get; }

    // Property: Gets or sets the required level value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: required level value exposed by the owning type.
    public byte RequiredLevel { get; }

    // Property: Gets or sets the required skill value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: required skill value exposed by the owning type.
    public ushort RequiredSkill { get; }

    // Property: Gets or sets the required skill rank value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: required skill rank value exposed by the owning type.
    public ushort RequiredSkillRank { get; }

    // Property: Gets or sets the required spell value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: required spell value exposed by the owning type.
    public uint RequiredSpell { get; }

    // Property: Gets or sets the required honor rank value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: required honor rank value exposed by the owning type.
    public uint RequiredHonorRank { get; }

    // Property: Gets or sets the required city rank value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: required city rank value exposed by the owning type.
    public uint RequiredCityRank { get; }

    // Property: Gets or sets the required reputation faction value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: required reputation faction value exposed by the owning type.
    public ushort RequiredReputationFaction { get; }

    // Property: Gets or sets the required reputation rank value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: required reputation rank value exposed by the owning type.
    public ushort RequiredReputationRank { get; }

    // Property: Gets or sets the max count value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: max count value exposed by the owning type.
    public ushort MaxCount { get; }

    // Property: Gets or sets the stackable value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: stackable value exposed by the owning type.
    public ushort Stackable { get; }

    // Property: Gets or sets the container slots value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: container slots value exposed by the owning type.
    public byte ContainerSlots { get; }

    // Property: Gets or sets the stats value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: stats value exposed by the owning type.
    public IReadOnlyList<ItemTemplateStatRecord> Stats { get; }

    // Property: Gets or sets the damages value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: damages value exposed by the owning type.
    public IReadOnlyList<ItemTemplateDamageRecord> Damages { get; }

    // Property: Gets or sets the armor value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: armor value exposed by the owning type.
    public ushort Armor { get; }

    // Property: Gets or sets the holy resistance value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: holy resistance value exposed by the owning type.
    public byte HolyResistance { get; }

    // Property: Gets or sets the fire resistance value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fire resistance value exposed by the owning type.
    public byte FireResistance { get; }

    // Property: Gets or sets the nature resistance value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: nature resistance value exposed by the owning type.
    public byte NatureResistance { get; }

    // Property: Gets or sets the frost resistance value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: frost resistance value exposed by the owning type.
    public byte FrostResistance { get; }

    // Property: Gets or sets the shadow resistance value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: shadow resistance value exposed by the owning type.
    public byte ShadowResistance { get; }

    // Property: Gets or sets the arcane resistance value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: arcane resistance value exposed by the owning type.
    public byte ArcaneResistance { get; }

    // Property: Gets or sets the delay value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: delay value exposed by the owning type.
    public ushort Delay { get; }

    // Property: Gets or sets the ammo type value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: ammo type value exposed by the owning type.
    public byte AmmoType { get; }

    // Property: Gets or sets the ranged mod range value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: ranged mod range value exposed by the owning type.
    public float RangedModRange { get; }

    // Property: Gets or sets the spells value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: spells value exposed by the owning type.
    public IReadOnlyList<ItemTemplateSpellRecord> Spells { get; }

    // Property: Gets or sets the bonding value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: bonding value exposed by the owning type.
    public byte Bonding { get; }

    // Property: Gets or sets the description value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: description value exposed by the owning type.
    public string Description { get; }

    // Property: Gets or sets the page text value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: page text value exposed by the owning type.
    public uint PageText { get; }

    // Property: Gets or sets the language ID value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: language ID value exposed by the owning type.
    public byte LanguageId { get; }

    // Property: Gets or sets the page material value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: page material value exposed by the owning type.
    public byte PageMaterial { get; }

    // Property: Gets or sets the start quest value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: start quest value exposed by the owning type.
    public uint StartQuest { get; }

    // Property: Gets or sets the lock ID value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: lock ID value exposed by the owning type.
    public uint LockId { get; }

    // Property: Gets or sets the material value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: material value exposed by the owning type.
    public sbyte Material { get; }

    // Property: Gets or sets the sheath value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: sheath value exposed by the owning type.
    public byte Sheath { get; }

    // Property: Gets or sets the random property value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: random property value exposed by the owning type.
    public uint RandomProperty { get; }

    // Property: Gets or sets the block value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: block value exposed by the owning type.
    public uint Block { get; }

    // Property: Gets or sets the item set value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: item set value exposed by the owning type.
    public uint ItemSet { get; }

    // Property: Gets or sets the max durability value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: max durability value exposed by the owning type.
    public uint MaxDurability { get; }

    // Property: Gets or sets the area value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: area value exposed by the owning type.
    public uint Area { get; }

    // Property: Gets or sets the map value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: map value exposed by the owning type.
    public int Map { get; }

    // Property: Gets or sets the bag family value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: bag family value exposed by the owning type.
    public int BagFamily { get; }

    // Property: Gets or sets the disenchant ID value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: disenchant ID value exposed by the owning type.
    public uint DisenchantId { get; }

    // Property: Gets or sets the food type value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: food type value exposed by the owning type.
    public byte FoodType { get; }

    // Property: Gets or sets the minimum money loot value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: minimum money loot value exposed by the owning type.
    public uint MinimumMoneyLoot { get; }

    // Property: Gets or sets the maximum money loot value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: maximum money loot value exposed by the owning type.
    public uint MaximumMoneyLoot { get; }

    // Property: Gets or sets the duration value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: duration value exposed by the owning type.
    public uint Duration { get; }

    // Property: Gets or sets the extra flags value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: extra flags value exposed by the owning type.
    public byte ExtraFlags { get; }

    // Method: T
    // Purpose: Executes the T operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - values: Values value supplied by the caller for this operation.
    // - fixedCount: Fixed count value supplied by the caller for this operation.
    // - emptyValue: Empty value value supplied by the caller for this operation.
    // Returns: Returns the I read only list normalize< value produced by this operation.
    // Notes: This keeps the operation scoped to ItemTemplateRecord so callers do not duplicate validation, protocol, or persistence rules.
    private static T[] Normalize<T>(IReadOnlyList<T> values, int fixedCount, T emptyValue)
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
