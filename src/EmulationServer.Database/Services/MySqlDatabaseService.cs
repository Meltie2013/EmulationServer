//
// Copyright (C) 2026 Emulation Server Project
//
// This program is free software. You can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation. either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY. Without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// File: src/EmulationServer.Database/Services/MySqlDatabaseService.cs
// Purpose: Contains my SQL database service code for the database persistence, repository, and MySQL connectivity layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Database.Configuration;
using EmulationServer.Database.Interfaces;

using MySqlConnector;

namespace EmulationServer.Database.Services;

// Type: MySqlDatabaseService
// Purpose: Provides my SQL database service behavior for the database persistence, repository, and MySQL connectivity layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class MySqlDatabaseService : IDatabaseService
{

    // Field: Stores the connection string state used by the database persistence, repository, and MySQL connectivity layer.
    // Value: current connection string backing value maintained by the owning type.
    private readonly string _connectionString;

    // Constructor: MySqlDatabaseService
    // Purpose: Initializes a new MySqlDatabaseService instance with dependencies and values required by the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - settings: Settings values that control how this operation should run.
    // Returns: none.
    // Notes: This keeps the operation scoped to MySqlDatabaseService so callers do not duplicate validation, protocol, or persistence rules.
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
            ConnectionIdleTimeout = settings.ConnectionIdleTimeoutSeconds,
            ConnectionLifeTime = settings.ConnectionLifeTimeSeconds,
            Keepalive = settings.KeepAliveSeconds,
            ConnectionReset = settings.ConnectionReset,
            UseCompression = settings.UseCompression,
        };

        _connectionString = builder.ConnectionString;
    }

    // Method: CreateConnectionAsync
    // Purpose: Applies create connection changes for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to MySqlDatabaseService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: TestConnectionAsync
    // Purpose: Executes the test connection operation for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous Boolean result that is true when test connection async succeeds or the requested condition is met.
    // Notes: This keeps the operation scoped to MySqlDatabaseService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: ValidateConnectionAsync
    // Purpose: Validates or evaluates validate connection rules for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MySqlDatabaseService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await CreateConnectionAsync(cancellationToken);

        bool pingSucceeded = await connection.PingAsync(cancellationToken);
        if (!pingSucceeded)
        {
            throw new Exception("Failed to ping the MySQL database.");
        }
    }

    // Method: DisposeAsync
    // Purpose: Controls the dispose lifecycle step for the database persistence, repository, and MySQL connectivity layer.
    // Parameters: none.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to MySqlDatabaseService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
