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
// File: src/EmulationServer.Game/Data/Maps/MapTileTerrainData.cs
// Purpose: Contains map tile terrain data code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Shared.Data.MapStore;

namespace EmulationServer.Game.Data.Maps;

// Type: MapTileTerrainData
// Purpose: Provides map tile terrain data behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class MapTileTerrainData
{
    // Constructor: MapTileTerrainData
    // Purpose: Initializes a new MapTileTerrainData instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - key: Key value supplied by the caller for this operation.
    // - build: Build value supplied by the caller for this operation.
    // - areaFlags: Area flags value supplied by the caller for this operation.
    // - gridAreaFlag: Grid area flag value supplied by the caller for this operation.
    // - ushortareaGrid: Ushortarea grid value supplied by the caller for this operation.
    // - heightFlags: Height flags value supplied by the caller for this operation.
    // - gridHeight: Grid height value supplied by the caller for this operation.
    // - gridMaxHeight: Grid max height value supplied by the caller for this operation.
    // - v9Heights: V9 heights value supplied by the caller for this operation.
    // - v8Heights: V8 heights value supplied by the caller for this operation.
    // - ushortholes: Ushortholes value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapTileTerrainData so callers do not duplicate validation, protocol, or persistence rules.
    public MapTileTerrainData(
        MapTileKey key,
        ushort build,
        ushort areaFlags,
        ushort gridAreaFlag,
        ushort[] areaGrid,
        uint heightFlags,
        float gridHeight,
        float gridMaxHeight,
        float[]? v9Heights,
        float[]? v8Heights,
        ushort[] holes)
    {
        Key = key;
        Build = build;
        AreaFlags = areaFlags;
        GridAreaFlag = gridAreaFlag;
        AreaGrid = areaGrid ?? throw new ArgumentNullException();
        HeightFlags = heightFlags;
        GridHeight = gridHeight;
        GridMaxHeight = gridMaxHeight;
        V9Heights = v9Heights;
        V8Heights = v8Heights;
        Holes = holes ?? throw new ArgumentNullException();
    }

    // Method: CreateDisabled
    // Purpose: Applies create disabled changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - key: Key value supplied by the caller for this operation.
    // Returns: Returns the map tile terrain data value produced by this operation.
    // Notes: This keeps the operation scoped to MapTileTerrainData so callers do not duplicate validation, protocol, or persistence rules.
    public static MapTileTerrainData CreateDisabled(MapTileKey key)
    {
        return new MapTileTerrainData(
            key,
            0,
            MapStorePayloadConstants.MapAreaNoArea,
            0,
            new ushort[MapStorePayloadConstants.AreaCellCount],
            MapStorePayloadConstants.MapHeightNoHeight,
            0.0f,
            0.0f,
            null,
            null,
            new ushort[MapStorePayloadConstants.AreaCellCount]);
    }

    // Property: Gets or sets the key value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: key value exposed by the owning type.
    public MapTileKey Key { get; }
    // Property: Gets or sets the build value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: build value exposed by the owning type.
    public ushort Build { get; }
    // Property: Gets or sets the area flags value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: area flags value exposed by the owning type.
    public ushort AreaFlags { get; }
    // Property: Gets or sets the grid area flag value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: grid area flag value exposed by the owning type.
    public ushort GridAreaFlag { get; }
    // Property: Gets or sets the area grid value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: area grid value exposed by the owning type.
    public ushort[] AreaGrid { get; }
    // Property: Gets or sets the height flags value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: height flags value exposed by the owning type.
    public uint HeightFlags { get; }
    // Property: Gets or sets the grid height value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: grid height value exposed by the owning type.
    public float GridHeight { get; }
    // Property: Gets or sets the grid max height value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: grid max height value exposed by the owning type.
    public float GridMaxHeight { get; }
    // Property: Gets or sets the V9 heights value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: V9 heights value exposed by the owning type.
    public float[]? V9Heights { get; }
    // Property: Gets or sets the V8 heights value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: V8 heights value exposed by the owning type.
    public float[]? V8Heights { get; }
    // Property: Gets or sets the holes value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: holes value exposed by the owning type.
    public ushort[] Holes { get; }
    // Method: HasAreaGrid
    // Purpose: Validates or evaluates has area grid rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - MapAreaNoArea: Map area no area value supplied by the caller for this operation.
    // Returns: Returns true when has area grid succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to MapTileTerrainData so callers do not duplicate validation, protocol, or persistence rules.
    public bool HasAreaGrid => (AreaFlags & MapStorePayloadConstants.MapAreaNoArea) == 0;
    // Method: HasHeightGrid
    // Purpose: Validates or evaluates has height grid rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - MapHeightNoHeight: Map height no height value supplied by the caller for this operation.
    // Returns: Returns true when has height grid succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to MapTileTerrainData so callers do not duplicate validation, protocol, or persistence rules.
    public bool HasHeightGrid => (HeightFlags & MapStorePayloadConstants.MapHeightNoHeight) == 0 && V9Heights is not null;
    // Method: Any
    // Purpose: Executes the any operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the bool has holes => holes. value produced by this operation.
    // Notes: This keeps the operation scoped to MapTileTerrainData so callers do not duplicate validation, protocol, or persistence rules.
    public bool HasHoles => Holes.Any(value => value != 0);
}
