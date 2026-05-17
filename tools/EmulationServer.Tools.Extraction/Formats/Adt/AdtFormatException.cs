
namespace EmulationServer.Tools.Extraction.Formats.Adt;

public sealed class AdtFormatException : Exception
{
    public AdtFormatException(string message)
        : base(message)
    {
    }

    public AdtFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
