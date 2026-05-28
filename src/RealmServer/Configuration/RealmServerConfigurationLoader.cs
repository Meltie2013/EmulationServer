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

using System.Globalization;

using EmulationServer.Core.Configuration;
using EmulationServer.Database.Configuration;
using EmulationServer.Network.Configuration;
using EmulationServer.RealmServer.Realms;
using EmulationServer.Shared.Configuration;

/**
  * File overview: src/RealmServer/Configuration/RealmServerConfigurationLoader.cs
  * Documents the RealmServerConfigurationLoader source file in the realm authentication, realm-list handling, and external client login services area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.RealmServer.Configuration;

/**
  * Owns the realm server configuration loader behavior for the realm authentication, realm-list handling, and external client login services layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public static class RealmServerConfigurationLoader
{
    /**
      * Defines the constant value for realm server section.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const string RealmServerSection = "RealmServer";
    /**
      * Defines the constant value for database section.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const string DatabaseSection = "Database";
    /**
      * Defines the constant value for realms section.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const string RealmsSection = "Realms";
    /**
      * Defines the constant value for realm list section.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const string RealmListSection = "RealmList";
    /**
      * Defines the constant value for internal network section.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const string InternalNetworkSection = "InternalNetwork";

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of RealmServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    public static RealmServerSettings Load(string path)
    {
        string fullPath = Path.GetFullPath(path);

        IniConfiguration configuration = IniConfiguration.Load(fullPath);

        RealmServerSettings settings = new()
        {
            Logging = ServerConfigurationLoader.LoadLoggingSettings(configuration, fullPath, "RealmServer"),

            Socket = LoadSocketSettings(configuration),
            Database = LoadDatabaseSettings(configuration),
            InternalNetwork = ServerConfigurationLoader.LoadInternalNetworkSettings(
                configuration,
                InternalNetworkSection,
                "RealmServer",
                5005),
            RealmList = LoadRealmListSettings(configuration),
            Realms = LoadRealmSettings(configuration),
        };

        settings.Validate();

        return settings;
    }

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of RealmServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static RealmSocketListenerSettings LoadSocketSettings(IniConfiguration configuration)
    {
        return new RealmSocketListenerSettings
        {
            BindAddress = configuration.GetString(
                RealmServerSection,
                "BindAddress",
                "0.0.0.0"),

            Port = configuration.GetInt(
                RealmServerSection,
                "Port",
                3724,
                minimum: 0,
                maximum: 65535),

            Backlog = configuration.GetInt(
                RealmServerSection,
                "Backlog",
                128,
                minimum: 1),

            ReceiveBufferSize = configuration.GetInt(
                RealmServerSection,
                "ReceiveBufferSize",
                65536,
                minimum: 1024),

            SendBufferSize = configuration.GetInt(
                RealmServerSection,
                "SendBufferSize",
                65536,
                minimum: 1024),

            KeepAlive = configuration.GetBool(
                RealmServerSection,
                "KeepAlive",
                true),

            KeepAliveTimeSeconds = configuration.GetInt(
                RealmServerSection,
                "KeepAliveTimeSeconds",
                30,
                minimum: 0),

            KeepAliveIntervalSeconds = configuration.GetInt(
                RealmServerSection,
                "KeepAliveIntervalSeconds",
                10,
                minimum: 0),

            ShutdownGracePeriod = configuration.GetTimeSpan(
                RealmServerSection,
                "ShutdownGracePeriod",
                TimeSpan.FromSeconds(15)),
        };
    }

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of RealmServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static DatabaseSettings LoadDatabaseSettings(IniConfiguration configuration)
    {
        return new DatabaseSettings
        {
            Host = configuration.GetString(
                DatabaseSection,
                "Host",
                "127.0.0.1"),

            Port = configuration.GetInt(
                DatabaseSection,
                "Port",
                3306,
                minimum: 1,
                maximum: 65535),

            Database = configuration.GetString(
                DatabaseSection,
                "Database",
                "realmd"),

            Username = configuration.GetString(
                DatabaseSection,
                "Username",
                "root"),

            Password = configuration.GetString(
                DatabaseSection,
                "Password",
                ""),

            MinimumPoolSize = configuration.GetUInt(
                DatabaseSection,
                "MinimumPoolSize",
                5),

            MaximumPoolSize = configuration.GetUInt(
                DatabaseSection,
                "MaximumPoolSize",
                100,
                minimum: 1),

            UseSsl = configuration.GetBool(
                DatabaseSection,
                "UseSsl",
                false),

            ConnectionTimeoutSeconds = configuration.GetUInt(
                DatabaseSection,
                "ConnectionTimeoutSeconds",
                15,
                minimum: 1),

            DefaultCommandTimeoutSeconds = configuration.GetUInt(
                DatabaseSection,
                "DefaultCommandTimeoutSeconds",
                30,
                minimum: 1),

            ConnectionIdleTimeoutSeconds = configuration.GetUInt(
                DatabaseSection,
                "ConnectionIdleTimeoutSeconds",
                180,
                minimum: 1),

            ConnectionLifeTimeSeconds = configuration.GetUInt(
                DatabaseSection,
                "ConnectionLifeTimeSeconds",
                0),

            KeepAliveSeconds = configuration.GetUInt(
                DatabaseSection,
                "KeepAliveSeconds",
                30),

            ConnectionReset = configuration.GetBool(
                DatabaseSection,
                "ConnectionReset",
                true),

            UseCompression = configuration.GetBool(
                DatabaseSection,
                "UseCompression",
                false),
        };
    }

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of RealmServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static RealmListSettings LoadRealmListSettings(IniConfiguration configuration)
    {
        return new RealmListSettings
        {
            RequireWorldServerStatus = configuration.GetBool(
                RealmListSection,
                "RequireWorldServerStatus",
                true),

            HideStaleRealms = configuration.GetBool(
                RealmListSection,
                "HideStaleRealms",
                true),

            StaleRealmTimeout = configuration.GetTimeSpan(
                RealmListSection,
                "StaleRealmTimeout",
                TimeSpan.FromMinutes(5)),
        };
    }

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of RealmServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static IReadOnlyList<ConfiguredRealmSettings> LoadRealmSettings(IniConfiguration configuration)
    {
        string ids = configuration.GetString(RealmsSection, "RealmIds", "1");
        List<ConfiguredRealmSettings> realms = [];

        foreach (string idText in SplitList(ids))
        {
            if (!uint.TryParse(idText, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint realmId))
            {
                throw new ConfigurationException($"Invalid realm id in [{RealmsSection}] RealmIds: '{idText}'.");
            }

            string section = $"Realm.{realmId}";
            string builds = configuration.GetString(section, "Builds", "5875;6005;6141");

            realms.Add(new ConfiguredRealmSettings
            {
                Id = realmId,
                Name = configuration.GetString(section, "Name", "Emulation Server"),
                Address = configuration.GetString(section, "Address", "127.0.0.1"),
                Port = (ushort)configuration.GetInt(section, "Port", 8085, minimum: 1, maximum: 65535),
                Icon = (byte)configuration.GetInt(section, "Icon", 0, minimum: 0, maximum: byte.MaxValue),
                RealmFlags = ParseRealmFlags(configuration.GetString(section, "RealmFlags", "0"), section),
                Timezone = (byte)configuration.GetInt(section, "Timezone", 1, minimum: 0, maximum: byte.MaxValue),
                AllowedSecurityLevel = (byte)configuration.GetInt(section, "AllowedSecurityLevel", 0, minimum: 0, maximum: byte.MaxValue),
                Online = configuration.GetBool(section, "Online", false),
                ActiveConnections = configuration.GetInt(section, "ActiveConnections", 0, minimum: 0),
                Builds = ParseBuilds(builds, section),
            });
        }

        return realms;
    }

    /**
      * Parses configured realm flags from decimal, hexadecimal, or named values.
      * The method is part of RealmServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static RealmFlags ParseRealmFlags(string value, string section)
    {
        try
        {
            return RealmFlagUtilities.ParseConfigurationValue(value);
        }
        catch (InvalidOperationException ex)
        {
            throw new ConfigurationException($"Invalid realm flags in [{section}] RealmFlags: {ex.Message}", ex);
        }
    }

    /**
      * Parses text input into a strongly typed value used by the server runtime.
      * The method is part of RealmServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static IReadOnlySet<ushort> ParseBuilds(string value, string section)
    {
        HashSet<ushort> builds = [];

        foreach (string buildText in SplitList(value))
        {
            if (!ushort.TryParse(buildText, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort build))
            {
                throw new ConfigurationException($"Invalid client build in [{section}] Builds: '{buildText}'.");
            }

            builds.Add(build);
        }

        return builds;
    }

    /**
      * Splits the supplied text into command parts while preserving quoted values.
      * The method is part of RealmServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static IEnumerable<string> SplitList(string value)
    {
        return value.Split([';', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}
