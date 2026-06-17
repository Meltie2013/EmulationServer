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
// File: src/EmulationServer.Game/Data/Maps/MapTileKey.cs
// Purpose: Contains map tile key code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Shared.Data.MapStore;

namespace EmulationServer.Game.Data.Maps;

// Type: MapTileKey
// Purpose: Represents map tile key data passed through the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - MapId: Map ID identifier used to select the exact record, object, or runtime owner.
// - TileX: Tile X value supplied by the caller for this operation.
// - TileY: Tile Y value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public readonly record struct MapTileKey(uint MapId, byte TileX, byte TileY)
{

    // Method: ToString
    // Purpose: Executes the to string operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to MapTileKey so callers do not duplicate validation, protocol, or persistence rules.
    public override string ToString()
    {
        return MapStoreFileNames.FormatTileKey(MapId, TileX, TileY);
    }
}
