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

using EmulationServer.Database.Configuration;
using EmulationServer.Database.Interfaces;

using MySqlConnector;

/**
  * File overview: src/EmulationServer.Database/Services/MySqlDatabaseService.cs
  * Documents the MySqlDatabaseService source file in the database access, account persistence, and MySQL connectivity area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Database.Services;

/**
  * Owns the my sql database service behavior for the database access, account persistence, and MySQL connectivity layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class MySqlDatabaseService : IDatabaseService
{
    /**
      * Holds the private connection string state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly string _connectionString;

    /**
      * Initializes a new MySqlDatabaseService instance with the dependencies required by the database access, account persistence, and MySQL connectivity workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: settings.
      */
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

    /**
      * Creates the connection result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Performs the test connection operation for the database access, account persistence, and MySQL connectivity workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of MySqlDatabaseService and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async Task ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = await CreateConnectionAsync(cancellationToken);

        bool pingSucceeded = await connection.PingAsync();
        if (!pingSucceeded)
        {
            throw new Exception("Failed to ping the MySQL database.");
        }
    }

    /**
      * Stops the dispose workflow and releases owned runtime resources in a controlled order.
      * Shutdown logic is centralized to avoid dangling connections, incomplete saves, or partially registered services.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
