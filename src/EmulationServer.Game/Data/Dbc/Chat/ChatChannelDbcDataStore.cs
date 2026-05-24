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
  * File overview: src/EmulationServer.Game/Data/Dbc/Chat/ChatChannelDbcDataStore.cs
  * Documents the ChatChannelDbcDataStore source file in the DBC loading and strongly typed client data records area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Data.Dbc.Chat;

/**
  * Owns the chat channel dbc data store behavior for the DBC loading and strongly typed client data records layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class ChatChannelDbcDataStore
{
    /**
      * Stores the default auto join shortcuts value used when the caller does not supply an override.
      * Centralizing the default keeps configuration and packet behavior consistent across the server process.
      */
    private static readonly HashSet<string> AutoJoinShortcuts = new(StringComparer.OrdinalIgnoreCase)
    {
        "General",
        "LocalDefense",
        "LookingForGroup",
    };

    private readonly Dictionary<string, ChatChannelDbcRecord> _recordsByShortcut;

    /**
      * Initializes a new ChatChannelDbcDataStore instance with the dependencies required by the DBC loading and strongly typed client data records workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: records.
      */
    private ChatChannelDbcDataStore(IReadOnlyDictionary<int, ChatChannelDbcRecord> records)
    {
        Records = records;
        _recordsByShortcut = records.Values
            .Where(record => !string.IsNullOrWhiteSpace(record.ShortcutName))
            .GroupBy(record => record.ShortcutName.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    /**
      * Exposes the empty value to callers that need this runtime or configuration data.
      * The property keeps the public surface strongly typed and documents which part of the server workflow owns the value.
      */
    public static ChatChannelDbcDataStore Empty { get; } = new(new Dictionary<int, ChatChannelDbcRecord>());

    public IReadOnlyDictionary<int, ChatChannelDbcRecord> Records { get; }

    /**
      * Performs the from dbc stores operation for the DBC loading and strongly typed client data records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: dbcStores, ownerName.
      */
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
        Logger.Write(LogType.SUCCESS, $"{ownerName} typed chat-channel DBC data loaded: channels={data.Records.Count}.", "ChatChannelDbcDataStore");
        return data;
    }

    /**
      * Resolves the auto join channel names value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: zoneName.
      */
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

    /**
      * Resolves the channel name value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: requestedName, zoneName.
      */
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

    /**
      * Resolves the channel flags value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: requestedName, zoneName.
      */
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

    /**
      * Parses read record input into the strongly typed server representation.
      * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
      * Inputs used by this operation: record.
      */
    private static ChatChannelDbcRecord ReadRecord(DbcRecord record)
    {
        return new ChatChannelDbcRecord(
            DbcRecordReader.ReadInt32(record, 0),
            DbcRecordReader.ReadInt32(record, 1),
            DbcRecordReader.ReadInt32(record, 2),
            DbcRecordReader.ReadString(record, 3),
            DbcRecordReader.ReadString(record, 12));
    }

    /**
      * Performs the format channel name operation for the DBC loading and strongly typed client data records workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: record, zoneName.
      */
    private static string FormatChannelName(ChatChannelDbcRecord record, string zoneName)
    {
        string template = string.IsNullOrWhiteSpace(record.Name) ? record.ShortcutName : record.Name;
        string formatted = template.Replace("%s", zoneName, StringComparison.OrdinalIgnoreCase).Trim();
        return string.IsNullOrWhiteSpace(formatted) ? record.ShortcutName : formatted;
    }
}
