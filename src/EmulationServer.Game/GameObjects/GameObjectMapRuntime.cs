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
// File: src/EmulationServer.Game/GameObjects/GameObjectMapRuntime.cs
// Purpose: Contains game object map runtime code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Game.WorldData;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Game.GameObjects;

// Type: GameObjectMapRuntime
// Purpose: Provides game object map runtime behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class GameObjectMapRuntime
{
    // Field: Stores the map ID state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current map ID backing value maintained by the owning type.
    private readonly int _mapId;
    // Field: Stores the int state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current int backing value maintained by the owning type.
    private readonly Func<int, CancellationToken, Task<IReadOnlyList<GameObjectSpawnRecord>>> _loadSpawnsAsync;
    // Field: Stores the uint state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current uint backing value maintained by the owning type.
    private readonly Func<uint, GameObjectTemplateRecord?> _templateResolver;
    private readonly object _syncRoot = new();
    // Field: Stores the uint state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current uint backing value maintained by the owning type.
    private readonly Dictionary<uint, GameObjectRuntimeSpawn> _activeSpawns = [];

    // Constructor: GameObjectMapRuntime
    // Purpose: Initializes a new GameObjectMapRuntime instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - loadSpawnsAsync: Load spawns async value supplied by the caller for this operation.
    // - templateResolver: Template resolver value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to GameObjectMapRuntime so callers do not duplicate validation, protocol, or persistence rules.
    public GameObjectMapRuntime(
        int mapId,
        Func<int, CancellationToken, Task<IReadOnlyList<GameObjectSpawnRecord>>> loadSpawnsAsync,
        Func<uint, GameObjectTemplateRecord?> templateResolver)
    {
        if (mapId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mapId), "Map ID cannot be negative.");
        }

        _mapId = mapId;
        _loadSpawnsAsync = loadSpawnsAsync ?? throw new ArgumentNullException(nameof(loadSpawnsAsync));
        _templateResolver = templateResolver ?? throw new ArgumentNullException(nameof(templateResolver));
    }

    public int ActiveSpawnCount
    {
        get
        {
            lock (_syncRoot)
            {
                return _activeSpawns.Count;
            }
        }
    }

    // Method: Snapshot
    // Purpose: Executes the snapshot operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to GameObjectMapRuntime so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyList<GameObjectRuntimeSpawn> Snapshot()
    {
        lock (_syncRoot)
        {
            return _activeSpawns.Values.OrderBy(spawn => spawn.Spawn.Guid).ToArray();
        }
    }

    // Method: LoadAsync
    // Purpose: Retrieves load data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to GameObjectMapRuntime so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<GameObjectSpawnRecord> spawns = await _loadSpawnsAsync(_mapId, cancellationToken);
        Dictionary<uint, GameObjectRuntimeSpawn> activeSpawns = [];
        int missingTemplates = 0;

        int invalidSpawns = 0;
        int invalidTemplates = 0;

        foreach (GameObjectSpawnRecord spawn in spawns.OrderBy(spawn => spawn.Guid))
        {
            if (!GameObjectDataValidation.IsLoadableSpawn(spawn))
            {
                invalidSpawns++;
                continue;
            }

            GameObjectTemplateRecord? template = _templateResolver(spawn.Entry);
            if (template is null)
            {
                missingTemplates++;
                continue;
            }

            if (!GameObjectDataValidation.IsLoadableTemplate(template))
            {
                invalidTemplates++;
                continue;
            }

            activeSpawns[spawn.Guid] = new GameObjectRuntimeSpawn(spawn, template, true);
        }

        lock (_syncRoot)
        {
            _activeSpawns.Clear();
            foreach (KeyValuePair<uint, GameObjectRuntimeSpawn> spawn in activeSpawns)
            {
                _activeSpawns[spawn.Key] = spawn.Value;
            }
        }

        LogType logType = missingTemplates == 0 && invalidSpawns == 0 && invalidTemplates == 0 ? LogType.DATABASE : LogType.WARNING;
        Logger.Write(logType, $"GameObject runtime loaded {activeSpawns.Count} spawn(s) for MapId={_mapId}. MissingTemplates={missingTemplates}, InvalidSpawns={invalidSpawns}, InvalidTemplates={invalidTemplates}.", "GameObjectMapRuntime");
    }

    // Method: DespawnAll
    // Purpose: Executes the despawn all operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - reason: Reason value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to GameObjectMapRuntime so callers do not duplicate validation, protocol, or persistence rules.
    public void DespawnAll(string reason)
    {
        int despawned;
        lock (_syncRoot)
        {
            despawned = _activeSpawns.Count;
            _activeSpawns.Clear();
        }

        Logger.Write(LogType.SYSTEM, $"GameObject runtime despawned {despawned} object(s) for MapId={_mapId}. Reason={reason}", "GameObjectMapRuntime");
    }
}
