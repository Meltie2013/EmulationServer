namespace EmulationServer.Game.Maps.Runtime;

public sealed class MapServiceDefinition
{
    public int MapId { get; init; }

    public long InstanceId { get; init; }

    public string Name { get; init; } = string.Empty;

    public MapServiceKind Kind { get; init; }

    public TimeSpan TickInterval { get; init; } = TimeSpan.FromMilliseconds(100);

    public bool LogTicks { get; init; }

    public void Validate()
    {
        if (MapId < 0)
        {
            throw new InvalidOperationException("Map service map id must be greater than or equal to zero.");
        }

        if (InstanceId < 0)
        {
            throw new InvalidOperationException("Map service instance id must be greater than or equal to zero.");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException($"Map service {MapId} requires a display name.");
        }

        if (TickInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"Map service '{Name}' tick interval must be greater than zero.");
        }
    }
}
