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
// File: src/EmulationServer.Shared/Data/MapStore/MapStoreFileNames.cs
// Purpose: Contains map store file names code for the shared infrastructure, logging, timing, and cross-service utility layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Globalization;

namespace EmulationServer.Shared.Data.MapStore;

// Type: MapStoreFileNames
// Purpose: Provides map store file names behavior for the shared infrastructure, logging, timing, and cross-service utility layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class MapStoreFileNames
{
    // Constant: Defines the map ID digit count constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed map ID digit count value used anywhere this rule or protocol value is needed.
    public const int MapIdDigitCount = 3;
    // Constant: Defines the tile coordinate digit count constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed tile coordinate digit count value used anywhere this rule or protocol value is needed.
    public const int TileCoordinateDigitCount = 2;
    // Constant: Defines the maps directory name constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed maps directory name value used anywhere this rule or protocol value is needed.
    public const string MapsDirectoryName = "maps";
    // Constant: Defines the tiles directory name constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed tiles directory name value used anywhere this rule or protocol value is needed.
    public const string TilesDirectoryName = "tiles";
    // Constant: Defines the index file name constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed index file name value used anywhere this rule or protocol value is needed.
    public const string IndexFileName = "map.index.bin";
    // Constant: Defines the terrain suffix constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed terrain suffix value used anywhere this rule or protocol value is needed.
    public const string TerrainSuffix = ".terrain.bin";
    // Constant: Defines the liquid suffix constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed liquid suffix value used anywhere this rule or protocol value is needed.
    public const string LiquidSuffix = ".liquid.bin";
    // Constant: Defines the collision suffix constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed collision suffix value used anywhere this rule or protocol value is needed.
    public const string CollisionSuffix = ".collision.bin";
    // Constant: Defines the navmesh suffix constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed navmesh suffix value used anywhere this rule or protocol value is needed.
    public const string NavmeshSuffix = ".navmesh.bin";

    // Method: GetMapDirectory
    // Purpose: Retrieves get map directory data for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - mapStoreRootDirectory: Map store root directory value supplied by the caller for this operation.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to MapStoreFileNames so callers do not duplicate validation, protocol, or persistence rules.
    public static string GetMapDirectory(string mapStoreRootDirectory, uint mapId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapStoreRootDirectory);
        return Path.Combine(mapStoreRootDirectory, MapsDirectoryName, FormatMapId(mapId));
    }

    // Method: GetTilesDirectory
    // Purpose: Retrieves get tiles directory data for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - mapStoreRootDirectory: Map store root directory value supplied by the caller for this operation.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to MapStoreFileNames so callers do not duplicate validation, protocol, or persistence rules.
    public static string GetTilesDirectory(string mapStoreRootDirectory, uint mapId)
    {
        return Path.Combine(GetMapDirectory(mapStoreRootDirectory, mapId), TilesDirectoryName);
    }

    // Method: GetIndexPath
    // Purpose: Retrieves get index path data for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - mapStoreRootDirectory: Map store root directory value supplied by the caller for this operation.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to MapStoreFileNames so callers do not duplicate validation, protocol, or persistence rules.
    public static string GetIndexPath(string mapStoreRootDirectory, uint mapId)
    {
        return Path.Combine(GetMapDirectory(mapStoreRootDirectory, mapId), IndexFileName);
    }

    // Method: GetTileFileName
    // Purpose: Retrieves get tile file name data for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - tileX: Tile X value supplied by the caller for this operation.
    // - tileY: Tile Y value supplied by the caller for this operation.
    // - kind: Kind value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to MapStoreFileNames so callers do not duplicate validation, protocol, or persistence rules.
    public static string GetTileFileName(byte tileX, byte tileY, MapStoreDataKind kind)
    {
        return FormatTileCoordinate(tileX) + "_" + FormatTileCoordinate(tileY) + GetSuffix(kind);
    }

    // Method: GetTileFilePath
    // Purpose: Retrieves get tile file path data for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - mapStoreRootDirectory: Map store root directory value supplied by the caller for this operation.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - tileX: Tile X value supplied by the caller for this operation.
    // - tileY: Tile Y value supplied by the caller for this operation.
    // - kind: Kind value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to MapStoreFileNames so callers do not duplicate validation, protocol, or persistence rules.
    public static string GetTileFilePath(string mapStoreRootDirectory, uint mapId, byte tileX, byte tileY, MapStoreDataKind kind)
    {
        return Path.Combine(GetTilesDirectory(mapStoreRootDirectory, mapId), GetTileFileName(tileX, tileY, kind));
    }

    // Method: GetSuffix
    // Purpose: Retrieves get suffix data for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - kind: Kind value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to MapStoreFileNames so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: TryParseMapDirectoryName
    // Purpose: Attempts to retrieve or parse try parse map directory name data without treating normal misses as failures.
    // Parameters:
    // - directoryName: Directory name value supplied by the caller for this operation.
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns true when try parse map directory name succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to MapStoreFileNames so callers do not duplicate validation, protocol, or persistence rules.
    public static bool TryParseMapDirectoryName(string directoryName, out uint mapId)
    {
        mapId = 0;
        return directoryName.Length == MapIdDigitCount &&
               uint.TryParse(directoryName, NumberStyles.None, CultureInfo.InvariantCulture, out mapId);
    }

    // Method: TryParseTileFileName
    // Purpose: Attempts to retrieve or parse try parse tile file name data without treating normal misses as failures.
    // Parameters:
    // - fileName: File name value supplied by the caller for this operation.
    // - tileX: Tile X value supplied by the caller for this operation.
    // - tileY: Tile Y value supplied by the caller for this operation.
    // - kind: Kind value supplied by the caller for this operation.
    // Returns: Returns true when try parse tile file name succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to MapStoreFileNames so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: FormatMapId
    // Purpose: Executes the format map ID operation for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to MapStoreFileNames so callers do not duplicate validation, protocol, or persistence rules.
    public static string FormatMapId(uint mapId)
    {
        return mapId.ToString("D" + MapIdDigitCount.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }

    // Method: FormatTileCoordinate
    // Purpose: Executes the format tile coordinate operation for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - tileCoordinate: Tile coordinate value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to MapStoreFileNames so callers do not duplicate validation, protocol, or persistence rules.
    public static string FormatTileCoordinate(byte tileCoordinate)
    {
        return tileCoordinate.ToString("D" + TileCoordinateDigitCount.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }

    // Method: FormatTileKey
    // Purpose: Executes the format tile key operation for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - tileX: Tile X value supplied by the caller for this operation.
    // - tileY: Tile Y value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to MapStoreFileNames so callers do not duplicate validation, protocol, or persistence rules.
    public static string FormatTileKey(uint mapId, byte tileX, byte tileY)
    {
        return FormatTileKey(mapId, (int)tileX, (int)tileY);
    }

    // Method: FormatTileKey
    // Purpose: Executes the format tile key operation for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - tileX: Tile X value supplied by the caller for this operation.
    // - tileY: Tile Y value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to MapStoreFileNames so callers do not duplicate validation, protocol, or persistence rules.
    public static string FormatTileKey(uint mapId, int tileX, int tileY)
    {
        return FormatMapId(mapId) + "/" + FormatTileCoordinate(tileX) + "_" + FormatTileCoordinate(tileY);
    }

    // Method: FormatTileCoordinate
    // Purpose: Executes the format tile coordinate operation for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - tileCoordinate: Tile coordinate value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to MapStoreFileNames so callers do not duplicate validation, protocol, or persistence rules.
    public static string FormatTileCoordinate(int tileCoordinate)
    {
        return tileCoordinate.ToString("D" + TileCoordinateDigitCount.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }
}
