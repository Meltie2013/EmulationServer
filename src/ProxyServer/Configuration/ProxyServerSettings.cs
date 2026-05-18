
using EmulationServer.Network.Configuration;

using EmulationServer.Shared.Logging.Configuration;
namespace EmulationServer.ProxyServer.Configuration;

public sealed class ProxyServerSettings
{
    public LoggingSettings Logging { get; init; } = new();

    public InternalNetworkSettings InternalNetwork { get; init; } = new();

    public ProxyDependencySettings DependencyPolicy { get; init; } = new();

    public void Validate()
    {
        Logging.Validate();
        InternalNetwork.Validate();
        DependencyPolicy.Validate();
    }
}
