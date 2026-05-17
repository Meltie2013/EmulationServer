
using System.Globalization;

using EmulationServer.Database.Configuration;
using EmulationServer.Network.Configuration;
using EmulationServer.Shared.Configuration;

namespace EmulationServer.Core.Configuration;

public static class ServerConfigurationLoader
{
    private const string DatabaseSection = "Database";

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

    private static IReadOnlyList<InternalPeerSettings> LoadInternalPeers(
        IniConfiguration configuration,
        string sectionName)
    {
        string peers = configuration.GetString(sectionName, "Peers", string.Empty);
        string reconnectDelay = configuration.GetString(sectionName, "PeerReconnectDelay", "5s");
        TimeSpan defaultReconnectDelay = ParseDurationOrThrow(sectionName, "PeerReconnectDelay", reconnectDelay);

        if (string.IsNullOrWhiteSpace(peers))
        {
            return [];
        }

        List<InternalPeerSettings> settings = [];

        string[] entries = peers.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (string entry in entries)
        {
            settings.Add(ParsePeer(entry, defaultReconnectDelay));
        }

        return settings;
    }

    private static InternalPeerSettings ParsePeer(string entry, TimeSpan reconnectDelay)
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
        };
    }

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
}
