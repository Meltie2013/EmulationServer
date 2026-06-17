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
// File: src/EmulationServer.Game/Data/Maps/MapTileCollisionQueryService.cs
// Purpose: Contains map tile collision query service code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Data.Maps;

// Type: MapTileCollisionQueryService
// Purpose: Provides map tile collision query service behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class MapTileCollisionQueryService
{
    // Field: Stores the collision state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current collision backing value maintained by the owning type.
    private readonly MapTileCollisionData _collision;

    // Constructor: MapTileCollisionQueryService
    // Purpose: Initializes a new MapTileCollisionQueryService instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - collision: Collision value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapTileCollisionQueryService so callers do not duplicate validation, protocol, or persistence rules.
    public MapTileCollisionQueryService(MapTileCollisionData collision)
    {
        _collision = collision ?? throw new ArgumentNullException();
    }

    // Property: Gets or sets the placements value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: placements value exposed by the owning type.
    public IReadOnlyList<MapTileCollisionPlacement> Placements => _collision.Placements;

    // Method: FindPlacementsContaining
    // Purpose: Retrieves find placements containing data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - point: Point value supplied by the caller for this operation.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to MapTileCollisionQueryService so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyList<MapTileCollisionPlacement> FindPlacementsContaining(MapTileVector3 point)
    {
        if (_collision.Placements.Count == 0)
        {
            return [];
        }

        List<MapTileCollisionPlacement> matches = [];
        foreach (MapTileCollisionPlacement placement in _collision.Placements)
        {
            if (placement.Bounds.Contains(point))
            {
                matches.Add(placement);
            }
        }

        return matches;
    }
}
