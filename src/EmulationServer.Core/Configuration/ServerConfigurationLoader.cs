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

using EmulationServer.Database.Configuration;
using EmulationServer.Network.Configuration;
using EmulationServer.Shared.Configuration;
using EmulationServer.Shared.Logging.Configuration;
using EmulationServer.Shared.Logging.Enums;

/**
  * File overview: src/EmulationServer.Core/Configuration/ServerConfigurationLoader.cs
  * This file belongs to the server configuration loading and strongly typed settings portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Core.Configuration;

/**
  * Represents the server configuration loader component in the server configuration loading and strongly typed settings area.
  * It centralizes INI parsing so startup code can work with strongly typed settings instead of raw strings.
  */
public static class ServerConfigurationLoader
{

    private const string LoggingSection = "Logging";

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of ServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    public static LoggingSettings LoadLoggingSettings(
        IniConfiguration configuration,
        string configurationPath,
        string serverName)
    {
        if (string.IsNullOrWhiteSpace(configurationPath))
        {
            throw new ArgumentException("Configuration path is required.", nameof(configurationPath));
        }

        if (string.IsNullOrWhiteSpace(serverName))
        {
            throw new ArgumentException("Server name is required.", nameof(serverName));
        }

        string configurationDirectory = Path.GetDirectoryName(Path.GetFullPath(configurationPath))
            ?? AppContext.BaseDirectory;

        string logFolder = configuration.GetString(LoggingSection, "LogFolder", "logs");
        string resolvedLogFolder = Path.GetFullPath(Path.IsPathRooted(logFolder)
            ? logFolder
            : Path.Combine(configurationDirectory, logFolder));

        HashSet<LogType> enabledTypes = ParseLogTypes(
            configuration.GetString(LoggingSection, "EnabledTypes", "All"),
            allowAll: true);

        HashSet<LogType> disabledTypes = ParseLogTypes(
            configuration.GetString(LoggingSection, "DisabledTypes", string.Empty),
            allowAll: true);

        enabledTypes.ExceptWith(disabledTypes);

        return new LoggingSettings
        {
            ServerName = serverName,
            Output = ParseLogOutputMode(configuration.GetString(LoggingSection, "Output", "Console")),
            LogFolder = resolvedLogFolder,
            FileName = configuration.GetString(LoggingSection, "FileName", $"{serverName}.log"),
            EnabledTypes = enabledTypes,
        };
    }

    private const string DatabaseSection = "Database";

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of ServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    public static DatabaseSettings LoadDatabaseSettings(IniConfiguration configuration)
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
        };
    }

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of ServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    public static InternalNetworkSettings LoadInternalNetworkSettings(
        IniConfiguration configuration,
        string sectionName,
        string serverName,
        int defaultPort)
    {
        if (string.IsNullOrWhiteSpace(sectionName))
        {
            throw new ArgumentException("Configuration section name is required.", nameof(sectionName));
        }

        if (string.IsNullOrWhiteSpace(serverName))
        {
            throw new ArgumentException("Server name is required.", nameof(serverName));
        }

        return new InternalNetworkSettings
        {
            ServerName = serverName,

            BindAddress = configuration.GetString(
                sectionName,
                "BindAddress",
                "127.0.0.1"),

            Port = configuration.GetInt(
                sectionName,
                "Port",
                defaultPort,
                minimum: 0,
                maximum: 65535),

            RegistrationKey = configuration.GetString(
                sectionName,
                "RegistrationKey",
                string.Empty),

            Backlog = configuration.GetInt(
                sectionName,
                "Backlog",
                128,
                minimum: 1),


            ShutdownGracePeriod = configuration.GetTimeSpan(
                sectionName,
                "ShutdownGracePeriod",
                TimeSpan.FromSeconds(15)),

            LatencyReportInterval = configuration.GetTimeSpan(
                sectionName,
                "LatencyReportInterval",
                TimeSpan.FromSeconds(15)),

            PingTimeout = configuration.GetTimeSpan(
                sectionName,
                "PingTimeout",
                TimeSpan.FromSeconds(5)),

            Peers = LoadInternalPeers(configuration, sectionName),
        };
    }

    /**
      * Loads configuration or data from the configured source and validates the result before it is used.
      * The method is part of ServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static IReadOnlyList<InternalPeerSettings> LoadInternalPeers(
        IniConfiguration configuration,
        string sectionName)
    {
        string peers = configuration.GetString(sectionName, "Peers", string.Empty);
        string reconnectDelay = configuration.GetString(sectionName, "PeerReconnectDelay", "5s");
        TimeSpan defaultReconnectDelay = ParseDurationOrThrow(sectionName, "PeerReconnectDelay", reconnectDelay);

        string reconnectTimeout = configuration.GetString(sectionName, "PeerReconnectTimeout", "120s");
        TimeSpan defaultReconnectTimeout = ParseDurationOrThrow(sectionName, "PeerReconnectTimeout", reconnectTimeout);

        if (string.IsNullOrWhiteSpace(peers))
        {
            return [];
        }

        List<InternalPeerSettings> settings = [];

        string[] entries = peers.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (string entry in entries)
        {
            settings.Add(ParsePeer(entry, defaultReconnectDelay, defaultReconnectTimeout));
        }

        return settings;
    }

    /**
      * Parses text input into a strongly typed value used by the server runtime.
      * The method is part of ServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static InternalPeerSettings ParsePeer(string entry, TimeSpan reconnectDelay, TimeSpan reconnectTimeout)
    {
        int nameSeparator = entry.IndexOf('@');
        if (nameSeparator <= 0 || nameSeparator == entry.Length - 1)
        {
            throw new ConfigurationException($"Invalid internal peer entry: '{entry}'. Expected Name@Host:Port.");
        }

        string name = entry[..nameSeparator].Trim();
        string endpoint = entry[(nameSeparator + 1)..].Trim();

        int portSeparator = endpoint.LastIndexOf(':');
        if (portSeparator <= 0 || portSeparator == endpoint.Length - 1)
        {
            throw new ConfigurationException($"Invalid internal peer endpoint: '{endpoint}'. Expected Host:Port.");
        }

        string host = endpoint[..portSeparator].Trim();
        string portText = endpoint[(portSeparator + 1)..].Trim();

        if (!int.TryParse(portText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int port))
        {
            throw new ConfigurationException($"Invalid internal peer port: '{portText}'.");
        }

        return new InternalPeerSettings
        {
            Name = name,
            Host = host,
            Port = port,
            ReconnectDelay = reconnectDelay,
            ReconnectTimeout = reconnectTimeout,
        };
    }

    /**
      * Parses text input into a strongly typed value used by the server runtime.
      * The method is part of ServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static TimeSpan ParseDurationOrThrow(string sectionName, string key, string value)
    {
        string text = value.Trim().ToLowerInvariant();

        if (TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out TimeSpan parsed))
        {
            return parsed;
        }

        (string Suffix, Func<double, TimeSpan> Factory)[] suffixes =
        [
            ("ms", TimeSpan.FromMilliseconds),
            ("s", TimeSpan.FromSeconds),
            ("m", TimeSpan.FromMinutes),
            ("h", TimeSpan.FromHours),
        ];

        foreach ((string suffix, Func<double, TimeSpan> factory) in suffixes)
        {
            if (!text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string numberPart = text[..^suffix.Length];

            if (!double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
            {
                break;
            }

            return factory(number);
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds))
        {
            return TimeSpan.FromSeconds(seconds);
        }

        throw new ConfigurationException(
            $"Invalid time span value for [{sectionName}] {key}: '{value}'. Examples: 15s, 5m, 1h, 00:00:15.");
    }

    /**
      * Parses text input into a strongly typed value used by the server runtime.
      * The method is part of ServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static LogOutputMode ParseLogOutputMode(string value)
    {
        string normalized = value.Trim().Replace("-", string.Empty).Replace("_", string.Empty);

        return normalized.ToLowerInvariant() switch
        {
            "console" => LogOutputMode.Console,
            "consoleonly" => LogOutputMode.Console,
            "file" => LogOutputMode.File,
            "fileonly" => LogOutputMode.File,
            "both" => LogOutputMode.Both,
            "consoleandfile" => LogOutputMode.Both,
            "fileandconsole" => LogOutputMode.Both,
            _ => throw new ConfigurationException($"Invalid logging output mode '{value}'. Expected Console, File, or Both."),
        };
    }

    /**
      * Parses text input into a strongly typed value used by the server runtime.
      * The method is part of ServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static HashSet<LogType> ParseLogTypes(string value, bool allowAll)
    {
        HashSet<LogType> logTypes = [];

        if (string.IsNullOrWhiteSpace(value))
        {
            return logTypes;
        }

        foreach (string entry in value.Split([';', ',', '|'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (entry.Equals("all", StringComparison.OrdinalIgnoreCase) || entry == "*")
            {
                if (!allowAll)
                {
                    throw new ConfigurationException("Logging type list does not allow All.");
                }

                logTypes.UnionWith(Enum.GetValues<LogType>());
                continue;
            }

            if (entry.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            logTypes.Add(ParseLogType(entry));
        }

        return logTypes;
    }

    /**
      * Parses text input into a strongly typed value used by the server runtime.
      * The method is part of ServerConfigurationLoader and keeps this workflow isolated from the caller.
      */
    private static LogType ParseLogType(string value)
    {
        string normalized = value.Trim().Replace("-", string.Empty).Replace("_", string.Empty);

        return normalized.ToUpperInvariant() switch
        {
            "INFO" => LogType.INFORMATION,
            "INFORMATION" => LogType.INFORMATION,
            "WARN" => LogType.WARNING,
            "WARNING" => LogType.WARNING,
            "ERROR" => LogType.FAILED,
            "FAILED" => LogType.FAILED,
            "FAIL" => LogType.FAILED,
            "FATAL" => LogType.CRITICAL,
            "CRITICAL" => LogType.CRITICAL,
            "EMERGENCY" => LogType.EMERG,
            "EMERG" => LogType.EMERG,
            _ when Enum.TryParse(value, ignoreCase: true, out LogType logType) => logType,
            _ => throw new ConfigurationException($"Invalid logging type '{value}'."),
        };
    }

}
