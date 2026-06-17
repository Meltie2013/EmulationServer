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
// File: src/WorldServer/Players/PlayerSessionRegistry.cs
// Purpose: Contains player session registry code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Collections.Concurrent;

using EmulationServer.Game.Players;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.WorldServer.Networking.Sessions;

namespace EmulationServer.WorldServer.Players;

// Type: PlayerSessionRegistry
// Purpose: Provides player session registry behavior for the world server gameplay, session, and character runtime layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class PlayerSessionRegistry
{
    private readonly ConcurrentDictionary<uint, WorldClientSession> _playersByGuid = new();
    private readonly ConcurrentDictionary<uint, WorldClientSession> _sessionsByAccount = new();

    // Property: Gets or sets the active player count value used by the world server gameplay, session, and character runtime layer.
    // Value: active player count value exposed by the owning type.
    public int ActivePlayerCount => _playersByGuid.Count;

    // Method: TryRegister
    // Purpose: Executes the try register operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - session: Session value supplied by the caller for this operation.
    // Returns: Returns true when try register succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to PlayerSessionRegistry so callers do not duplicate validation, protocol, or persistence rules.
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

        Logger.Write(LogType.SYSTEM, $"Registered in-world player '{player.Name}' ({player.Guid}) for account {player.AccountId}. Active players={ActivePlayerCount}.", "PlayerSessionRegistry");
        return true;
    }

    // Method: Unregister
    // Purpose: Executes the unregister operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - session: Session value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to PlayerSessionRegistry so callers do not duplicate validation, protocol, or persistence rules.
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

        Logger.Write(LogType.SYSTEM, $"Unregistered in-world player '{player.Name}' ({player.Guid}). Active players={ActivePlayerCount}.", "PlayerSessionRegistry");
    }

    // Method: SnapshotSessions
    // Purpose: Executes the snapshot sessions operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to PlayerSessionRegistry so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyList<WorldClientSession> SnapshotSessions()
    {
        return _playersByGuid.Values
            .Distinct()
            .ToArray();
    }

    // Method: EnumerateSessions
    // Purpose: Executes the enumerate sessions operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the I enumerable value produced by this operation.
    // Notes: This keeps the operation scoped to PlayerSessionRegistry so callers do not duplicate validation, protocol, or persistence rules.
    public IEnumerable<WorldClientSession> EnumerateSessions()
    {
        return _playersByGuid.Values;
    }

    // Method: GetSessionsForFaction
    // Purpose: Retrieves get sessions for faction data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - faction: Faction value supplied by the caller for this operation.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to PlayerSessionRegistry so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyList<WorldClientSession> GetSessionsForFaction(PlayerFaction faction)
    {
        return _playersByGuid.Values
            .Where(session => session.CurrentPlayer?.Faction == faction)
            .Distinct()
            .ToArray();
    }

    // Method: GetSessionsInChannel
    // Purpose: Retrieves get sessions in channel data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - channelName: Channel name value supplied by the caller for this operation.
    // - faction: Faction value supplied by the caller for this operation.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to PlayerSessionRegistry so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyList<WorldClientSession> GetSessionsInChannel(string channelName, PlayerFaction faction)
    {
        return _playersByGuid.Values
            .Where(session => session.CurrentPlayer?.Faction == faction && session.IsInChatChannel(channelName))
            .Distinct()
            .ToArray();
    }
}
