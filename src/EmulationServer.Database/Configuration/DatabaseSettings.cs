
namespace EmulationServer.Database.Configuration;

public sealed class DatabaseSettings
{
    public string Host { get; init; } = "127.0.0.1";

    public int Port { get; init; } = 3306;

    public string Database { get; init; } = "realmd";

    public string Username { get; init; } = "root";

    public string Password { get; init; } = "";

    public uint MinimumPoolSize { get; init; } = 5;

    public uint MaximumPoolSize { get; init; } = 100;

    public bool UseSsl { get; init; } = false;

    public uint ConnectionTimeoutSeconds { get; init; } = 15;

    public uint DefaultCommandTimeoutSeconds { get; init; } = 30;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host))
        {
            throw new InvalidOperationException("Database host is required.");
        }

        if (Port is < 1 or > 65535)
        {
            throw new InvalidOperationException($"Invalid database port: {Port}. Valid range is 1-65535.");
        }

        if (string.IsNullOrWhiteSpace(Database))
        {
            throw new InvalidOperationException("Database name is required.");
        }

        if (string.IsNullOrWhiteSpace(Username))
        {
            throw new InvalidOperationException("Database username is required.");
        }

        if (MinimumPoolSize > MaximumPoolSize)
        {
            throw new InvalidOperationException("Database minimum pool size cannot be greater than maximum pool size.");
        }

        if (MaximumPoolSize == 0)
        {
            throw new InvalidOperationException("Database maximum pool size must be greater than zero.");
        }

        if (ConnectionTimeoutSeconds == 0)
        {
            throw new InvalidOperationException("Database connection timeout must be greater than zero.");
        }

        if (DefaultCommandTimeoutSeconds == 0)
        {
            throw new InvalidOperationException("Database command timeout must be greater than zero.");
        }
    }
}
