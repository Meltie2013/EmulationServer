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
// File: src/EmulationServer.Game/Data/Dbc/DbcTypedRecordLoader.cs
// Purpose: Contains DBC typed record loader code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Game.Data.Dbc;

// Type: DbcTypedRecordLoader
// Purpose: Provides DBC typed record loader behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
internal static class DbcTypedRecordLoader
{

    // Method: TRecord
    // Purpose: Executes the T record operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - dbcStores: Dbc stores value supplied by the caller for this operation.
    // - fileName: File name value supplied by the caller for this operation.
    // - ownerName: Owner name value supplied by the caller for this operation.
    // - requiredFieldCount: Required field count value supplied by the caller for this operation.
    // - readRecord: Read record value supplied by the caller for this operation.
    // - getKey: Get key value supplied by the caller for this operation.
    // Returns: Returns the dictionary load indexed<t key, value produced by this operation.
    // Notes: This keeps the operation scoped to DbcTypedRecordLoader so callers do not duplicate validation, protocol, or persistence rules.
    public static Dictionary<TKey, TRecord> LoadIndexed<TKey, TRecord>(
        IReadOnlyDictionary<string, DbcDataStore> dbcStores,
        string fileName,
        string ownerName,
        int requiredFieldCount,
        Func<DbcRecord, TRecord> readRecord,
        Func<TRecord, TKey> getKey)
        where TKey : notnull
        where TRecord : notnull
    {
        Dictionary<TKey, TRecord> records = [];
        if (!dbcStores.TryGetValue(fileName, out DbcDataStore? store))
        {
            Logger.Write(LogType.WARNING, $"{ownerName} did not load {fileName}; typed data from that file will be unavailable.", "DbcTypedRecordLoader");
            return records;
        }

        DbcRecordReader.ValidateFieldCount(store, fileName, requiredFieldCount);

        foreach (DbcRecord record in store.EnumerateRecords())
        {
            TRecord typedRecord = readRecord(record);
            records[getKey(typedRecord)] = typedRecord;
        }

        return records;
    }

    // Method: TRecord
    // Purpose: Executes the T record operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - dbcStores: Dbc stores value supplied by the caller for this operation.
    // - fileName: File name value supplied by the caller for this operation.
    // - ownerName: Owner name value supplied by the caller for this operation.
    // - requiredFieldCount: Required field count value supplied by the caller for this operation.
    // - readRecord: Read record value supplied by the caller for this operation.
    // Returns: Returns the list load list< value produced by this operation.
    // Notes: This keeps the operation scoped to DbcTypedRecordLoader so callers do not duplicate validation, protocol, or persistence rules.
    public static List<TRecord> LoadList<TRecord>(
        IReadOnlyDictionary<string, DbcDataStore> dbcStores,
        string fileName,
        string ownerName,
        int requiredFieldCount,
        Func<DbcRecord, TRecord> readRecord)
        where TRecord : notnull
    {
        List<TRecord> records = [];
        if (!dbcStores.TryGetValue(fileName, out DbcDataStore? store))
        {
            Logger.Write(LogType.WARNING, $"{ownerName} did not load {fileName}; typed data from that file will be unavailable.", "DbcTypedRecordLoader");
            return records;
        }

        DbcRecordReader.ValidateFieldCount(store, fileName, requiredFieldCount);

        foreach (DbcRecord record in store.EnumerateRecords())
        {
            records.Add(readRecord(record));
        }

        return records;
    }
}
