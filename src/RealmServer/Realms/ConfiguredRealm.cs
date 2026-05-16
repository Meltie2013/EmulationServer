
using EmulationServer.RealmServer.Configuration;

namespace EmulationServer.RealmServer.Realms;

public sealed class ConfiguredRealm
{
    private readonly object _syncRoot = new();

    private bool _online;
    private int _activeConnections;
    private int _maxConnections;

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

        _online = settings.Online;
        _activeConnections = settings.ActiveConnections;
        _maxConnections = settings.MaxConnections;
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

    public bool IsOnline
    {
        get
        {
            lock (_syncRoot)
            {
                return _online;
            }
        }
    }

    public int ActiveConnections
    {
        get
        {
            lock (_syncRoot)
            {
                return _activeConnections;
            }
        }
    }

    public int MaxConnections
    {
        get
        {
            lock (_syncRoot)
            {
                return _maxConnections;
            }
        }
    }

    public float Population
    {
        get
        {
            lock (_syncRoot)
            {
                return RealmPopulationCalculator.Calculate(_activeConnections, _maxConnections);
            }
        }
    }

    public string ClientAddress => $"{Address}:{Port}";

    public void SetStatus(bool online, int activeConnections, int maxConnections)
    {
        lock (_syncRoot)
        {
            _online = online;
            _activeConnections = Math.Max(0, activeConnections);
            _maxConnections = Math.Max(1, maxConnections);
        }
    }
}
