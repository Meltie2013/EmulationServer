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

/**
  * File overview: src/EmulationServer.Game/Maps/Runtime/MapPlayerTracker.cs
  * Documents the MapPlayerTracker source file in the runtime map-player state tracking area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Maps.Runtime;

/**
  * Owns the map player tracker behavior for the runtime map-player state tracking layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class MapPlayerTracker
{
    private readonly ConcurrentDictionary<uint, MapPlayerRuntimeState> _players = new();

    /**
      * Stores the default active player count value used when the caller does not supply an override.
      * Centralizing the default keeps configuration and packet behavior consistent across the server process.
      */
    public int ActivePlayerCount => _players.Count;

    /**
      * Performs the snapshot players operation for the runtime map-player state tracking workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      */
    public IReadOnlyCollection<MapPlayerRuntimeState> SnapshotPlayers()
    {
        return _players.Values.ToArray();
    }

    /**
      * Returns the active player count grouped by map id so hosted map services can publish accurate status snapshots.
      */
    public IReadOnlyDictionary<uint, int> CountPlayersByMap()
    {
        return _players.Values
            .GroupBy(player => player.Map)
            .ToDictionary(group => group.Key, group => group.Count());
    }

    /**
      * Performs the player entered operation for the runtime map-player state tracking workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: player.
      */
    public void PlayerEntered(MapPlayerRuntimeState player)
    {
        ArgumentNullException.ThrowIfNull(player);
        _players[player.Guid] = player;
    }

    /**
      * Performs the player left operation for the runtime map-player state tracking workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: guid.
      */
    public bool PlayerLeft(uint guid)
    {
        return PlayerLeft(guid, out _);
    }

    /**
      * Removes a player and returns the last tracked map state when the caller needs to refresh per-map service counts.
      */
    public bool PlayerLeft(uint guid, out MapPlayerRuntimeState? player)
    {
        return _players.TryRemove(guid, out player);
    }

    /**
      * Performs the player moved operation for the runtime map-player state tracking workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: accountId, guid, map, zone, positionX, positionY....
      */
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
        return PlayerMoved(
            accountId,
            guid,
            map,
            zone,
            positionX,
            positionY,
            positionZ,
            orientation,
            opcode,
            movementFlags,
            clientMovementTime,
            out _,
            out _);
    }

    /**
      * Updates movement state and tells the caller whether hosted service player counts may have changed.
      * Counts only change when a new player is first observed or an existing player changes maps, so movement can stay lightweight.
      */
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
        uint clientMovementTime,
        out uint previousMap,
        out bool serviceCountChanged)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        uint observedPreviousMap = map;
        bool observedExistingPlayer = false;
        bool observedMapChange = false;

        MapPlayerRuntimeState updatedState = _players.AddOrUpdate(
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
            (_, existing) =>
            {
                observedExistingPlayer = true;
                observedPreviousMap = existing.Map;
                observedMapChange = existing.Map != map;

                return existing with
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
                };
            });

        previousMap = observedPreviousMap;
        serviceCountChanged = !observedExistingPlayer || observedMapChange;
        return updatedState;
    }
}
