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
 * File overview: src/EmulationServer.Game/Data/Dbc/Chat/LanguageDbcDataStore.cs
 * Documents the LanguageDbcDataStore source file in the DBC loading and strongly typed client data records area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Game.Data.Dbc.Chat;

/**
 * Owns the language dbc data store behavior for the DBC loading and strongly typed client data records layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public sealed class LanguageDbcDataStore
{
    /**
     * Initializes a new LanguageDbcDataStore instance with the dependencies required by the DBC loading and strongly typed client data records workflow.
     * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
     * Inputs used by this operation: records.
     */
    private LanguageDbcDataStore(IReadOnlyDictionary<int, LanguageDbcRecord> records)
    {
        Records = records;
    }

    /**
     * Exposes the empty value to callers that need this runtime or configuration data.
     * The property keeps the public surface strongly typed and documents which part of the server workflow owns the value.
     */
    public static LanguageDbcDataStore Empty { get; } = new(new Dictionary<int, LanguageDbcRecord>());

    public IReadOnlyDictionary<int, LanguageDbcRecord> Records { get; }

    /**
     * Performs the from dbc stores operation for the DBC loading and strongly typed client data records workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: dbcStores, ownerName.
     */
    public static LanguageDbcDataStore FromDbcStores(IReadOnlyDictionary<string, DbcDataStore> dbcStores, string ownerName)
    {
        ArgumentNullException.ThrowIfNull(dbcStores);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerName);

        Dictionary<int, LanguageDbcRecord> languages = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            ChatDbcFileNames.Languages,
            ownerName,
            10,
            ReadRecord,
            record => record.Id);

        LanguageDbcDataStore data = new(languages);
        Logger.Write(LogType.SUCCESS, $"{ownerName} typed language DBC data loaded: languages={data.Records.Count}.", nameof(LanguageDbcDataStore));
        return data;
    }

    /**
     * Determines whether known language for the DBC loading and strongly typed client data records workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: languageId.
     */
    public bool IsKnownLanguage(int languageId)
    {
        return languageId == 0 || Records.ContainsKey(languageId);
    }

    /**
     * Resolves the language name value requested by the caller.
     * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
     * Inputs used by this operation: languageId.
     */
    public string GetLanguageName(int languageId)
    {
        if (languageId == 0)
        {
            return "Universal";
        }

        return Records.TryGetValue(languageId, out LanguageDbcRecord? language)
            ? language.Name
            : $"Language {languageId}";
    }

    /**
     * Parses read record input into the strongly typed server representation.
     * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
     * Inputs used by this operation: record.
     */
    private static LanguageDbcRecord ReadRecord(DbcRecord record)
    {
        return new LanguageDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadString(record, 1));
    }
}
