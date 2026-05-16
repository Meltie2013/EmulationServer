
using EmulationServer.Core.Configuration;
using EmulationServer.Shared.Configuration;

namespace EmulationServer.CharacterServer.Configuration;

public static class CharacterServerConfigurationLoader
{
    private const string CharacterServerSection = "CharacterServer";

    public static CharacterServerSettings Load(string path)
    {
        string fullPath = Path.GetFullPath(path);

        IniConfiguration configuration = IniConfiguration.Load(fullPath);

        CharacterServerSettings settings = new()
        {
            InternalNetwork = ServerConfigurationLoader.LoadInternalNetworkSettings(
                configuration,
                CharacterServerSection,
                "CharacterServer",
                5001),

            Database = ServerConfigurationLoader.LoadDatabaseSettings(configuration),
        };

        settings.Validate();

        return settings;
    }
}
