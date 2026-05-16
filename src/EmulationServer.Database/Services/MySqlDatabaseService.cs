
using EmulationServer.Database.Configuration;
using EmulationServer.Database.Interfaces;

using MySqlConnector;

namespace EmulationServer.Database.Services;

public sealed class MySqlDatabaseService : IDatabaseService
{
    private readonly string _connectionString;

    public MySqlDatabaseService(DatabaseSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        var builder = new MySqlConnectionStringBuilder
        {
            Server = settings.Host,
            Port = (uint)settings.Port,
            Database = settings.Database,
            UserID = settings.Username,
            Password = settings.Password,

            Pooling = true,
            MinimumPoolSize = settings.MinimumPoolSize,
            MaximumPoolSize = settings.MaximumPoolSize,

            SslMode = settings.UseSsl ? MySqlSslMode.Required : MySqlSslMode.None,

            ConnectionTimeout = settings.ConnectionTimeoutSeconds,
            DefaultCommandTimeout = settings.DefaultCommandTimeoutSeconds,
        };

        _connectionString = builder.ConnectionString;
    }

    public async ValueTask<MySqlConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        MySqlConnection connection = new(_connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using MySqlConnection connection = await CreateConnectionAsync(cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    public async Task ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await CreateConnectionAsync(cancellationToken);

        bool pingSucceeded = await connection.PingAsync();
        if (!pingSucceeded)
        {
            throw new Exception("Failed to ping the MySQL database.");
        }
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
