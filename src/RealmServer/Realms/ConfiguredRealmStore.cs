
using EmulationServer.RealmServer.Configuration;

namespace EmulationServer.RealmServer.Realms;

public sealed class ConfiguredRealmStore
{
    private readonly Dictionary<uint, ConfiguredRealm> _realms;

    public ConfiguredRealmStore(IEnumerable<ConfiguredRealmSettings> realmSettings)
    {
        ArgumentNullException.ThrowIfNull(realmSettings);

        _realms = realmSettings
            .Select(settings => new ConfiguredRealm(settings))
            .ToDictionary(realm => realm.Id);

        if (_realms.Count == 0)
        {
            throw new InvalidOperationException("At least one configured realm is required.");
        }
    }

    public IReadOnlyCollection<ConfiguredRealm> GetRealmsForBuild(ushort build)
    {
        return _realms.Values
            .Where(realm => realm.Builds.Contains(build))
            .OrderBy(realm => realm.Id)
            .ToArray();
    }

    public bool TrySetRealmStatus(uint realmId, bool online, int activeConnections, int capacityLimit)
    {
        if (!_realms.TryGetValue(realmId, out ConfiguredRealm? realm))
        {
            return false;
        }

        realm.SetStatus(online, activeConnections, capacityLimit);
        return true;
    }
}
