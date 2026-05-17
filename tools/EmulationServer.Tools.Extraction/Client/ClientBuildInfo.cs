
namespace EmulationServer.Tools.Extraction.Client;

public sealed record ClientBuildInfo(
    ushort Build,
    string Version,
    SupportedClientExpansion Expansion,
    string MangosLine)
{
    public override string ToString()
    {
        return $"{Version} ({Build}) - {Expansion} / {MangosLine}";
    }
}
