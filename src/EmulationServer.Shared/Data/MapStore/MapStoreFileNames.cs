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

using System.Globalization;

namespace EmulationServer.Shared.Data.MapStore;

/**
  * Centralizes the folder and filename rules for extracted mapstore files.
  */
public static class MapStoreFileNames
{
    public const int MapIdDigitCount = 3;
    public const int TileCoordinateDigitCount = 2;
    public const string MapsDirectoryName = "maps";
    public const string TilesDirectoryName = "tiles";
    public const string IndexFileName = "map.index.bin";
    public const string TerrainSuffix = ".terrain.bin";
    public const string LiquidSuffix = ".liquid.bin";
    public const string CollisionSuffix = ".collision.bin";
    public const string NavmeshSuffix = ".navmesh.bin";

    /**
      * Resolves the directory for one map inside the mapstore root.
      */
    public static string GetMapDirectory(string mapStoreRootDirectory, uint mapId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapStoreRootDirectory);
        return Path.Combine(mapStoreRootDirectory, MapsDirectoryName, FormatMapId(mapId));
    }

    /**
      * Resolves the tiles directory for one map inside the mapstore root.
      */
    public static string GetTilesDirectory(string mapStoreRootDirectory, uint mapId)
    {
        return Path.Combine(GetMapDirectory(mapStoreRootDirectory, mapId), TilesDirectoryName);
    }

    /**
      * Resolves the map index file path for one map inside the mapstore root.
      */
    public static string GetIndexPath(string mapStoreRootDirectory, uint mapId)
    {
        return Path.Combine(GetMapDirectory(mapStoreRootDirectory, mapId), IndexFileName);
    }

    /**
      * Resolves the filename for one tile payload.
      */
    public static string GetTileFileName(byte tileX, byte tileY, MapStoreDataKind kind)
    {
        return FormatTileCoordinate(tileX) + "_" + FormatTileCoordinate(tileY) + GetSuffix(kind);
    }

    /**
      * Resolves the path for one tile payload inside the mapstore root.
      */
    public static string GetTileFilePath(string mapStoreRootDirectory, uint mapId, byte tileX, byte tileY, MapStoreDataKind kind)
    {
        return Path.Combine(GetTilesDirectory(mapStoreRootDirectory, mapId), GetTileFileName(tileX, tileY, kind));
    }

    /**
      * Resolves the suffix assigned to a runtime payload kind.
      */
    public static string GetSuffix(MapStoreDataKind kind)
    {
        return kind switch
        {
            MapStoreDataKind.Terrain => TerrainSuffix,
            MapStoreDataKind.Liquid => LiquidSuffix,
            MapStoreDataKind.Collision => CollisionSuffix,
            MapStoreDataKind.Navmesh => NavmeshSuffix,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown mapstore data kind."),
        };
    }


    /**
      * Tries to parse a canonical map directory name such as 000.
      */
    public static bool TryParseMapDirectoryName(string directoryName, out uint mapId)
    {
        mapId = 0;
        return directoryName.Length == MapIdDigitCount &&
               uint.TryParse(directoryName, NumberStyles.None, CultureInfo.InvariantCulture, out mapId);
    }

    /**
      * Tries to parse a tile filename such as 31_48.terrain.bin.
      */
    public static bool TryParseTileFileName(string fileName, out byte tileX, out byte tileY, out MapStoreDataKind kind)
    {
        tileX = 0;
        tileY = 0;
        kind = default;

        string? suffix = null;
        foreach (MapStoreDataKind candidate in Enum.GetValues<MapStoreDataKind>())
        {
            string candidateSuffix = GetSuffix(candidate);
            if (fileName.EndsWith(candidateSuffix, StringComparison.OrdinalIgnoreCase))
            {
                kind = candidate;
                suffix = candidateSuffix;
                break;
            }
        }

        if (suffix is null)
        {
            return false;
        }

        string coordinateText = fileName[..^suffix.Length];
        string[] parts = coordinateText.Split('_', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2 &&
               parts[0].Length == TileCoordinateDigitCount &&
               parts[1].Length == TileCoordinateDigitCount &&
               byte.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out tileX) &&
               byte.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out tileY);
    }

    /**
      * Formats a map id with the canonical mapstore width.
      */
    public static string FormatMapId(uint mapId)
    {
        return mapId.ToString("D" + MapIdDigitCount.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }

    /**
      * Formats one tile coordinate with the canonical mapstore width.
      */
    public static string FormatTileCoordinate(byte tileCoordinate)
    {
        return tileCoordinate.ToString("D" + TileCoordinateDigitCount.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }

    /**
      * Formats one map/tile identifier for logs and validation messages.
      */
    public static string FormatTileKey(uint mapId, byte tileX, byte tileY)
    {
        return FormatTileKey(mapId, (int)tileX, (int)tileY);
    }

    /**
      * Formats one map/tile identifier when an inner payload used wider coordinate fields.
      */
    public static string FormatTileKey(uint mapId, int tileX, int tileY)
    {
        return FormatMapId(mapId) + "/" + FormatTileCoordinate(tileX) + "_" + FormatTileCoordinate(tileY);
    }

    /**
      * Formats one tile coordinate with the canonical mapstore width.
      */
    public static string FormatTileCoordinate(int tileCoordinate)
    {
        return tileCoordinate.ToString("D" + TileCoordinateDigitCount.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }
}

