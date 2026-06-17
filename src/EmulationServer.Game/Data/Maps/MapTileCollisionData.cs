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
// File: src/EmulationServer.Game/Data/Maps/MapTileCollisionData.cs
// Purpose: Contains map tile collision data code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Data.Maps;

// Type: MapTileCollisionData
// Purpose: Provides map tile collision data behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class MapTileCollisionData
{
    // Constructor: MapTileCollisionData
    // Purpose: Initializes a new MapTileCollisionData instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - key: Key value supplied by the caller for this operation.
    // - build: Build value supplied by the caller for this operation.
    // - version: Version value supplied by the caller for this operation.
    // - placements: Placements value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapTileCollisionData so callers do not duplicate validation, protocol, or persistence rules.
    public MapTileCollisionData(MapTileKey key, ushort build, uint version, IReadOnlyList<MapTileCollisionPlacement> placements)
    {
        Key = key;
        Build = build;
        Version = version;
        Placements = placements ?? throw new ArgumentNullException();
    }

    // Method: CreateDisabled
    // Purpose: Applies create disabled changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - key: Key value supplied by the caller for this operation.
    // Returns: Returns the map tile collision data value produced by this operation.
    // Notes: This keeps the operation scoped to MapTileCollisionData so callers do not duplicate validation, protocol, or persistence rules.
    public static MapTileCollisionData CreateDisabled(MapTileKey key)
    {
        return new MapTileCollisionData(key, 0, 0, []);
    }

    // Property: Gets or sets the key value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: key value exposed by the owning type.
    public MapTileKey Key { get; }
    // Property: Gets or sets the build value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: build value exposed by the owning type.
    public ushort Build { get; }
    // Property: Gets or sets the version value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: version value exposed by the owning type.
    public uint Version { get; }
    // Property: Gets or sets the placements value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: placements value exposed by the owning type.
    public IReadOnlyList<MapTileCollisionPlacement> Placements { get; }
    // Property: Gets or sets the has placements value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: has placements value exposed by the owning type.
    public bool HasPlacements => Placements.Count > 0;
}
