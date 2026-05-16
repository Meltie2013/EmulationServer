
using EmulationServer.Network.Configuration;

namespace EmulationServer.ProxyServer.Configuration;

public sealed class ProxyServerSettings
{
    public InternalNetworkSettings InternalNetwork { get; init; } = new();

    public ProxyDependencySettings DependencyPolicy { get; init; } = new();

    public void Validate()
    {
        InternalNetwork.Validate();
        DependencyPolicy.Validate();
    }
}
