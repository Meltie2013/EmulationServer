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
// File: src/EmulationServer.Game/Data/Dbc/Factions/FactionDbcDataStore.cs
// Purpose: Contains faction DBC data store code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Game.Data.Dbc;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Game.Data.Dbc.Factions;

// Type: FactionDbcDataStore
// Purpose: Provides faction DBC data store behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class FactionDbcDataStore
{

    // Constructor: FactionDbcDataStore
    // Purpose: Initializes a new FactionDbcDataStore instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to FactionDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private FactionDbcDataStore()
    {
        Factions = new Dictionary<int, FactionDbcRecord>();
        Templates = new Dictionary<int, FactionTemplateDbcRecord>();
    }

    // Constructor: FactionDbcDataStore
    // Purpose: Initializes a new FactionDbcDataStore instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - factions: Factions value supplied by the caller for this operation.
    // - templates: Templates value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to FactionDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private FactionDbcDataStore(
        IReadOnlyDictionary<int, FactionDbcRecord> factions,
        IReadOnlyDictionary<int, FactionTemplateDbcRecord> templates)
    {
        Factions = factions;
        Templates = templates;
    }

    public static FactionDbcDataStore Empty { get; } = new();

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, FactionDbcRecord> Factions { get; }

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, FactionTemplateDbcRecord> Templates { get; }

    // Method: FromDbcStores
    // Purpose: Executes the from DBC stores operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - dbcStores: Dbc stores value supplied by the caller for this operation.
    // - ownerName: Owner name value supplied by the caller for this operation.
    // Returns: Returns the faction DBC data store value produced by this operation.
    // Notes: This keeps the operation scoped to FactionDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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
            string.Join(Environment.NewLine,
                $"{ownerName}: faction DBC loaded:",
                $"  Faction.dbc: {data.Factions.Count}",
                $"  FactionTemplate.dbc: {data.Templates.Count}"),
            "FactionDbcDataStore");

        return data;
    }

    // Method: TryGetFaction
    // Purpose: Attempts to retrieve or parse try get faction data without treating normal misses as failures.
    // Parameters:
    // - factionId: Faction ID identifier used to select the exact record, object, or runtime owner.
    // - faction: Faction value supplied by the caller for this operation.
    // Returns: Returns true when try get faction succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to FactionDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetFaction(int factionId, out FactionDbcRecord faction)
    {
        return Factions.TryGetValue(factionId, out faction!);
    }

    // Method: TryGetFactionTemplate
    // Purpose: Attempts to retrieve or parse try get faction template data without treating normal misses as failures.
    // Parameters:
    // - templateId: Template ID identifier used to select the exact record, object, or runtime owner.
    // - template: Template value supplied by the caller for this operation.
    // Returns: Returns true when try get faction template succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to FactionDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetFactionTemplate(int templateId, out FactionTemplateDbcRecord template)
    {
        return Templates.TryGetValue(templateId, out template!);
    }

    // Method: ReadFactionRecord
    // Purpose: Retrieves read faction record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the faction DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to FactionDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadFactionTemplateRecord
    // Purpose: Retrieves read faction template record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the faction template DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to FactionDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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
