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
// File: src/EmulationServer.Game/Data/Maps/MapTileTerrainQueryService.cs
// Purpose: Contains map tile terrain query service code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Shared.Data.MapStore;

namespace EmulationServer.Game.Data.Maps;

// Type: MapTileTerrainQueryService
// Purpose: Provides map tile terrain query service behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class MapTileTerrainQueryService
{
    // Field: Stores the terrain state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current terrain backing value maintained by the owning type.
    private readonly MapTileTerrainData _terrain;

    // Constructor: MapTileTerrainQueryService
    // Purpose: Initializes a new MapTileTerrainQueryService instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - terrain: Terrain value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapTileTerrainQueryService so callers do not duplicate validation, protocol, or persistence rules.
    public MapTileTerrainQueryService(MapTileTerrainData terrain)
    {
        _terrain = terrain ?? throw new ArgumentNullException();
    }

    // Method: GetAreaFlag
    // Purpose: Retrieves get area flag data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - cellX: Cell X value supplied by the caller for this operation.
    // - cellY: Cell Y value supplied by the caller for this operation.
    // Returns: Returns the ushort value produced by this operation.
    // Notes: This keeps the operation scoped to MapTileTerrainQueryService so callers do not duplicate validation, protocol, or persistence rules.
    public ushort GetAreaFlag(int cellX, int cellY)
    {
        ValidateCellCoordinate(cellX, nameof(cellX));
        ValidateCellCoordinate(cellY, nameof(cellY));
        return _terrain.AreaGrid[cellY * MapStorePayloadConstants.CellsPerGrid + cellX];
    }

    // Method: GetHoleMask
    // Purpose: Retrieves get hole mask data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - cellX: Cell X value supplied by the caller for this operation.
    // - cellY: Cell Y value supplied by the caller for this operation.
    // Returns: Returns the ushort value produced by this operation.
    // Notes: This keeps the operation scoped to MapTileTerrainQueryService so callers do not duplicate validation, protocol, or persistence rules.
    public ushort GetHoleMask(int cellX, int cellY)
    {
        ValidateCellCoordinate(cellX, nameof(cellX));
        ValidateCellCoordinate(cellY, nameof(cellY));
        return _terrain.Holes[cellY * MapStorePayloadConstants.CellsPerGrid + cellX];
    }

    // Method: IsHole
    // Purpose: Validates or evaluates is hole rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - cellX: Cell X value supplied by the caller for this operation.
    // - cellY: Cell Y value supplied by the caller for this operation.
    // - holeX: Hole X value supplied by the caller for this operation.
    // - holeY: Hole Y value supplied by the caller for this operation.
    // Returns: Returns true when is hole succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to MapTileTerrainQueryService so callers do not duplicate validation, protocol, or persistence rules.
    public bool IsHole(int cellX, int cellY, int holeX, int holeY)
    {
        ValidateCellCoordinate(cellX, nameof(cellX));
        ValidateCellCoordinate(cellY, nameof(cellY));
        if (holeX < 0 || holeX >= 4)
        {
            throw new ArgumentOutOfRangeException(nameof(holeX), holeX, "Hole X coordinate must be between 0 and 3.");
        }

        if (holeY < 0 || holeY >= 4)
        {
            throw new ArgumentOutOfRangeException(nameof(holeY), holeY, "Hole Y coordinate must be between 0 and 3.");
        }

        ushort mask = GetHoleMask(cellX, cellY);
        int bit = holeY * 4 + holeX;
        return (mask & (1 << bit)) != 0;
    }

    // Method: TryGetVertexHeight
    // Purpose: Attempts to retrieve or parse try get vertex height data without treating normal misses as failures.
    // Parameters:
    // - vertexX: Vertex X value supplied by the caller for this operation.
    // - vertexY: Vertex Y value supplied by the caller for this operation.
    // - height: Height value supplied by the caller for this operation.
    // Returns: Returns true when try get vertex height succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to MapTileTerrainQueryService so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetVertexHeight(int vertexX, int vertexY, out float height)
    {
        height = 0.0f;
        if (vertexX < 0 || vertexX > MapStorePayloadConstants.GridSize || vertexY < 0 || vertexY > MapStorePayloadConstants.GridSize)
        {
            return false;
        }

        if (!_terrain.HasHeightGrid || _terrain.V9Heights is null)
        {
            height = _terrain.GridHeight;
            return true;
        }

        height = _terrain.V9Heights[vertexY * (MapStorePayloadConstants.GridSize + 1) + vertexX];
        return true;
    }

    // Method: SampleHeight
    // Purpose: Executes the sample height operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - gridX: Grid X value supplied by the caller for this operation.
    // - gridY: Grid Y value supplied by the caller for this operation.
    // Returns: Returns the float value produced by this operation.
    // Notes: This keeps the operation scoped to MapTileTerrainQueryService so callers do not duplicate validation, protocol, or persistence rules.
    public float SampleHeight(float gridX, float gridY)
    {
        if (!_terrain.HasHeightGrid || _terrain.V9Heights is null)
        {
            return _terrain.GridHeight;
        }

        float clampedX = Math.Clamp(gridX, 0.0f, MapStorePayloadConstants.GridSize);
        float clampedY = Math.Clamp(gridY, 0.0f, MapStorePayloadConstants.GridSize);
        int x0 = Math.Clamp((int)MathF.Floor(clampedX), 0, MapStorePayloadConstants.GridSize);
        int y0 = Math.Clamp((int)MathF.Floor(clampedY), 0, MapStorePayloadConstants.GridSize);
        int x1 = Math.Clamp(x0 + 1, 0, MapStorePayloadConstants.GridSize);
        int y1 = Math.Clamp(y0 + 1, 0, MapStorePayloadConstants.GridSize);
        float tx = clampedX - x0;
        float ty = clampedY - y0;

        float h00 = GetV9Height(x0, y0);
        float h10 = GetV9Height(x1, y0);
        float h01 = GetV9Height(x0, y1);
        float h11 = GetV9Height(x1, y1);
        float hx0 = h00 + (h10 - h00) * tx;
        float hx1 = h01 + (h11 - h01) * tx;
        return hx0 + (hx1 - hx0) * ty;
    }

    // Method: GetV9Height
    // Purpose: Retrieves get V9 height data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - vertexX: Vertex X value supplied by the caller for this operation.
    // - vertexY: Vertex Y value supplied by the caller for this operation.
    // Returns: Returns the float value produced by this operation.
    // Notes: This keeps the operation scoped to MapTileTerrainQueryService so callers do not duplicate validation, protocol, or persistence rules.
    private float GetV9Height(int vertexX, int vertexY)
    {
        return _terrain.V9Heights![vertexY * (MapStorePayloadConstants.GridSize + 1) + vertexX];
    }

    // Method: ValidateCellCoordinate
    // Purpose: Validates or evaluates validate cell coordinate rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // - parameterName: Parameter name value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapTileTerrainQueryService so callers do not duplicate validation, protocol, or persistence rules.
    private static void ValidateCellCoordinate(int value, string parameterName)
    {
        if (value < 0 || value >= MapStorePayloadConstants.CellsPerGrid)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"Cell coordinate must be between 0 and {MapStorePayloadConstants.CellsPerGrid - 1}.");
        }
    }
}
