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
// File: src/EmulationServer.Game/Data/Dbc/Chat/ChatChannelDbcDataStore.cs
// Purpose: Contains chat channel DBC data store code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Game.Data.Dbc;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Game.Data.Dbc.Chat;

// Type: ChatChannelDbcDataStore
// Purpose: Provides chat channel DBC data store behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class ChatChannelDbcDataStore
{

    private static readonly HashSet<string> AutoJoinShortcuts = new(StringComparer.OrdinalIgnoreCase)
    {
        "General",
        "LocalDefense",
        "LookingForGroup",
    };

    // Field: Stores the string state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current string backing value maintained by the owning type.
    private readonly Dictionary<string, ChatChannelDbcRecord> _recordsByShortcut;

    // Constructor: ChatChannelDbcDataStore
    // Purpose: Initializes a new ChatChannelDbcDataStore instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - records: Records value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to ChatChannelDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private ChatChannelDbcDataStore(IReadOnlyDictionary<int, ChatChannelDbcRecord> records)
    {
        Records = records;
        _recordsByShortcut = records.Values
            .Where(record => !string.IsNullOrWhiteSpace(record.ShortcutName))
            .GroupBy(record => record.ShortcutName.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    public static ChatChannelDbcDataStore Empty { get; } = new(new Dictionary<int, ChatChannelDbcRecord>());

    // Property: Gets or sets the int value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: int value exposed by the owning type.
    public IReadOnlyDictionary<int, ChatChannelDbcRecord> Records { get; }

    // Method: FromDbcStores
    // Purpose: Executes the from DBC stores operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - dbcStores: Dbc stores value supplied by the caller for this operation.
    // - ownerName: Owner name value supplied by the caller for this operation.
    // Returns: Returns the chat channel DBC data store value produced by this operation.
    // Notes: This keeps the operation scoped to ChatChannelDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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
        Logger.Write(
            LogType.SUCCESS,
            string.Join(Environment.NewLine,
                $"{ownerName}: chat-channel DBC loaded:",
                $"  ChatChannels.dbc: {data.Records.Count}"),
            "ChatChannelDbcDataStore");
        return data;
    }

    // Method: GetAutoJoinChannelNames
    // Purpose: Retrieves get auto join channel names data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - zoneName: Zone name value supplied by the caller for this operation.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to ChatChannelDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ResolveChannelName
    // Purpose: Retrieves resolve channel name data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - requestedName: Requested name value supplied by the caller for this operation.
    // - zoneName: Zone name value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to ChatChannelDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ResolveChannelFlags
    // Purpose: Retrieves resolve channel flags data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - requestedName: Requested name value supplied by the caller for this operation.
    // - zoneName: Zone name value supplied by the caller for this operation.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to ChatChannelDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    public int ResolveChannelFlags(string requestedName, string zoneName)
    {
        string safeRequestedName = string.IsNullOrWhiteSpace(requestedName) ? "General" : requestedName.Trim();
        string safeZoneName = string.IsNullOrWhiteSpace(zoneName) ? "Local" : zoneName.Trim();

        if (_recordsByShortcut.TryGetValue(safeRequestedName, out ChatChannelDbcRecord? shortcutRecord))
        {
            return shortcutRecord.Flags;
        }

        foreach (ChatChannelDbcRecord record in Records.Values)
        {
            string formattedName = FormatChannelName(record, safeZoneName);
            if (string.Equals(formattedName, safeRequestedName, StringComparison.OrdinalIgnoreCase))
            {
                return record.Flags;
            }
        }

        return 0;
    }

    // Method: ReadRecord
    // Purpose: Retrieves read record data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // Returns: Returns the chat channel DBC record value produced by this operation.
    // Notes: This keeps the operation scoped to ChatChannelDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static ChatChannelDbcRecord ReadRecord(DbcRecord record)
    {
        return new ChatChannelDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadInt32(record, 1),
            DbcRecordReader.ReadInt32(record, 2),
            DbcRecordReader.ReadString(record, 3),
            DbcRecordReader.ReadString(record, 12));
    }

    // Method: FormatChannelName
    // Purpose: Executes the format channel name operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - record: Record value supplied by the caller for this operation.
    // - zoneName: Zone name value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to ChatChannelDbcDataStore so callers do not duplicate validation, protocol, or persistence rules.
    private static string FormatChannelName(ChatChannelDbcRecord record, string zoneName)
    {
        string template = string.IsNullOrWhiteSpace(record.Name) ? record.ShortcutName : record.Name;
        string formatted = template.Replace("%s", zoneName, StringComparison.OrdinalIgnoreCase).Trim();
        return string.IsNullOrWhiteSpace(formatted) ? record.ShortcutName : formatted;
    }
}
