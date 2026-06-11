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
  * Provides typed liquid queries for one loaded mapstore tile.
  */
public sealed class MapTileLiquidQueryService
{
    private readonly MapTileLiquidData _liquid;

    public MapTileLiquidQueryService(MapTileLiquidData liquid)
    {
        _liquid = liquid ?? throw new ArgumentNullException();
    }

    public bool HasLiquid => _liquid.HasLiquid;

    public bool TryGetLiquidInfo(float gridX, float gridY, out MapTileLiquidInfo liquidInfo)
    {
        liquidInfo = default;
        if (!_liquid.HasLiquid || _liquid.Width == 0 || _liquid.Height == 0)
        {
            return false;
        }

        int sampleX = Math.Clamp((int)MathF.Floor(gridX), 0, MapStorePayloadConstants.GridSize - 1);
        int sampleY = Math.Clamp((int)MathF.Floor(gridY), 0, MapStorePayloadConstants.GridSize - 1);
        int lastLiquidX = _liquid.OffsetX + _liquid.Width - 2;
        int lastLiquidY = _liquid.OffsetY + _liquid.Height - 2;
        if (sampleX < _liquid.OffsetX || sampleY < _liquid.OffsetY || sampleX > lastLiquidX || sampleY > lastLiquidY)
        {
            return false;
        }

        int cellX = Math.Clamp(sampleX / 8, 0, MapStorePayloadConstants.CellsPerGrid - 1);
        int cellY = Math.Clamp(sampleY / 8, 0, MapStorePayloadConstants.CellsPerGrid - 1);
        ushort liquidTypeId = _liquid.LiquidTypeIds is null
            ? _liquid.LiquidTypeId
            : _liquid.LiquidTypeIds[cellY * MapStorePayloadConstants.CellsPerGrid + cellX];
        byte liquidFlag = _liquid.LiquidFlags is null
            ? MapStorePayloadConstants.MapLiquidTypeNoWater
            : _liquid.LiquidFlags[cellY * MapStorePayloadConstants.CellsPerGrid + cellX];

        float height = SampleLiquidHeight(gridX, gridY);
        liquidInfo = new MapTileLiquidInfo(liquidTypeId, liquidFlag, height, _liquid.HasLiquidHeightGrid);
        return true;
    }

    private float SampleLiquidHeight(float gridX, float gridY)
    {
        if (_liquid.LiquidHeights is null || _liquid.Width == 0 || _liquid.Height == 0)
        {
            return _liquid.LiquidLevel;
        }

        float localX = Math.Clamp(gridX - _liquid.OffsetX, 0.0f, (float)Math.Max(0, _liquid.Width - 1));
        float localY = Math.Clamp(gridY - _liquid.OffsetY, 0.0f, (float)Math.Max(0, _liquid.Height - 1));
        int x0 = Math.Clamp((int)MathF.Floor(localX), 0, _liquid.Width - 1);
        int y0 = Math.Clamp((int)MathF.Floor(localY), 0, _liquid.Height - 1);
        int x1 = Math.Clamp(x0 + 1, 0, _liquid.Width - 1);
        int y1 = Math.Clamp(y0 + 1, 0, _liquid.Height - 1);
        float tx = localX - x0;
        float ty = localY - y0;

        float h00 = GetHeight(x0, y0);
        float h10 = GetHeight(x1, y0);
        float h01 = GetHeight(x0, y1);
        float h11 = GetHeight(x1, y1);
        float hx0 = h00 + (h10 - h00) * tx;
        float hx1 = h01 + (h11 - h01) * tx;
        return hx0 + (hx1 - hx0) * ty;
    }

    private float GetHeight(int x, int y)
    {
        return _liquid.LiquidHeights![y * _liquid.Width + x];
    }
}
