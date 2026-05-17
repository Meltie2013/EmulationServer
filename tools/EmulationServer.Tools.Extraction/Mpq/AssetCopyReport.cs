
namespace EmulationServer.Tools.Extraction.Mpq;

public sealed class AssetCopyReport
{
    public int CandidateFiles { get; set; }

    public int ExtractedFiles { get; set; }

    public int SkippedExisting { get; set; }

    public int FailedFiles { get; set; }

    public int UnknownFileNames { get; set; }

    public List<string> Messages { get; } = [];
}
