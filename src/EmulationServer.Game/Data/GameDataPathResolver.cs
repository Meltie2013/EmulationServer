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
// File: src/EmulationServer.Game/Data/GameDataPathResolver.cs
// Purpose: Contains game data path resolver code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Data;

// Type: GameDataPathResolver
// Purpose: Provides game data path resolver behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class GameDataPathResolver
{

    // Method: ResolveDirectory
    // Purpose: Retrieves resolve directory data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - dataDirectory: Data directory value supplied by the caller for this operation.
    // - childDirectory: Child directory value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to GameDataPathResolver so callers do not duplicate validation, protocol, or persistence rules.
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
