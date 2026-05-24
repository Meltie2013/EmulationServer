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
  * File overview: src/ProxyServer/Configuration/ProxyDependencySettings.cs
  * Documents the ProxyDependencySettings source file in the proxy startup, service discovery, and client-routing support area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.ProxyServer.Configuration;

/**
  * Owns the proxy dependency settings behavior for the proxy startup, service discovery, and client-routing support layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class ProxyDependencySettings
{
    /**
      * Gets or stores the critical servers value used by ProxyDependencySettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public IReadOnlySet<string> CriticalServers { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "WorldServer",
    };

    /**
      * Gets or stores the non critical servers value used by ProxyDependencySettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public IReadOnlySet<string> NonCriticalServers { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "MapServer",
        "InstanceServer",
    };

    /**
      * Gets or stores the critical server packet timeout value used by ProxyDependencySettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public TimeSpan CriticalServerPacketTimeout { get; init; } = TimeSpan.FromSeconds(45);

    /**
      * Gets or stores the non critical reconnect report interval value used by ProxyDependencySettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public TimeSpan NonCriticalReconnectReportInterval { get; init; } = TimeSpan.FromSeconds(30);

    /**
      * Gets or stores the maximum window where ProxyServer keeps reporting that a non-critical service is down.
      * Once this timeout expires, ProxyServer resets the dependency to passive wait mode and stops repeated reconnect warnings.
      */
    public TimeSpan NonCriticalReconnectTimeout { get; init; } = TimeSpan.FromSeconds(120);

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of ProxyDependencySettings and keeps this workflow isolated from the caller.
      */
    public void Validate()
    {
        if (CriticalServers.Count == 0)
        {
            throw new InvalidOperationException("Proxy dependency policy requires at least one critical server.");
        }

        if (CriticalServerPacketTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Proxy critical server packet timeout must be greater than zero.");
        }

        if (NonCriticalReconnectReportInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Proxy non-critical reconnect report interval must be greater than zero.");
        }

        if (NonCriticalReconnectTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Proxy non-critical reconnect timeout must be greater than zero.");
        }

        foreach (string serverName in CriticalServers)
        {
            ValidateServerName(serverName);
        }

        foreach (string serverName in NonCriticalServers)
        {
            ValidateServerName(serverName);

            if (CriticalServers.Contains(serverName))
            {
                throw new InvalidOperationException($"Server '{serverName}' cannot be both critical and non-critical.");
            }
        }
    }

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of ProxyDependencySettings and keeps this workflow isolated from the caller.
      */
    private static void ValidateServerName(string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
        {
            throw new InvalidOperationException("Proxy dependency settings contain an empty server name.");
        }
    }
}
