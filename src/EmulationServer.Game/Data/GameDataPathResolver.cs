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
  * This file belongs to the project runtime logic and supporting data models portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Game.Data;

/**
  * Represents the game data path resolver component in the project runtime logic and supporting data models area.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public static class GameDataPathResolver
{
    /**
      * Performs the resolve directory operation for GameDataPathResolver.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
    public static string ResolveDirectory(string dataDirectory, string childDirectory)
    {
        if (string.IsNullOrWhiteSpace(dataDirectory))
        {
            throw new ArgumentException("Data directory is required.", nameof(dataDirectory));
        }

        if (string.IsNullOrWhiteSpace(childDirectory))
        {
            throw new ArgumentException("Child directory is required.", nameof(childDirectory));
        }

        return Path.GetFullPath(Path.IsPathRooted(childDirectory)
            ? childDirectory
            : Path.Combine(dataDirectory, childDirectory));
    }
}
