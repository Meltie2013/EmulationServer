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
  * File overview: src/EmulationServer.Game/Data/Dbc/Factions/FactionDbcDataStore.cs
  * This file converts raw faction DBC tables into typed metadata for race, reputation, and hostile/friendly validation.
  */

namespace EmulationServer.Game.Data.Dbc.Factions;

/**
  * Owns typed faction DBC data and lookup indexes.
  */
public sealed class FactionDbcDataStore
{
    private FactionDbcDataStore()
    {
        Factions = new Dictionary<int, FactionDbcRecord>();
        Templates = new Dictionary<int, FactionTemplateDbcRecord>();
    }

    private FactionDbcDataStore(
        IReadOnlyDictionary<int, FactionDbcRecord> factions,
        IReadOnlyDictionary<int, FactionTemplateDbcRecord> templates)
    {
        Factions = factions;
        Templates = templates;
    }

    public static FactionDbcDataStore Empty { get; } = new();

    /**
      * Gets factions indexed by faction id.
      */
    public IReadOnlyDictionary<int, FactionDbcRecord> Factions { get; }

    /**
      * Gets faction templates indexed by template id.
      */
    public IReadOnlyDictionary<int, FactionTemplateDbcRecord> Templates { get; }

    /**
      * Converts loaded raw DBC stores into typed faction DBC indexes.
      */
    public static FactionDbcDataStore FromDbcStores(IReadOnlyDictionary<string, DbcDataStore> dbcStores, string ownerName)
    {
        ArgumentNullException.ThrowIfNull(dbcStores);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerName);

        Dictionary<int, FactionDbcRecord> factions = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            FactionDbcFileNames.Faction,
            ownerName,
            37,
            ReadFactionRecord,
            record => record.Id);

        Dictionary<int, FactionTemplateDbcRecord> templates = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            FactionDbcFileNames.FactionTemplate,
            ownerName,
            14,
            ReadFactionTemplateRecord,
            record => record.Id);

        FactionDbcDataStore data = new(factions, templates);

        Logger.Write(
            LogType.SUCCESS,
            $"{ownerName} typed faction DBC data loaded: factions={data.Factions.Count}, templates={data.Templates.Count}.",
            nameof(FactionDbcDataStore));

        return data;
    }

    public bool TryGetFaction(int factionId, out FactionDbcRecord faction)
    {
        return Factions.TryGetValue(factionId, out faction!);
    }

    public bool TryGetFactionTemplate(int templateId, out FactionTemplateDbcRecord template)
    {
        return Templates.TryGetValue(templateId, out template!);
    }

    private static FactionDbcRecord ReadFactionRecord(DbcRecord record)
    {
        int[] raceMasks = Enumerable.Range(2, 4).Select(fieldIndex => DbcRecordReader.ReadInt32(record, fieldIndex)).ToArray();
        int[] classMasks = Enumerable.Range(6, 4).Select(fieldIndex => DbcRecordReader.ReadInt32(record, fieldIndex)).ToArray();
        int[] bases = Enumerable.Range(10, 4).Select(fieldIndex => DbcRecordReader.ReadInt32(record, fieldIndex)).ToArray();
        int[] flags = Enumerable.Range(14, 4).Select(fieldIndex => DbcRecordReader.ReadInt32(record, fieldIndex)).ToArray();

        return new FactionDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadInt32(record, 1),
            raceMasks,
            classMasks,
            bases,
            flags,
            DbcRecordReader.ReadInt32(record, 18),
            DbcRecordReader.ReadString(record, 19),
            DbcRecordReader.ReadString(record, 28));
    }

    private static FactionTemplateDbcRecord ReadFactionTemplateRecord(DbcRecord record)
    {
        int[] enemyFactionIds = Enumerable.Range(6, 4)
            .Select(fieldIndex => DbcRecordReader.ReadInt32(record, fieldIndex))
            .Where(value => value > 0)
            .ToArray();

        int[] friendFactionIds = Enumerable.Range(10, 4)
            .Select(fieldIndex => DbcRecordReader.ReadInt32(record, fieldIndex))
            .Where(value => value > 0)
            .ToArray();

        return new FactionTemplateDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadInt32(record, 1),
            DbcRecordReader.ReadInt32(record, 2),
            DbcRecordReader.ReadInt32(record, 3),
            DbcRecordReader.ReadInt32(record, 4),
            DbcRecordReader.ReadInt32(record, 5),
            enemyFactionIds,
            friendFactionIds);
    }
}
