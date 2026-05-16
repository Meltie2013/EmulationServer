
using EmulationServer.Core.Configuration;
using EmulationServer.Shared.Configuration;

namespace EmulationServer.WorldServer.Configuration;

public static class WorldServerConfigurationLoader
{
    private const string WorldServerSection = "WorldServer";
    private const string RealmStatusSection = "RealmStatus";

    public static WorldServerSettings Load(string path)
    {
        string fullPath = Path.GetFullPath(path);
        IniConfiguration configuration = IniConfiguration.Load(fullPath);

        WorldServerSettings settings = new()
        {
            InternalNetwork = ServerConfigurationLoader.LoadInternalNetworkSettings(
                configuration,
                WorldServerSection,
                "WorldServer",
                5002),
            Database = ServerConfigurationLoader.LoadDatabaseSettings(configuration),
            RealmStatus = LoadRealmStatusSettings(configuration),
        };

        settings.Validate();

        return settings;
    }

    private static RealmStatusSettings LoadRealmStatusSettings(IniConfiguration configuration)
    {
        return new RealmStatusSettings
        {
            Enabled = configuration.GetBool(RealmStatusSection, "Enabled", true),
            RealmId = (uint)configuration.GetInt(RealmStatusSection, "RealmId", 1, minimum: 1),
            RealmServerHost = configuration.GetString(RealmStatusSection, "RealmServerHost", "127.0.0.1"),
            RealmServerPort = (ushort)configuration.GetInt(RealmStatusSection, "RealmServerPort", 5005, minimum: 1, maximum: 65535),
            UpdateInterval = configuration.GetTimeSpan(RealmStatusSection, "UpdateInterval", TimeSpan.FromSeconds(15)),
            MaxConnections = configuration.GetInt(RealmStatusSection, "MaxConnections", 1000, minimum: 1),
        };
    }
}
