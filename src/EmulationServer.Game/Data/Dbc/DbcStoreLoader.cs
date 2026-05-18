using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Game.Data.Dbc;

public static class DbcStoreLoader
{
    public static Dictionary<string, DbcDataStore> LoadRequiredStores(string dbcDirectory, IEnumerable<string> requiredDbcFiles, string ownerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dbcDirectory);
        ArgumentNullException.ThrowIfNull(requiredDbcFiles);

        string fullDbcDirectory = Path.GetFullPath(dbcDirectory);
        if (!Directory.Exists(fullDbcDirectory))
        {
            throw new DirectoryNotFoundException($"DBC directory was not found: {fullDbcDirectory}");
        }

        string[] requiredFiles = requiredDbcFiles
            .Where(file => !string.IsNullOrWhiteSpace(file))
            .Select(file => file.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        ValidateRequiredDbcFiles(fullDbcDirectory, requiredFiles);

        Dictionary<string, DbcDataStore> stores = new(StringComparer.OrdinalIgnoreCase);
        foreach (string fileName in requiredFiles.OrderBy(file => file, StringComparer.OrdinalIgnoreCase))
        {
            string path = Path.Combine(fullDbcDirectory, fileName);
            DbcDataStore store = DbcDataStore.Load(path);
            stores[store.Name] = store;
        }

        Logger.Write(LogType.SUCCESS, $"{ownerName} loaded {stores.Count} required DBC file(s) from '{fullDbcDirectory}'.", nameof(DbcStoreLoader));
        return stores;
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
}
