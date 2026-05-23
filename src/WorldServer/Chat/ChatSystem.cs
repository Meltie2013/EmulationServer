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
using EmulationServer.WorldServer.Networking.Sessions;
using EmulationServer.WorldServer.Players;

namespace EmulationServer.WorldServer.Chat;

public sealed class ChatSystem
{
    private readonly PlayerSessionRegistry _sessionRegistry;

    public ChatSystem(PlayerSessionRegistry sessionRegistry)
    {
        _sessionRegistry = sessionRegistry ?? throw new ArgumentNullException(nameof(sessionRegistry));
    }

    public IReadOnlyList<WorldClientSession> GetRecipients(WorldClientSession sender, ChatIncomingMessage message)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(message);

        PlayerLoginRecord player = sender.RequireCurrentPlayer();
        string channelName = string.IsNullOrWhiteSpace(message.Target) ? "General" : message.Target.Trim();

        return message.Type == ChatMessageType.Channel
            ? _sessionRegistry.GetSessionsInChannel(channelName, player.Faction)
            : _sessionRegistry.GetSessionsForFaction(player.Faction);
    }

    public static bool IsCommandMessage(ChatIncomingMessage message)
    {
        return !string.IsNullOrWhiteSpace(message.Text) && message.Text[0] == '.';
    }

    public void JoinChannel(WorldClientSession session, string channelName)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (string.IsNullOrWhiteSpace(channelName))
        {
            return;
        }

        PlayerLoginRecord player = session.RequireCurrentPlayer();
        session.JoinChatChannel(channelName.Trim());
        Logger.Write(LogType.NETWORK, $"Player '{player.Name}' joined faction-scoped channel '{channelName}'.", nameof(ChatSystem));
    }

    public void LeaveChannel(WorldClientSession session, string channelName)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (string.IsNullOrWhiteSpace(channelName))
        {
            return;
        }

        PlayerLoginRecord player = session.RequireCurrentPlayer();
        session.LeaveChatChannel(channelName.Trim());
        Logger.Write(LogType.NETWORK, $"Player '{player.Name}' left faction-scoped channel '{channelName}'.", nameof(ChatSystem));
    }
}
