
using EmulationServer.RealmServer.Configuration;

namespace EmulationServer.RealmServer.Realms;

public sealed class ConfiguredRealm
{
    private int _online;
    private float _population;

    public ConfiguredRealm(ConfiguredRealmSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        Id = settings.Id;
        Name = settings.Name;
        Address = settings.Address;
        Port = settings.Port;
        Icon = settings.Icon;
        BaseRealmFlags = settings.RealmFlags;
        Timezone = settings.Timezone;
        AllowedSecurityLevel = settings.AllowedSecurityLevel;
        Builds = settings.Builds;

        _online = settings.Online ? 1 : 0;
        _population = settings.Population;
    }

    public uint Id { get; }

    public string Name { get; }

    public string Address { get; }

    public ushort Port { get; }

    public byte Icon { get; }

    public byte BaseRealmFlags { get; }

    public byte Timezone { get; }

    public byte AllowedSecurityLevel { get; }

    public IReadOnlySet<ushort> Builds { get; }

    public bool IsOnline => Volatile.Read(ref _online) == 1;

    public float Population => _population;

    public string ClientAddress => $"{Address}:{Port}";

    public void SetStatus(bool online, float? population = null)
    {
        Volatile.Write(ref _online, online ? 1 : 0);

        if (population.HasValue)
        {
            _population = Math.Max(0.0f, population.Value);
        }
    }
}
