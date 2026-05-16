
using EmulationServer.Core.Configuration;
using EmulationServer.Shared.Configuration;

namespace EmulationServer.InstanceServer.Configuration;

public static class InstanceServerConfigurationLoader
{
    private const string InstanceServerSection = "InstanceServer";

    public static InstanceServerSettings Load(string path)
    {
        string fullPath = Path.GetFullPath(path);

        IniConfiguration configuration = IniConfiguration.Load(fullPath);

        InstanceServerSettings settings = new()
        {
            InternalNetwork = ServerConfigurationLoader.LoadInternalNetworkSettings(
                configuration,
                InstanceServerSection,
                "InstanceServer",
                5004),
        };

        settings.Validate();

        return settings;
    }
}
