
namespace EmulationServer.Tools.Extraction.Formats.Maps.Conversion;

public sealed record MapTileConversionReport(
    bool HasLiquidData,
    int MclqCells,
    int Mh2oCells,
    int LiquidCells,
    int VisibleLiquidTiles,
    int LiquidSectionBytes)
{
    public string GetLiquidStatus()
    {
        return $"{(HasLiquidData ? "yes" : "no")} " +
               $"(MCLQ cells={MclqCells}, " +
               $"MH2O cells={Mh2oCells}, " +
               $"liquid cells={LiquidCells}, " +
               $"visible tiles={VisibleLiquidTiles}, " +
               $"MLIQ bytes={LiquidSectionBytes})";
    }
}
