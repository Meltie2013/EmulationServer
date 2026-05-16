
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

            DependencyPolicy = LoadDependencyPolicy(configuration),
        };

        settings.Validate();

        return settings;
    }

    private static ProxyDependencySettings LoadDependencyPolicy(IniConfiguration configuration)
    {
        return new ProxyDependencySettings
        {
            CriticalServers = LoadServerNameSet(
                configuration,
                "CriticalServers",
                new[] { "WorldServer" }),

            NonCriticalServers = LoadServerNameSet(
                configuration,
                "NonCriticalServers",
                new[] { "MapServer", "InstanceServer" }),

            CriticalServerPacketTimeout = configuration.GetTimeSpan(
                ProxyServerSection,
                "CriticalServerPacketTimeout",
                TimeSpan.FromSeconds(15)),

            NonCriticalReconnectReportInterval = configuration.GetTimeSpan(
                ProxyServerSection,
                "NonCriticalReconnectReportInterval",
                TimeSpan.FromSeconds(30)),
        };
    }

    private static IReadOnlySet<string> LoadServerNameSet(
        IniConfiguration configuration,
        string key,
        IEnumerable<string> defaultServerNames)
    {
        string configuredServerNames = configuration.GetString(
            ProxyServerSection,
            key,
            string.Join(';', defaultServerNames));

        HashSet<string> serverNames = new(StringComparer.OrdinalIgnoreCase);

        string[] entries = configuredServerNames.Split(
            new[] { ';', ',' },
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        foreach (string entry in entries)
        {
            serverNames.Add(entry);
        }

        return serverNames;
    }
}
