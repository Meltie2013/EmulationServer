
using EmulationServer.Database.Configuration;
using EmulationServer.Network.Configuration;

using EmulationServer.Shared.Logging.Configuration;
namespace EmulationServer.RealmServer.Configuration;

public sealed class RealmServerSettings
{
    public LoggingSettings Logging { get; init; } = new();

    public RealmSocketListenerSettings Socket { get; init; } = new();

    public DatabaseSettings Database { get; init; } = new();

    public InternalNetworkSettings InternalNetwork { get; init; } = new();

    public IReadOnlyList<ConfiguredRealmSettings> Realms { get; init; } = [];

    public void Validate()
    {
        Logging.Validate();
        Socket.Validate();
        Database.Validate();
        InternalNetwork.Validate();

        if (Realms.Count == 0)
        {
            throw new InvalidOperationException("At least one realm must be configured.");
        }

        HashSet<uint> realmIds = [];
        foreach (ConfiguredRealmSettings realm in Realms)
        {
            realm.Validate();

            if (!realmIds.Add(realm.Id))
            {
                throw new InvalidOperationException($"Duplicate realm id configured: {realm.Id}.");
            }
        }
    }
}
