
using EmulationServer.Core.Configuration;
using EmulationServer.Shared.Configuration;

namespace EmulationServer.ProxyServer.Configuration;

public static class ProxyServerConfigurationLoader
{
    private const string ProxyServerSection = "ProxyServer";

    public static ProxyServerSettings Load(string path)
    {
        string fullPath = Path.GetFullPath(path);

        IniConfiguration configuration = IniConfiguration.Load(fullPath);

        ProxyServerSettings settings = new()
        {
            InternalNetwork = ServerConfigurationLoader.LoadInternalNetworkSettings(
                configuration,
                ProxyServerSection,
                "ProxyServer",
                5000),
        };

        settings.Validate();

        return settings;
    }
}
