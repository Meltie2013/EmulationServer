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
// File: src/EmulationServer.Game/Data/Dbc/Items/ItemDbcDataStore.cs
// Purpose: Contains item DBC data store code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Game.Data.Dbc;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Game.Data.Dbc.Items;

// Type: ItemDbcDataStore
// Purpose: Provides item DBC data store behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class ItemDbcDataStore
{

    // Constructor: ItemDbcDataStore
    // Purpose: Initializes a new ItemDbcDataStore instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to ItemDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Constructor: ItemDbcDataStore
    // Purpose: Initializes a new ItemDbcDataStore instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - classes: Classes value supplied by the caller for this operation.
    // - subClasses: Sub classes value supplied by the caller for this operation.
    // - displayInfo: Display info value supplied by the caller for this operation.
    // - sets: Sets value supplied by the caller for this operation.
    // - bagFamilies: Bag families value supplied by the caller for this operation.
    // - randomProperties: Random properties value supplied by the caller for this operation.
    // - spellItemEnchantments: Spell item enchantments value supplied by the caller for this operation.
    // - durabilityCosts: Durability costs value supplied by the caller for this operation.
    // - durabilityQualities: Durability qualities value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to ItemDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    public static ItemDbcDataStore Empty { get; } = new();

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, ItemClassDbcRecord> Classes { get; }

    // Constructor: IReadOnlyDictionary
    // Purpose: Executes the I read only dictionary operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - ItemClassId: Item class ID identifier used to select the exact record, object, or runtime owner.
    // - SubClassId: Sub class ID identifier used to select the exact record, object, or runtime owner.
    // Returns: none.
    // Notes: This keeps the operation scoped to ItemDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyDictionary<(int ItemClassId, int SubClassId), ItemSubClassDbcRecord> SubClasses { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, ItemDisplayInfoDbcRecord> DisplayInfo { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, ItemSetDbcRecord> Sets { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, ItemBagFamilyDbcRecord> BagFamilies { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, ItemRandomPropertyDbcRecord> RandomProperties { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, SpellItemEnchantmentDbcRecord> SpellItemEnchantments { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, DurabilityCostDbcRecord> DurabilityCosts { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, DurabilityQualityDbcRecord> DurabilityQualities { get; }

    // Method: FromDbcStores
    // Purpose: Executes the from DBC stores operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - dbcStores: Dbc stores value supplied by the caller for this operation.
    // - ownerName: Owner name value supplied by the caller for this operation.
    // Returns: Returns the item DBC data store value produced by this operation.
    // Notes: This keeps the operation scoped to ItemDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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
            string.Join(Environment.NewLine,
                $"{ownerName}: item DBC loaded:",
                $"  ItemClass.dbc: {data.Classes.Count}",
                $"  ItemSubClass.dbc: {data.SubClasses.Count}",
                $"  ItemDisplayInfo.dbc: {data.DisplayInfo.Count}",
                $"  ItemSet.dbc: {data.Sets.Count}",
                $"  ItemBagFamily.dbc: {data.BagFamilies.Count}",
                $"  ItemRandomProperties.dbc: {data.RandomProperties.Count}",
                $"  SpellItemEnchantment.dbc: {data.SpellItemEnchantments.Count}",
                $"  DurabilityCosts.dbc: {data.DurabilityCosts.Count}",
                $"  DurabilityQuality.dbc: {data.DurabilityQualities.Count}"),
            "ItemDbcDataStore");

        return data;
    }

    // Method: TryGetClass
    // Purpose: Attempts to retrieve or parse try get class data without treating normal misses as failures.
    // Parameters:
    // - classId: Class ID identifier used to select the exact record, object, or runtime owner.
    // - itemClass: Item class value supplied by the caller for this operation.
    // Returns: Returns true when try get class succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to ItemDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetClass(int classId, out ItemClassDbcRecord itemClass)
    {
        return Classes.TryGetValue(classId, out itemClass!);
    }

    // Method: TryGetSubClass
    // Purpose: Attempts to retrieve or parse try get sub class data without treating normal misses as failures.
    // Parameters:
    // - classId: Class ID identifier used to select the exact record, object, or runtime owner.
    // - subClassId: Sub class ID identifier used to select the exact record, object, or runtime owner.
    // - subClass: Sub class value supplied by the caller for this operation.
    // Returns: Returns true when try get sub class succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to ItemDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetSubClass(int classId, int subClassId, out ItemSubClassDbcRecord subClass)
    {
        return SubClasses.TryGetValue((classId, subClassId), out subClass!);
    }

    // Method: TryGetDisplayInfo
    // Purpose: Attempts to retrieve or parse try get display info data without treating normal misses as failures.
    // Parameters:
    // - displayId: Display ID identifier used to select the exact record, object, or runtime owner.
    // - displayInfo: Display info value supplied by the caller for this operation.
    // Returns: Returns true when try get display info succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to ItemDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetDisplayInfo(int displayId, out ItemDisplayInfoDbcRecord displayInfo)
    {
        return DisplayInfo.TryGetValue(displayId, out displayInfo!);
    }

    // Method: TryGetRandomProperty
    // Purpose: Attempts to retrieve or parse try get random property data without treating normal misses as failures.
    // Parameters:
    // - randomPropertyId: Random property ID identifier used to select the exact record, object, or runtime owner.
    // - randomProperty: Random property value supplied by the caller for this operation.
    // Returns: Returns true when try get random property succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to ItemDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetRandomProperty(int randomPropertyId, out ItemRandomPropertyDbcRecord randomProperty)
    {
        return RandomProperties.TryGetValue(randomPropertyId, out randomProperty!);
    }

    // Method: TryGetSpellItemEnchantment
    // Purpose: Attempts to retrieve or parse try get spell item enchantment data without treating normal misses as failures.
    // Parameters:
    // - enchantmentId: Enchantment ID identifier used to select the exact record, object, or runtime owner.
    // - enchantment: Enchantment value supplied by the caller for this operation.
    // Returns: Returns true when try get spell item enchantment succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to ItemDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetSpellItemEnchantment(int enchantmentId, out SpellItemEnchantmentDbcRecord enchantment)
    {
        return SpellItemEnchantments.TryGetValue(enchantmentId, out enchantment!);
    }

    // Method: ReadItemClassRecord
    // Purpose: Retrieves read item class record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the item class DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to ItemDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static ItemClassDbcRecord ReadItemClassRecord(DbcRecord record)
    {
        return new ItemClassDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadInt32(record, 1),
            DbcRecordReader.ReadInt32(record, 2),
            DbcRecordReader.ReadString(record, 3));
    }

    // Method: ReadItemSubClassRecord
    // Purpose: Retrieves read item sub class record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the item sub class DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to ItemDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadItemDisplayInfoRecord
    // Purpose: Retrieves read item display info record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the item display info DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to ItemDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadItemSetRecord
    // Purpose: Retrieves read item set record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the item set DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to ItemDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadItemBagFamilyRecord
    // Purpose: Retrieves read item bag family record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the item bag family DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to ItemDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static ItemBagFamilyDbcRecord ReadItemBagFamilyRecord(DbcRecord record)
    {
        return new ItemBagFamilyDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadString(record, 1));
    }

    // Method: ReadItemRandomPropertyRecord
    // Purpose: Retrieves read item random property record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the item random property DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to ItemDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadSpellItemEnchantmentRecord
    // Purpose: Retrieves read spell item enchantment record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the spell item enchantment DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to ItemDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadDurabilityCostRecord
    // Purpose: Retrieves read durability cost record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the durability cost DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to ItemDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static DurabilityCostDbcRecord ReadDurabilityCostRecord(DbcRecord record)
    {
        return new DurabilityCostDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            Enumerable.Range(1, 21).Select(fieldIndex => DbcRecordReader.ReadInt32(record, fieldIndex)).ToArray(),
            Enumerable.Range(22, 8).Select(fieldIndex => DbcRecordReader.ReadInt32(record, fieldIndex)).ToArray());
    }

    // Method: ReadDurabilityQualityRecord
    // Purpose: Retrieves read durability quality record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the durability quality DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to ItemDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static DurabilityQualityDbcRecord ReadDurabilityQualityRecord(DbcRecord record)
    {
        return new DurabilityQualityDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadSingle(record, 1));
    }
}
