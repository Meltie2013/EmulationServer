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

using EmulationServer.Shared.Data.MapStore;

namespace EmulationServer.Game.Data.Maps;

/**
  * Provides typed terrain queries for one loaded mapstore tile.
  */
public sealed class MapTileTerrainQueryService
{
    private readonly MapTileTerrainData _terrain;

    public MapTileTerrainQueryService(MapTileTerrainData terrain)
    {
        _terrain = terrain ?? throw new ArgumentNullException();
    }

    public ushort GetAreaFlag(int cellX, int cellY)
    {
        ValidateCellCoordinate(cellX, nameof(cellX));
        ValidateCellCoordinate(cellY, nameof(cellY));
        return _terrain.AreaGrid[cellY * MapStorePayloadConstants.CellsPerGrid + cellX];
    }

    public ushort GetHoleMask(int cellX, int cellY)
    {
        ValidateCellCoordinate(cellX, nameof(cellX));
        ValidateCellCoordinate(cellY, nameof(cellY));
        return _terrain.Holes[cellY * MapStorePayloadConstants.CellsPerGrid + cellX];
    }

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

    private float GetV9Height(int vertexX, int vertexY)
    {
        return _terrain.V9Heights![vertexY * (MapStorePayloadConstants.GridSize + 1) + vertexX];
    }

    private static void ValidateCellCoordinate(int value, string parameterName)
    {
        if (value < 0 || value >= MapStorePayloadConstants.CellsPerGrid)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"Cell coordinate must be between 0 and {MapStorePayloadConstants.CellsPerGrid - 1}.");
        }
    }
}
