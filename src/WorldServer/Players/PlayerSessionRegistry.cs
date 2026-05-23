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

using System.Collections.Concurrent;

using EmulationServer.Game.Players;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.WorldServer.Networking.Sessions;

namespace EmulationServer.WorldServer.Players;

public sealed class PlayerSessionRegistry
{
    private readonly ConcurrentDictionary<uint, WorldClientSession> _playersByGuid = new();
    private readonly ConcurrentDictionary<uint, WorldClientSession> _sessionsByAccount = new();

    public int ActivePlayerCount => _playersByGuid.Count;

    public bool TryRegister(PlayerLoginRecord player, WorldClientSession session)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(session);

        if (!_sessionsByAccount.TryAdd(player.AccountId, session))
        {
            return false;
        }

        if (!_playersByGuid.TryAdd(player.Guid, session))
        {
            _sessionsByAccount.TryRemove(player.AccountId, out _);
            return false;
        }

        Logger.Write(LogType.NETWORK, $"Registered in-world player '{player.Name}' ({player.Guid}) for account {player.AccountId}. Active players={ActivePlayerCount}.", nameof(PlayerSessionRegistry));
        return true;
    }

    public void Unregister(PlayerLoginRecord? player, WorldClientSession session)
    {
        if (player is null)
        {
            return;
        }

        if (_playersByGuid.TryGetValue(player.Guid, out WorldClientSession? characterSession) && ReferenceEquals(characterSession, session))
        {
            _playersByGuid.TryRemove(player.Guid, out _);
        }

        if (_sessionsByAccount.TryGetValue(player.AccountId, out WorldClientSession? accountSession) && ReferenceEquals(accountSession, session))
        {
            _sessionsByAccount.TryRemove(player.AccountId, out _);
        }

        Logger.Write(LogType.NETWORK, $"Unregistered in-world player '{player.Name}' ({player.Guid}). Active players={ActivePlayerCount}.", nameof(PlayerSessionRegistry));
    }


    public IReadOnlyList<WorldClientSession> SnapshotSessions()
    {
        return _playersByGuid.Values
            .Distinct()
            .ToArray();
    }

    public IReadOnlyList<WorldClientSession> GetSessionsForFaction(PlayerFaction faction)
    {
        return _playersByGuid.Values
            .Where(session => session.CurrentPlayer?.Faction == faction)
            .Distinct()
            .ToArray();
    }

    public IReadOnlyList<WorldClientSession> GetSessionsInChannel(string channelName, PlayerFaction faction)
    {
        return _playersByGuid.Values
            .Where(session => session.CurrentPlayer?.Faction == faction && session.IsInChatChannel(channelName))
            .Distinct()
            .ToArray();
    }
}
