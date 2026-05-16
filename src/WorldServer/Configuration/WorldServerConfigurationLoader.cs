
using EmulationServer.Core.Configuration;
using EmulationServer.Shared.Configuration;

namespace EmulationServer.WorldServer.Configuration;

public static class WorldServerConfigurationLoader
{
    private const string WorldServerSection = "WorldServer";

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
        };

        settings.Validate();

        return settings;
    }
}
