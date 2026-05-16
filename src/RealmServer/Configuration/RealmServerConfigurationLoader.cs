
using EmulationServer.Database.Configuration;
using EmulationServer.Network.Configuration;
using EmulationServer.Shared.Configuration;

namespace EmulationServer.RealmServer.Configuration;

public static class RealmServerConfigurationLoader
{
    private const string RealmServerSection = "RealmServer";
    private const string DatabaseSection = "Database";

    public static RealmServerSettings Load(string path)
    {
        string fullPath = Path.GetFullPath(path);

        IniConfiguration configuration = IniConfiguration.Load(fullPath);

        RealmServerSettings settings = new()
        {
            Socket = LoadSocketSettings(configuration),
            Database = LoadDatabaseSettings(configuration),
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
}
