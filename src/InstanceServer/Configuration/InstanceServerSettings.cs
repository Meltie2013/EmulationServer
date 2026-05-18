using EmulationServer.Game.Maps.Runtime;
using EmulationServer.Network.Configuration;

using EmulationServer.Shared.Logging.Configuration;
namespace EmulationServer.InstanceServer.Configuration;

public sealed class InstanceServerSettings
{
    public LoggingSettings Logging { get; init; } = new();

    public InternalNetworkSettings InternalNetwork { get; init; } = new();

    public MapRuntimeSettings InstanceServices { get; init; } = new();

    public void Validate()
    {
        Logging.Validate();
        InternalNetwork.Validate();
        InstanceServices.Validate();
    }
}
