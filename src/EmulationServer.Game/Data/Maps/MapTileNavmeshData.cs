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
// File: src/EmulationServer.Game/Data/Maps/MapTileNavmeshData.cs
// Purpose: Contains map tile navmesh data code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Data.Maps;

// Type: MapTileNavmeshData
// Purpose: Provides map tile navmesh data behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class MapTileNavmeshData
{
    // Constructor: MapTileNavmeshData
    // Purpose: Initializes a new MapTileNavmeshData instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - key: Key value supplied by the caller for this operation.
    // - build: Build value supplied by the caller for this operation.
    // - polygonCount: Polygon count value supplied by the caller for this operation.
    // - connectionCount: Connection count value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapTileNavmeshData so callers do not duplicate validation, protocol, or persistence rules.
    public MapTileNavmeshData(MapTileKey key, ushort build, uint polygonCount, uint connectionCount)
    {
        Key = key;
        Build = build;
        PolygonCount = polygonCount;
        ConnectionCount = connectionCount;
    }

    // Method: CreateDisabled
    // Purpose: Applies create disabled changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - key: Key value supplied by the caller for this operation.
    // Returns: Returns the map tile navmesh data value produced by this operation.
    // Notes: This keeps the operation scoped to MapTileNavmeshData so callers do not duplicate validation, protocol, or persistence rules.
    public static MapTileNavmeshData CreateDisabled(MapTileKey key)
    {
        return new MapTileNavmeshData(key, 0, 0, 0);
    }

    // Property: Gets or sets the key value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: key value exposed by the owning type.
    public MapTileKey Key { get; }
    // Property: Gets or sets the build value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: build value exposed by the owning type.
    public ushort Build { get; }
    // Property: Gets or sets the polygon count value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: polygon count value exposed by the owning type.
    public uint PolygonCount { get; }
    // Property: Gets or sets the connection count value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: connection count value exposed by the owning type.
    public uint ConnectionCount { get; }
    // Property: Gets or sets the has navigation data value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: has navigation data value exposed by the owning type.
    public bool HasNavigationData => PolygonCount > 0;
}
