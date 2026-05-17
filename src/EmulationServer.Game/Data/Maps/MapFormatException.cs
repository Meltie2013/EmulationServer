namespace EmulationServer.Game.Data.Maps;

public sealed class MapFormatException : IOException
{
    public MapFormatException(string message)
        : base(message)
    {
    }

    public MapFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
