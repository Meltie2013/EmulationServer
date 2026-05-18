namespace EmulationServer.Game.Maps.Runtime;

public sealed record MapServiceSnapshot(
    string OwnerServerName,
    MapServiceKind Kind,
    int MapId,
    long InstanceId,
    string Name,
    MapServiceState State,
    long Tick,
    int ActivePlayers,
    int ActiveGrids,
    double LastTickMilliseconds,
    double AverageTickMilliseconds,
    double LoadPercent,
    DateTimeOffset StartedUtc,
    DateTimeOffset LastTickUtc);
