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
// File: src/EmulationServer.Game/Data/Dbc/Spells/SpellDbcDataStore.cs
// Purpose: Contains spell DBC data store code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Game.Data.Dbc;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Game.Data.Dbc.Spells;

// Type: SpellDbcDataStore
// Purpose: Provides spell DBC data store behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class SpellDbcDataStore
{

    // Constructor: SpellDbcDataStore
    // Purpose: Initializes a new SpellDbcDataStore instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to SpellDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Constructor: SpellDbcDataStore
    // Purpose: Initializes a new SpellDbcDataStore instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - skills: Skills value supplied by the caller for this operation.
    // - skillAbilities: Skill abilities value supplied by the caller for this operation.
    // - skillRaceClassInfo: Skill race class info value supplied by the caller for this operation.
    // - spells: Spells value supplied by the caller for this operation.
    // - spellIcons: Spell icons value supplied by the caller for this operation.
    // - spellDurations: Spell durations value supplied by the caller for this operation.
    // - spellRanges: Spell ranges value supplied by the caller for this operation.
    // - spellCastTimes: Spell cast times value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to SpellDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    public static SpellDbcDataStore Empty { get; } = new();

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, SkillLineDbcRecord> Skills { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, SkillLineAbilityDbcRecord> SkillAbilities { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, SkillRaceClassInfoDbcRecord> SkillRaceClassInfo { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, SpellDbcRecord> Spells { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, SpellIconDbcRecord> SpellIcons { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, SpellDurationDbcRecord> SpellDurations { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, SpellRangeDbcRecord> SpellRanges { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, SpellCastTimeDbcRecord> SpellCastTimes { get; }

    // Method: FromDbcStores
    // Purpose: Executes the from DBC stores operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - dbcStores: Dbc stores value supplied by the caller for this operation.
    // - ownerName: Owner name value supplied by the caller for this operation.
    // Returns: Returns the spell DBC data store value produced by this operation.
    // Notes: This keeps the operation scoped to SpellDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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
            string.Join(Environment.NewLine,
                $"{ownerName}: spell DBC loaded:",
                $"  SkillLine.dbc: {data.Skills.Count}",
                $"  SkillLineAbility.dbc: {data.SkillAbilities.Count}",
                $"  SkillRaceClassInfo.dbc: {data.SkillRaceClassInfo.Count}",
                $"  Spell.dbc: {data.Spells.Count}",
                $"  SpellIcon.dbc: {data.SpellIcons.Count}",
                $"  SpellDuration.dbc: {data.SpellDurations.Count}",
                $"  SpellRange.dbc: {data.SpellRanges.Count}",
                $"  SpellCastTimes.dbc: {data.SpellCastTimes.Count}"),
            "SpellDbcDataStore");

        return data;
    }

    // Method: TryGetSpell
    // Purpose: Attempts to retrieve or parse try get spell data without treating normal misses as failures.
    // Parameters:
    // - spellId: Spell ID identifier used to select the exact record, object, or runtime owner.
    // - spell: Spell value supplied by the caller for this operation.
    // Returns: Returns true when try get spell succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to SpellDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetSpell(int spellId, out SpellDbcRecord spell)
    {
        return Spells.TryGetValue(spellId, out spell!);
    }

    // Method: ReadSkillLineRecord
    // Purpose: Retrieves read skill line record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the skill line DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to SpellDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadSkillLineAbilityRecord
    // Purpose: Retrieves read skill line ability record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the skill line ability DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to SpellDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadSkillRaceClassInfoRecord
    // Purpose: Retrieves read skill race class info record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the skill race class info DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to SpellDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadSpellRecord
    // Purpose: Retrieves read spell record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the spell DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to SpellDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadSpellIconRecord
    // Purpose: Retrieves read spell icon record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the spell icon DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to SpellDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static SpellIconDbcRecord ReadSpellIconRecord(DbcRecord record)
    {
        return new SpellIconDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadString(record, 1));
    }

    // Method: ReadSpellDurationRecord
    // Purpose: Retrieves read spell duration record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the spell duration DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to SpellDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static SpellDurationDbcRecord ReadSpellDurationRecord(DbcRecord record)
    {
        return new SpellDurationDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadInt32(record, 1),
            DbcRecordReader.ReadInt32(record, 2),
            DbcRecordReader.ReadInt32(record, 3));
    }

    // Method: ReadSpellRangeRecord
    // Purpose: Retrieves read spell range record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the spell range DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to SpellDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadSpellCastTimeRecord
    // Purpose: Retrieves read spell cast time record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the spell cast time DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to SpellDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static SpellCastTimeDbcRecord ReadSpellCastTimeRecord(DbcRecord record)
    {
        return new SpellCastTimeDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadInt32(record, 1),
            DbcRecordReader.ReadInt32(record, 2),
            DbcRecordReader.ReadInt32(record, 3));
    }
}
