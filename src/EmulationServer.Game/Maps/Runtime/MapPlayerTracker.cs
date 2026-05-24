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

namespace EmulationServer.Game.Maps.Runtime;

public sealed class MapPlayerTracker
{
    private readonly ConcurrentDictionary<uint, MapPlayerRuntimeState> _players = new();

    public int ActivePlayerCount => _players.Count;

    public IReadOnlyCollection<MapPlayerRuntimeState> SnapshotPlayers()
    {
        return _players.Values.ToArray();
    }

    public void PlayerEntered(MapPlayerRuntimeState player)
    {
        ArgumentNullException.ThrowIfNull(player);
        _players[player.Guid] = player;
    }

    public bool PlayerLeft(uint guid)
    {
        return _players.TryRemove(guid, out _);
    }

    public MapPlayerRuntimeState PlayerMoved(
        uint accountId,
        uint guid,
        uint map,
        uint zone,
        float positionX,
        float positionY,
        float positionZ,
        float orientation,
        ushort opcode,
        uint movementFlags,
        uint clientMovementTime)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return _players.AddOrUpdate(
            guid,
            _ => new MapPlayerRuntimeState(
                accountId,
                guid,
                string.Empty,
                map,
                zone,
                positionX,
                positionY,
                positionZ,
                orientation,
                opcode,
                movementFlags,
                clientMovementTime,
                now),
            (_, existing) => existing with
            {
                AccountId = accountId,
                Map = map,
                Zone = zone,
                PositionX = positionX,
                PositionY = positionY,
                PositionZ = positionZ,
                Orientation = orientation,
                LastMovementOpcode = opcode,
                MovementFlags = movementFlags,
                ClientMovementTime = clientMovementTime,
                LastUpdatedUtc = now,
            });
    }
}
