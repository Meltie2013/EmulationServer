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
// File: src/EmulationServer.Game/Data/Dbc/Chat/LanguageDbcDataStore.cs
// Purpose: Contains language DBC data store code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Game.Data.Dbc;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Game.Data.Dbc.Chat;

// Type: LanguageDbcDataStore
// Purpose: Provides language DBC data store behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class LanguageDbcDataStore
{

    // Constructor: LanguageDbcDataStore
    // Purpose: Initializes a new LanguageDbcDataStore instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - records: Records value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to LanguageDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private LanguageDbcDataStore(IReadOnlyDictionary<int, LanguageDbcRecord> records)
    {
        Records = records;
    }

    public static LanguageDbcDataStore Empty { get; } = new(new Dictionary<int, LanguageDbcRecord>());

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, LanguageDbcRecord> Records { get; }

    // Method: FromDbcStores
    // Purpose: Executes the from DBC stores operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - dbcStores: Dbc stores value supplied by the caller for this operation.
    // - ownerName: Owner name value supplied by the caller for this operation.
    // Returns: Returns the language DBC data store value produced by this operation.
    // Notes: This keeps the operation scoped to LanguageDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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
        Logger.Write(
            LogType.SUCCESS,
            string.Join(Environment.NewLine,
                $"{ownerName}: language DBC loaded:",
                $"  Languages.dbc: {data.Records.Count}"),
            "LanguageDbcDataStore");
        return data;
    }

    // Method: IsKnownLanguage
    // Purpose: Validates or evaluates is known language rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - languageId: Language ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns true when is known language succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to LanguageDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool IsKnownLanguage(int languageId)
    {
        return languageId == 0 || Records.ContainsKey(languageId);
    }

    // Method: GetLanguageName
    // Purpose: Retrieves get language name data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - languageId: Language ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to LanguageDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadRecord
    // Purpose: Retrieves read record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the language DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to LanguageDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static LanguageDbcRecord ReadRecord(DbcRecord record)
    {
        return new LanguageDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadString(record, 1));
    }
}
