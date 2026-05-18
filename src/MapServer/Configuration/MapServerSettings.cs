using EmulationServer.Game.Maps.Runtime;
using EmulationServer.Network.Configuration;

using EmulationServer.Shared.Logging.Configuration;
namespace EmulationServer.MapServer.Configuration;

public sealed class MapServerSettings
{
    public LoggingSettings Logging { get; init; } = new();

    public InternalNetworkSettings InternalNetwork { get; init; } = new();

    public MapRuntimeSettings MapServices { get; init; } = new();

    public void Validate()
    {
        Logging.Validate();
        InternalNetwork.Validate();
        MapServices.Validate();
    }
}
