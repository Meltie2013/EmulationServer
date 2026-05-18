using EmulationServer.Game.Data.Maps;

namespace EmulationServer.Game.Maps.Runtime;

public sealed class LoadedMapGrid
{
    public LoadedMapGrid(MapTileDataStore tile)
    {
        Tile = tile ?? throw new ArgumentNullException(nameof(tile));
        LoadedUtc = DateTimeOffset.UtcNow;
        LastUsedUtc = LoadedUtc;
    }

    public MapTileDataStore Tile { get; }

    public DateTimeOffset LoadedUtc { get; }

    public DateTimeOffset LastUsedUtc { get; private set; }

    public void Touch()
    {
        LastUsedUtc = DateTimeOffset.UtcNow;
    }
}
