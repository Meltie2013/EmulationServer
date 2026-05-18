using EmulationServer.Game.Maps.Runtime;
using EmulationServer.Network.Configuration;

namespace EmulationServer.MapServer.Configuration;

public sealed class MapServerSettings
{
    public InternalNetworkSettings InternalNetwork { get; init; } = new();

    public MapRuntimeSettings MapServices { get; init; } = new();

    public void Validate()
    {
        InternalNetwork.Validate();
        MapServices.Validate();
    }
}
