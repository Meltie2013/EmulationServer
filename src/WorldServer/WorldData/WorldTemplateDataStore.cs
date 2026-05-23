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

namespace EmulationServer.WorldServer.WorldData;

public sealed class WorldTemplateDataStore
{
    public static WorldTemplateDataStore Empty { get; } = new([], []);

    private readonly Dictionary<(byte Race, byte Class), PlayerCreateInfoRecord> _playerCreateInfo;
    private readonly Dictionary<uint, ItemTemplateRecord> _itemTemplates;

    public WorldTemplateDataStore(
        IEnumerable<PlayerCreateInfoRecord> playerCreateInfo,
        IEnumerable<ItemTemplateRecord> itemTemplates)
    {
        ArgumentNullException.ThrowIfNull(playerCreateInfo);
        ArgumentNullException.ThrowIfNull(itemTemplates);

        _playerCreateInfo = playerCreateInfo
            .GroupBy(record => (record.Race, record.Class))
            .ToDictionary(group => group.Key, group => group.First());

        _itemTemplates = itemTemplates
            .GroupBy(record => record.Entry)
            .ToDictionary(group => group.Key, group => group.First());
    }

    public IReadOnlyDictionary<(byte Race, byte Class), PlayerCreateInfoRecord> PlayerCreateInfo => _playerCreateInfo;

    public IReadOnlyDictionary<uint, ItemTemplateRecord> ItemTemplates => _itemTemplates;

    public bool TryGetPlayerCreateInfo(byte race, byte characterClass, out PlayerCreateInfoRecord createInfo)
    {
        return _playerCreateInfo.TryGetValue((race, characterClass), out createInfo!);
    }

    public bool TryGetItemTemplate(uint entry, out ItemTemplateRecord itemTemplate)
    {
        return _itemTemplates.TryGetValue(entry, out itemTemplate!);
    }

    public IReadOnlyDictionary<uint, ItemTemplateRecord> GetItemTemplates(IEnumerable<uint> itemEntries)
    {
        ArgumentNullException.ThrowIfNull(itemEntries);

        Dictionary<uint, ItemTemplateRecord> result = [];
        foreach (uint entry in itemEntries)
        {
            if (entry == 0 || result.ContainsKey(entry))
            {
                continue;
            }

            if (_itemTemplates.TryGetValue(entry, out ItemTemplateRecord? template))
            {
                result[entry] = template;
            }
        }

        return result;
    }
}
