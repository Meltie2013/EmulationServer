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
