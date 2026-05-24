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

/**
  * File overview: tests/EmulationServer.Tests/Database/MySqlConnectionTests.cs
  * Documents the MySqlConnectionTests source file in the automated test coverage for server behavior and data helpers area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Tests.Database;

/**
  * Owns the my sql connection tests behavior for the automated test coverage for server behavior and data helpers layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class MySqlConnectionTests
{
    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of MySqlConnectionTests and keeps this workflow isolated from the caller.
      */
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

    /**
      * Performs the database connection should succeed operation for the automated test coverage for server behavior and data helpers workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    [DatabaseIntegrationFact]
    public async Task DatabaseConnection_ShouldSucceed()
    {
        DatabaseSettings settings = LoadSettings();

        await using var database = new MySqlDatabaseService(settings);

        bool connected = await database.TestConnectionAsync();

        Assert.True(connected, "Failed to connect to the MySQL server.");
    }

    /**
      * Performs the database query should execute operation for the automated test coverage for server behavior and data helpers workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of MySqlConnectionTests and keeps this workflow isolated from the caller.
      */
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

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of MySqlConnectionTests and keeps this workflow isolated from the caller.
      */
    private static int GetInt(IConfiguration configuration, string configurationKey, string environmentVariableName)
    {
        string value = GetString(configuration, configurationKey, environmentVariableName);
        return int.Parse(value);
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of MySqlConnectionTests and keeps this workflow isolated from the caller.
      */
    private static uint GetUInt(IConfiguration configuration, string configurationKey, string environmentVariableName)
    {
        string value = GetString(configuration, configurationKey, environmentVariableName);
        return uint.Parse(value);
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of MySqlConnectionTests and keeps this workflow isolated from the caller.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    private static bool GetBool(IConfiguration configuration, string configurationKey, string environmentVariableName)
    {
        string value = GetString(configuration, configurationKey, environmentVariableName);
        return bool.Parse(value);
    }
}
