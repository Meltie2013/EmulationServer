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

using EmulationServer.Game.Data.Dbc;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

/**
  * File overview: src/EmulationServer.Game/Data/Dbc/Items/ItemDbcDataStore.cs
  * Documents the ItemDbcDataStore source file in the DBC loading and strongly typed client data records area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Data.Dbc.Items;

/**
  * Owns typed item DBC data and item classification/display indexes.
  */
public sealed class ItemDbcDataStore
{
    /**
      * Initializes an empty typed item store for disabled DBC loading paths.
      */
    private ItemDbcDataStore()
    {
        Classes = new Dictionary<int, ItemClassDbcRecord>();
        SubClasses = new Dictionary<(int ItemClassId, int SubClassId), ItemSubClassDbcRecord>();
        DisplayInfo = new Dictionary<int, ItemDisplayInfoDbcRecord>();
        Sets = new Dictionary<int, ItemSetDbcRecord>();
        BagFamilies = new Dictionary<int, ItemBagFamilyDbcRecord>();
        RandomProperties = new Dictionary<int, ItemRandomPropertyDbcRecord>();
        SpellItemEnchantments = new Dictionary<int, SpellItemEnchantmentDbcRecord>();
        DurabilityCosts = new Dictionary<int, DurabilityCostDbcRecord>();
        DurabilityQualities = new Dictionary<int, DurabilityQualityDbcRecord>();
    }

    /**
      * Initializes a populated item DBC store.
      */
    private ItemDbcDataStore(
        IReadOnlyDictionary<int, ItemClassDbcRecord> classes,
        IReadOnlyDictionary<(int ItemClassId, int SubClassId), ItemSubClassDbcRecord> subClasses,
        IReadOnlyDictionary<int, ItemDisplayInfoDbcRecord> displayInfo,
        IReadOnlyDictionary<int, ItemSetDbcRecord> sets,
        IReadOnlyDictionary<int, ItemBagFamilyDbcRecord> bagFamilies,
        IReadOnlyDictionary<int, ItemRandomPropertyDbcRecord> randomProperties,
        IReadOnlyDictionary<int, SpellItemEnchantmentDbcRecord> spellItemEnchantments,
        IReadOnlyDictionary<int, DurabilityCostDbcRecord> durabilityCosts,
        IReadOnlyDictionary<int, DurabilityQualityDbcRecord> durabilityQualities)
    {
        Classes = classes;
        SubClasses = subClasses;
        DisplayInfo = displayInfo;
        Sets = sets;
        BagFamilies = bagFamilies;
        RandomProperties = randomProperties;
        SpellItemEnchantments = spellItemEnchantments;
        DurabilityCosts = durabilityCosts;
        DurabilityQualities = durabilityQualities;
    }

    /**
      * Gets an empty typed item store for disabled DBC loading paths.
      */
    public static ItemDbcDataStore Empty { get; } = new();

    /**
      * Gets item classes indexed by class id.
      */
    public IReadOnlyDictionary<int, ItemClassDbcRecord> Classes { get; }

    /**
      * Gets item subclasses indexed by class/subclass id.
      */
    public IReadOnlyDictionary<(int ItemClassId, int SubClassId), ItemSubClassDbcRecord> SubClasses { get; }

    /**
      * Gets item display rows indexed by display id.
      */
    public IReadOnlyDictionary<int, ItemDisplayInfoDbcRecord> DisplayInfo { get; }

    /**
      * Gets item sets indexed by set id.
      */
    public IReadOnlyDictionary<int, ItemSetDbcRecord> Sets { get; }

    /**
      * Gets bag-family rows indexed by family id.
      */
    public IReadOnlyDictionary<int, ItemBagFamilyDbcRecord> BagFamilies { get; }

    /**
      * Gets random-property rows indexed by random property id.
      */
    public IReadOnlyDictionary<int, ItemRandomPropertyDbcRecord> RandomProperties { get; }

    /**
      * Gets spell-item enchantment rows indexed by enchantment id.
      */
    public IReadOnlyDictionary<int, SpellItemEnchantmentDbcRecord> SpellItemEnchantments { get; }

    /**
      * Gets durability-cost rows indexed by item level.
      */
    public IReadOnlyDictionary<int, DurabilityCostDbcRecord> DurabilityCosts { get; }

    /**
      * Gets durability-quality rows indexed by quality id.
      */
    public IReadOnlyDictionary<int, DurabilityQualityDbcRecord> DurabilityQualities { get; }

    /**
      * Converts loaded raw DBC stores into typed item DBC indexes.
      */
    public static ItemDbcDataStore FromDbcStores(IReadOnlyDictionary<string, DbcDataStore> dbcStores, string ownerName)
    {
        ArgumentNullException.ThrowIfNull(dbcStores);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerName);

        Dictionary<int, ItemClassDbcRecord> classes = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            ItemDbcFileNames.ItemClass,
            ownerName,
            12,
            ReadItemClassRecord,
            record => record.Id);

        Dictionary<(int ItemClassId, int SubClassId), ItemSubClassDbcRecord> subClasses = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            ItemDbcFileNames.ItemSubClass,
            ownerName,
            28,
            ReadItemSubClassRecord,
            record => (record.ItemClassId, record.SubClassId));

        Dictionary<int, ItemDisplayInfoDbcRecord> displayInfo = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            ItemDbcFileNames.ItemDisplayInfo,
            ownerName,
            23,
            ReadItemDisplayInfoRecord,
            record => record.Id);

        Dictionary<int, ItemSetDbcRecord> sets = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            ItemDbcFileNames.ItemSet,
            ownerName,
            45,
            ReadItemSetRecord,
            record => record.Id);

        Dictionary<int, ItemBagFamilyDbcRecord> bagFamilies = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            ItemDbcFileNames.ItemBagFamily,
            ownerName,
            10,
            ReadItemBagFamilyRecord,
            record => record.Id);

        Dictionary<int, ItemRandomPropertyDbcRecord> randomProperties = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            ItemDbcFileNames.ItemRandomProperties,
            ownerName,
            16,
            ReadItemRandomPropertyRecord,
            record => record.Id);

        Dictionary<int, SpellItemEnchantmentDbcRecord> spellItemEnchantments = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            ItemDbcFileNames.SpellItemEnchantment,
            ownerName,
            24,
            ReadSpellItemEnchantmentRecord,
            record => record.Id);

        Dictionary<int, DurabilityCostDbcRecord> durabilityCosts = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            ItemDbcFileNames.DurabilityCosts,
            ownerName,
            30,
            ReadDurabilityCostRecord,
            record => record.ItemLevel);

        Dictionary<int, DurabilityQualityDbcRecord> durabilityQualities = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            ItemDbcFileNames.DurabilityQuality,
            ownerName,
            2,
            ReadDurabilityQualityRecord,
            record => record.Id);

        ItemDbcDataStore data = new(
            classes,
            subClasses,
            displayInfo,
            sets,
            bagFamilies,
            randomProperties,
            spellItemEnchantments,
            durabilityCosts,
            durabilityQualities);

        Logger.Write(
            LogType.SUCCESS,
            $"{ownerName}: item DBC loaded (classes={data.Classes.Count}, subclasses={data.SubClasses.Count}, displays={data.DisplayInfo.Count}, sets={data.Sets.Count}, bagFamilies={data.BagFamilies.Count}, randomProperties={data.RandomProperties.Count}, enchantments={data.SpellItemEnchantments.Count}, durabilityCosts={data.DurabilityCosts.Count}, durabilityQualities={data.DurabilityQualities.Count}).",
            "ItemDbcDataStore");

        return data;
    }

    /**
      * Attempts to get one item class by id.
      */
    public bool TryGetClass(int classId, out ItemClassDbcRecord itemClass)
    {
        return Classes.TryGetValue(classId, out itemClass!);
    }

    /**
      * Attempts to get one item subclass by class/subclass id.
      */
    public bool TryGetSubClass(int classId, int subClassId, out ItemSubClassDbcRecord subClass)
    {
        return SubClasses.TryGetValue((classId, subClassId), out subClass!);
    }

    /**
      * Attempts to get one item display row by display id.
      */
    public bool TryGetDisplayInfo(int displayId, out ItemDisplayInfoDbcRecord displayInfo)
    {
        return DisplayInfo.TryGetValue(displayId, out displayInfo!);
    }

    /**
      * Attempts to get one random-property row by id.
      */
    public bool TryGetRandomProperty(int randomPropertyId, out ItemRandomPropertyDbcRecord randomProperty)
    {
        return RandomProperties.TryGetValue(randomPropertyId, out randomProperty!);
    }

    /**
      * Attempts to get one spell item enchantment row by id.
      */
    public bool TryGetSpellItemEnchantment(int enchantmentId, out SpellItemEnchantmentDbcRecord enchantment)
    {
        return SpellItemEnchantments.TryGetValue(enchantmentId, out enchantment!);
    }

    /**
      * Parses read item class record input into the strongly typed server representation.
      */
    private static ItemClassDbcRecord ReadItemClassRecord(DbcRecord record)
    {
        return new ItemClassDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadInt32(record, 1),
            DbcRecordReader.ReadInt32(record, 2),
            DbcRecordReader.ReadString(record, 3));
    }

    /**
      * Parses read item sub class record input into the strongly typed server representation.
      */
    private static ItemSubClassDbcRecord ReadItemSubClassRecord(DbcRecord record)
    {
        return new ItemSubClassDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadInt32(record, 1),
            DbcRecordReader.ReadInt32(record, 2),
            DbcRecordReader.ReadInt32(record, 3),
            DbcRecordReader.ReadInt32(record, 4),
            DbcRecordReader.ReadInt32(record, 5),
            DbcRecordReader.ReadString(record, 10),
            DbcRecordReader.ReadString(record, 19));
    }

    /**
      * Parses read item display info record input into the strongly typed server representation.
      */
    private static ItemDisplayInfoDbcRecord ReadItemDisplayInfoRecord(DbcRecord record)
    {
        string[] textures =
        [
            DbcRecordReader.ReadString(record, 14),
            DbcRecordReader.ReadString(record, 15),
            DbcRecordReader.ReadString(record, 16),
            DbcRecordReader.ReadString(record, 17),
            DbcRecordReader.ReadString(record, 18),
            DbcRecordReader.ReadString(record, 19),
            DbcRecordReader.ReadString(record, 20),
            DbcRecordReader.ReadString(record, 21),
        ];

        return new ItemDisplayInfoDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadString(record, 1),
            DbcRecordReader.ReadString(record, 2),
            DbcRecordReader.ReadString(record, 3),
            DbcRecordReader.ReadString(record, 4),
            DbcRecordReader.ReadString(record, 5),
            DbcRecordReader.ReadString(record, 6),
            DbcRecordReader.ReadInt32(record, 7),
            DbcRecordReader.ReadInt32(record, 8),
            DbcRecordReader.ReadInt32(record, 9),
            DbcRecordReader.ReadInt32(record, 10),
            DbcRecordReader.ReadInt32(record, 11),
            DbcRecordReader.ReadInt32(record, 12),
            DbcRecordReader.ReadInt32(record, 13),
            textures,
            DbcRecordReader.ReadInt32(record, 22));
    }

    /**
      * Parses read item set record input into the strongly typed server representation.
      */
    private static ItemSetDbcRecord ReadItemSetRecord(DbcRecord record)
    {
        int[] itemIds = Enumerable.Range(10, 17)
            .Select(fieldIndex => DbcRecordReader.ReadInt32(record, fieldIndex))
            .Where(value => value > 0)
            .ToArray();

        int[] setSpellIds = Enumerable.Range(27, 8)
            .Select(fieldIndex => DbcRecordReader.ReadInt32(record, fieldIndex))
            .ToArray();

        int[] setThresholds = Enumerable.Range(35, 8)
            .Select(fieldIndex => DbcRecordReader.ReadInt32(record, fieldIndex))
            .ToArray();

        return new ItemSetDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadString(record, 1),
            itemIds,
            setSpellIds,
            setThresholds,
            DbcRecordReader.ReadInt32(record, 43),
            DbcRecordReader.ReadInt32(record, 44));
    }

    /**
      * Parses read item bag family record input into the strongly typed server representation.
      */
    private static ItemBagFamilyDbcRecord ReadItemBagFamilyRecord(DbcRecord record)
    {
        return new ItemBagFamilyDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadString(record, 1));
    }

    /**
      * Parses read item random property record input into the strongly typed server representation.
      */
    private static ItemRandomPropertyDbcRecord ReadItemRandomPropertyRecord(DbcRecord record)
    {
        int[] enchantmentIds = Enumerable.Range(2, 5)
            .Select(fieldIndex => DbcRecordReader.ReadInt32(record, fieldIndex))
            .ToArray();

        return new ItemRandomPropertyDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadString(record, 1),
            enchantmentIds,
            DbcRecordReader.ReadString(record, 7));
    }

    /**
      * Parses read spell item enchantment record input into the strongly typed server representation.
      */
    private static SpellItemEnchantmentDbcRecord ReadSpellItemEnchantmentRecord(DbcRecord record)
    {
        return new SpellItemEnchantmentDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            Enumerable.Range(1, 3).Select(fieldIndex => DbcRecordReader.ReadInt32(record, fieldIndex)).ToArray(),
            Enumerable.Range(4, 3).Select(fieldIndex => DbcRecordReader.ReadInt32(record, fieldIndex)).ToArray(),
            Enumerable.Range(7, 3).Select(fieldIndex => DbcRecordReader.ReadInt32(record, fieldIndex)).ToArray(),
            Enumerable.Range(10, 3).Select(fieldIndex => DbcRecordReader.ReadInt32(record, fieldIndex)).ToArray(),
            DbcRecordReader.ReadString(record, 13),
            DbcRecordReader.ReadInt32(record, 22),
            DbcRecordReader.ReadInt32(record, 23));
    }

    /**
      * Parses read durability cost record input into the strongly typed server representation.
      */
    private static DurabilityCostDbcRecord ReadDurabilityCostRecord(DbcRecord record)
    {
        return new DurabilityCostDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            Enumerable.Range(1, 21).Select(fieldIndex => DbcRecordReader.ReadInt32(record, fieldIndex)).ToArray(),
            Enumerable.Range(22, 8).Select(fieldIndex => DbcRecordReader.ReadInt32(record, fieldIndex)).ToArray());
    }

    /**
      * Parses read durability quality record input into the strongly typed server representation.
      */
    private static DurabilityQualityDbcRecord ReadDurabilityQualityRecord(DbcRecord record)
    {
        return new DurabilityQualityDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadSingle(record, 1));
    }
}
