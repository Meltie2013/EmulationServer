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
using EmulationServer.Game.WorldData;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Game.GameObjects;

/**
  * Stores game object snapshots received from WorldServer inside MapServer or InstanceServer.
  * The store keeps MapServer and InstanceServer database-free while allowing maps and instances to rebuild runtime object state on startup and restart.
  */
public sealed class GameObjectSnapshotStore
{
    private readonly string _ownerName;
    private readonly object _syncRoot = new();
    private readonly ConcurrentDictionary<string, PendingGameObjectSnapshot> _pendingSnapshots = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<uint, GameObjectTemplateRecord> _templates = [];
    private readonly Dictionary<int, IReadOnlyList<GameObjectSpawnRecord>> _spawnsByMap = [];

    public GameObjectSnapshotStore(string ownerName)
    {
        _ownerName = string.IsNullOrWhiteSpace(ownerName) ? "GameObjectSnapshotStore" : ownerName.Trim();
    }

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

    public GameObjectTemplateRecord? GetTemplateOrDefault(uint entry)
    {
        lock (_syncRoot)
        {
            return _templates.TryGetValue(entry, out GameObjectTemplateRecord? template)
                ? template
                : null;
        }
    }

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

    private sealed class PendingGameObjectSnapshot
    {
        private readonly object _syncRoot = new();
        private readonly List<GameObjectTemplateRecord> _templates = [];
        private readonly List<GameObjectSpawnRecord> _spawns = [];

        public PendingGameObjectSnapshot(string snapshotId, int mapId, int expectedTemplateCount, int expectedSpawnCount)
        {
            SnapshotId = snapshotId;
            MapId = mapId;
            ExpectedTemplateCount = expectedTemplateCount;
            ExpectedSpawnCount = expectedSpawnCount;
        }

        public string SnapshotId { get; }

        public int MapId { get; }

        public int ExpectedTemplateCount { get; }

        public int ExpectedSpawnCount { get; }

        public void AddTemplate(GameObjectTemplateRecord template)
        {
            lock (_syncRoot)
            {
                _templates.Add(template);
            }
        }

        public void AddSpawn(GameObjectSpawnRecord spawn)
        {
            lock (_syncRoot)
            {
                _spawns.Add(spawn);
            }
        }

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
