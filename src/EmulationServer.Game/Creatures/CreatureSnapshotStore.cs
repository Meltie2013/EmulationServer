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

namespace EmulationServer.Game.Creatures;

/**
  * Stores creature snapshots received from WorldServer inside MapServer or InstanceServer.
  * The store keeps MapServer and InstanceServer database-free while allowing maps and instances to rebuild runtime creature state on startup and restart.
  */
public sealed class CreatureSnapshotStore
{
    private readonly string _ownerName;
    private readonly object _syncRoot = new();
    private readonly ConcurrentDictionary<string, PendingCreatureSnapshot> _pendingSnapshots = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<uint, CreatureTemplateRecord> _templates = [];
    private readonly Dictionary<int, IReadOnlyList<CreatureSpawnRecord>> _spawnsByMap = [];

    public CreatureSnapshotStore(string ownerName)
    {
        _ownerName = string.IsNullOrWhiteSpace(ownerName) ? "CreatureSnapshotStore" : ownerName.Trim();
    }

    public IReadOnlyList<CreatureSpawnRecord> GetSpawnsForMap(int mapId)
    {
        if (mapId < 0)
        {
            return Array.Empty<CreatureSpawnRecord>();
        }

        lock (_syncRoot)
        {
            return _spawnsByMap.TryGetValue(mapId, out IReadOnlyList<CreatureSpawnRecord>? records)
                ? records
                : Array.Empty<CreatureSpawnRecord>();
        }
    }

    public CreatureTemplateRecord? GetTemplateOrDefault(uint entry)
    {
        lock (_syncRoot)
        {
            return _templates.TryGetValue(entry, out CreatureTemplateRecord? template)
                ? template
                : null;
        }
    }

    public bool TryHandleSnapshotPacket(string remoteServerName, string packet, out CreatureSnapshotApplyResult result)
    {
        result = default;
        if (!CreatureSnapshotProtocol.IsSnapshotPacket(packet))
        {
            return false;
        }

        if (CreatureSnapshotProtocol.TryParseBegin(packet, out string beginSnapshotId, out int beginMapId, out int templateCount, out int spawnCount))
        {
            _pendingSnapshots[beginSnapshotId] = new PendingCreatureSnapshot(beginSnapshotId, beginMapId, templateCount, spawnCount);
            Logger.Write(LogType.NETWORK, $"{_ownerName} began receiving creature snapshot {beginSnapshotId} for MapId={beginMapId} from {remoteServerName}: templates={templateCount}, spawns={spawnCount}.", "CreatureSnapshotStore");
            return true;
        }

        if (CreatureSnapshotProtocol.TryParseTemplate(packet, out string templateSnapshotId, out CreatureTemplateRecord template))
        {
            if (_pendingSnapshots.TryGetValue(templateSnapshotId, out PendingCreatureSnapshot? pendingTemplateSnapshot))
            {
                pendingTemplateSnapshot.AddTemplate(template);
            }
            else
            {
                Logger.Write(LogType.WARNING, $"{_ownerName} received creature template for unknown snapshot {templateSnapshotId} from {remoteServerName}.", "CreatureSnapshotStore");
            }

            return true;
        }

        if (CreatureSnapshotProtocol.TryParseSpawn(packet, out string spawnSnapshotId, out CreatureSpawnRecord spawn))
        {
            if (_pendingSnapshots.TryGetValue(spawnSnapshotId, out PendingCreatureSnapshot? pendingSpawnSnapshot))
            {
                pendingSpawnSnapshot.AddSpawn(spawn);
            }
            else
            {
                Logger.Write(LogType.WARNING, $"{_ownerName} received creature spawn for unknown snapshot {spawnSnapshotId} from {remoteServerName}.", "CreatureSnapshotStore");
            }

            return true;
        }

        if (CreatureSnapshotProtocol.TryParseEnd(packet, out string endSnapshotId, out int endMapId))
        {
            if (!_pendingSnapshots.TryRemove(endSnapshotId, out PendingCreatureSnapshot? completedSnapshot))
            {
                Logger.Write(LogType.WARNING, $"{_ownerName} received creature snapshot end for unknown snapshot {endSnapshotId} from {remoteServerName}.", "CreatureSnapshotStore");
                return true;
            }

            if (completedSnapshot.MapId != endMapId)
            {
                Logger.Write(LogType.WARNING, $"{_ownerName} received creature snapshot {endSnapshotId} end for MapId={endMapId}, but snapshot began for MapId={completedSnapshot.MapId}.", "CreatureSnapshotStore");
                return true;
            }

            IReadOnlyList<CreatureTemplateRecord> receivedTemplates = completedSnapshot.GetTemplates();
            IReadOnlyList<CreatureSpawnRecord> receivedSpawns = completedSnapshot.GetSpawns();
            CreatureTemplateRecord[] templates = receivedTemplates
                .Where(CreatureDataValidation.IsLoadableTemplate)
                .ToArray();
            CreatureSpawnRecord[] loadableSpawns = receivedSpawns
                .Where(CreatureDataValidation.IsLoadableSpawn)
                .ToArray();
            int invalidTemplates = receivedTemplates.Count - templates.Length;
            int invalidSpawns = receivedSpawns.Count - loadableSpawns.Length;
            int missingTemplateSpawns = 0;
            CreatureSpawnRecord[] finalSpawns;

            lock (_syncRoot)
            {
                foreach (CreatureTemplateRecord snapshotTemplate in templates)
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
                    $"{_ownerName} completed creature snapshot {endSnapshotId} for MapId={endMapId}, but received templates={receivedTemplates.Count}/{completedSnapshot.ExpectedTemplateCount}, spawns={receivedSpawns.Count}/{completedSnapshot.ExpectedSpawnCount}.",
                    "CreatureSnapshotStore");
            }

            if (invalidTemplates != 0 || invalidSpawns != 0 || missingTemplateSpawns != 0)
            {
                Logger.Write(
                    LogType.WARNING,
                    $"{_ownerName} skipped invalid creature snapshot rows for MapId={endMapId}: invalidTemplates={invalidTemplates}, invalidSpawns={invalidSpawns}, missingTemplateSpawns={missingTemplateSpawns}.",
                    "CreatureSnapshotStore");
            }

            result = new CreatureSnapshotApplyResult(true, endMapId, templates.Length, finalSpawns.Length);
            Logger.Write(LogType.DATABASE, $"{_ownerName} applied creature snapshot {endSnapshotId} for MapId={endMapId}: templates={templates.Length}, spawns={finalSpawns.Length}.", "CreatureSnapshotStore");
            return true;
        }

        Logger.Write(LogType.WARNING, $"{_ownerName} received malformed creature snapshot packet from {remoteServerName}: {packet}", "CreatureSnapshotStore");
        return true;
    }

    private sealed class PendingCreatureSnapshot
    {
        private readonly object _syncRoot = new();
        private readonly List<CreatureTemplateRecord> _templates = [];
        private readonly List<CreatureSpawnRecord> _spawns = [];

        public PendingCreatureSnapshot(string snapshotId, int mapId, int expectedTemplateCount, int expectedSpawnCount)
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

        public void AddTemplate(CreatureTemplateRecord template)
        {
            lock (_syncRoot)
            {
                _templates.Add(template);
            }
        }

        public void AddSpawn(CreatureSpawnRecord spawn)
        {
            lock (_syncRoot)
            {
                _spawns.Add(spawn);
            }
        }

        public IReadOnlyList<CreatureTemplateRecord> GetTemplates()
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

        public IReadOnlyList<CreatureSpawnRecord> GetSpawns()
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
