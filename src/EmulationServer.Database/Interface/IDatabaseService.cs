
using MySqlConnector;

namespace EmulationServer.Database.Interfaces;

public interface IDatabaseService : IAsyncDisposable
{
    ValueTask<MySqlConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);

    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    Task ValidateConnectionAsync(CancellationToken cancellationToken = default);
}
