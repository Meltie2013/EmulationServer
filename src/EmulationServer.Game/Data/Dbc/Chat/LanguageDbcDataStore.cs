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

namespace EmulationServer.Game.Data.Dbc.Chat;

public sealed class LanguageDbcDataStore
{
    private LanguageDbcDataStore(IReadOnlyDictionary<int, LanguageDbcRecord> records)
    {
        Records = records;
    }

    public static LanguageDbcDataStore Empty { get; } = new(new Dictionary<int, LanguageDbcRecord>());

    public IReadOnlyDictionary<int, LanguageDbcRecord> Records { get; }

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

    public bool IsKnownLanguage(int languageId)
    {
        return languageId == 0 || Records.ContainsKey(languageId);
    }

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

    private static LanguageDbcRecord ReadRecord(DbcRecord record)
    {
        return new LanguageDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadString(record, 1));
    }
}
