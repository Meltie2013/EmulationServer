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
// File: src/EmulationServer.Game/Maps/Runtime/MapPlayerTracker.cs
// Purpose: Contains map player tracker code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Collections.Concurrent;

namespace EmulationServer.Game.Maps.Runtime;

// Type: MapPlayerTracker
// Purpose: Provides map player tracker behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class MapPlayerTracker
{
    private readonly ConcurrentDictionary<uint, MapPlayerRuntimeState> _players = new();

    // Property: Gets or sets the active player count value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: active player count value exposed by the owning type.
    public int ActivePlayerCount => _players.Count;

    // Method: SnapshotPlayers
    // Purpose: Executes the snapshot players operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the I read only collection value produced by this operation.
    // Notes: This keeps the operation scoped to MapPlayerTracker so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyCollection<MapPlayerRuntimeState> SnapshotPlayers()
    {
        return [.. _players.Values];
    }

    // Method: CountPlayersByMap
    // Purpose: Calculates count players by map values for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the I read only dictionary value produced by this operation.
    // Notes: This keeps the operation scoped to MapPlayerTracker so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyDictionary<uint, int> CountPlayersByMap()
    {
        return _players.Values
            .GroupBy(player => player.Map)
            .ToDictionary(group => group.Key, group => group.Count());
    }

    // Method: PlayerEntered
    // Purpose: Executes the player entered operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapPlayerTracker so callers do not duplicate validation, protocol, or persistence rules.
    public void PlayerEntered(MapPlayerRuntimeState player)
    {
        ArgumentNullException.ThrowIfNull(player);
        _players[player.Guid] = player;
    }

    // Method: TryGetPlayer
    // Purpose: Attempts to retrieve or parse try get player data without treating normal misses as failures.
    // Parameters:
    // - guid: Guid identifier used to select the exact record, object, or runtime owner.
    // - player: Player value supplied by the caller for this operation.
    // Returns: Returns true when try get player succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to MapPlayerTracker so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetPlayer(uint guid, out MapPlayerRuntimeState? player)
    {
        return _players.TryGetValue(guid, out player);
    }

    // Method: PlayerLeft
    // Purpose: Executes the player left operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - guid: Guid identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns true when player left succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to MapPlayerTracker so callers do not duplicate validation, protocol, or persistence rules.
    public bool PlayerLeft(uint guid)
    {
        return PlayerLeft(guid, out _);
    }

    // Method: PlayerLeft
    // Purpose: Executes the player left operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - guid: Guid identifier used to select the exact record, object, or runtime owner.
    // - player: Player value supplied by the caller for this operation.
    // Returns: Returns true when player left succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to MapPlayerTracker so callers do not duplicate validation, protocol, or persistence rules.
    public bool PlayerLeft(uint guid, out MapPlayerRuntimeState? player)
    {
        return _players.TryRemove(guid, out player);
    }

    // Method: PlayerMoved
    // Purpose: Executes the player moved operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - accountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // - guid: Guid identifier used to select the exact record, object, or runtime owner.
    // - map: Map value supplied by the caller for this operation.
    // - zone: Zone value supplied by the caller for this operation.
    // - positionX: Position X value supplied by the caller for this operation.
    // - positionY: Position Y value supplied by the caller for this operation.
    // - positionZ: Position Z value supplied by the caller for this operation.
    // - orientation: Orientation value supplied by the caller for this operation.
    // - opcode: Opcode value supplied by the caller for this operation.
    // - movementFlags: Movement flags value supplied by the caller for this operation.
    // - clientMovementTime: Client movement time value supplied by the caller for this operation.
    // Returns: Returns the map player runtime state value produced by this operation.
    // Notes: This keeps the operation scoped to MapPlayerTracker so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: PlayerMoved
    // Purpose: Executes the player moved operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - accountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // - guid: Guid identifier used to select the exact record, object, or runtime owner.
    // - map: Map value supplied by the caller for this operation.
    // - zone: Zone value supplied by the caller for this operation.
    // - positionX: Position X value supplied by the caller for this operation.
    // - positionY: Position Y value supplied by the caller for this operation.
    // - positionZ: Position Z value supplied by the caller for this operation.
    // - orientation: Orientation value supplied by the caller for this operation.
    // - opcode: Opcode value supplied by the caller for this operation.
    // - movementFlags: Movement flags value supplied by the caller for this operation.
    // - clientMovementTime: Client movement time value supplied by the caller for this operation.
    // - previousMap: Previous map value supplied by the caller for this operation.
    // - serviceCountChanged: Service count changed value supplied by the caller for this operation.
    // Returns: Returns the map player runtime state value produced by this operation.
    // Notes: This keeps the operation scoped to MapPlayerTracker so callers do not duplicate validation, protocol, or persistence rules.
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
