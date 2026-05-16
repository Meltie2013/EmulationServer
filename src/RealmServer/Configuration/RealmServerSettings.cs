
using EmulationServer.Database.Configuration;
using EmulationServer.Network.Configuration;

namespace EmulationServer.RealmServer.Configuration;

public sealed class RealmServerSettings
{
    public RealmSocketListenerSettings Socket { get; init; } = new();

    public DatabaseSettings Database { get; init; } = new();

    public void Validate()
    {
        Socket.Validate();
        Database.Validate();
    }
}
