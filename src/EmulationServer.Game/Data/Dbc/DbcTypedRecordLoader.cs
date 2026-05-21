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

using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

/**
  * File overview: src/EmulationServer.Game/Data/Dbc/DbcTypedRecordLoader.cs
  * This file provides reusable typed DBC load helpers for character, item, spell, faction, and future stores.
  */

namespace EmulationServer.Game.Data.Dbc;

/**
  * Converts raw DBC stores into typed dictionaries or lists while keeping missing optional stores non-fatal.
  */
internal static class DbcTypedRecordLoader
{
    /**
      * Reads one DBC file into typed records indexed by a caller-provided key.
      */
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
            Logger.Write(LogType.WARNING, $"{ownerName} did not load {fileName}; typed data from that file will be unavailable.", nameof(DbcTypedRecordLoader));
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

    /**
      * Reads one DBC file into a typed list.
      */
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
            Logger.Write(LogType.WARNING, $"{ownerName} did not load {fileName}; typed data from that file will be unavailable.", nameof(DbcTypedRecordLoader));
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
