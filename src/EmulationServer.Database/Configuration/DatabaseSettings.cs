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
// File: src/EmulationServer.Database/Configuration/DatabaseSettings.cs
// Purpose: Contains database settings code for the database persistence, repository, and MySQL connectivity layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Database.Configuration;

// Type: DatabaseSettings
// Purpose: Provides database settings behavior for the database persistence, repository, and MySQL connectivity layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class DatabaseSettings
{

    // Property: Gets or sets the host value used by the database persistence, repository, and MySQL connectivity layer.
    // Value: host value exposed by the owning type.
    public string Host { get; init; } = "127.0.0.1";

    // Property: Gets or sets the port value used by the database persistence, repository, and MySQL connectivity layer.
    // Value: port value exposed by the owning type.
    public int Port { get; init; } = 3306;

    // Property: Gets or sets the database value used by the database persistence, repository, and MySQL connectivity layer.
    // Value: database value exposed by the owning type.
    public string Database { get; init; } = "realmd";

    // Property: Gets or sets the username value used by the database persistence, repository, and MySQL connectivity layer.
    // Value: username value exposed by the owning type.
    public string Username { get; init; } = "root";

    // Property: Gets or sets the password value used by the database persistence, repository, and MySQL connectivity layer.
    // Value: password value exposed by the owning type.
    public string Password { get; init; } = "";

    // Property: Gets or sets the minimum pool size value used by the database persistence, repository, and MySQL connectivity layer.
    // Value: minimum pool size value exposed by the owning type.
    public uint MinimumPoolSize { get; init; } = 5;

    // Property: Gets or sets the maximum pool size value used by the database persistence, repository, and MySQL connectivity layer.
    // Value: maximum pool size value exposed by the owning type.
    public uint MaximumPoolSize { get; init; } = 100;

    // Property: Gets or sets the use ssl value used by the database persistence, repository, and MySQL connectivity layer.
    // Value: use ssl value exposed by the owning type.
    public bool UseSsl { get; init; } = false;

    // Property: Gets or sets the connection timeout seconds value used by the database persistence, repository, and MySQL connectivity layer.
    // Value: connection timeout seconds value exposed by the owning type.
    public uint ConnectionTimeoutSeconds { get; init; } = 15;

    // Property: Gets or sets the default command timeout seconds value used by the database persistence, repository, and MySQL connectivity layer.
    // Value: default command timeout seconds value exposed by the owning type.
    public uint DefaultCommandTimeoutSeconds { get; init; } = 30;

    // Property: Gets or sets the connection idle timeout seconds value used by the database persistence, repository, and MySQL connectivity layer.
    // Value: connection idle timeout seconds value exposed by the owning type.
    public uint ConnectionIdleTimeoutSeconds { get; init; } = 180;

    // Property: Gets or sets the connection life time seconds value used by the database persistence, repository, and MySQL connectivity layer.
    // Value: connection life time seconds value exposed by the owning type.
    public uint ConnectionLifeTimeSeconds { get; init; } = 0;

    // Property: Gets or sets the keep alive seconds value used by the database persistence, repository, and MySQL connectivity layer.
    // Value: keep alive seconds value exposed by the owning type.
    public uint KeepAliveSeconds { get; init; } = 30;

    // Property: Gets or sets the connection reset value used by the database persistence, repository, and MySQL connectivity layer.
    // Value: connection reset value exposed by the owning type.
    public bool ConnectionReset { get; init; } = true;

    // Property: Gets or sets the use compression value used by the database persistence, repository, and MySQL connectivity layer.
    // Value: use compression value exposed by the owning type.
    public bool UseCompression { get; init; } = false;

    // Method: Validate
    // Purpose: Validates or evaluates validate rules for the database persistence, repository, and MySQL connectivity layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to DatabaseSettings so callers do not duplicate validation, protocol, or persistence rules.
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

        if (ConnectionIdleTimeoutSeconds == 0)
        {
            throw new InvalidOperationException("Database idle connection timeout must be greater than zero.");
        }
    }
}
