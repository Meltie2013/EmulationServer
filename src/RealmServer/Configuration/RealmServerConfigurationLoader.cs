
using System.Globalization;

using EmulationServer.Core.Configuration;
using EmulationServer.Database.Configuration;
using EmulationServer.Network.Configuration;
using EmulationServer.Shared.Configuration;

namespace EmulationServer.RealmServer.Configuration;

public static class RealmServerConfigurationLoader
{
    private const string RealmServerSection = "RealmServer";
    private const string DatabaseSection = "Database";
    private const string RealmsSection = "Realms";
    private const string InternalNetworkSection = "InternalNetwork";

    public static RealmServerSettings Load(string path)
    {
        string fullPath = Path.GetFullPath(path);

        IniConfiguration configuration = IniConfiguration.Load(fullPath);

        RealmServerSettings settings = new()
        {
            Socket = LoadSocketSettings(configuration),
            Database = LoadDatabaseSettings(configuration),
            InternalNetwork = ServerConfigurationLoader.LoadInternalNetworkSettings(
                configuration,
                InternalNetworkSection,
                "RealmServer",
                5005),
            Realms = LoadRealmSettings(configuration),
        };

        settings.Validate();

        return settings;
    }

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

            MaxConnections = configuration.GetInt(
                RealmServerSection,
                "MaxConnections",
                1024,
                minimum: 1),

            ShutdownGracePeriod = configuration.GetTimeSpan(
                RealmServerSection,
                "ShutdownGracePeriod",
                TimeSpan.FromSeconds(15)),
        };
    }

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
        };
    }

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
                RealmFlags = (byte)configuration.GetInt(section, "RealmFlags", 0, minimum: 0, maximum: byte.MaxValue),
                Timezone = (byte)configuration.GetInt(section, "Timezone", 1, minimum: 0, maximum: byte.MaxValue),
                AllowedSecurityLevel = (byte)configuration.GetInt(section, "AllowedSecurityLevel", 0, minimum: 0, maximum: byte.MaxValue),
                Online = configuration.GetBool(section, "Online", false),
                ActiveConnections = configuration.GetInt(section, "ActiveConnections", 0, minimum: 0),
                MaxConnections = configuration.GetInt(section, "MaxConnections", 1000, minimum: 1),
                Builds = ParseBuilds(builds, section),
            });
        }

        return realms;
    }

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

    private static IEnumerable<string> SplitList(string value)
    {
        return value.Split([';', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}
