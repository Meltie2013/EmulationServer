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
// File: src/EmulationServer.Game/GameObjects/GameObjectSnapshotStore.cs
// Purpose: Contains game object snapshot store code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Collections.Concurrent;
using EmulationServer.Game.WorldData;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Game.GameObjects;

// Type: GameObjectSnapshotStore
// Purpose: Provides game object snapshot store behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class GameObjectSnapshotStore
{
    // Field: Stores the owner name state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current owner name backing value maintained by the owning type.
    private readonly string _ownerName;
    private readonly object _syncRoot = new();
    private readonly ConcurrentDictionary<string, PendingGameObjectSnapshot> _pendingSnapshots = new(StringComparer.OrdinalIgnoreCase);
    // Field: Stores the uint state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current uint backing value maintained by the owning type.
    private readonly Dictionary<uint, GameObjectTemplateRecord> _templates = [];
    // Field: Stores the int state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current int backing value maintained by the owning type.
    private readonly Dictionary<int, IReadOnlyList<GameObjectSpawnRecord>> _spawnsByMap = [];

    // Constructor: GameObjectSnapshotStore
    // Purpose: Initializes a new GameObjectSnapshotStore instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - ownerName: Owner name value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to GameObjectSnapshotStore so callers do not duplicate validation, protocol, or persistence rules.
    public GameObjectSnapshotStore(string ownerName)
    {
        _ownerName = string.IsNullOrWhiteSpace(ownerName) ? "GameObjectSnapshotStore" : ownerName.Trim();
    }

    // Method: GetSpawnsForMap
    // Purpose: Retrieves get spawns for map data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to GameObjectSnapshotStore so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyList<GameObjectSpawnRecord> GetSpawnsForMap(int mapId)
    {
        if (mapId < 0)
        {
            return Array.Empty<GameObjectSpawnRecord>();
        }

        lock (_syncRoot)
        {
            return _spawnsByMap.TryGetValue(mapId, out IReadOnlyList<GameObjectSpawnRecord>? records)
                ? records
                : Array.Empty<GameObjectSpawnRecord>();
        }
    }

    // Method: GetTemplateOrDefault
    // Purpose: Retrieves get template or default data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - entry: Entry value supplied by the caller for this operation.
    // Returns: Returns the game object template record? value produced by this operation.
    // Notes: This keeps the operation scoped to GameObjectSnapshotStore so callers do not duplicate validation, protocol, or persistence rules.
    public GameObjectTemplateRecord? GetTemplateOrDefault(uint entry)
    {
        lock (_syncRoot)
        {
            return _templates.TryGetValue(entry, out GameObjectTemplateRecord? template)
                ? template
                : null;
        }
    }

    // Method: TryHandleSnapshotPacket
    // Purpose: Executes the try handle snapshot packet operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - result: Result value supplied by the caller for this operation.
    // Returns: Returns true when try handle snapshot packet succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to GameObjectSnapshotStore so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryHandleSnapshotPacket(string remoteServerName, string packet, out GameObjectSnapshotApplyResult result)
    {
        result = default;
        if (!GameObjectSnapshotProtocol.IsSnapshotPacket(packet))
        {
            return false;
        }

        if (GameObjectSnapshotProtocol.TryParseBegin(packet, out string beginSnapshotId, out int beginMapId, out int templateCount, out int spawnCount))
        {
            _pendingSnapshots[beginSnapshotId] = new PendingGameObjectSnapshot(beginSnapshotId, beginMapId, templateCount, spawnCount);
            Logger.Write(LogType.NETWORK, $"{_ownerName} began receiving gameobject snapshot {beginSnapshotId} for MapId={beginMapId} from {remoteServerName}: templates={templateCount}, spawns={spawnCount}.", "GameObjectSnapshotStore");
            return true;
        }

        if (GameObjectSnapshotProtocol.TryParseTemplate(packet, out string templateSnapshotId, out GameObjectTemplateRecord template))
        {
            if (_pendingSnapshots.TryGetValue(templateSnapshotId, out PendingGameObjectSnapshot? pendingTemplateSnapshot))
            {
                pendingTemplateSnapshot.AddTemplate(template);
            }
            else
            {
                Logger.Write(LogType.WARNING, $"{_ownerName} received gameobject template for unknown snapshot {templateSnapshotId} from {remoteServerName}.", "GameObjectSnapshotStore");
            }

            return true;
        }

        if (GameObjectSnapshotProtocol.TryParseSpawn(packet, out string spawnSnapshotId, out GameObjectSpawnRecord spawn))
        {
            if (_pendingSnapshots.TryGetValue(spawnSnapshotId, out PendingGameObjectSnapshot? pendingSpawnSnapshot))
            {
                pendingSpawnSnapshot.AddSpawn(spawn);
            }
            else
            {
                Logger.Write(LogType.WARNING, $"{_ownerName} received gameobject spawn for unknown snapshot {spawnSnapshotId} from {remoteServerName}.", "GameObjectSnapshotStore");
            }

            return true;
        }

        if (GameObjectSnapshotProtocol.TryParseEnd(packet, out string endSnapshotId, out int endMapId))
        {
            if (!_pendingSnapshots.TryRemove(endSnapshotId, out PendingGameObjectSnapshot? completedSnapshot))
            {
                Logger.Write(LogType.WARNING, $"{_ownerName} received gameobject snapshot end for unknown snapshot {endSnapshotId} from {remoteServerName}.", "GameObjectSnapshotStore");
                return true;
            }

            if (completedSnapshot.MapId != endMapId)
            {
                Logger.Write(LogType.WARNING, $"{_ownerName} received gameobject snapshot {endSnapshotId} end for MapId={endMapId}, but snapshot began for MapId={completedSnapshot.MapId}.", "GameObjectSnapshotStore");
                return true;
            }

            IReadOnlyList<GameObjectTemplateRecord> receivedTemplates = completedSnapshot.GetTemplates();
            IReadOnlyList<GameObjectSpawnRecord> receivedSpawns = completedSnapshot.GetSpawns();
            GameObjectTemplateRecord[] templates = receivedTemplates
                .Where(GameObjectDataValidation.IsLoadableTemplate)
                .ToArray();
            GameObjectSpawnRecord[] loadableSpawns = receivedSpawns
                .Where(GameObjectDataValidation.IsLoadableSpawn)
                .ToArray();
            int invalidTemplates = receivedTemplates.Count - templates.Length;
            int invalidSpawns = receivedSpawns.Count - loadableSpawns.Length;
            int missingTemplateSpawns = 0;
            GameObjectSpawnRecord[] finalSpawns;

            lock (_syncRoot)
            {
                foreach (GameObjectTemplateRecord snapshotTemplate in templates)
                {
                    _templates[snapshotTemplate.Entry] = snapshotTemplate;
                }

                HashSet<uint> templateEntries = _templates.Keys.ToHashSet();
                finalSpawns = loadableSpawns
                    .Where(spawn => templateEntries.Contains(spawn.Entry))
                    .ToArray();
                missingTemplateSpawns = loadableSpawns.Length - finalSpawns.Length;

                _spawnsByMap[endMapId] = finalSpawns;
            }

            if (completedSnapshot.ExpectedTemplateCount != receivedTemplates.Count || completedSnapshot.ExpectedSpawnCount != receivedSpawns.Count)
            {
                Logger.Write(
                    LogType.WARNING,
                    $"{_ownerName} completed gameobject snapshot {endSnapshotId} for MapId={endMapId}, but received templates={receivedTemplates.Count}/{completedSnapshot.ExpectedTemplateCount}, spawns={receivedSpawns.Count}/{completedSnapshot.ExpectedSpawnCount}.",
                    "GameObjectSnapshotStore");
            }

            if (invalidTemplates != 0 || invalidSpawns != 0 || missingTemplateSpawns != 0)
            {
                Logger.Write(
                    LogType.WARNING,
                    $"{_ownerName} skipped invalid gameobject snapshot rows for MapId={endMapId}: invalidTemplates={invalidTemplates}, invalidSpawns={invalidSpawns}, missingTemplateSpawns={missingTemplateSpawns}.",
                    "GameObjectSnapshotStore");
            }

            result = new GameObjectSnapshotApplyResult(true, endMapId, templates.Length, finalSpawns.Length);
            Logger.Write(LogType.DATABASE, $"{_ownerName} applied gameobject snapshot {endSnapshotId} for MapId={endMapId}: templates={templates.Length}, spawns={finalSpawns.Length}.", "GameObjectSnapshotStore");
            return true;
        }

        Logger.Write(LogType.WARNING, $"{_ownerName} received malformed gameobject snapshot packet from {remoteServerName}: {packet}", "GameObjectSnapshotStore");
        return true;
    }

    // Type: PendingGameObjectSnapshot
    // Purpose: Provides pending game object snapshot behavior for the game-domain data, player state, DBC, and world-template layer.
    // Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
    private sealed class PendingGameObjectSnapshot
    {
        private readonly object _syncRoot = new();
        // Field: Stores the templates state used by the game-domain data, player state, DBC, and world-template layer.
        // Value: current templates backing value maintained by the owning type.
        private readonly List<GameObjectTemplateRecord> _templates = [];
        // Field: Stores the spawns state used by the game-domain data, player state, DBC, and world-template layer.
        // Value: current spawns backing value maintained by the owning type.
        private readonly List<GameObjectSpawnRecord> _spawns = [];

        // Constructor: PendingGameObjectSnapshot
        // Purpose: Initializes a new PendingGameObjectSnapshot instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
        // Parameters:
        // - snapshotId: Snapshot ID identifier used to select the exact record, object, or runtime owner.
        // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
        // - expectedTemplateCount: Expected template count value supplied by the caller for this operation.
        // - expectedSpawnCount: Expected spawn count value supplied by the caller for this operation.
        // Returns: none.
        // Notes: This keeps the operation scoped to PendingGameObjectSnapshot so callers do not duplicate validation, protocol, or persistence rules.
        public PendingGameObjectSnapshot(string snapshotId, int mapId, int expectedTemplateCount, int expectedSpawnCount)
        {
            SnapshotId = snapshotId;
            MapId = mapId;
            ExpectedTemplateCount = expectedTemplateCount;
            ExpectedSpawnCount = expectedSpawnCount;
        }

        // Property: Gets or sets the snapshot ID value used by the game-domain data, player state, DBC, and world-template layer.
        // Value: snapshot ID value exposed by the owning type.
        public string SnapshotId { get; }

        // Property: Gets or sets the map ID value used by the game-domain data, player state, DBC, and world-template layer.
        // Value: map ID value exposed by the owning type.
        public int MapId { get; }

        // Property: Gets or sets the expected template count value used by the game-domain data, player state, DBC, and world-template layer.
        // Value: expected template count value exposed by the owning type.
        public int ExpectedTemplateCount { get; }

        // Property: Gets or sets the expected spawn count value used by the game-domain data, player state, DBC, and world-template layer.
        // Value: expected spawn count value exposed by the owning type.
        public int ExpectedSpawnCount { get; }

        // Method: AddTemplate
        // Purpose: Applies add template changes for the game-domain data, player state, DBC, and world-template layer.
        // Parameters:
        // - template: Template value supplied by the caller for this operation.
        // Returns: none.
        // Notes: This keeps the operation scoped to PendingGameObjectSnapshot so callers do not duplicate validation, protocol, or persistence rules.
        public void AddTemplate(GameObjectTemplateRecord template)
        {
            lock (_syncRoot)
            {
                _templates.Add(template);
            }
        }

        // Method: AddSpawn
        // Purpose: Applies add spawn changes for the game-domain data, player state, DBC, and world-template layer.
        // Parameters:
        // - spawn: Spawn value supplied by the caller for this operation.
        // Returns: none.
        // Notes: This keeps the operation scoped to PendingGameObjectSnapshot so callers do not duplicate validation, protocol, or persistence rules.
        public void AddSpawn(GameObjectSpawnRecord spawn)
        {
            lock (_syncRoot)
            {
                _spawns.Add(spawn);
            }
        }

        // Method: GetTemplates
        // Purpose: Retrieves get templates data for the game-domain data, player state, DBC, and world-template layer.
        // Parameters: none.
        // Returns: Returns the I read only list value produced by this operation.
        // Notes: This keeps the operation scoped to PendingGameObjectSnapshot so callers do not duplicate validation, protocol, or persistence rules.
        public IReadOnlyList<GameObjectTemplateRecord> GetTemplates()
        {
            lock (_syncRoot)
            {
                return _templates
                    .GroupBy(template => template.Entry)
                    .Select(group => group.Last())
                    .OrderBy(template => template.Entry)
                    .ToArray();
            }
        }

        // Method: GetSpawns
        // Purpose: Retrieves get spawns data for the game-domain data, player state, DBC, and world-template layer.
        // Parameters: none.
        // Returns: Returns the I read only list value produced by this operation.
        // Notes: This keeps the operation scoped to PendingGameObjectSnapshot so callers do not duplicate validation, protocol, or persistence rules.
        public IReadOnlyList<GameObjectSpawnRecord> GetSpawns()
        {
            lock (_syncRoot)
            {
                return _spawns
                    .GroupBy(spawn => spawn.Guid)
                    .Select(group => group.Last())
                    .OrderBy(spawn => spawn.Guid)
                    .ToArray();
            }
        }
    }
}
