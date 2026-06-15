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

using EmulationServer.Game.WorldData;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Game.Creatures;

/**
  * Owns the map-local creature/NPC spawn lifecycle.
  * WorldServer supplies immutable template/spawn snapshots; MapServer and InstanceServer own the runtime state after load.
  */
public sealed class CreatureMapRuntime
{
    private readonly int _mapId;
    private readonly Func<int, CancellationToken, Task<IReadOnlyList<CreatureSpawnRecord>>> _loadSpawnsAsync;
    private readonly Func<uint, CreatureTemplateRecord?> _templateResolver;
    private readonly object _syncRoot = new();
    private readonly Dictionary<uint, CreatureRuntimeSpawn> _activeSpawns = [];

    public CreatureMapRuntime(
        int mapId,
        Func<int, CancellationToken, Task<IReadOnlyList<CreatureSpawnRecord>>> loadSpawnsAsync,
        Func<uint, CreatureTemplateRecord?> templateResolver)
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

    public IReadOnlyList<CreatureRuntimeSpawn> Snapshot()
    {
        lock (_syncRoot)
        {
            return _activeSpawns.Values.OrderBy(spawn => spawn.Spawn.Guid).ToArray();
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<CreatureSpawnRecord> spawns = await _loadSpawnsAsync(_mapId, cancellationToken);
        Dictionary<uint, CreatureRuntimeSpawn> activeSpawns = [];
        int missingTemplates = 0;
        int invalidSpawns = 0;
        int invalidTemplates = 0;
        int missingDisplayModels = 0;

        foreach (CreatureSpawnRecord spawn in spawns.OrderBy(spawn => spawn.Guid))
        {
            if (!CreatureDataValidation.IsLoadableSpawn(spawn))
            {
                invalidSpawns++;
                continue;
            }

            CreatureTemplateRecord? template = _templateResolver(spawn.Entry);
            if (template is null)
            {
                missingTemplates++;
                continue;
            }

            if (!CreatureDataValidation.IsLoadableTemplate(template))
            {
                invalidTemplates++;
                continue;
            }

            if (!CreatureDataValidation.HasClientVisibleDisplay(spawn, template))
            {
                missingDisplayModels++;
            }

            activeSpawns[spawn.Guid] = new CreatureRuntimeSpawn(spawn, template, true);
        }

        lock (_syncRoot)
        {
            _activeSpawns.Clear();
            foreach (KeyValuePair<uint, CreatureRuntimeSpawn> spawn in activeSpawns)
            {
                _activeSpawns[spawn.Key] = spawn.Value;
            }
        }

        LogType logType = missingTemplates == 0 && invalidSpawns == 0 && invalidTemplates == 0 && missingDisplayModels == 0 ? LogType.DATABASE : LogType.WARNING;
        Logger.Write(logType, $"Creature runtime loaded {activeSpawns.Count} spawn(s) for MapId={_mapId}. MissingTemplates={missingTemplates}, InvalidSpawns={invalidSpawns}, InvalidTemplates={invalidTemplates}, MissingDisplayModels={missingDisplayModels}.", "CreatureMapRuntime");
    }

    public void DespawnAll(string reason)
    {
        int despawned;
        lock (_syncRoot)
        {
            despawned = _activeSpawns.Count;
            _activeSpawns.Clear();
        }

        Logger.Write(LogType.SYSTEM, $"Creature runtime despawned {despawned} creature(s) for MapId={_mapId}. Reason={reason}", "CreatureMapRuntime");
    }
}
