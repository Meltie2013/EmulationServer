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

/**
  * File overview: src/EmulationServer.Database/Configuration/DatabaseSettings.cs
  * This file belongs to the server configuration loading and strongly typed settings portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Database.Configuration;

/**
  * Represents the database settings component in the server configuration loading and strongly typed settings area.
  * It keeps configuration values grouped by responsibility and prevents unrelated server code from reading raw INI keys.
  */
public sealed class DatabaseSettings
{
    /**
      * Gets or stores the host value used by DatabaseSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string Host { get; init; } = "127.0.0.1";

    /**
      * Gets or stores the port value used by DatabaseSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int Port { get; init; } = 3306;

    /**
      * Gets or stores the database value used by DatabaseSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string Database { get; init; } = "realmd";

    /**
      * Gets or stores the username value used by DatabaseSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string Username { get; init; } = "root";

    /**
      * Gets or stores the password value used by DatabaseSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string Password { get; init; } = "";

    /**
      * Gets or stores the minimum pool size value used by DatabaseSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public uint MinimumPoolSize { get; init; } = 5;

    /**
      * Gets or stores the maximum pool size value used by DatabaseSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public uint MaximumPoolSize { get; init; } = 100;

    /**
      * Gets or stores the use ssl value used by DatabaseSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public bool UseSsl { get; init; } = false;

    /**
      * Gets or stores the connection timeout seconds value used by DatabaseSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public uint ConnectionTimeoutSeconds { get; init; } = 15;

    /**
      * Gets or stores the default command timeout seconds value used by DatabaseSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public uint DefaultCommandTimeoutSeconds { get; init; } = 30;

    /**
      * Gets or stores how long idle pooled database connections may remain open.
      */
    public uint ConnectionIdleTimeoutSeconds { get; init; } = 180;

    /**
      * Gets or stores the maximum lifetime for pooled database connections.
      * A value of 0 leaves the provider default lifetime behavior in place.
      */
    public uint ConnectionLifeTimeSeconds { get; init; } = 0;

    /**
      * Gets or stores the TCP keep-alive interval used by database connections.
      * A value of 0 disables provider-level database keep-alive.
      */
    public uint KeepAliveSeconds { get; init; } = 30;

    /**
      * Gets or stores whether pooled database connections are reset before reuse.
      * Keep this true unless all session state is tightly controlled.
      */
    public bool ConnectionReset { get; init; } = true;

    /**
      * Gets or stores whether MySQL protocol compression should be enabled.
      * Compression is disabled by default because local/LAN database traffic is usually faster without it.
      */
    public bool UseCompression { get; init; } = false;

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of DatabaseSettings and keeps this workflow isolated from the caller.
      */
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
