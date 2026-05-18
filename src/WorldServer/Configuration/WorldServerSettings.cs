
using EmulationServer.Database.Configuration;
using EmulationServer.Network.Configuration;

using EmulationServer.Shared.Logging.Configuration;
namespace EmulationServer.WorldServer.Configuration;

public sealed class WorldServerSettings
{
    public LoggingSettings Logging { get; init; } = new();

    public InternalNetworkSettings InternalNetwork { get; init; } = new();

    public int MaxConnections { get; init; } = 1000;

    public DatabaseSettings Database { get; init; } = new();

    public RealmStatusSettings RealmStatus { get; init; } = new();

    public GameDataSettings GameData { get; init; } = new();

    public void Validate()
    {
        Logging.Validate();
        InternalNetwork.Validate();

        if (MaxConnections <= 0)
        {
            throw new InvalidOperationException("WorldServer max connections must be greater than zero.");
        }

        Database.Validate();
        RealmStatus.Validate();
        GameData.Validate();
    }
}
