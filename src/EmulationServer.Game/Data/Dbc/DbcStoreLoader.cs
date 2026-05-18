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

using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

/**
  * File overview: src/EmulationServer.Game/Data/Dbc/DbcStoreLoader.cs
  * This file belongs to the DBC file loading, validation, and raw record access portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Game.Data.Dbc;

/**
  * Represents the dbc store loader component in the DBC file loading, validation, and raw record access area.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
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

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of DbcStoreLoader and keeps this workflow isolated from the caller.
      */
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
