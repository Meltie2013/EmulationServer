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

/**
  * File overview: src/EmulationServer.Game/Data/GameDataPathResolver.cs
  * Documents the GameDataPathResolver source file in the server runtime support area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Data;

/**
  * Owns the game data path resolver behavior for the server runtime support layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public static class GameDataPathResolver
{
    /**
      * Resolves the directory value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: dataDirectory, childDirectory.
      */
    public static string ResolveDirectory(string dataDirectory, string childDirectory)
    {
        if (string.IsNullOrWhiteSpace(dataDirectory))
        {
            throw new ArgumentException("Data directory is required.");
        }

        if (string.IsNullOrWhiteSpace(childDirectory))
        {
            throw new ArgumentException("Child directory is required.");
        }

        return Path.GetFullPath(Path.IsPathRooted(childDirectory)
            ? childDirectory
            : Path.Combine(dataDirectory, childDirectory));
    }
}
