
namespace EmulationServer.Tools.Extraction.Extraction;

public sealed class AssetExtractionResult
{
    private readonly List<string> _messages = [];

    public AssetExtractionResult(AssetExtractionKind kind)
    {
        Kind = kind;
    }

    public AssetExtractionKind Kind { get; }

    public int ExtractedFiles { get; private set; }

    public int SkippedFiles { get; private set; }

    public IReadOnlyList<string> Messages => _messages;

    public void AddExtractedFile()
    {
        ExtractedFiles++;
    }

    public void AddSkippedFile()
    {
        SkippedFiles++;
    }

    public void AddMessage(string message)
    {
        _messages.Add(message);
    }
}
