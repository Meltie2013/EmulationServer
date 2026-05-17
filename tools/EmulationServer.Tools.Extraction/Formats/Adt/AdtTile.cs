
namespace EmulationServer.Tools.Extraction.Formats.Adt;

public sealed class AdtTile
{
    public AdtTile(string path, IReadOnlyList<AdtCell> cells, AdtLiquidData liquid)
    {
        Path = path;
        Cells = cells;
        Liquid = liquid;
    }

    public string Path { get; }

    public IReadOnlyList<AdtCell> Cells { get; }

    public AdtLiquidData Liquid { get; }
}
