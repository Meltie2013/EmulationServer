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
  * This file belongs to the project runtime logic and supporting data models portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Database.Services;

/**
  * Represents the my sql database service component in the project runtime logic and supporting data models area.
  * It encapsulates a focused runtime behavior so callers can use a small public API instead of duplicating workflow code.
  */
public sealed class MySqlDatabaseService : IDatabaseService
{
    /**
      * Stores the connection string dependency or runtime value for MySqlDatabaseService.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly string _connectionString;

    /**
      * Creates a new MySqlDatabaseService instance and stores the dependencies required by the component.
      * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
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
      * Creates a new object with validated defaults so callers receive a ready-to-use instance.
      * The method is part of MySqlDatabaseService and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
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
      * Performs the test connection async operation for MySqlDatabaseService.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
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
      * Releases owned resources and ensures background work is stopped safely.
      * The method is part of MySqlDatabaseService and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
