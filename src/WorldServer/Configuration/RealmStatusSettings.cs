
namespace EmulationServer.WorldServer.Configuration;

public sealed class RealmStatusSettings
{
    public bool Enabled { get; init; } = true;

    public uint RealmId { get; init; } = 1;

    public string RealmServerHost { get; init; } = "127.0.0.1";

    public ushort RealmServerPort { get; init; } = 5005;

    public TimeSpan UpdateInterval { get; init; } = TimeSpan.FromSeconds(15);


    public void Validate()
    {
        if (!Enabled)
        {
            return;
        }

        if (RealmId == 0)
        {
            throw new InvalidOperationException("Realm status realm id must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(RealmServerHost))
        {
            throw new InvalidOperationException("Realm status RealmServer host is required.");
        }

        if (RealmServerPort == 0)
        {
            throw new InvalidOperationException("Realm status RealmServer port must be greater than zero.");
        }

        if (UpdateInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Realm status update interval must be greater than zero.");
        }

    }
}
