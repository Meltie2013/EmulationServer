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

/**
  * File overview: src/WorldServer/Players/PlayerSessionRegistry.cs
  * Documents the PlayerSessionRegistry source file in the active player session registration and lookup area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.WorldServer.Players;

/**
  * Owns the player session registry behavior for the active player session registration and lookup layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class PlayerSessionRegistry
{
    private readonly ConcurrentDictionary<uint, WorldClientSession> _playersByGuid = new();
    private readonly ConcurrentDictionary<uint, WorldClientSession> _sessionsByAccount = new();

    /**
      * Stores the default active player count value used when the caller does not supply an override.
      * Centralizing the default keeps configuration and packet behavior consistent across the server process.
      */
    public int ActivePlayerCount => _playersByGuid.Count;

    /**
      * Tries to resolve the register value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: player, session.
      */
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

        Logger.Write(LogType.NETWORK, $"Registered in-world player '{player.Name}' ({player.Guid}) for account {player.AccountId}. Active players={ActivePlayerCount}.", "PlayerSessionRegistry");
        return true;
    }

    /**
      * Applies the unregister state transition to the current runtime session.
      * State changes are routed through one method so logging, validation, and side effects stay aligned with the server lifecycle.
      * Inputs used by this operation: player, session.
      */
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

        Logger.Write(LogType.NETWORK, $"Unregistered in-world player '{player.Name}' ({player.Guid}). Active players={ActivePlayerCount}.", "PlayerSessionRegistry");
    }

    /**
      * Performs the snapshot sessions operation for the active player session registration and lookup workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      */
    public IReadOnlyList<WorldClientSession> SnapshotSessions()
    {
        return _playersByGuid.Values
            .Distinct()
            .ToArray();
    }

    /**
      * Enumerates the current in-world sessions without allocating a new array.
      * Hot paths such as movement broadcasting use this to avoid per-packet snapshots.
      */
    public IEnumerable<WorldClientSession> EnumerateSessions()
    {
        return _playersByGuid.Values;
    }

    /**
      * Resolves the sessions for faction value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: faction.
      */
    public IReadOnlyList<WorldClientSession> GetSessionsForFaction(PlayerFaction faction)
    {
        return _playersByGuid.Values
            .Where(session => session.CurrentPlayer?.Faction == faction)
            .Distinct()
            .ToArray();
    }

    /**
      * Resolves the sessions in channel value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: channelName, faction.
      */
    public IReadOnlyList<WorldClientSession> GetSessionsInChannel(string channelName, PlayerFaction faction)
    {
        return _playersByGuid.Values
            .Where(session => session.CurrentPlayer?.Faction == faction && session.IsInChatChannel(channelName))
            .Distinct()
            .ToArray();
    }
}
