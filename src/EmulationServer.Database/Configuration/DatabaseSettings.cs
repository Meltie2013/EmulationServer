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

namespace EmulationServer.Database.Configuration;

public sealed class DatabaseSettings
{
    public string Host { get; init; } = "127.0.0.1";

    public int Port { get; init; } = 3306;

    public string Database { get; init; } = "realmd";

    public string Username { get; init; } = "root";

    public string Password { get; init; } = "";

    public uint MinimumPoolSize { get; init; } = 5;

    public uint MaximumPoolSize { get; init; } = 100;

    public bool UseSsl { get; init; } = false;

    public uint ConnectionTimeoutSeconds { get; init; } = 15;

    public uint DefaultCommandTimeoutSeconds { get; init; } = 30;

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
    }
}
