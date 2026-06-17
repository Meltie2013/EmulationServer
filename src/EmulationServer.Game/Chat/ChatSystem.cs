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
// File: src/EmulationServer.Game/Chat/ChatSystem.cs
// Purpose: Contains chat system code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Game.Data.Dbc.Maps;
using EmulationServer.Game.Data.Stores;
using EmulationServer.Game.Players;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Game.Chat;

// Type: ChatSystem
// Purpose: Provides chat system behavior for the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - gameDataAccessor: Game data accessor value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class ChatSystem(Func<WorldGameDataStore>? gameDataAccessor = null)
{

    // Property: Gets or sets the default channels value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: default channels value exposed by the owning type.
    public static IReadOnlyList<string> DefaultChannels { get; } =
    [
        "General",
        "LocalDefense",
        "LookingForGroup",
    ];

    // Method: gameDataAccessor
    // Purpose: Executes the game data accessor operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the func game data accessor = value produced by this operation.
    // Notes: This keeps the operation scoped to ChatSystem so callers do not duplicate validation, protocol, or persistence rules.
    private readonly Func<WorldGameDataStore> _gameDataAccessor = gameDataAccessor ?? (() => WorldGameDataStore.Empty);

    // Method: GetDefaultChannelNames
    // Purpose: Retrieves get default channel names data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to ChatSystem so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyList<string> GetDefaultChannelNames(PlayerLoginRecord player)
    {
        ArgumentNullException.ThrowIfNull(player);

        WorldGameDataStore gameData = _gameDataAccessor();
        string zoneName = ResolveZoneName(gameData, player);
        IReadOnlyList<string> dbcChannels = gameData.ChatData.GetAutoJoinChannelNames(zoneName);
        return dbcChannels.Count == 0 ? DefaultChannels : dbcChannels;
    }

    // Method: ResolveChannelName
    // Purpose: Retrieves resolve channel name data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - channelName: Channel name value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to ChatSystem so callers do not duplicate validation, protocol, or persistence rules.
    public string ResolveChannelName(PlayerLoginRecord player, string channelName)
    {
        ArgumentNullException.ThrowIfNull(player);

        WorldGameDataStore gameData = _gameDataAccessor();
        string zoneName = ResolveZoneName(gameData, player);
        return NormalizeChannelName(gameData.ChatData.ResolveChannelName(channelName, zoneName));
    }

    // Method: NormalizeIncomingMessage
    // Purpose: Converts incoming data into normalize incoming message form for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - message: Message value supplied by the caller for this operation.
    // Returns: Returns the chat incoming message value produced by this operation.
    // Notes: This keeps the operation scoped to ChatSystem so callers do not duplicate validation, protocol, or persistence rules.
    public ChatIncomingMessage NormalizeIncomingMessage(PlayerLoginRecord player, ChatIncomingMessage message)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(message);

        ChatMessageType messageType = IsAllowedClientChatType(message.Type)
            ? message.Type
            : ChatMessageType.Say;

        ChatLanguage language = ResolveLanguageForPlayer(player, message.Language);
        string target = message.Target.Trim();
        string text = message.Text.Trim();

        if (messageType == ChatMessageType.Channel)
        {
            target = ResolveChannelName(player, target);
        }

        return message with
        {
            Type = messageType,
            Language = language,
            Target = target,
            Text = text,
        };
    }

    // Method: ResolveLanguageForPlayer
    // Purpose: Retrieves resolve language for player data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - requestedLanguage: Requested language value supplied by the caller for this operation.
    // Returns: Returns the chat language value produced by this operation.
    // Notes: This keeps the operation scoped to ChatSystem so callers do not duplicate validation, protocol, or persistence rules.
    public ChatLanguage ResolveLanguageForPlayer(PlayerLoginRecord player, ChatLanguage requestedLanguage)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (requestedLanguage == ChatLanguage.Universal)
        {
            return GetDefaultLanguage(player);
        }

        WorldGameDataStore gameData = _gameDataAccessor();
        if (!gameData.LanguageData.IsKnownLanguage(unchecked((int)requestedLanguage)))
        {
            Logger.Write(
                LogType.WARNING,
                $"Player '{player.Name}' attempted to chat with unknown language {(uint)requestedLanguage}; falling back to default faction language.",
                "ChatSystem");

            return GetDefaultLanguage(player);
        }

        if (LanguageKnowledgeSystem.PlayerKnowsLanguage(player, requestedLanguage))
        {
            return requestedLanguage;
        }

        Logger.Write(
            LogType.WARNING,
            $"Player '{player.Name}' attempted to chat with unlearned language {(uint)requestedLanguage}; falling back to default faction language.",
            "ChatSystem");

        return GetDefaultLanguage(player);
    }

    // Method: GetDefaultLanguage
    // Purpose: Retrieves get default language data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // Returns: Returns the chat language value produced by this operation.
    // Notes: This keeps the operation scoped to ChatSystem so callers do not duplicate validation, protocol, or persistence rules.
    public static ChatLanguage GetDefaultLanguage(PlayerLoginRecord player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return LanguageKnowledgeSystem.GetDefaultLanguage(player.Faction);
    }

    // Method: ResolveChannelFlags
    // Purpose: Retrieves resolve channel flags data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - channelName: Channel name value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to ChatSystem so callers do not duplicate validation, protocol, or persistence rules.
    public uint ResolveChannelFlags(PlayerLoginRecord player, string channelName)
    {
        ArgumentNullException.ThrowIfNull(player);

        WorldGameDataStore gameData = _gameDataAccessor();
        string zoneName = ResolveZoneName(gameData, player);
        int flags = gameData.ChatData.ResolveChannelFlags(channelName, zoneName);
        return unchecked((uint)flags);
    }

    // Method: ResolveChannelPlayerRank
    // Purpose: Retrieves resolve channel player rank data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to ChatSystem so callers do not duplicate validation, protocol, or persistence rules.
    public static uint ResolveChannelPlayerRank(PlayerLoginRecord player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return 0;
    }

    // Method: IsAllowedClientChatType
    // Purpose: Validates or evaluates is allowed client chat type rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - messageType: Message type value supplied by the caller for this operation.
    // Returns: Returns true when is allowed client chat type succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to ChatSystem so callers do not duplicate validation, protocol, or persistence rules.
    private static bool IsAllowedClientChatType(ChatMessageType messageType)
    {
        return messageType is
            ChatMessageType.Say or
            ChatMessageType.Party or
            ChatMessageType.Raid or
            ChatMessageType.Guild or
            ChatMessageType.Officer or
            ChatMessageType.Yell or
            ChatMessageType.Whisper or
            ChatMessageType.Emote or
            ChatMessageType.Channel;
    }

    // Method: GetRecipients
    // Purpose: Retrieves get recipients data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - sender: Sender value supplied by the caller for this operation.
    // - message: Message value supplied by the caller for this operation.
    // - availableSessions: Available sessions value supplied by the caller for this operation.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to ChatSystem so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyList<IChatSession> GetRecipients(
        IChatSession sender,
        ChatIncomingMessage message,
        IEnumerable<IChatSession> availableSessions)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(availableSessions);

        PlayerLoginRecord player = sender.RequireCurrentPlayer();
        string channelName = ResolveChannelName(player, message.Target);

        return message.Type switch
        {
            ChatMessageType.Channel => [.. availableSessions
                .Where(session => session.CurrentPlayer?.Faction == player.Faction && session.IsInChatChannel(channelName))
                .Distinct()],

            ChatMessageType.Whisper => [.. availableSessions
                .Where(session => session.CurrentPlayer?.Faction == player.Faction && string.Equals(session.CurrentPlayer?.Name, message.Target, StringComparison.OrdinalIgnoreCase))
                .Distinct()],

            _ => availableSessions
                .Where(session => session.CurrentPlayer?.Faction == player.Faction)
                .Distinct()
                .ToArray(),
        };
    }

    // Method: IsCommandMessage
    // Purpose: Validates or evaluates is command message rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - message: Message value supplied by the caller for this operation.
    // Returns: Returns true when is command message succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to ChatSystem so callers do not duplicate validation, protocol, or persistence rules.
    public static bool IsCommandMessage(ChatIncomingMessage message)
    {
        return !string.IsNullOrWhiteSpace(message.Text) && message.Text[0] == '.';
    }

    // Method: NormalizeChannelName
    // Purpose: Converts incoming data into normalize channel name form for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - channelName: Channel name value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to ChatSystem so callers do not duplicate validation, protocol, or persistence rules.
    public static string NormalizeChannelName(string channelName)
    {
        return string.IsNullOrWhiteSpace(channelName) ? "General" : channelName.Trim();
    }

    // Method: JoinChannel
    // Purpose: Executes the join channel operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - session: Session value supplied by the caller for this operation.
    // - channelName: Channel name value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to ChatSystem so callers do not duplicate validation, protocol, or persistence rules.
    public void JoinChannel(IChatSession session, string channelName)
    {
        ArgumentNullException.ThrowIfNull(session);

        PlayerLoginRecord player = session.RequireCurrentPlayer();
        string normalized = ResolveChannelName(player, channelName);
        session.JoinChatChannel(normalized);
        Logger.Write(LogType.SYSTEM, $"Player '{player.Name}' joined faction-scoped channel '{normalized}'.", "ChatSystem");
    }

    // Method: LeaveChannel
    // Purpose: Executes the leave channel operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - session: Session value supplied by the caller for this operation.
    // - channelName: Channel name value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to ChatSystem so callers do not duplicate validation, protocol, or persistence rules.
    public void LeaveChannel(IChatSession session, string channelName)
    {
        ArgumentNullException.ThrowIfNull(session);

        PlayerLoginRecord player = session.RequireCurrentPlayer();
        string normalized = ResolveChannelName(player, channelName);
        session.LeaveChatChannel(normalized);
        Logger.Write(LogType.SYSTEM, $"Player '{player.Name}' left faction-scoped channel '{normalized}'.", "ChatSystem");
    }

    // Method: ResolveZoneName
    // Purpose: Retrieves resolve zone name data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - gameData: Game data value supplied by the caller for this operation.
    // - player: Player value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to ChatSystem so callers do not duplicate validation, protocol, or persistence rules.
    private static string ResolveZoneName(WorldGameDataStore gameData, PlayerLoginRecord player)
    {
        if (gameData.MapData.Areas.TryGetValue(unchecked((int)player.Zone), out AreaTableDbcRecord? area))
        {
            if (area.ParentAreaTableId != 0 && gameData.MapData.Areas.TryGetValue(area.ParentAreaTableId, out AreaTableDbcRecord? parentArea))
            {
                return string.IsNullOrWhiteSpace(parentArea.Name) ? area.Name : parentArea.Name;
            }

            if (!string.IsNullOrWhiteSpace(area.Name))
            {
                return area.Name;
            }
        }

        return player.Zone == 0 ? "Local" : $"Area {player.Zone}";
    }
}
