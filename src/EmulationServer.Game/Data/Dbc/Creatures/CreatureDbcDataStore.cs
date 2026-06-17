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
// File: src/EmulationServer.Game/Data/Dbc/Creatures/CreatureDbcDataStore.cs
// Purpose: Contains creature DBC data store code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Game.Data.Dbc.Creatures;

// Type: CreatureDbcDataStore
// Purpose: Provides creature DBC data store behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class CreatureDbcDataStore
{
    // Constructor: CreatureDbcDataStore
    // Purpose: Initializes a new CreatureDbcDataStore instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to CreatureDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Constructor: CreatureDbcDataStore
    // Purpose: Initializes a new CreatureDbcDataStore instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - displayInfo: Display info value supplied by the caller for this operation.
    // - displayInfoExtra: Display info extra value supplied by the caller for this operation.
    // - families: Families value supplied by the caller for this operation.
    // - modelData: Model data value supplied by the caller for this operation.
    // - soundData: Sound data value supplied by the caller for this operation.
    // - spellData: Spell data value supplied by the caller for this operation.
    // - types: Types value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to CreatureDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, CreatureDisplayInfoDbcRecord> DisplayInfo { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, CreatureDisplayInfoExtraDbcRecord> DisplayInfoExtra { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, CreatureFamilyDbcRecord> Families { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, CreatureModelDataDbcRecord> ModelData { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, CreatureSoundDataDbcRecord> SoundData { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, CreatureSpellDataDbcRecord> SpellData { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, CreatureTypeDbcRecord> Types { get; }

    // Method: FromDbcStores
    // Purpose: Executes the from DBC stores operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - dbcStores: Dbc stores value supplied by the caller for this operation.
    // - ownerName: Owner name value supplied by the caller for this operation.
    // Returns: Returns the creature DBC data store value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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
            string.Join(Environment.NewLine,
                $"{ownerName}: creature DBC loaded:",
                $"  CreatureDisplayInfo.dbc: {data.DisplayInfo.Count}",
                $"  CreatureDisplayInfoExtra.dbc: {data.DisplayInfoExtra.Count}",
                $"  CreatureFamily.dbc: {data.Families.Count}",
                $"  CreatureModelData.dbc: {data.ModelData.Count}",
                $"  CreatureSoundData.dbc: {data.SoundData.Count}",
                $"  CreatureSpellData.dbc: {data.SpellData.Count}",
                $"  CreatureType.dbc: {data.Types.Count}"),
            "CreatureDbcDataStore");

        return data;
    }

    // Method: ReadCreatureDisplayInfoRecord
    // Purpose: Retrieves read creature display info record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the creature display info DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadCreatureDisplayInfoExtraRecord
    // Purpose: Retrieves read creature display info extra record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the creature display info extra DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadCreatureFamilyRecord
    // Purpose: Retrieves read creature family record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the creature family DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadCreatureModelDataRecord
    // Purpose: Retrieves read creature model data record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the creature model data DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadCreatureSoundDataRecord
    // Purpose: Retrieves read creature sound data record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the creature sound data DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadCreatureSpellDataRecord
    // Purpose: Retrieves read creature spell data record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the creature spell data DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static CreatureSpellDataDbcRecord ReadCreatureSpellDataRecord(DbcRecord record)
    {
        return new CreatureSpellDataDbcRecord(
            record.GetInt32(0),
            [record.GetInt32(1), record.GetInt32(2), record.GetInt32(3), record.GetInt32(4)],
            [record.GetInt32(5), record.GetInt32(6), record.GetInt32(7), record.GetInt32(8)]);
    }

    // Method: ReadCreatureTypeRecord
    // Purpose: Retrieves read creature type record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the creature type DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to CreatureDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static CreatureTypeDbcRecord ReadCreatureTypeRecord(DbcRecord record)
    {
        return new CreatureTypeDbcRecord(
            record.GetInt32(0),
            DbcRecordReader.ReadString(record, 1),
            record.GetInt32(10));
    }
}
