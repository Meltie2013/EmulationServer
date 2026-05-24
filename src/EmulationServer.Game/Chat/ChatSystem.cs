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

using EmulationServer.Game.Data.Dbc.Maps;
using EmulationServer.Game.Data.Stores;
using EmulationServer.Game.Players;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

/**
 * File overview: src/EmulationServer.Game/Chat/ChatSystem.cs
 * Documents the ChatSystem source file in the chat channel normalization, language handling, and message routing area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Game.Chat;

/**
 * Owns the chat system behavior for the chat channel normalization, language handling, and message routing layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public sealed class ChatSystem
{
    /**
     * Exposes the default channels value to callers that need this runtime or configuration data.
     * The property keeps the public surface strongly typed and documents which part of the server workflow owns the value.
     */
    public static IReadOnlyList<string> DefaultChannels { get; } =
    [
        "General",
        "LocalDefense",
        "LookingForGroup",
    ];

    /**
     * Holds the private game data accessor state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly Func<WorldGameDataStore> _gameDataAccessor;

    /**
     * Initializes a new ChatSystem instance with the dependencies required by the chat channel normalization, language handling, and message routing workflow.
     * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
     * Inputs used by this operation: gameDataAccessor.
     */
    public ChatSystem(Func<WorldGameDataStore>? gameDataAccessor = null)
    {
        _gameDataAccessor = gameDataAccessor ?? (() => WorldGameDataStore.Empty);
    }

    /**
     * Resolves the default channel names value requested by the caller.
     * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
     * Inputs used by this operation: player.
     */
    public IReadOnlyList<string> GetDefaultChannelNames(PlayerLoginRecord player)
    {
        ArgumentNullException.ThrowIfNull(player);

        WorldGameDataStore gameData = _gameDataAccessor();
        string zoneName = ResolveZoneName(gameData, player);
        IReadOnlyList<string> dbcChannels = gameData.ChatData.GetAutoJoinChannelNames(zoneName);
        return dbcChannels.Count == 0 ? DefaultChannels : dbcChannels;
    }

    /**
     * Resolves the channel name value requested by the caller.
     * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
     * Inputs used by this operation: player, channelName.
     */
    public string ResolveChannelName(PlayerLoginRecord player, string channelName)
    {
        ArgumentNullException.ThrowIfNull(player);

        WorldGameDataStore gameData = _gameDataAccessor();
        string zoneName = ResolveZoneName(gameData, player);
        return NormalizeChannelName(gameData.ChatData.ResolveChannelName(channelName, zoneName));
    }

    /**
     * Normalizes the incoming message for the chat channel normalization, language handling, and message routing workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: player, message.
     */
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

    /**
     * Resolves the language for player value requested by the caller.
     * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
     * Inputs used by this operation: player, requestedLanguage.
     */
    public ChatLanguage ResolveLanguageForPlayer(PlayerLoginRecord player, ChatLanguage requestedLanguage)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (requestedLanguage == ChatLanguage.Universal)
        {
            return GetDefaultLanguage(player);
        }

        WorldGameDataStore gameData = _gameDataAccessor();
        if (gameData.LanguageData.IsKnownLanguage(unchecked((int)requestedLanguage)))
        {
            return requestedLanguage;
        }

        Logger.Write(
            LogType.WARNING,
            $"Player '{player.Name}' attempted to chat with unknown language {(uint)requestedLanguage}; falling back to default faction language.",
            nameof(ChatSystem));

        return GetDefaultLanguage(player);
    }

    /**
     * Resolves the default language value requested by the caller.
     * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
     * Inputs used by this operation: player.
     */
    public ChatLanguage GetDefaultLanguage(PlayerLoginRecord player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return player.Faction == PlayerFaction.Alliance
            ? ChatLanguage.Common
            : ChatLanguage.Orcish;
    }

    /**
     * Resolves the channel flags value requested by the caller.
     * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
     * Inputs used by this operation: player, channelName.
     */
    public uint ResolveChannelFlags(PlayerLoginRecord player, string channelName)
    {
        ArgumentNullException.ThrowIfNull(player);

        WorldGameDataStore gameData = _gameDataAccessor();
        string zoneName = ResolveZoneName(gameData, player);
        int flags = gameData.ChatData.ResolveChannelFlags(channelName, zoneName);
        return unchecked((uint)flags);
    }

    /**
     * Resolves the channel player rank value requested by the caller.
     * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
     * Inputs used by this operation: player.
     */
    public uint ResolveChannelPlayerRank(PlayerLoginRecord player)
    {
        ArgumentNullException.ThrowIfNull(player);

        // MaNGOS sends the player's channel rank before the sender GUID for CHAT_MSG_CHANNEL.
        // Rank management is not implemented yet, so every joined player is a normal member.
        return 0;
    }

    /**
     * Determines whether allowed client chat type for the chat channel normalization, language handling, and message routing workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: messageType.
     */
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

    /**
     * Resolves the recipients value requested by the caller.
     * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
     * Inputs used by this operation: sender, message, availableSessions.
     */
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
            ChatMessageType.Channel => availableSessions
                .Where(session => session.CurrentPlayer?.Faction == player.Faction)
                .Where(session => session.IsInChatChannel(channelName))
                .Distinct()
                .ToArray(),

            ChatMessageType.Whisper => availableSessions
                .Where(session => session.CurrentPlayer?.Faction == player.Faction)
                .Where(session => string.Equals(session.CurrentPlayer?.Name, message.Target, StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .ToArray(),

            _ => availableSessions
                .Where(session => session.CurrentPlayer?.Faction == player.Faction)
                .Distinct()
                .ToArray(),
        };
    }

    /**
     * Determines whether command message for the chat channel normalization, language handling, and message routing workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: message.
     */
    public static bool IsCommandMessage(ChatIncomingMessage message)
    {
        return !string.IsNullOrWhiteSpace(message.Text) && message.Text[0] == '.';
    }

    /**
     * Normalizes the channel name for the chat channel normalization, language handling, and message routing workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: channelName.
     */
    public static string NormalizeChannelName(string channelName)
    {
        return string.IsNullOrWhiteSpace(channelName) ? "General" : channelName.Trim();
    }

    /**
     * Applies the join channel state transition to the current runtime session.
     * State changes are routed through one method so logging, validation, and side effects stay aligned with the server lifecycle.
     * Inputs used by this operation: session, channelName.
     */
    public void JoinChannel(IChatSession session, string channelName)
    {
        ArgumentNullException.ThrowIfNull(session);

        PlayerLoginRecord player = session.RequireCurrentPlayer();
        string normalized = ResolveChannelName(player, channelName);
        session.JoinChatChannel(normalized);
        Logger.Write(LogType.NETWORK, $"Player '{player.Name}' joined faction-scoped channel '{normalized}'.", nameof(ChatSystem));
    }

    /**
     * Applies the leave channel state transition to the current runtime session.
     * State changes are routed through one method so logging, validation, and side effects stay aligned with the server lifecycle.
     * Inputs used by this operation: session, channelName.
     */
    public void LeaveChannel(IChatSession session, string channelName)
    {
        ArgumentNullException.ThrowIfNull(session);

        PlayerLoginRecord player = session.RequireCurrentPlayer();
        string normalized = ResolveChannelName(player, channelName);
        session.LeaveChatChannel(normalized);
        Logger.Write(LogType.NETWORK, $"Player '{player.Name}' left faction-scoped channel '{normalized}'.", nameof(ChatSystem));
    }

    /**
     * Resolves the zone name value requested by the caller.
     * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
     * Inputs used by this operation: gameData, player.
     */
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
