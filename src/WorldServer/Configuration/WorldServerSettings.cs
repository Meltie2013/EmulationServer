
using EmulationServer.Database.Configuration;
using EmulationServer.Network.Configuration;

namespace EmulationServer.WorldServer.Configuration;

public sealed class WorldServerSettings
{
    public InternalNetworkSettings InternalNetwork { get; init; } = new();

    public DatabaseSettings Database { get; init; } = new();

    public RealmStatusSettings RealmStatus { get; init; } = new();

    public void Validate()
    {
        InternalNetwork.Validate();
        Database.Validate();
        RealmStatus.Validate();
    }
}
