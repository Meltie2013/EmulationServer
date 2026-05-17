
namespace EmulationServer.Tools.Extraction.Formats.Maps.Conversion;

public sealed class MapConversionResult
{
    public int SourceFiles { get; set; }

    public int ConvertedFiles { get; set; }

    public int SkippedFiles { get; set; }

    public int FailedFiles { get; set; }

    public List<string> Messages { get; } = [];
}
