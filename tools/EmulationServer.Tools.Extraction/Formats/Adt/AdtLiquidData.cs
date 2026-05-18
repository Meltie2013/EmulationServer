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

using EmulationServer.Tools.Extraction.Formats.Maps;

namespace EmulationServer.Tools.Extraction.Formats.Adt;

public sealed class AdtLiquidData
{
    public ushort[,] Entry { get; } = new ushort[16, 16];

    public byte[,] Flags { get; } = new byte[16, 16];

    public bool[,] Show { get; } = new bool[128, 128];

    public float[,] Heights { get; } = new float[129, 129];

    public int MclqCells { get; set; }

    public int Mh2oCells { get; set; }

    public int VisibleLiquidTiles { get; private set; }

    public bool HasLiquid => VisibleLiquidTiles > 0;

    public int LiquidCells
    {
        get
        {
            int count = 0;

            foreach (byte flags in Flags)
            {
                if (flags != MapFormatConstants.MapLiquidTypeNoWater)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public void MarkVisible(int y, int x)
    {
        if (y < 0 || y >= 128 || x < 0 || x >= 128)
        {
            return;
        }

        if (Show[y, x])
        {
            return;
        }

        Show[y, x] = true;
        VisibleLiquidTiles++;
    }
}
