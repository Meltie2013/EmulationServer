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
// File: src/EmulationServer.Game/Data/Dbc/DbcStoreLoader.cs
// Purpose: Contains DBC store loader code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Game.Data.Dbc;

// Type: DbcStoreLoader
// Purpose: Provides DBC store loader behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class DbcStoreLoader
{
    // Method: LoadRequiredStores
    // Purpose: Retrieves load required stores data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - dbcDirectory: Dbc directory value supplied by the caller for this operation.
    // - requiredDbcFiles: Required DBC files value supplied by the caller for this operation.
    // - ownerName: Owner name value supplied by the caller for this operation.
    // Returns: Returns the dictionary value produced by this operation.
    // Notes: This keeps the operation scoped to DbcStoreLoader so callers do not duplicate validation, protocol, or persistence rules.
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

            Logger.Write(
                LogType.DATABASE,
                $"{ownerName}: DBC `{store.Name}` loaded {store.RecordCount} record(s), {store.FieldCount} field(s).",
                "DbcStoreLoader");
        }

        Logger.Write(LogType.SUCCESS, $"{ownerName}: loaded required DBC files (count={stores.Count}, path='{fullDbcDirectory}').", "DbcStoreLoader");
        return stores;
    }

    // Method: ValidateRequiredDbcFiles
    // Purpose: Validates or evaluates validate required DBC files rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - dbcDirectory: Dbc directory value supplied by the caller for this operation.
    // - requiredDbcFiles: Required DBC files value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to DbcStoreLoader so callers do not duplicate validation, protocol, or persistence rules.
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
