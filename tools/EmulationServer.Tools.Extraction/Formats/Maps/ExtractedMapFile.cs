
namespace EmulationServer.Tools.Extraction.Formats.Maps;

public sealed record ExtractedMapFile(
    string Path,
    MapFileHeader Header,
    MapAreaSection? Area,
    MapHeightSection? Height,
    MapLiquidSection? Liquid,
    int HolesByteCount);
