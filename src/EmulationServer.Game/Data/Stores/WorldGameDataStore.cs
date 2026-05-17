using EmulationServer.Game.Data.Dbc;
using EmulationServer.Game.Data.Maps;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Game.Data.Stores;

public sealed class WorldGameDataStore
{
    private readonly Dictionary<string, DbcDataStore> _dbcStores;
    private readonly Dictionary<MapTileKey, MapTileDataStore> _mapTiles;

    private WorldGameDataStore(
        Dictionary<string, DbcDataStore> dbcStores,
        Dictionary<MapTileKey, MapTileDataStore> mapTiles)
    {
        _dbcStores = dbcStores;
        _mapTiles = mapTiles;
    }

    public static WorldGameDataStore Empty { get; } = new([], []);

    public IReadOnlyDictionary<string, DbcDataStore> DbcStores => _dbcStores;

    public IReadOnlyDictionary<MapTileKey, MapTileDataStore> MapTiles => _mapTiles;

    public bool TryGetDbcStore(string fileName, out DbcDataStore store)
    {
        return _dbcStores.TryGetValue(fileName, out store!);
    }

    public bool TryGetMapTile(MapTileKey key, out MapTileDataStore tile)
    {
        return _mapTiles.TryGetValue(key, out tile!);
    }

    public static WorldGameDataStore Load(
        string dataDirectory,
        string dbcDirectory,
        string mapsDirectory,
        IEnumerable<string> requiredDbcFiles,
        bool loadMaps)
    {
        string fullDataDirectory = Path.GetFullPath(dataDirectory);
        string fullDbcDirectory = Path.GetFullPath(Path.Combine(fullDataDirectory, dbcDirectory));
        string fullMapsDirectory = Path.GetFullPath(Path.Combine(fullDataDirectory, mapsDirectory));

        if (!Directory.Exists(fullDbcDirectory))
        {
            throw new DirectoryNotFoundException($"DBC directory was not found: {fullDbcDirectory}");
        }

        ValidateRequiredDbcFiles(fullDbcDirectory, requiredDbcFiles);

        Dictionary<string, DbcDataStore> dbcStores = LoadDbcStores(fullDbcDirectory);
        Dictionary<MapTileKey, MapTileDataStore> mapTiles = loadMaps
            ? LoadMapTiles(fullMapsDirectory)
            : [];

        Logger.Write(LogType.SUCCESS, $"World game data loaded: {dbcStores.Count} DBC file(s), {mapTiles.Count} map tile(s).", nameof(WorldGameDataStore));

        return new WorldGameDataStore(dbcStores, mapTiles);
    }

    private static void ValidateRequiredDbcFiles(string dbcDirectory, IEnumerable<string> requiredDbcFiles)
    {
        List<string> missingFiles = [];

        foreach (string requiredDbcFile in requiredDbcFiles)
        {
            string requiredPath = Path.Combine(dbcDirectory, requiredDbcFile);
            if (!File.Exists(requiredPath))
            {
                missingFiles.Add(requiredDbcFile);
            }
        }

        if (missingFiles.Count > 0)
        {
            throw new FileNotFoundException($"Missing required DBC file(s): {string.Join(", ", missingFiles)}");
        }
    }

    private static Dictionary<string, DbcDataStore> LoadDbcStores(string dbcDirectory)
    {
        Dictionary<string, DbcDataStore> stores = new(StringComparer.OrdinalIgnoreCase);

        foreach (string path in Directory.EnumerateFiles(dbcDirectory, "*.dbc", SearchOption.TopDirectoryOnly).OrderBy(Path.GetFileName))
        {
            DbcDataStore store = DbcDataStore.Load(path);
            stores[store.Name] = store;

            // Keep this logger commented out until we have a better way to track progress of loading large DBC files, as it can create a lot of log spam when loading all DBC files.
            // Logger.Write(LogType.NOTICE, $"Loaded DBC '{store.Name}' ({store.RecordCount} record(s), {store.FieldCount} field(s)).", nameof(WorldGameDataStore));
        }

        return stores;
    }

    private static Dictionary<MapTileKey, MapTileDataStore> LoadMapTiles(string mapsDirectory)
    {
        Dictionary<MapTileKey, MapTileDataStore> tiles = new();

        if (!Directory.Exists(mapsDirectory))
        {
            throw new DirectoryNotFoundException($"Map directory was not found: {mapsDirectory}");
        }

        foreach (string path in Directory.EnumerateFiles(mapsDirectory, "*.map", SearchOption.AllDirectories).OrderBy(Path.GetFileName))
        {
            MapTileDataStore tile = MapTileDataStore.Load(path);
            tiles[tile.Key] = tile;
        }

        return tiles;
    }
}
