//
// Copyright (C) 2026 Emulation Server Project
//

using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Game.Data.Dbc.Creatures;

/**
  * Owns typed creature DBC data used for creature/NPC display, family, type, sound, spell, and model validation.
  */
public sealed class CreatureDbcDataStore
{
    private CreatureDbcDataStore()
    {
        DisplayInfo = new Dictionary<int, CreatureDisplayInfoDbcRecord>();
        DisplayInfoExtra = new Dictionary<int, CreatureDisplayInfoExtraDbcRecord>();
        Families = new Dictionary<int, CreatureFamilyDbcRecord>();
        ModelData = new Dictionary<int, CreatureModelDataDbcRecord>();
        SoundData = new Dictionary<int, CreatureSoundDataDbcRecord>();
        SpellData = new Dictionary<int, CreatureSpellDataDbcRecord>();
        Types = new Dictionary<int, CreatureTypeDbcRecord>();
    }

    private CreatureDbcDataStore(
        IReadOnlyDictionary<int, CreatureDisplayInfoDbcRecord> displayInfo,
        IReadOnlyDictionary<int, CreatureDisplayInfoExtraDbcRecord> displayInfoExtra,
        IReadOnlyDictionary<int, CreatureFamilyDbcRecord> families,
        IReadOnlyDictionary<int, CreatureModelDataDbcRecord> modelData,
        IReadOnlyDictionary<int, CreatureSoundDataDbcRecord> soundData,
        IReadOnlyDictionary<int, CreatureSpellDataDbcRecord> spellData,
        IReadOnlyDictionary<int, CreatureTypeDbcRecord> types)
    {
        DisplayInfo = displayInfo;
        DisplayInfoExtra = displayInfoExtra;
        Families = families;
        ModelData = modelData;
        SoundData = soundData;
        SpellData = spellData;
        Types = types;
    }

    public static CreatureDbcDataStore Empty { get; } = new();

    public IReadOnlyDictionary<int, CreatureDisplayInfoDbcRecord> DisplayInfo { get; }

    public IReadOnlyDictionary<int, CreatureDisplayInfoExtraDbcRecord> DisplayInfoExtra { get; }

    public IReadOnlyDictionary<int, CreatureFamilyDbcRecord> Families { get; }

    public IReadOnlyDictionary<int, CreatureModelDataDbcRecord> ModelData { get; }

    public IReadOnlyDictionary<int, CreatureSoundDataDbcRecord> SoundData { get; }

    public IReadOnlyDictionary<int, CreatureSpellDataDbcRecord> SpellData { get; }

    public IReadOnlyDictionary<int, CreatureTypeDbcRecord> Types { get; }

    public static CreatureDbcDataStore FromDbcStores(IReadOnlyDictionary<string, DbcDataStore> dbcStores, string ownerName)
    {
        ArgumentNullException.ThrowIfNull(dbcStores);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerName);

        Dictionary<int, CreatureDisplayInfoDbcRecord> displayInfo = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            CreatureDbcFileNames.CreatureDisplayInfo,
            ownerName,
            12,
            ReadCreatureDisplayInfoRecord,
            record => record.Id);

        Dictionary<int, CreatureDisplayInfoExtraDbcRecord> displayInfoExtra = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            CreatureDbcFileNames.CreatureDisplayInfoExtra,
            ownerName,
            19,
            ReadCreatureDisplayInfoExtraRecord,
            record => record.Id);

        Dictionary<int, CreatureFamilyDbcRecord> families = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            CreatureDbcFileNames.CreatureFamily,
            ownerName,
            18,
            ReadCreatureFamilyRecord,
            record => record.Id);

        Dictionary<int, CreatureModelDataDbcRecord> modelData = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            CreatureDbcFileNames.CreatureModelData,
            ownerName,
            16,
            ReadCreatureModelDataRecord,
            record => record.Id);

        Dictionary<int, CreatureSoundDataDbcRecord> soundData = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            CreatureDbcFileNames.CreatureSoundData,
            ownerName,
            30,
            ReadCreatureSoundDataRecord,
            record => record.Id);

        Dictionary<int, CreatureSpellDataDbcRecord> spellData = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            CreatureDbcFileNames.CreatureSpellData,
            ownerName,
            9,
            ReadCreatureSpellDataRecord,
            record => record.Id);

        Dictionary<int, CreatureTypeDbcRecord> types = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            CreatureDbcFileNames.CreatureType,
            ownerName,
            11,
            ReadCreatureTypeRecord,
            record => record.Id);

        CreatureDbcDataStore data = new(displayInfo, displayInfoExtra, families, modelData, soundData, spellData, types);
        Logger.Write(
            LogType.SUCCESS,
            $"{ownerName}: creature DBC loaded (displays={data.DisplayInfo.Count}, displayExtras={data.DisplayInfoExtra.Count}, families={data.Families.Count}, models={data.ModelData.Count}, sounds={data.SoundData.Count}, spells={data.SpellData.Count}, types={data.Types.Count}).",
            "CreatureDbcDataStore");

        return data;
    }

    private static CreatureDisplayInfoDbcRecord ReadCreatureDisplayInfoRecord(DbcRecord record)
    {
        return new CreatureDisplayInfoDbcRecord(
            record.GetInt32(0),
            record.GetInt32(1),
            record.GetInt32(2),
            record.GetInt32(3),
            record.GetSingle(4),
            record.GetInt32(5),
            DbcRecordReader.ReadString(record, 6),
            DbcRecordReader.ReadString(record, 7),
            DbcRecordReader.ReadString(record, 8),
            record.GetInt32(9),
            record.GetInt32(10),
            record.GetInt32(11));
    }

    private static CreatureDisplayInfoExtraDbcRecord ReadCreatureDisplayInfoExtraRecord(DbcRecord record)
    {
        List<int> itemDisplays = [];
        for (int index = 8; index < 19; index++)
        {
            itemDisplays.Add(record.GetInt32(index));
        }

        return new CreatureDisplayInfoExtraDbcRecord(
            record.GetInt32(0),
            record.GetInt32(1),
            record.GetInt32(2),
            record.GetInt32(3),
            record.GetInt32(4),
            record.GetInt32(5),
            record.GetInt32(6),
            record.GetInt32(7),
            itemDisplays);
    }

    private static CreatureFamilyDbcRecord ReadCreatureFamilyRecord(DbcRecord record)
    {
        return new CreatureFamilyDbcRecord(
            record.GetInt32(0),
            record.GetSingle(1),
            record.GetInt32(2),
            record.GetSingle(3),
            record.GetInt32(4),
            record.GetInt32(5),
            record.GetInt32(6),
            record.GetInt32(7),
            record.GetInt32(8),
            DbcRecordReader.ReadString(record, 9),
            DbcRecordReader.ReadString(record, 17));
    }

    private static CreatureModelDataDbcRecord ReadCreatureModelDataRecord(DbcRecord record)
    {
        return new CreatureModelDataDbcRecord(
            record.GetInt32(0),
            record.GetInt32(1),
            DbcRecordReader.ReadString(record, 2),
            record.GetInt32(3),
            record.GetSingle(4),
            record.GetInt32(5),
            record.GetInt32(6),
            record.GetSingle(7),
            record.GetSingle(8),
            record.GetSingle(9),
            record.GetInt32(10),
            record.GetInt32(11),
            record.GetInt32(12),
            record.GetSingle(13),
            record.GetSingle(14),
            record.GetSingle(15));
    }

    private static CreatureSoundDataDbcRecord ReadCreatureSoundDataRecord(DbcRecord record)
    {
        List<int> soundIds = [];
        for (int index = 1; index < 24; index++)
        {
            soundIds.Add(record.GetInt32(index));
        }

        soundIds.Add(record.GetInt32(26));
        soundIds.Add(record.GetInt32(27));
        soundIds.Add(record.GetInt32(28));
        soundIds.Add(record.GetInt32(29));

        return new CreatureSoundDataDbcRecord(
            record.GetInt32(0),
            soundIds,
            record.GetInt32(18),
            record.GetInt32(24),
            record.GetInt32(25));
    }

    private static CreatureSpellDataDbcRecord ReadCreatureSpellDataRecord(DbcRecord record)
    {
        return new CreatureSpellDataDbcRecord(
            record.GetInt32(0),
            [record.GetInt32(1), record.GetInt32(2), record.GetInt32(3), record.GetInt32(4)],
            [record.GetInt32(5), record.GetInt32(6), record.GetInt32(7), record.GetInt32(8)]);
    }

    private static CreatureTypeDbcRecord ReadCreatureTypeRecord(DbcRecord record)
    {
        return new CreatureTypeDbcRecord(
            record.GetInt32(0),
            DbcRecordReader.ReadString(record, 1),
            record.GetInt32(10));
    }
}
