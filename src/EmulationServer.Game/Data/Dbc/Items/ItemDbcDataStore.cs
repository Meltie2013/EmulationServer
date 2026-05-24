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
     * Initializes a new ItemDbcDataStore instance with the dependencies required by the DBC loading and strongly typed client data records workflow.
     * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
     */
    private ItemDbcDataStore()
    {
        Classes = new Dictionary<int, ItemClassDbcRecord>();
        SubClasses = new Dictionary<(int ItemClassId, int SubClassId), ItemSubClassDbcRecord>();
        DisplayInfo = new Dictionary<int, ItemDisplayInfoDbcRecord>();
        Sets = new Dictionary<int, ItemSetDbcRecord>();
        BagFamilies = new Dictionary<int, ItemBagFamilyDbcRecord>();
    }

    /**
     * Initializes a new ItemDbcDataStore instance with the dependencies required by the DBC loading and strongly typed client data records workflow.
     * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
     * Inputs used by this operation: classes, subClasses, displayInfo, sets, bagFamilies.
     */
    private ItemDbcDataStore(
        IReadOnlyDictionary<int, ItemClassDbcRecord> classes,
        IReadOnlyDictionary<(int ItemClassId, int SubClassId), ItemSubClassDbcRecord> subClasses,
        IReadOnlyDictionary<int, ItemDisplayInfoDbcRecord> displayInfo,
        IReadOnlyDictionary<int, ItemSetDbcRecord> sets,
        IReadOnlyDictionary<int, ItemBagFamilyDbcRecord> bagFamilies)
    {
        Classes = classes;
        SubClasses = subClasses;
        DisplayInfo = displayInfo;
        Sets = sets;
        BagFamilies = bagFamilies;
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

        ItemDbcDataStore data = new(classes, subClasses, displayInfo, sets, bagFamilies);

        Logger.Write(
            LogType.SUCCESS,
            $"{ownerName} typed item DBC data loaded: classes={data.Classes.Count}, subclasses={data.SubClasses.Count}, displayInfo={data.DisplayInfo.Count}, itemSets={data.Sets.Count}, bagFamilies={data.BagFamilies.Count}.",
            nameof(ItemDbcDataStore));

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
     * Parses read item class record input into the strongly typed server representation.
     * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
     * Inputs used by this operation: record.
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
     * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
     * Inputs used by this operation: record.
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
     * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
     * Inputs used by this operation: record.
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
     * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
     * Inputs used by this operation: record.
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
     * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
     * Inputs used by this operation: record.
     */
    private static ItemBagFamilyDbcRecord ReadItemBagFamilyRecord(DbcRecord record)
    {
        return new ItemBagFamilyDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadString(record, 1));
    }
}
