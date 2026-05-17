
namespace EmulationServer.Tools.Extraction.Formats.Dbc;

public sealed record DbcHeader(
    string Magic,
    int RecordCount,
    int FieldCount,
    int RecordSize,
    int StringBlockSize)
{
    public const string ExpectedMagic = "WDBC";
}
