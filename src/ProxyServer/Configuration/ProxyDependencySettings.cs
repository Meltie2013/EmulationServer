
namespace EmulationServer.ProxyServer.Configuration;

public sealed class ProxyDependencySettings
{
    public IReadOnlySet<string> CriticalServers { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "CharacterServer",
        "WorldServer",
    };

    public IReadOnlySet<string> NonCriticalServers { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "MapServer",
        "InstanceServer",
    };

    public TimeSpan CriticalServerPacketTimeout { get; init; } = TimeSpan.FromSeconds(15);

    public TimeSpan NonCriticalReconnectReportInterval { get; init; } = TimeSpan.FromSeconds(30);

    public void Validate()
    {
        if (CriticalServers.Count == 0)
        {
            throw new InvalidOperationException("Proxy dependency policy requires at least one critical server.");
        }

        if (CriticalServerPacketTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Proxy critical server packet timeout must be greater than zero.");
        }

        if (NonCriticalReconnectReportInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Proxy non-critical reconnect report interval must be greater than zero.");
        }

        foreach (string serverName in CriticalServers)
        {
            ValidateServerName(serverName, nameof(CriticalServers));
        }

        foreach (string serverName in NonCriticalServers)
        {
            ValidateServerName(serverName, nameof(NonCriticalServers));

            if (CriticalServers.Contains(serverName))
            {
                throw new InvalidOperationException($"Server '{serverName}' cannot be both critical and non-critical.");
            }
        }
    }

    private static void ValidateServerName(string serverName, string settingName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
        {
            throw new InvalidOperationException($"Proxy dependency setting '{settingName}' contains an empty server name.");
        }
    }
}
