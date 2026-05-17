namespace EmulationServer.Game.Data.Maps;

public readonly record struct MapTileKey(uint MapId, byte TileX, byte TileY)
{
    public override string ToString()
    {
        return $"{MapId:D3}{TileX:D2}{TileY:D2}";
    }
}
