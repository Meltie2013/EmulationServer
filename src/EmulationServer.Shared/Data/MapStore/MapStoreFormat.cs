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
// File: src/EmulationServer.Shared/Data/MapStore/MapStoreFormat.cs
// Purpose: Contains map store format code for the shared infrastructure, logging, timing, and cross-service utility layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Shared.Data.MapStore;

// Type: MapStoreFormat
// Purpose: Provides map store format behavior for the shared infrastructure, logging, timing, and cross-service utility layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class MapStoreFormat
{
    // Constant: Defines the current version constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed current version value used anywhere this rule or protocol value is needed.
    public const ushort CurrentVersion = 1;
    // Constant: Defines the file header size constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed file header size value used anywhere this rule or protocol value is needed.
    public const int FileHeaderSize = 24;
    // Constant: Defines the terrain magic constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed terrain magic value used anywhere this rule or protocol value is needed.
    public const string TerrainMagic = "ESTR";
    // Constant: Defines the liquid magic constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed liquid magic value used anywhere this rule or protocol value is needed.
    public const string LiquidMagic = "ESLQ";
    // Constant: Defines the collision magic constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed collision magic value used anywhere this rule or protocol value is needed.
    public const string CollisionMagic = "ESCO";
    // Constant: Defines the navmesh magic constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed navmesh magic value used anywhere this rule or protocol value is needed.
    public const string NavmeshMagic = "ESNM";
    // Constant: Defines the index magic constant used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: fixed index magic value used anywhere this rule or protocol value is needed.
    public const string IndexMagic = "ESIX";

    // Method: GetMagic
    // Purpose: Retrieves get magic data for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - kind: Kind value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to MapStoreFormat so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: GetTileDataFlag
    // Purpose: Retrieves get tile data flag data for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - kind: Kind value supplied by the caller for this operation.
    // Returns: Returns the map store tile data flags value produced by this operation.
    // Notes: This keeps the operation scoped to MapStoreFormat so callers do not duplicate validation, protocol, or persistence rules.
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
