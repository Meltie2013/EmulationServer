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

using EmulationServer.Core.Configuration;
using EmulationServer.Shared.Configuration;

/**
  * File overview: src/ProxyServer/Configuration/ProxyServerConfigurationLoader.cs
  * Documents the ProxyServerConfigurationLoader source file in the proxy startup, service discovery, and client-routing support area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.ProxyServer.Configuration;

/**
  * Owns the proxy server configuration loader behavior for the proxy startup, service discovery, and client-routing support layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public static class ProxyServerConfigurationLoader
{
    /**
      * Defines the constant value for proxy server section.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const string ProxyServerSection = "ProxyServer";

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of ProxyServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    public static ProxyServerSettings Load(string path)
    {
        string fullPath = Path.GetFullPath(path);

        IniConfiguration configuration = IniConfiguration.Load(fullPath);

        ProxyServerSettings settings = new()
        {
            Logging = ServerConfigurationLoader.LoadLoggingSettings(configuration, fullPath, "ProxyServer"),

            InternalNetwork = ServerConfigurationLoader.LoadInternalNetworkSettings(
                configuration,
                ProxyServerSection,
                "ProxyServer",
                5000),

            DependencyPolicy = LoadDependencyPolicy(configuration),
        };

        settings.Validate();

        return settings;
    }

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of ProxyServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static ProxyDependencySettings LoadDependencyPolicy(IniConfiguration configuration)
    {
        return new ProxyDependencySettings
        {
            CriticalServers = LoadServerNameSet(
                configuration,
                "CriticalServers",
                new[] { "WorldServer" }),

            NonCriticalServers = LoadServerNameSet(
                configuration,
                "NonCriticalServers",
                new[] { "MapServer", "InstanceServer" }),

            CriticalServerPacketTimeout = configuration.GetTimeSpan(
                ProxyServerSection,
                "CriticalServerPacketTimeout",
                TimeSpan.FromSeconds(15)),

            NonCriticalReconnectReportInterval = configuration.GetTimeSpan(
                ProxyServerSection,
                "NonCriticalReconnectReportInterval",
                TimeSpan.FromSeconds(30)),

            NonCriticalReconnectTimeout = configuration.GetTimeSpan(
                ProxyServerSection,
                "NonCriticalReconnectTimeout",
                configuration.GetTimeSpan(
                    ProxyServerSection,
                    "PeerReconnectTimeout",
                    TimeSpan.FromSeconds(120))),
        };
    }

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of ProxyServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static IReadOnlySet<string> LoadServerNameSet(
        IniConfiguration configuration,
        string key,
        IEnumerable<string> defaultServerNames)
    {
        string configuredServerNames = configuration.GetString(
            ProxyServerSection,
            key,
            string.Join(';', defaultServerNames));

        HashSet<string> serverNames = new(StringComparer.OrdinalIgnoreCase);

        string[] entries = configuredServerNames.Split(
            new[] { ';', ',' },
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        foreach (string entry in entries)
        {
            serverNames.Add(entry);
        }

        return serverNames;
    }
}
