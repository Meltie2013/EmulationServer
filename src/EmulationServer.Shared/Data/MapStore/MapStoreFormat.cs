//
// Copyright (C) 2026 Emulation Server Project
//
// This program is free software. You can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation. either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY. Without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//

namespace EmulationServer.Shared.Data.MapStore;

/**
  * Owns the stable binary constants used by extracted mapstore runtime files.
  */
public static class MapStoreFormat
{
    public const ushort CurrentVersion = 1;
    public const int FileHeaderSize = 24;
    public const string TerrainMagic = "ESTR";
    public const string LiquidMagic = "ESLQ";
    public const string CollisionMagic = "ESCO";
    public const string NavmeshMagic = "ESNM";
    public const string IndexMagic = "ESIX";

    /**
      * Resolves the expected four-character magic for a runtime payload kind.
      */
    public static string GetMagic(MapStoreDataKind kind)
    {
        return kind switch
        {
            MapStoreDataKind.Terrain => TerrainMagic,
            MapStoreDataKind.Liquid => LiquidMagic,
            MapStoreDataKind.Collision => CollisionMagic,
            MapStoreDataKind.Navmesh => NavmeshMagic,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown mapstore data kind."),
        };
    }

    /**
      * Resolves the index bit assigned to a runtime payload kind.
      */
    public static MapStoreTileDataFlags GetTileDataFlag(MapStoreDataKind kind)
    {
        return kind switch
        {
            MapStoreDataKind.Terrain => MapStoreTileDataFlags.Terrain,
            MapStoreDataKind.Liquid => MapStoreTileDataFlags.Liquid,
            MapStoreDataKind.Collision => MapStoreTileDataFlags.Collision,
            MapStoreDataKind.Navmesh => MapStoreTileDataFlags.Navmesh,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown mapstore data kind."),
        };
    }
}
