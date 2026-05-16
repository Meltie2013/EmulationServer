
using EmulationServer.Network.Configuration;

namespace EmulationServer.InstanceServer.Configuration;

public sealed class InstanceServerSettings
{
    public InternalNetworkSettings InternalNetwork { get; init; } = new();

    public void Validate()
    {
        InternalNetwork.Validate();
    }
}
