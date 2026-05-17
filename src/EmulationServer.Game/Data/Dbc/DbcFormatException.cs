namespace EmulationServer.Game.Data.Dbc;

public sealed class DbcFormatException : IOException
{
    public DbcFormatException(string message)
        : base(message)
    {
    }

    public DbcFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
