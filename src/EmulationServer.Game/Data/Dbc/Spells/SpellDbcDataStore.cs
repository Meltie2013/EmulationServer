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
  * File overview: src/EmulationServer.Game/Data/Dbc/Spells/SpellDbcDataStore.cs
  * Documents the SpellDbcDataStore source file in the DBC loading and strongly typed client data records area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Data.Dbc.Spells;

/**
  * Owns typed spell/skill DBC data and lookup indexes.
  */
public sealed class SpellDbcDataStore
{
    /**
      * Initializes a new SpellDbcDataStore instance with the dependencies required by the DBC loading and strongly typed client data records workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      */
    private SpellDbcDataStore()
    {
        Skills = new Dictionary<int, SkillLineDbcRecord>();
        SkillAbilities = new Dictionary<int, SkillLineAbilityDbcRecord>();
        SkillRaceClassInfo = new Dictionary<int, SkillRaceClassInfoDbcRecord>();
        Spells = new Dictionary<int, SpellDbcRecord>();
        SpellIcons = new Dictionary<int, SpellIconDbcRecord>();
        SpellDurations = new Dictionary<int, SpellDurationDbcRecord>();
        SpellRanges = new Dictionary<int, SpellRangeDbcRecord>();
        SpellCastTimes = new Dictionary<int, SpellCastTimeDbcRecord>();
    }

    /**
      * Initializes a new SpellDbcDataStore instance with the dependencies required by the DBC loading and strongly typed client data records workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: skills, skillAbilities, skillRaceClassInfo, spells, spellIcons, spellDurations....
      */
    private SpellDbcDataStore(
        IReadOnlyDictionary<int, SkillLineDbcRecord> skills,
        IReadOnlyDictionary<int, SkillLineAbilityDbcRecord> skillAbilities,
        IReadOnlyDictionary<int, SkillRaceClassInfoDbcRecord> skillRaceClassInfo,
        IReadOnlyDictionary<int, SpellDbcRecord> spells,
        IReadOnlyDictionary<int, SpellIconDbcRecord> spellIcons,
        IReadOnlyDictionary<int, SpellDurationDbcRecord> spellDurations,
        IReadOnlyDictionary<int, SpellRangeDbcRecord> spellRanges,
        IReadOnlyDictionary<int, SpellCastTimeDbcRecord> spellCastTimes)
    {
        Skills = skills;
        SkillAbilities = skillAbilities;
        SkillRaceClassInfo = skillRaceClassInfo;
        Spells = spells;
        SpellIcons = spellIcons;
        SpellDurations = spellDurations;
        SpellRanges = spellRanges;
        SpellCastTimes = spellCastTimes;
    }

    /**
      * Exposes the empty value to callers that need this runtime or configuration data.
      * The property keeps the public surface strongly typed and documents which part of the server workflow owns the value.
      */
    public static SpellDbcDataStore Empty { get; } = new();

    public IReadOnlyDictionary<int, SkillLineDbcRecord> Skills { get; }

    public IReadOnlyDictionary<int, SkillLineAbilityDbcRecord> SkillAbilities { get; }

    public IReadOnlyDictionary<int, SkillRaceClassInfoDbcRecord> SkillRaceClassInfo { get; }

    public IReadOnlyDictionary<int, SpellDbcRecord> Spells { get; }

    public IReadOnlyDictionary<int, SpellIconDbcRecord> SpellIcons { get; }

    public IReadOnlyDictionary<int, SpellDurationDbcRecord> SpellDurations { get; }

    public IReadOnlyDictionary<int, SpellRangeDbcRecord> SpellRanges { get; }

    public IReadOnlyDictionary<int, SpellCastTimeDbcRecord> SpellCastTimes { get; }

    /**
      * Converts loaded raw DBC stores into typed spell and skill DBC indexes.
      */
    public static SpellDbcDataStore FromDbcStores(IReadOnlyDictionary<string, DbcDataStore> dbcStores, string ownerName)
    {
        ArgumentNullException.ThrowIfNull(dbcStores);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerName);

        Dictionary<int, SkillLineDbcRecord> skills = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            SpellDbcFileNames.SkillLine,
            ownerName,
            22,
            ReadSkillLineRecord,
            record => record.Id);

        Dictionary<int, SkillLineAbilityDbcRecord> skillAbilities = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            SpellDbcFileNames.SkillLineAbility,
            ownerName,
            15,
            ReadSkillLineAbilityRecord,
            record => record.Id);

        Dictionary<int, SkillRaceClassInfoDbcRecord> skillRaceClassInfo = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            SpellDbcFileNames.SkillRaceClassInfo,
            ownerName,
            8,
            ReadSkillRaceClassInfoRecord,
            record => record.Id);

        Dictionary<int, SpellDbcRecord> spells = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            SpellDbcFileNames.Spell,
            ownerName,
            173,
            ReadSpellRecord,
            record => record.Id);

        Dictionary<int, SpellIconDbcRecord> spellIcons = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            SpellDbcFileNames.SpellIcon,
            ownerName,
            2,
            ReadSpellIconRecord,
            record => record.Id);

        Dictionary<int, SpellDurationDbcRecord> spellDurations = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            SpellDbcFileNames.SpellDuration,
            ownerName,
            4,
            ReadSpellDurationRecord,
            record => record.Id);

        Dictionary<int, SpellRangeDbcRecord> spellRanges = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            SpellDbcFileNames.SpellRange,
            ownerName,
            22,
            ReadSpellRangeRecord,
            record => record.Id);

        Dictionary<int, SpellCastTimeDbcRecord> spellCastTimes = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            SpellDbcFileNames.SpellCastTimes,
            ownerName,
            4,
            ReadSpellCastTimeRecord,
            record => record.Id);

        SpellDbcDataStore data = new(skills, skillAbilities, skillRaceClassInfo, spells, spellIcons, spellDurations, spellRanges, spellCastTimes);

        Logger.Write(
            LogType.SUCCESS,
            $"{ownerName}: spell DBC loaded (skillLines={data.Skills.Count}, skillAbilities={data.SkillAbilities.Count}, skillRaceClassInfo={data.SkillRaceClassInfo.Count}, spells={data.Spells.Count}, icons={data.SpellIcons.Count}, durations={data.SpellDurations.Count}, ranges={data.SpellRanges.Count}, castTimes={data.SpellCastTimes.Count}).",
            "SpellDbcDataStore");

        return data;
    }

    /**
      * Tries to resolve the get spell value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: spellId, spell.
      */
    public bool TryGetSpell(int spellId, out SpellDbcRecord spell)
    {
        return Spells.TryGetValue(spellId, out spell!);
    }

    /**
      * Parses read skill line record input into the strongly typed server representation.
      * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
      * Inputs used by this operation: record.
      */
    private static SkillLineDbcRecord ReadSkillLineRecord(DbcRecord record)
    {
        return new SkillLineDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadInt32(record, 1),
            DbcRecordReader.ReadInt32(record, 2),
            DbcRecordReader.ReadString(record, 3),
            DbcRecordReader.ReadString(record, 12),
            DbcRecordReader.ReadInt32(record, 21));
    }

    /**
      * Parses read skill line ability record input into the strongly typed server representation.
      * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
      * Inputs used by this operation: record.
      */
    private static SkillLineAbilityDbcRecord ReadSkillLineAbilityRecord(DbcRecord record)
    {
        return new SkillLineAbilityDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadInt32(record, 1),
            DbcRecordReader.ReadInt32(record, 2),
            DbcRecordReader.ReadInt32(record, 3),
            DbcRecordReader.ReadInt32(record, 4),
            DbcRecordReader.ReadInt32(record, 5),
            DbcRecordReader.ReadInt32(record, 6),
            DbcRecordReader.ReadInt32(record, 7),
            DbcRecordReader.ReadInt32(record, 8),
            DbcRecordReader.ReadInt32(record, 9),
            DbcRecordReader.ReadInt32(record, 12),
            DbcRecordReader.ReadInt32(record, 14));
    }

    /**
      * Parses read skill race class info record input into the strongly typed server representation.
      * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
      * Inputs used by this operation: record.
      */
    private static SkillRaceClassInfoDbcRecord ReadSkillRaceClassInfoRecord(DbcRecord record)
    {
        return new SkillRaceClassInfoDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadInt32(record, 1),
            DbcRecordReader.ReadInt32(record, 2),
            DbcRecordReader.ReadInt32(record, 3),
            DbcRecordReader.ReadInt32(record, 4),
            DbcRecordReader.ReadInt32(record, 5),
            DbcRecordReader.ReadInt32(record, 6),
            DbcRecordReader.ReadInt32(record, 7));
    }

    /**
      * Parses read spell record input into the strongly typed server representation.
      * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
      * Inputs used by this operation: record.
      */
    private static SpellDbcRecord ReadSpellRecord(DbcRecord record)
    {
        return new SpellDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadInt32(record, 1),
            DbcRecordReader.ReadInt32(record, 2),
            DbcRecordReader.ReadInt32(record, 4),
            DbcRecordReader.ReadInt32(record, 5),
            DbcRecordReader.ReadInt32(record, 6),
            DbcRecordReader.ReadInt32(record, 7),
            DbcRecordReader.ReadInt32(record, 18),
            DbcRecordReader.ReadInt32(record, 29),
            DbcRecordReader.ReadInt32(record, 30),
            DbcRecordReader.ReadInt32(record, 31),
            DbcRecordReader.ReadInt32(record, 35),
            DbcRecordReader.ReadInt32(record, 116),
            DbcRecordReader.ReadString(record, 120),
            DbcRecordReader.ReadString(record, 129),
            DbcRecordReader.ReadString(record, 138));
    }

    /**
      * Parses read spell icon record input into the strongly typed server representation.
      * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
      * Inputs used by this operation: record.
      */
    private static SpellIconDbcRecord ReadSpellIconRecord(DbcRecord record)
    {
        return new SpellIconDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadString(record, 1));
    }

    /**
      * Parses read spell duration record input into the strongly typed server representation.
      * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
      * Inputs used by this operation: record.
      */
    private static SpellDurationDbcRecord ReadSpellDurationRecord(DbcRecord record)
    {
        return new SpellDurationDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadInt32(record, 1),
            DbcRecordReader.ReadInt32(record, 2),
            DbcRecordReader.ReadInt32(record, 3));
    }

    /**
      * Parses read spell range record input into the strongly typed server representation.
      * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
      * Inputs used by this operation: record.
      */
    private static SpellRangeDbcRecord ReadSpellRangeRecord(DbcRecord record)
    {
        return new SpellRangeDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadSingle(record, 1),
            DbcRecordReader.ReadSingle(record, 2),
            DbcRecordReader.ReadInt32(record, 3),
            DbcRecordReader.ReadString(record, 4),
            DbcRecordReader.ReadString(record, 13));
    }

    /**
      * Parses read spell cast time record input into the strongly typed server representation.
      * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
      * Inputs used by this operation: record.
      */
    private static SpellCastTimeDbcRecord ReadSpellCastTimeRecord(DbcRecord record)
    {
        return new SpellCastTimeDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadInt32(record, 1),
            DbcRecordReader.ReadInt32(record, 2),
            DbcRecordReader.ReadInt32(record, 3));
    }
}
