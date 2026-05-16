
using EmulationServer.Database.Configuration;
using EmulationServer.Network.Configuration;

namespace EmulationServer.CharacterServer.Configuration;

public sealed class CharacterServerSettings
{
    public InternalNetworkSettings InternalNetwork { get; init; } = new();

    public DatabaseSettings Database { get; init; } = new();

    public void Validate()
    {
        InternalNetwork.Validate();
        Database.Validate();
    }
}
