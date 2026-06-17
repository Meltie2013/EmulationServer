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
// File: src/EmulationServer.Game/Data/Dbc/Characters/CharacterDbcDataStore.cs
// Purpose: Contains character DBC data store code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Game.Data.Dbc;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Game.Data.Dbc.Characters;

// Type: CharacterDbcDataStore
// Purpose: Provides character DBC data store behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class CharacterDbcDataStore
{

    // Constant: Defines the char start outfit packed header size constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed char start outfit packed header size value used anywhere this rule or protocol value is needed.
    private const int CharStartOutfitPackedHeaderSize = 8;

    // Constant: Defines the char start outfit item count constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed char start outfit item count value used anywhere this rule or protocol value is needed.
    private const int CharStartOutfitItemCount = 12;

    private const int CharStartOutfitRequiredBytes = CharStartOutfitPackedHeaderSize + CharStartOutfitItemCount * sizeof(int) * 3;

    // Constructor: HashSet
    // Purpose: Validates or evaluates hash set rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - RaceId: Race ID identifier used to select the exact record, object, or runtime owner.
    // - ClassId: Class ID identifier used to select the exact record, object, or runtime owner.
    // Returns: none.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private readonly HashSet<(int RaceId, int ClassId)> _allowedRaceClasses;
    // Constructor: Dictionary
    // Purpose: Executes the dictionary operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - RaceId: Race ID identifier used to select the exact record, object, or runtime owner.
    // - ClassId: Class ID identifier used to select the exact record, object, or runtime owner.
    // - GenderId: Gender ID identifier used to select the exact record, object, or runtime owner.
    // - OutfitId: Outfit ID identifier used to select the exact record, object, or runtime owner.
    // Returns: none.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private readonly Dictionary<(int RaceId, int ClassId, int GenderId, int OutfitId), CharStartOutfitDbcRecord> _startOutfitsByCreateKey;
    // Constructor: Dictionary
    // Purpose: Executes the dictionary operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - RaceId: Race ID identifier used to select the exact record, object, or runtime owner.
    // - SexId: Sex ID identifier used to select the exact record, object, or runtime owner.
    // - SectionType: Section type value supplied by the caller for this operation.
    // - VariationIndex: Variation index value supplied by the caller for this operation.
    // - ColorIndex: Color index value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private readonly Dictionary<(int RaceId, int SexId, int SectionType, int VariationIndex, int ColorIndex), CharSectionDbcRecord> _sectionsByCustomizationKey;
    // Constructor: Dictionary
    // Purpose: Executes the dictionary operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - RaceId: Race ID identifier used to select the exact record, object, or runtime owner.
    // - SexId: Sex ID identifier used to select the exact record, object, or runtime owner.
    // - VariationId: Variation ID identifier used to select the exact record, object, or runtime owner.
    // Returns: none.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private readonly Dictionary<(int RaceId, int SexId, int VariationId), CharacterFacialHairStyleDbcRecord> _facialHairByCustomizationKey;
    // Constructor: Dictionary
    // Purpose: Executes the dictionary operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - RaceId: Race ID identifier used to select the exact record, object, or runtime owner.
    // - SexId: Sex ID identifier used to select the exact record, object, or runtime owner.
    // - VariationId: Variation ID identifier used to select the exact record, object, or runtime owner.
    // Returns: none.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private readonly Dictionary<(int RaceId, int SexId, int VariationId), CharHairGeosetDbcRecord> _hairGeosetsByCustomizationKey;

    // Constructor: CharacterDbcDataStore
    // Purpose: Initializes a new CharacterDbcDataStore instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private CharacterDbcDataStore()
    {
        Races = new Dictionary<int, ChrRaceDbcRecord>();
        Classes = new Dictionary<int, ChrClassDbcRecord>();
        BaseInfo = [];
        StartOutfits = new Dictionary<int, CharStartOutfitDbcRecord>();
        Sections = new Dictionary<int, CharSectionDbcRecord>();
        FacialHairStyles = [];
        HairGeosets = new Dictionary<int, CharHairGeosetDbcRecord>();
        _allowedRaceClasses = [];
        _startOutfitsByCreateKey = [];
        _sectionsByCustomizationKey = [];
        _facialHairByCustomizationKey = [];
        _hairGeosetsByCustomizationKey = [];
    }

    // Constructor: CharacterDbcDataStore
    // Purpose: Initializes a new CharacterDbcDataStore instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - races: Races value supplied by the caller for this operation.
    // - classes: Classes value supplied by the caller for this operation.
    // - baseInfo: Base info value supplied by the caller for this operation.
    // - startOutfits: Start outfits value supplied by the caller for this operation.
    // - sections: Sections value supplied by the caller for this operation.
    // - facialHairStyles: Facial hair styles value supplied by the caller for this operation.
    // - hairGeosets: Hair geosets value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private CharacterDbcDataStore(
        IReadOnlyDictionary<int, ChrRaceDbcRecord> races,
        IReadOnlyDictionary<int, ChrClassDbcRecord> classes,
        IReadOnlyList<CharBaseInfoDbcRecord> baseInfo,
        IReadOnlyDictionary<int, CharStartOutfitDbcRecord> startOutfits,
        IReadOnlyDictionary<int, CharSectionDbcRecord> sections,
        IReadOnlyList<CharacterFacialHairStyleDbcRecord> facialHairStyles,
        IReadOnlyDictionary<int, CharHairGeosetDbcRecord> hairGeosets)
    {
        Races = races;
        Classes = classes;
        BaseInfo = baseInfo;
        StartOutfits = startOutfits;
        Sections = sections;
        FacialHairStyles = facialHairStyles;
        HairGeosets = hairGeosets;
        _allowedRaceClasses = BuildAllowedRaceClassSet(baseInfo);
        _startOutfitsByCreateKey = BuildStartOutfitIndex(startOutfits.Values);
        _sectionsByCustomizationKey = BuildSectionIndex(sections.Values);
        _facialHairByCustomizationKey = BuildFacialHairIndex(facialHairStyles);
        _hairGeosetsByCustomizationKey = BuildHairGeosetIndex(hairGeosets.Values);
    }

    public static CharacterDbcDataStore Empty { get; } = new();

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, ChrRaceDbcRecord> Races { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, ChrClassDbcRecord> Classes { get; }

    // Property: Gets or sets the base info value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: base info value exposed by the owning type.
    public IReadOnlyList<CharBaseInfoDbcRecord> BaseInfo { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, CharStartOutfitDbcRecord> StartOutfits { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, CharSectionDbcRecord> Sections { get; }

    // Property: Gets or sets the facial hair styles value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: facial hair styles value exposed by the owning type.
    public IReadOnlyList<CharacterFacialHairStyleDbcRecord> FacialHairStyles { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, CharHairGeosetDbcRecord> HairGeosets { get; }

    // Method: FromDbcStores
    // Purpose: Executes the from DBC stores operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - dbcStores: Dbc stores value supplied by the caller for this operation.
    // - ownerName: Owner name value supplied by the caller for this operation.
    // Returns: Returns the character DBC data store value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public static CharacterDbcDataStore FromDbcStores(IReadOnlyDictionary<string, DbcDataStore> dbcStores, string ownerName)
    {
        ArgumentNullException.ThrowIfNull(dbcStores);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerName);

        Dictionary<int, ChrRaceDbcRecord> races = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            CharacterDbcFileNames.ChrRaces,
            ownerName,
            29,
            ReadRaceRecord,
            record => record.Id);

        Dictionary<int, ChrClassDbcRecord> classes = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            CharacterDbcFileNames.ChrClasses,
            ownerName,
            17,
            ReadClassRecord,
            record => record.Id);

        List<CharBaseInfoDbcRecord> baseInfo = LoadBaseInfoRecords(dbcStores, ownerName);
        Dictionary<int, CharStartOutfitDbcRecord> startOutfits = LoadStartOutfitRecords(dbcStores, ownerName);

        Dictionary<int, CharSectionDbcRecord> sections = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            CharacterDbcFileNames.CharSections,
            ownerName,
            10,
            ReadSectionRecord,
            record => record.Id);

        List<CharacterFacialHairStyleDbcRecord> facialHairStyles = DbcTypedRecordLoader.LoadList(
            dbcStores,
            CharacterDbcFileNames.CharacterFacialHairStyles,
            ownerName,
            9,
            ReadFacialHairStyleRecord);

        Dictionary<int, CharHairGeosetDbcRecord> hairGeosets = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            CharacterDbcFileNames.CharHairGeosets,
            ownerName,
            6,
            ReadHairGeosetRecord,
            record => record.Id);

        CharacterDbcDataStore data = new(races, classes, baseInfo, startOutfits, sections, facialHairStyles, hairGeosets);

        Logger.Write(
            LogType.SUCCESS,
            string.Join(Environment.NewLine,
                $"{ownerName}: character DBC loaded:",
                $"  ChrRaces.dbc: {data.Races.Count}",
                $"  ChrClasses.dbc: {data.Classes.Count}",
                $"  ChrBaseInfo.dbc: {data.BaseInfo.Count}",
                $"  CharStartOutfit.dbc: {data.StartOutfits.Count}",
                $"  CharSections.dbc: {data.Sections.Count}",
                $"  CharacterFacialHairStyles.dbc: {data.FacialHairStyles.Count}",
                $"  CharHairGeosets.dbc: {data.HairGeosets.Count}"),
            "CharacterDbcDataStore");

        return data;
    }

    // Method: TryGetRace
    // Purpose: Attempts to retrieve or parse try get race data without treating normal misses as failures.
    // Parameters:
    // - raceId: Race ID identifier used to select the exact record, object, or runtime owner.
    // - race: Race value supplied by the caller for this operation.
    // Returns: Returns true when try get race succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetRace(int raceId, out ChrRaceDbcRecord race)
    {
        return Races.TryGetValue(raceId, out race!);
    }

    // Method: TryGetClass
    // Purpose: Attempts to retrieve or parse try get class data without treating normal misses as failures.
    // Parameters:
    // - classId: Class ID identifier used to select the exact record, object, or runtime owner.
    // - characterClass: Character class value supplied by the caller for this operation.
    // Returns: Returns true when try get class succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetClass(int classId, out ChrClassDbcRecord characterClass)
    {
        return Classes.TryGetValue(classId, out characterClass!);
    }

    // Method: IsRaceClassAllowed
    // Purpose: Validates or evaluates is race class allowed rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - raceId: Race ID identifier used to select the exact record, object, or runtime owner.
    // - classId: Class ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns true when is race class allowed succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool IsRaceClassAllowed(int raceId, int classId)
    {
        return _allowedRaceClasses.Contains((raceId, classId));
    }

    // Method: TryGetStartOutfit
    // Purpose: Attempts to retrieve or parse try get start outfit data without treating normal misses as failures.
    // Parameters:
    // - raceId: Race ID identifier used to select the exact record, object, or runtime owner.
    // - classId: Class ID identifier used to select the exact record, object, or runtime owner.
    // - genderId: Gender ID identifier used to select the exact record, object, or runtime owner.
    // - outfitId: Outfit ID identifier used to select the exact record, object, or runtime owner.
    // - outfit: Outfit value supplied by the caller for this operation.
    // Returns: Returns true when try get start outfit succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetStartOutfit(int raceId, int classId, int genderId, int outfitId, out CharStartOutfitDbcRecord outfit)
    {
        return _startOutfitsByCreateKey.TryGetValue((raceId, classId, genderId, outfitId), out outfit!);
    }

    // Method: IsSectionCustomizationValid
    // Purpose: Validates or evaluates is section customization valid rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - raceId: Race ID identifier used to select the exact record, object, or runtime owner.
    // - sexId: Sex ID identifier used to select the exact record, object, or runtime owner.
    // - sectionType: Section type value supplied by the caller for this operation.
    // - variationIndex: Variation index value supplied by the caller for this operation.
    // - colorIndex: Color index value supplied by the caller for this operation.
    // Returns: Returns true when is section customization valid succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool IsSectionCustomizationValid(int raceId, int sexId, int sectionType, int variationIndex, int colorIndex)
    {
        return _sectionsByCustomizationKey.ContainsKey((raceId, sexId, sectionType, variationIndex, colorIndex));
    }

    // Method: IsFacialHairValid
    // Purpose: Validates or evaluates is facial hair valid rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - raceId: Race ID identifier used to select the exact record, object, or runtime owner.
    // - sexId: Sex ID identifier used to select the exact record, object, or runtime owner.
    // - variationId: Variation ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns true when is facial hair valid succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool IsFacialHairValid(int raceId, int sexId, int variationId)
    {
        return _facialHairByCustomizationKey.ContainsKey((raceId, sexId, variationId));
    }

    // Method: IsHairStyleValid
    // Purpose: Validates or evaluates is hair style valid rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - raceId: Race ID identifier used to select the exact record, object, or runtime owner.
    // - sexId: Sex ID identifier used to select the exact record, object, or runtime owner.
    // - variationId: Variation ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns true when is hair style valid succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool IsHairStyleValid(int raceId, int sexId, int variationId)
    {
        return _hairGeosetsByCustomizationKey.ContainsKey((raceId, sexId, variationId));
    }

    // Method: ReadRaceRecord
    // Purpose: Retrieves read race record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the chr race DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static ChrRaceDbcRecord ReadRaceRecord(DbcRecord record)
    {
        return new ChrRaceDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadInt32(record, 1),
            DbcRecordReader.ReadInt32(record, 2),
            DbcRecordReader.ReadInt32(record, 3),
            DbcRecordReader.ReadInt32(record, 4),
            DbcRecordReader.ReadInt32(record, 5),
            DbcRecordReader.ReadString(record, 6),
            DbcRecordReader.ReadSingle(record, 7),
            DbcRecordReader.ReadInt32(record, 8),
            DbcRecordReader.ReadInt32(record, 9),
            DbcRecordReader.ReadInt32(record, 10),
            DbcRecordReader.ReadInt32(record, 12),
            DbcRecordReader.ReadInt32(record, 13),
            DbcRecordReader.ReadInt32(record, 14),
            DbcRecordReader.ReadString(record, 15),
            DbcRecordReader.ReadInt32(record, 16),
            DbcRecordReader.ReadString(record, 17),
            DbcRecordReader.ReadString(record, 26),
            DbcRecordReader.ReadString(record, 27),
            DbcRecordReader.ReadString(record, 28));
    }

    // Method: ReadClassRecord
    // Purpose: Retrieves read class record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the chr class DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static ChrClassDbcRecord ReadClassRecord(DbcRecord record)
    {
        return new ChrClassDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadInt32(record, 1),
            DbcRecordReader.ReadInt32(record, 3),
            DbcRecordReader.ReadString(record, 4),
            DbcRecordReader.ReadString(record, 5),
            DbcRecordReader.ReadString(record, 14),
            DbcRecordReader.ReadInt32(record, 15),
            DbcRecordReader.ReadInt32(record, 16));
    }

    // Method: LoadBaseInfoRecords
    // Purpose: Retrieves load base info records data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - dbcStores: Dbc stores value supplied by the caller for this operation.
    // - ownerName: Owner name value supplied by the caller for this operation.
    // Returns: Returns the list value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static List<CharBaseInfoDbcRecord> LoadBaseInfoRecords(IReadOnlyDictionary<string, DbcDataStore> dbcStores, string ownerName)
    {
        List<CharBaseInfoDbcRecord> records = [];
        if (!dbcStores.TryGetValue(CharacterDbcFileNames.CharBaseInfo, out DbcDataStore? store))
        {
            Logger.Write(LogType.WARNING, $"{ownerName} did not load {CharacterDbcFileNames.CharBaseInfo}; race/class validation will be unavailable.", "CharacterDbcDataStore");
            return records;
        }

        foreach (DbcRecord record in store.EnumerateRecords())
        {
            records.Add(ReadBaseInfoRecord(record));
        }

        return records;
    }

    // Method: ReadBaseInfoRecord
    // Purpose: Retrieves read base info record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the char base info DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static CharBaseInfoDbcRecord ReadBaseInfoRecord(DbcRecord record)
    {
        ReadOnlySpan<byte> raw = record.GetRawData();
        if (raw.Length >= 2)
        {
            return new CharBaseInfoDbcRecord(raw[0], raw[1]);
        }

        return new CharBaseInfoDbcRecord(
            DbcRecordReader.ReadByteAtOffset(record, 0),
            DbcRecordReader.ReadByteAtOffset(record, 1));
    }

    // Method: LoadStartOutfitRecords
    // Purpose: Retrieves load start outfit records data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - dbcStores: Dbc stores value supplied by the caller for this operation.
    // - ownerName: Owner name value supplied by the caller for this operation.
    // Returns: Returns the dictionary value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static Dictionary<int, CharStartOutfitDbcRecord> LoadStartOutfitRecords(IReadOnlyDictionary<string, DbcDataStore> dbcStores, string ownerName)
    {
        Dictionary<int, CharStartOutfitDbcRecord> records = [];
        if (!dbcStores.TryGetValue(CharacterDbcFileNames.CharStartOutfit, out DbcDataStore? store))
        {
            Logger.Write(LogType.WARNING, $"{ownerName} did not load {CharacterDbcFileNames.CharStartOutfit}; starter outfits will be unavailable.", "CharacterDbcDataStore");
            return records;
        }

        DbcRecordReader.ValidateRecordSize(store, CharacterDbcFileNames.CharStartOutfit, CharStartOutfitRequiredBytes);

        foreach (DbcRecord record in store.EnumerateRecords())
        {
            CharStartOutfitDbcRecord typedRecord = ReadStartOutfitRecord(record);
            records[typedRecord.Id] = typedRecord;
        }

        return records;
    }

    // Method: ReadStartOutfitRecord
    // Purpose: Retrieves read start outfit record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the char start outfit DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static CharStartOutfitDbcRecord ReadStartOutfitRecord(DbcRecord record)
    {
        int id = DbcRecordReader.ReadInt32AtOffset(record, 0);
        int raceId = DbcRecordReader.ReadByteAtOffset(record, 4);
        int classId = DbcRecordReader.ReadByteAtOffset(record, 5);
        int genderId = DbcRecordReader.ReadByteAtOffset(record, 6);
        int outfitId = DbcRecordReader.ReadByteAtOffset(record, 7);

        List<CharStartOutfitItemDbcRecord> items = [];
        int itemIdOffset = CharStartOutfitPackedHeaderSize;
        int itemDisplayIdOffset = itemIdOffset + CharStartOutfitItemCount * sizeof(int);
        int inventorySlotOffset = itemDisplayIdOffset + CharStartOutfitItemCount * sizeof(int);

        for (int index = 0; index < CharStartOutfitItemCount; index++)
        {
            int itemId = DbcRecordReader.ReadInt32AtOffset(record, itemIdOffset + index * sizeof(int));
            int itemDisplayId = DbcRecordReader.ReadInt32AtOffset(record, itemDisplayIdOffset + index * sizeof(int));
            int inventorySlotId = DbcRecordReader.ReadInt32AtOffset(record, inventorySlotOffset + index * sizeof(int));

            items.Add(new CharStartOutfitItemDbcRecord(index, itemId, itemDisplayId, inventorySlotId));
        }

        return new CharStartOutfitDbcRecord(id, raceId, classId, genderId, outfitId, items);
    }

    // Method: ReadSectionRecord
    // Purpose: Retrieves read section record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the char section DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static CharSectionDbcRecord ReadSectionRecord(DbcRecord record)
    {
        return new CharSectionDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadInt32(record, 1),
            DbcRecordReader.ReadInt32(record, 2),
            DbcRecordReader.ReadInt32(record, 3),
            DbcRecordReader.ReadInt32(record, 4),
            DbcRecordReader.ReadInt32(record, 5),
            DbcRecordReader.ReadString(record, 6),
            DbcRecordReader.ReadString(record, 7),
            DbcRecordReader.ReadString(record, 8),
            DbcRecordReader.ReadInt32(record, 9));
    }

    // Method: ReadFacialHairStyleRecord
    // Purpose: Retrieves read facial hair style record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the character facial hair style DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static CharacterFacialHairStyleDbcRecord ReadFacialHairStyleRecord(DbcRecord record)
    {
        int[] geosets =
        [
            DbcRecordReader.ReadInt32(record, 3),
            DbcRecordReader.ReadInt32(record, 4),
            DbcRecordReader.ReadInt32(record, 5),
            DbcRecordReader.ReadInt32(record, 6),
            DbcRecordReader.ReadInt32(record, 7),
            DbcRecordReader.ReadInt32(record, 8),
        ];

        return new CharacterFacialHairStyleDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadInt32(record, 1),
            DbcRecordReader.ReadInt32(record, 2),
            geosets);
    }

    // Method: ReadHairGeosetRecord
    // Purpose: Retrieves read hair geoset record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the char hair geoset DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static CharHairGeosetDbcRecord ReadHairGeosetRecord(DbcRecord record)
    {
        return new CharHairGeosetDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadInt32(record, 1),
            DbcRecordReader.ReadInt32(record, 2),
            DbcRecordReader.ReadInt32(record, 3),
            DbcRecordReader.ReadInt32(record, 4),
            DbcRecordReader.ReadInt32(record, 5));
    }

    // Constructor: HashSet
    // Purpose: Validates or evaluates hash set rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - RaceId: Race ID identifier used to select the exact record, object, or runtime owner.
    // - ClassId: Class ID identifier used to select the exact record, object, or runtime owner.
    // Returns: none.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static HashSet<(int RaceId, int ClassId)> BuildAllowedRaceClassSet(IEnumerable<CharBaseInfoDbcRecord> records)
    {
        return records
            .Select(record => (record.RaceId, record.ClassId))
            .ToHashSet();
    }

    // Constructor: Dictionary
    // Purpose: Executes the dictionary operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - RaceId: Race ID identifier used to select the exact record, object, or runtime owner.
    // - ClassId: Class ID identifier used to select the exact record, object, or runtime owner.
    // - GenderId: Gender ID identifier used to select the exact record, object, or runtime owner.
    // - OutfitId: Outfit ID identifier used to select the exact record, object, or runtime owner.
    // Returns: none.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static Dictionary<(int RaceId, int ClassId, int GenderId, int OutfitId), CharStartOutfitDbcRecord> BuildStartOutfitIndex(IEnumerable<CharStartOutfitDbcRecord> records)
    {
        Dictionary<(int RaceId, int ClassId, int GenderId, int OutfitId), CharStartOutfitDbcRecord> index = [];
        foreach (CharStartOutfitDbcRecord record in records)
        {
            index[(record.RaceId, record.ClassId, record.GenderId, record.OutfitId)] = record;
        }

        return index;
    }

    // Constructor: Dictionary
    // Purpose: Executes the dictionary operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - RaceId: Race ID identifier used to select the exact record, object, or runtime owner.
    // - SexId: Sex ID identifier used to select the exact record, object, or runtime owner.
    // - SectionType: Section type value supplied by the caller for this operation.
    // - VariationIndex: Variation index value supplied by the caller for this operation.
    // - ColorIndex: Color index value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static Dictionary<(int RaceId, int SexId, int SectionType, int VariationIndex, int ColorIndex), CharSectionDbcRecord> BuildSectionIndex(IEnumerable<CharSectionDbcRecord> records)
    {
        Dictionary<(int RaceId, int SexId, int SectionType, int VariationIndex, int ColorIndex), CharSectionDbcRecord> index = [];
        foreach (CharSectionDbcRecord record in records)
        {
            index[(record.RaceId, record.SexId, record.SectionType, record.VariationIndex, record.ColorIndex)] = record;
        }

        return index;
    }

    // Constructor: Dictionary
    // Purpose: Executes the dictionary operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - RaceId: Race ID identifier used to select the exact record, object, or runtime owner.
    // - SexId: Sex ID identifier used to select the exact record, object, or runtime owner.
    // - VariationId: Variation ID identifier used to select the exact record, object, or runtime owner.
    // Returns: none.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static Dictionary<(int RaceId, int SexId, int VariationId), CharacterFacialHairStyleDbcRecord> BuildFacialHairIndex(IEnumerable<CharacterFacialHairStyleDbcRecord> records)
    {
        Dictionary<(int RaceId, int SexId, int VariationId), CharacterFacialHairStyleDbcRecord> index = [];
        foreach (CharacterFacialHairStyleDbcRecord record in records)
        {
            index[(record.RaceId, record.SexId, record.VariationId)] = record;
        }

        return index;
    }

    // Constructor: Dictionary
    // Purpose: Executes the dictionary operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - RaceId: Race ID identifier used to select the exact record, object, or runtime owner.
    // - SexId: Sex ID identifier used to select the exact record, object, or runtime owner.
    // - VariationId: Variation ID identifier used to select the exact record, object, or runtime owner.
    // Returns: none.
    // Notes: This keeps the operation scoped to CharacterDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static Dictionary<(int RaceId, int SexId, int VariationId), CharHairGeosetDbcRecord> BuildHairGeosetIndex(IEnumerable<CharHairGeosetDbcRecord> records)
    {
        Dictionary<(int RaceId, int SexId, int VariationId), CharHairGeosetDbcRecord> index = [];
        foreach (CharHairGeosetDbcRecord record in records)
        {
            index[(record.RaceId, record.SexId, record.VariationId)] = record;
        }

        return index;
    }
}
