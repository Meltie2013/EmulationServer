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
// File: src/EmulationServer.Core/Configuration/ServerConfigurationLoader.cs
// Purpose: Contains server configuration loader code for the host orchestration, configuration loading, and service lifecycle layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Globalization;

using EmulationServer.Database.Configuration;
using EmulationServer.Network.Configuration;
using EmulationServer.Shared.Configuration;
using EmulationServer.Shared.Logging.Configuration;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Core.Configuration;

// Type: ServerConfigurationLoader
// Purpose: Provides server configuration loader behavior for the host orchestration, configuration loading, and service lifecycle layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class ServerConfigurationLoader
{

    // Constant: Defines the logging section constant used by the host orchestration, configuration loading, and service lifecycle layer.
    // Value: fixed logging section value used anywhere this rule or protocol value is needed.
    private const string LoggingSection = "Logging";

    // Method: LoadLoggingSettings
    // Purpose: Retrieves load logging settings data for the host orchestration, configuration loading, and service lifecycle layer.
    // Parameters:
    // - configuration: Configuration values that control how this operation should run.
    // - configurationPath: Configuration path value supplied by the caller for this operation.
    // - serverName: Server name value supplied by the caller for this operation.
    // Returns: Returns the logging settings value produced by this operation.
    // Notes: This keeps the operation scoped to ServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
    public static LoggingSettings LoadLoggingSettings(
        IniConfiguration configuration,
        string configurationPath,
        string serverName)
    {
        if (string.IsNullOrWhiteSpace(configurationPath))
        {
            throw new ArgumentException("Configuration path is required.");
        }

        if (string.IsNullOrWhiteSpace(serverName))
        {
            throw new ArgumentException("Server name is required.");
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

    // Constant: Defines the database section constant used by the host orchestration, configuration loading, and service lifecycle layer.
    // Value: fixed database section value used anywhere this rule or protocol value is needed.
    private const string DatabaseSection = "Database";

    // Method: LoadDatabaseSettings
    // Purpose: Retrieves load database settings data for the host orchestration, configuration loading, and service lifecycle layer.
    // Parameters:
    // - configuration: Configuration values that control how this operation should run.
    // Returns: Returns the database settings value produced by this operation.
    // Notes: This keeps the operation scoped to ServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: LoadInternalNetworkSettings
    // Purpose: Retrieves load internal network settings data for the host orchestration, configuration loading, and service lifecycle layer.
    // Parameters:
    // - configuration: Configuration values that control how this operation should run.
    // - sectionName: Section name value supplied by the caller for this operation.
    // - serverName: Server name value supplied by the caller for this operation.
    // - defaultPort: Default port value supplied by the caller for this operation.
    // Returns: Returns the internal network settings value produced by this operation.
    // Notes: This keeps the operation scoped to ServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
    public static InternalNetworkSettings LoadInternalNetworkSettings(
        IniConfiguration configuration,
        string sectionName,
        string serverName,
        int defaultPort)
    {
        if (string.IsNullOrWhiteSpace(sectionName))
        {
            throw new ArgumentException("Configuration section name is required.");
        }

        if (string.IsNullOrWhiteSpace(serverName))
        {
            throw new ArgumentException("Server name is required.");
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

            ReceiveBufferSize = configuration.GetInt(
                sectionName,
                "ReceiveBufferSize",
                65536,
                minimum: 1024),

            SendBufferSize = configuration.GetInt(
                sectionName,
                "SendBufferSize",
                65536,
                minimum: 1024),

            KeepAlive = configuration.GetBool(
                sectionName,
                "KeepAlive",
                true),

            KeepAliveTimeSeconds = configuration.GetInt(
                sectionName,
                "KeepAliveTimeSeconds",
                30,
                minimum: 0),

            KeepAliveIntervalSeconds = configuration.GetInt(
                sectionName,
                "KeepAliveIntervalSeconds",
                10,
                minimum: 0),

            AuthenticationTimeout = configuration.GetTimeSpan(
                sectionName,
                "AuthenticationTimeout",
                TimeSpan.FromSeconds(5)),

            AllowedServers = [.. SplitList(configuration.GetString(sectionName, "AllowedServers", string.Empty))],

            ShutdownGracePeriod = configuration.GetTimeSpan(
                sectionName,
                "ShutdownGracePeriod",
                TimeSpan.FromSeconds(15)),

            LatencyReportInterval = configuration.GetTimeSpan(
                sectionName,
                "LatencyReportInterval",
                TimeSpan.FromSeconds(15)),

            LatencyLoggingEnabled = configuration.GetBool(
                sectionName,
                "LatencyLoggingEnabled",
                true),

            LatencyLogInterval = configuration.GetTimeSpan(
                sectionName,
                "LatencyLogInterval",
                TimeSpan.FromSeconds(60)),

            PingTimeout = configuration.GetTimeSpan(
                sectionName,
                "PingTimeout",
                TimeSpan.FromSeconds(5)),

            Peers = LoadInternalPeers(configuration, sectionName),
        };
    }

    // Method: SplitList
    // Purpose: Executes the split list operation for the host orchestration, configuration loading, and service lifecycle layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the I enumerable value produced by this operation.
    // Notes: This keeps the operation scoped to ServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
    private static String[] SplitList(string value)
    {
        return value.Split([';', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    // Method: LoadInternalPeers
    // Purpose: Retrieves load internal peers data for the host orchestration, configuration loading, and service lifecycle layer.
    // Parameters:
    // - configuration: Configuration values that control how this operation should run.
    // - sectionName: Section name value supplied by the caller for this operation.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to ServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
    private static List<InternalPeerSettings> LoadInternalPeers(
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

    // Method: ParsePeer
    // Purpose: Converts incoming data into parse peer form for the host orchestration, configuration loading, and service lifecycle layer.
    // Parameters:
    // - entry: Entry value supplied by the caller for this operation.
    // - reconnectDelay: Reconnect delay value supplied by the caller for this operation.
    // - reconnectTimeout: Reconnect timeout value supplied by the caller for this operation.
    // Returns: Returns the internal peer settings value produced by this operation.
    // Notes: This keeps the operation scoped to ServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ParseDurationOrThrow
    // Purpose: Converts incoming data into parse duration or throw form for the host orchestration, configuration loading, and service lifecycle layer.
    // Parameters:
    // - sectionName: Section name value supplied by the caller for this operation.
    // - key: Key value supplied by the caller for this operation.
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the time span value produced by this operation.
    // Notes: This keeps the operation scoped to ServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
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

        throw new ConfigurationException($"Invalid time span value for [{sectionName}] {key}: '{value}'. Examples: 15s, 5m, 1h, 00:00:15.");
    }

    // Method: ParseLogOutputMode
    // Purpose: Converts incoming data into parse log output mode form for the host orchestration, configuration loading, and service lifecycle layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the log output mode value produced by this operation.
    // Notes: This keeps the operation scoped to ServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ParseLogTypes
    // Purpose: Converts incoming data into parse log types form for the host orchestration, configuration loading, and service lifecycle layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // - allowAll: Allow all value supplied by the caller for this operation.
    // Returns: Returns the hash set value produced by this operation.
    // Notes: This keeps the operation scoped to ServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ParseLogType
    // Purpose: Converts incoming data into parse log type form for the host orchestration, configuration loading, and service lifecycle layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the log type value produced by this operation.
    // Notes: This keeps the operation scoped to ServerConfigurationLoader so callers do not duplicate validation, protocol, or persistence rules.
    private static LogType ParseLogType(string value)
    {
        string normalized = value.Trim().Replace("-", string.Empty).Replace("_", string.Empty);

        return normalized.ToUpperInvariant() switch
        {
            "INFO" => LogType.INFORMATION,
            "INFORMATION" => LogType.INFORMATION,
            "USER" => LogType.SYSTEM,
            "SYSTEM" => LogType.SYSTEM,
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
