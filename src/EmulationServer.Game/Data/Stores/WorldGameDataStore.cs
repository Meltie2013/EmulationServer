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

using EmulationServer.Game.Data.Dbc;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Game.Data.Stores;

public sealed class WorldGameDataStore
{
    private readonly Dictionary<string, DbcDataStore> _dbcStores;

    private WorldGameDataStore(Dictionary<string, DbcDataStore> dbcStores)
    {
        _dbcStores = dbcStores;
    }

    public static WorldGameDataStore Empty { get; } = new([]);

    public IReadOnlyDictionary<string, DbcDataStore> DbcStores => _dbcStores;

    public bool TryGetDbcStore(string fileName, out DbcDataStore store)
    {
        return _dbcStores.TryGetValue(fileName, out store!);
    }

    public static WorldGameDataStore Load(
        string dataDirectory,
        string dbcDirectory,
        IEnumerable<string> requiredDbcFiles)
    {
        string fullDataDirectory = Path.GetFullPath(dataDirectory);
        string fullDbcDirectory = GameDataPathResolver.ResolveDirectory(fullDataDirectory, dbcDirectory);

        Dictionary<string, DbcDataStore> dbcStores = DbcStoreLoader.LoadRequiredStores(
            fullDbcDirectory,
            requiredDbcFiles,
            nameof(WorldGameDataStore));

        Logger.Write(LogType.SUCCESS, $"World game data loaded: {dbcStores.Count} DBC file(s). Map tiles are owned by MapServer and InstanceServer.", nameof(WorldGameDataStore));

        return new WorldGameDataStore(dbcStores);
    }
}
