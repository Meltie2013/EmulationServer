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
  * File overview: src/EmulationServer.Game/Data/Dbc/Characters/CharacterDbcDataStore.cs
  * Documents the CharacterDbcDataStore source file in the DBC loading and strongly typed client data records area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Data.Dbc.Characters;

/**
  * Owns typed character DBC data and validation indexes for WorldServer character flows.
  */
public sealed class CharacterDbcDataStore
{
    /**
      * Defines the constant value for char start outfit packed header size.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const int CharStartOutfitPackedHeaderSize = 8;
    /**
      * Defines the constant value for char start outfit item count.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const int CharStartOutfitItemCount = 12;
    /**
      * Defines the constant value for char start outfit required bytes.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const int CharStartOutfitRequiredBytes = CharStartOutfitPackedHeaderSize + CharStartOutfitItemCount * sizeof(int) * 3;

    private readonly HashSet<(int RaceId, int ClassId)> _allowedRaceClasses;
    private readonly Dictionary<(int RaceId, int ClassId, int GenderId, int OutfitId), CharStartOutfitDbcRecord> _startOutfitsByCreateKey;
    private readonly Dictionary<(int RaceId, int SexId, int SectionType, int VariationIndex, int ColorIndex), CharSectionDbcRecord> _sectionsByCustomizationKey;
    private readonly Dictionary<(int RaceId, int SexId, int VariationId), CharacterFacialHairStyleDbcRecord> _facialHairByCustomizationKey;
    private readonly Dictionary<(int RaceId, int SexId, int VariationId), CharHairGeosetDbcRecord> _hairGeosetsByCustomizationKey;

    /**
      * Initializes a new CharacterDbcDataStore instance with the dependencies required by the DBC loading and strongly typed client data records workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      */
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

    /**
      * Initializes a new CharacterDbcDataStore instance with the dependencies required by the DBC loading and strongly typed client data records workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: races, classes, baseInfo, startOutfits, sections, facialHairStyles....
      */
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

    /**
      * Gets an empty typed character store for disabled DBC loading paths.
      */
    public static CharacterDbcDataStore Empty { get; } = new();

    /**
      * Gets all ChrRaces.dbc records indexed by race id.
      */
    public IReadOnlyDictionary<int, ChrRaceDbcRecord> Races { get; }

    /**
      * Gets all ChrClasses.dbc records indexed by class id.
      */
    public IReadOnlyDictionary<int, ChrClassDbcRecord> Classes { get; }

    /**
      * Gets valid race/class combinations from CharBaseInfo.dbc.
      */
    public IReadOnlyList<CharBaseInfoDbcRecord> BaseInfo { get; }

    /**
      * Gets all CharStartOutfit.dbc records indexed by outfit record id.
      */
    public IReadOnlyDictionary<int, CharStartOutfitDbcRecord> StartOutfits { get; }

    /**
      * Gets all CharSections.dbc records indexed by section id.
      */
    public IReadOnlyDictionary<int, CharSectionDbcRecord> Sections { get; }

    /**
      * Gets all CharacterFacialHairStyles.dbc records.
      */
    public IReadOnlyList<CharacterFacialHairStyleDbcRecord> FacialHairStyles { get; }

    /**
      * Gets all CharHairGeosets.dbc records indexed by row id.
      */
    public IReadOnlyDictionary<int, CharHairGeosetDbcRecord> HairGeosets { get; }

    /**
      * Converts loaded raw DBC stores into typed character DBC indexes.
      */
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
            $"{ownerName}: character DBC loaded (races={data.Races.Count}, classes={data.Classes.Count}, raceClassPairs={data.BaseInfo.Count}, startOutfits={data.StartOutfits.Count}, sections={data.Sections.Count}, facialHairStyles={data.FacialHairStyles.Count}, hairGeosets={data.HairGeosets.Count}).",
            "CharacterDbcDataStore");

        return data;
    }

    /**
      * Attempts to get one race by id.
      */
    public bool TryGetRace(int raceId, out ChrRaceDbcRecord race)
    {
        return Races.TryGetValue(raceId, out race!);
    }

    /**
      * Attempts to get one class by id.
      */
    public bool TryGetClass(int classId, out ChrClassDbcRecord characterClass)
    {
        return Classes.TryGetValue(classId, out characterClass!);
    }

    /**
      * Returns true when the supplied race/class pair is allowed by CharBaseInfo.dbc.
      */
    public bool IsRaceClassAllowed(int raceId, int classId)
    {
        return _allowedRaceClasses.Contains((raceId, classId));
    }

    /**
      * Attempts to resolve the starter outfit for a race/class/gender/outfit choice.
      */
    public bool TryGetStartOutfit(int raceId, int classId, int genderId, int outfitId, out CharStartOutfitDbcRecord outfit)
    {
        return _startOutfitsByCreateKey.TryGetValue((raceId, classId, genderId, outfitId), out outfit!);
    }

    /**
      * Returns true when the requested CharSections customization tuple exists.
      */
    public bool IsSectionCustomizationValid(int raceId, int sexId, int sectionType, int variationIndex, int colorIndex)
    {
        return _sectionsByCustomizationKey.ContainsKey((raceId, sexId, sectionType, variationIndex, colorIndex));
    }

    /**
      * Returns true when the requested facial-hair variation exists for the race/gender pair.
      */
    public bool IsFacialHairValid(int raceId, int sexId, int variationId)
    {
        return _facialHairByCustomizationKey.ContainsKey((raceId, sexId, variationId));
    }

    /**
      * Returns true when the requested hair-style variation exists for the race/gender pair.
      */
    public bool IsHairStyleValid(int raceId, int sexId, int variationId)
    {
        return _hairGeosetsByCustomizationKey.ContainsKey((raceId, sexId, variationId));
    }

    /**
      * Parses read race record input into the strongly typed server representation.
      * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
      * Inputs used by this operation: record.
      */
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

    /**
      * Parses read class record input into the strongly typed server representation.
      * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
      * Inputs used by this operation: record.
      */
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

    /**
      * Loads load base info records information from configuration, files, or persistent storage.
      * The method normalizes external input before returning it so the rest of the server can work with validated, strongly typed data.
      * Inputs used by this operation: dbcStores, ownerName.
      */
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

    /**
      * Parses read base info record input into the strongly typed server representation.
      * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
      * Inputs used by this operation: record.
      */
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

    /**
      * Parses read start outfit record input into the strongly typed server representation.
      * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
      * Inputs used by this operation: record.
      */
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

    /**
      * Parses read section record input into the strongly typed server representation.
      * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
      * Inputs used by this operation: record.
      */
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

    /**
      * Parses read facial hair style record input into the strongly typed server representation.
      * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
      * Inputs used by this operation: record.
      */
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

    /**
      * Parses read hair geoset record input into the strongly typed server representation.
      * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
      * Inputs used by this operation: record.
      */
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

    private static HashSet<(int RaceId, int ClassId)> BuildAllowedRaceClassSet(IEnumerable<CharBaseInfoDbcRecord> records)
    {
        return records
            .Select(record => (record.RaceId, record.ClassId))
            .ToHashSet();
    }

    private static Dictionary<(int RaceId, int ClassId, int GenderId, int OutfitId), CharStartOutfitDbcRecord> BuildStartOutfitIndex(IEnumerable<CharStartOutfitDbcRecord> records)
    {
        Dictionary<(int RaceId, int ClassId, int GenderId, int OutfitId), CharStartOutfitDbcRecord> index = [];
        foreach (CharStartOutfitDbcRecord record in records)
        {
            index[(record.RaceId, record.ClassId, record.GenderId, record.OutfitId)] = record;
        }

        return index;
    }

    private static Dictionary<(int RaceId, int SexId, int SectionType, int VariationIndex, int ColorIndex), CharSectionDbcRecord> BuildSectionIndex(IEnumerable<CharSectionDbcRecord> records)
    {
        Dictionary<(int RaceId, int SexId, int SectionType, int VariationIndex, int ColorIndex), CharSectionDbcRecord> index = [];
        foreach (CharSectionDbcRecord record in records)
        {
            index[(record.RaceId, record.SexId, record.SectionType, record.VariationIndex, record.ColorIndex)] = record;
        }

        return index;
    }

    private static Dictionary<(int RaceId, int SexId, int VariationId), CharacterFacialHairStyleDbcRecord> BuildFacialHairIndex(IEnumerable<CharacterFacialHairStyleDbcRecord> records)
    {
        Dictionary<(int RaceId, int SexId, int VariationId), CharacterFacialHairStyleDbcRecord> index = [];
        foreach (CharacterFacialHairStyleDbcRecord record in records)
        {
            index[(record.RaceId, record.SexId, record.VariationId)] = record;
        }

        return index;
    }

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
