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
// File: src/EmulationServer.Game/Data/Maps/MapTileBounds.cs
// Purpose: Contains map tile bounds code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Data.Maps;

// Type: MapTileBounds
// Purpose: Represents map tile bounds data passed through the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - Minimum: Minimum value supplied by the caller for this operation.
// - Maximum: Maximum value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public readonly record struct MapTileBounds(MapTileVector3 Minimum, MapTileVector3 Maximum)
{
    public static MapTileBounds Empty { get; } = new(MapTileVector3.Zero, MapTileVector3.Zero);

    // Method: Contains
    // Purpose: Executes the contains operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - point: Point value supplied by the caller for this operation.
    // Returns: Returns true when contains succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to MapTileBounds so callers do not duplicate validation, protocol, or persistence rules.
    public bool Contains(MapTileVector3 point)
    {
        return point.X >= Minimum.X && point.X <= Maximum.X &&
               point.Y >= Minimum.Y && point.Y <= Maximum.Y &&
               point.Z >= Minimum.Z && point.Z <= Maximum.Z;
    }
}
