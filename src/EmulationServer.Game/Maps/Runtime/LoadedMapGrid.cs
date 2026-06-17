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
// File: src/EmulationServer.Game/Maps/Runtime/LoadedMapGrid.cs
// Purpose: Contains loaded map grid code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Game.Data.Maps;

namespace EmulationServer.Game.Maps.Runtime;

// Type: LoadedMapGrid
// Purpose: Provides loaded map grid behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class LoadedMapGrid
{

    // Constructor: LoadedMapGrid
    // Purpose: Initializes a new LoadedMapGrid instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - tile: Tile value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to LoadedMapGrid so callers do not duplicate validation, protocol, or persistence rules.
    public LoadedMapGrid(MapTileDataStore tile)
    {
        Tile = tile ?? throw new ArgumentNullException();
        LoadedUtc = DateTimeOffset.UtcNow;
        LastUsedUtc = LoadedUtc;
    }

    // Property: Gets or sets the tile value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: tile value exposed by the owning type.
    public MapTileDataStore Tile { get; }

    // Property: Gets or sets the loaded utc value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: loaded utc value exposed by the owning type.
    public DateTimeOffset LoadedUtc { get; }

    // Property: Gets or sets the last used utc value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: last used utc value exposed by the owning type.
    public DateTimeOffset LastUsedUtc { get; private set; }

    // Method: Touch
    // Purpose: Executes the touch operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to LoadedMapGrid so callers do not duplicate validation, protocol, or persistence rules.
    public void Touch()
    {
        LastUsedUtc = DateTimeOffset.UtcNow;
    }
}
