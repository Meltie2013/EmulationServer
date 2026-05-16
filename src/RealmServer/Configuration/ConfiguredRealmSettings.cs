
namespace EmulationServer.RealmServer.Configuration;

public sealed class ConfiguredRealmSettings
{
    public uint Id { get; init; }

    public string Name { get; init; } = "Emulation Server";

    public string Address { get; init; } = "127.0.0.1";

    public ushort Port { get; init; } = 8085;

    public byte Icon { get; init; }

    public byte RealmFlags { get; init; }

    public byte Timezone { get; init; } = 1;

    public byte AllowedSecurityLevel { get; init; }

    public float Population { get; init; }

    public bool Online { get; init; }

    public IReadOnlySet<ushort> Builds { get; init; } = new HashSet<ushort> { 5875, 6005, 6141 };

    public void Validate()
    {
        if (Id == 0)
        {
            throw new InvalidOperationException("Realm id must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException($"Realm {Id} name is required.");
        }

        if (string.IsNullOrWhiteSpace(Address))
        {
            throw new InvalidOperationException($"Realm {Id} address is required.");
        }

        if (Port == 0)
        {
            throw new InvalidOperationException($"Realm {Id} port is required.");
        }

        if (Builds.Count == 0)
        {
            throw new InvalidOperationException($"Realm {Id} must allow at least one client build.");
        }
    }
}
