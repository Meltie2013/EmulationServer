
using EmulationServer.Core.Configuration;
using EmulationServer.Shared.Configuration;

namespace EmulationServer.MapServer.Configuration;

public static class MapServerConfigurationLoader
{
    private const string MapServerSection = "MapServer";

    public static MapServerSettings Load(string path)
    {
        string fullPath = Path.GetFullPath(path);

        IniConfiguration configuration = IniConfiguration.Load(fullPath);

        MapServerSettings settings = new()
        {
            InternalNetwork = ServerConfigurationLoader.LoadInternalNetworkSettings(
                configuration,
                MapServerSection,
                "MapServer",
                5003),
        };

        settings.Validate();

        return settings;
    }
}
