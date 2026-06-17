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
// File: src/EmulationServer.Game/Data/Maps/MapStoreMapIndex.cs
// Purpose: Contains map store map index code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Data.Maps;

// Type: MapStoreMapIndex
// Purpose: Provides map store map index behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class MapStoreMapIndex
{
    // Constructor: MapStoreMapIndex
    // Purpose: Initializes a new MapStoreMapIndex instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - build: Build value supplied by the caller for this operation.
    // - records: Records value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapStoreMapIndex so callers do not duplicate validation, protocol, or persistence rules.
    public MapStoreMapIndex(uint mapId, ushort build, IReadOnlyList<MapStoreMapIndexRecord> records)
    {
        MapId = mapId;
        Build = build;
        Records = records ?? throw new ArgumentNullException();
    }

    // Property: Gets or sets the map ID value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: map ID value exposed by the owning type.
    public uint MapId { get; }
    // Property: Gets or sets the build value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: build value exposed by the owning type.
    public ushort Build { get; }
    // Property: Gets or sets the records value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: records value exposed by the owning type.
    public IReadOnlyList<MapStoreMapIndexRecord> Records { get; }
}
