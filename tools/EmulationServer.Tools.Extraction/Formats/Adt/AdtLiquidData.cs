
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
