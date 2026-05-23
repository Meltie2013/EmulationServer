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

public sealed class ChatChannelDbcDataStore
{
    private static readonly HashSet<string> AutoJoinShortcuts = new(StringComparer.OrdinalIgnoreCase)
    {
        "General",
        "LocalDefense",
        "LookingForGroup",
    };

    private readonly Dictionary<string, ChatChannelDbcRecord> _recordsByShortcut;

    private ChatChannelDbcDataStore(IReadOnlyDictionary<int, ChatChannelDbcRecord> records)
    {
        Records = records;
        _recordsByShortcut = records.Values
            .Where(record => !string.IsNullOrWhiteSpace(record.ShortcutName))
            .GroupBy(record => record.ShortcutName.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    public static ChatChannelDbcDataStore Empty { get; } = new(new Dictionary<int, ChatChannelDbcRecord>());

    public IReadOnlyDictionary<int, ChatChannelDbcRecord> Records { get; }

    public static ChatChannelDbcDataStore FromDbcStores(IReadOnlyDictionary<string, DbcDataStore> dbcStores, string ownerName)
    {
        ArgumentNullException.ThrowIfNull(dbcStores);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerName);

        Dictionary<int, ChatChannelDbcRecord> channels = DbcTypedRecordLoader.LoadIndexed(
            dbcStores,
            ChatDbcFileNames.ChatChannels,
            ownerName,
            21,
            ReadRecord,
            record => record.Id);

        ChatChannelDbcDataStore data = new(channels);
        Logger.Write(LogType.SUCCESS, $"{ownerName} typed chat-channel DBC data loaded: channels={data.Records.Count}.", nameof(ChatChannelDbcDataStore));
        return data;
    }

    public IReadOnlyList<string> GetAutoJoinChannelNames(string zoneName)
    {
        string safeZoneName = string.IsNullOrWhiteSpace(zoneName) ? "Local" : zoneName.Trim();
        List<string> channelNames = [];

        foreach (string shortcut in AutoJoinShortcuts)
        {
            if (_recordsByShortcut.TryGetValue(shortcut, out ChatChannelDbcRecord? record))
            {
                channelNames.Add(FormatChannelName(record, safeZoneName));
            }
        }

        if (channelNames.Count > 0)
        {
            return channelNames;
        }

        return
        [
            $"General - {safeZoneName}",
            $"LocalDefense - {safeZoneName}",
            "LookingForGroup",
        ];
    }

    public string ResolveChannelName(string requestedName, string zoneName)
    {
        string safeRequestedName = string.IsNullOrWhiteSpace(requestedName) ? "General" : requestedName.Trim();
        string safeZoneName = string.IsNullOrWhiteSpace(zoneName) ? "Local" : zoneName.Trim();

        if (_recordsByShortcut.TryGetValue(safeRequestedName, out ChatChannelDbcRecord? record))
        {
            return FormatChannelName(record, safeZoneName);
        }

        return safeRequestedName.Replace("%s", safeZoneName, StringComparison.OrdinalIgnoreCase);
    }

    private static ChatChannelDbcRecord ReadRecord(DbcRecord record)
    {
        return new ChatChannelDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadInt32(record, 1),
            DbcRecordReader.ReadInt32(record, 2),
            DbcRecordReader.ReadString(record, 3),
            DbcRecordReader.ReadString(record, 12));
    }

    private static string FormatChannelName(ChatChannelDbcRecord record, string zoneName)
    {
        string template = string.IsNullOrWhiteSpace(record.Name) ? record.ShortcutName : record.Name;
        string formatted = template.Replace("%s", zoneName, StringComparison.OrdinalIgnoreCase).Trim();
        return string.IsNullOrWhiteSpace(formatted) ? record.ShortcutName : formatted;
    }
}
