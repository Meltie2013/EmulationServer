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
using EmulationServer.Database.Services;

using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace EmulationServer.Tests.Database;

public sealed class MySqlConnectionTests
{
    private static DatabaseSettings LoadSettings()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(
                "appsettings.json",
                optional: false,
                reloadOnChange: false)
            .AddJsonFile(
                "appsettings.local.json",
                optional: true,
                reloadOnChange: false)
            .Build();

        return new DatabaseSettings
        {
            Host = GetString(configuration, "Database:Host", "EMULATIONSERVER_TEST_DATABASE_HOST"),
            Port = GetInt(configuration, "Database:Port", "EMULATIONSERVER_TEST_DATABASE_PORT"),
            Database = GetString(configuration, "Database:Database", "EMULATIONSERVER_TEST_DATABASE_NAME"),
            Username = GetString(configuration, "Database:Username", "EMULATIONSERVER_TEST_DATABASE_USERNAME"),
            Password = GetString(configuration, "Database:Password", "EMULATIONSERVER_TEST_DATABASE_PASSWORD"),
            MinimumPoolSize = GetUInt(configuration, "Database:MinimumPoolSize", "EMULATIONSERVER_TEST_DATABASE_MINIMUM_POOL_SIZE"),
            MaximumPoolSize = GetUInt(configuration, "Database:MaximumPoolSize", "EMULATIONSERVER_TEST_DATABASE_MAXIMUM_POOL_SIZE"),
            UseSsl = GetBool(configuration, "Database:UseSsl", "EMULATIONSERVER_TEST_DATABASE_USE_SSL"),
            ConnectionTimeoutSeconds = GetUInt(configuration, "Database:ConnectionTimeoutSeconds", "EMULATIONSERVER_TEST_DATABASE_CONNECTION_TIMEOUT_SECONDS"),
            DefaultCommandTimeoutSeconds = GetUInt(configuration, "Database:DefaultCommandTimeoutSeconds", "EMULATIONSERVER_TEST_DATABASE_DEFAULT_COMMAND_TIMEOUT_SECONDS"),
        };
    }

    [DatabaseIntegrationFact]
    public async Task DatabaseConnection_ShouldSucceed()
    {
        DatabaseSettings settings = LoadSettings();

        await using var database = new MySqlDatabaseService(settings);

        bool connected = await database.TestConnectionAsync();

        Assert.True(connected, "Failed to connect to the MySQL server.");
    }

    [DatabaseIntegrationFact]
    public async Task DatabaseQuery_ShouldExecute()
    {
        DatabaseSettings settings = LoadSettings();

        await using var database = new MySqlDatabaseService(settings);
        await using MySqlConnection connection = await database.CreateConnectionAsync();

        const string sql = "SELECT 1";

        await using var command = new MySqlCommand(sql, connection);

        object? result = await command.ExecuteScalarAsync();

        Assert.NotNull(result);

        int value = Convert.ToInt32(result);

        Assert.Equal(1, value);
    }

    private static string GetString(IConfiguration configuration, string configurationKey, string environmentVariableName)
    {
        string? environmentValue = Environment.GetEnvironmentVariable(environmentVariableName);
        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            return environmentValue;
        }

        return configuration[configurationKey]
            ?? throw new InvalidOperationException($"Missing required test setting '{configurationKey}'.");
    }

    private static int GetInt(IConfiguration configuration, string configurationKey, string environmentVariableName)
    {
        string value = GetString(configuration, configurationKey, environmentVariableName);
        return int.Parse(value);
    }

    private static uint GetUInt(IConfiguration configuration, string configurationKey, string environmentVariableName)
    {
        string value = GetString(configuration, configurationKey, environmentVariableName);
        return uint.Parse(value);
    }

    private static bool GetBool(IConfiguration configuration, string configurationKey, string environmentVariableName)
    {
        string value = GetString(configuration, configurationKey, environmentVariableName);
        return bool.Parse(value);
    }
}
