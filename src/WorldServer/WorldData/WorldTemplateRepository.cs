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

using System.Globalization;

using EmulationServer.Database.Interfaces;

using MySqlConnector;

namespace EmulationServer.WorldServer.WorldData;

public sealed class WorldTemplateRepository
{
    private readonly IDatabaseService _databaseService;

    public WorldTemplateRepository(IDatabaseService databaseService)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
    }


    public async Task<WorldTemplateDataStore> LoadAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PlayerCreateInfoRecord> playerCreateInfo = await LoadPlayerCreateInfoAsync(cancellationToken);
        IReadOnlyList<ItemTemplateRecord> itemTemplates = await LoadItemTemplatesAsync(cancellationToken);
        return new WorldTemplateDataStore(playerCreateInfo, itemTemplates);
    }

    public async Task<IReadOnlyList<PlayerCreateInfoRecord>> LoadPlayerCreateInfoAsync(CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT `race`, `class`, `map`, `zone`, `position_x`, `position_y`, `position_z`, `orientation`
            FROM `playercreateinfo`;
            """;

        List<PlayerCreateInfoRecord> records = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new PlayerCreateInfoRecord(
                Convert.ToByte(reader.GetValue(0), CultureInfo.InvariantCulture),
                Convert.ToByte(reader.GetValue(1), CultureInfo.InvariantCulture),
                Convert.ToUInt16(reader.GetValue(2), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(3), CultureInfo.InvariantCulture),
                Convert.ToSingle(reader.GetValue(4), CultureInfo.InvariantCulture),
                Convert.ToSingle(reader.GetValue(5), CultureInfo.InvariantCulture),
                Convert.ToSingle(reader.GetValue(6), CultureInfo.InvariantCulture),
                Convert.ToSingle(reader.GetValue(7), CultureInfo.InvariantCulture)));
        }

        return records;
    }

    public async Task<IReadOnlyList<ItemTemplateRecord>> LoadItemTemplatesAsync(CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT `entry`, `class`, `subclass`, `name`, `displayid`, `Flags`, `InventoryType`, `MaxDurability`
            FROM `item_template`;
            """;

        List<ItemTemplateRecord> records = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new ItemTemplateRecord(
                Convert.ToUInt32(reader.GetValue(0), CultureInfo.InvariantCulture),
                Convert.ToByte(reader.GetValue(1), CultureInfo.InvariantCulture),
                Convert.ToByte(reader.GetValue(2), CultureInfo.InvariantCulture),
                reader.GetString(3),
                Convert.ToUInt32(reader.GetValue(4), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(5), CultureInfo.InvariantCulture),
                Convert.ToByte(reader.GetValue(6), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(7), CultureInfo.InvariantCulture)));
        }

        return records;
    }

    public async Task<PlayerCreateInfoRecord?> GetPlayerCreateInfoAsync(byte race, byte characterClass, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT `race`, `class`, `map`, `zone`, `position_x`, `position_y`, `position_z`, `orientation`
            FROM `playercreateinfo`
            WHERE `race` = @race AND `class` = @class
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@race", race);
        command.Parameters.AddWithValue("@class", characterClass);

        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new PlayerCreateInfoRecord(
            Convert.ToByte(reader.GetValue(0), CultureInfo.InvariantCulture),
            Convert.ToByte(reader.GetValue(1), CultureInfo.InvariantCulture),
            Convert.ToUInt16(reader.GetValue(2), CultureInfo.InvariantCulture),
            Convert.ToUInt32(reader.GetValue(3), CultureInfo.InvariantCulture),
            Convert.ToSingle(reader.GetValue(4), CultureInfo.InvariantCulture),
            Convert.ToSingle(reader.GetValue(5), CultureInfo.InvariantCulture),
            Convert.ToSingle(reader.GetValue(6), CultureInfo.InvariantCulture),
            Convert.ToSingle(reader.GetValue(7), CultureInfo.InvariantCulture));
    }

    public async Task<IReadOnlyDictionary<uint, ItemTemplateRecord>> GetItemTemplatesAsync(IEnumerable<uint> itemEntries, CancellationToken cancellationToken = default)
    {
        uint[] entries = itemEntries.Where(entry => entry != 0).Distinct().ToArray();
        if (entries.Length == 0)
        {
            return new Dictionary<uint, ItemTemplateRecord>();
        }

        await using MySqlConnection connection = await _databaseService.CreateConnectionAsync(cancellationToken);
        using MySqlCommand command = connection.CreateCommand();

        List<string> parameterNames = [];
        for (int index = 0; index < entries.Length; index++)
        {
            string parameterName = $"@entry{index}";
            parameterNames.Add(parameterName);
            command.Parameters.AddWithValue(parameterName, entries[index]);
        }

        command.CommandText = $"""
            SELECT `entry`, `class`, `subclass`, `name`, `displayid`, `Flags`, `InventoryType`, `MaxDurability`
            FROM `item_template`
            WHERE `entry` IN ({string.Join(',', parameterNames)});
            """;

        Dictionary<uint, ItemTemplateRecord> result = [];
        await using MySqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            ItemTemplateRecord record = new(
                Convert.ToUInt32(reader.GetValue(0), CultureInfo.InvariantCulture),
                Convert.ToByte(reader.GetValue(1), CultureInfo.InvariantCulture),
                Convert.ToByte(reader.GetValue(2), CultureInfo.InvariantCulture),
                reader.GetString(3),
                Convert.ToUInt32(reader.GetValue(4), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(5), CultureInfo.InvariantCulture),
                Convert.ToByte(reader.GetValue(6), CultureInfo.InvariantCulture),
                Convert.ToUInt32(reader.GetValue(7), CultureInfo.InvariantCulture));

            result[record.Entry] = record;
        }

        return result;
    }
}
