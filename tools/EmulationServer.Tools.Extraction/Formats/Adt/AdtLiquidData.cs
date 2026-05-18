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

/**
  * File overview: tools/EmulationServer.Tools.Extraction/Formats/Adt/AdtLiquidData.cs
  * This file belongs to the developer tooling for data extraction, validation, and diagnostics portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Tools.Extraction.Formats.Adt;

/**
  * Represents the adt liquid data component in the developer tooling for data extraction, validation, and diagnostics area.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public sealed class AdtLiquidData
{
    /**
      * Gets or stores the entry value used by AdtLiquidData.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public ushort[,] Entry { get; } = new ushort[16, 16];

    /**
      * Gets or stores the flags value used by AdtLiquidData.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public byte[,] Flags { get; } = new byte[16, 16];

    /**
      * Gets or stores the show value used by AdtLiquidData.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public bool[,] Show { get; } = new bool[128, 128];

    /**
      * Gets or stores the heights value used by AdtLiquidData.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public float[,] Heights { get; } = new float[129, 129];

    /**
      * Gets or stores the mclq cells value used by AdtLiquidData.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int MclqCells { get; set; }

    /**
      * Gets or stores the mh2o cells value used by AdtLiquidData.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int Mh2oCells { get; set; }

    /**
      * Gets or stores the visible liquid tiles value used by AdtLiquidData.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int VisibleLiquidTiles { get; private set; }

    /**
      * Gets or stores the has liquid value used by AdtLiquidData.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public bool HasLiquid => VisibleLiquidTiles > 0;

    /**
      * Gets or stores the liquid cells value used by AdtLiquidData.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
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

    /**
      * Performs the mark visible operation for AdtLiquidData.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
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
