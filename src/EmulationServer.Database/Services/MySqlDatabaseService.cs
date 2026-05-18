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
