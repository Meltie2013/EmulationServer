
using EmulationServer.Core.Configuration;
using EmulationServer.Shared.Configuration;

namespace EmulationServer.WorldServer.Configuration;

public static class WorldServerConfigurationLoader
{
    private const string WorldServerSection = "WorldServer";
    private const string RealmStatusSection = "RealmStatus";
    private const string GameDataSection = "GameData";

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
            MaxConnections = configuration.GetInt(WorldServerSection, "MaxConnections", 1000, minimum: 1),
            Database = ServerConfigurationLoader.LoadDatabaseSettings(configuration),
            RealmStatus = LoadRealmStatusSettings(configuration),
            GameData = LoadGameDataSettings(configuration),
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
        };
    }


    private static GameDataSettings LoadGameDataSettings(IniConfiguration configuration)
    {
        string requiredDbcFiles = configuration.GetString(
            GameDataSection,
            "RequiredDbcFiles",
            string.Join(';', GameDataSettings.DefaultRequiredDbcFiles));

        return new GameDataSettings
        {
            Enabled = configuration.GetBool(GameDataSection, "Enabled", false),
            DataDirectory = configuration.GetString(GameDataSection, "DataDirectory", "Data"),
            DbcDirectory = configuration.GetString(GameDataSection, "DbcDirectory", "dbc"),
            RequiredDbcFiles = SplitList(requiredDbcFiles).ToArray(),
        };
    }

    private static IEnumerable<string> SplitList(string value)
    {
        return value.Split([';', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}
