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
// File: src/EmulationServer.Game/Data/Maps/MapStoreRuntimeFeatures.cs
// Purpose: Contains map store runtime features code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Shared.Data.MapStore;

namespace EmulationServer.Game.Data.Maps;

// Type: MapStoreRuntimeFeatures
// Purpose: Provides map store runtime features behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class MapStoreRuntimeFeatures
{
#if EMULATIONSERVER_MAPSTORE_DISABLE_TERRAIN
    // Constant: Defines the terrain enabled constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed terrain enabled value used anywhere this rule or protocol value is needed.
    public const bool TerrainEnabled = false;
#else
    // Constant: Defines the terrain enabled constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed terrain enabled value used anywhere this rule or protocol value is needed.
    public const bool TerrainEnabled = true;
#endif

#if EMULATIONSERVER_MAPSTORE_DISABLE_LIQUID
    // Constant: Defines the liquid enabled constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed liquid enabled value used anywhere this rule or protocol value is needed.
    public const bool LiquidEnabled = false;
#else
    // Constant: Defines the liquid enabled constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed liquid enabled value used anywhere this rule or protocol value is needed.
    public const bool LiquidEnabled = true;
#endif

#if EMULATIONSERVER_MAPSTORE_DISABLE_COLLISION
    // Constant: Defines the collision enabled constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed collision enabled value used anywhere this rule or protocol value is needed.
    public const bool CollisionEnabled = false;
#else
    // Constant: Defines the collision enabled constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed collision enabled value used anywhere this rule or protocol value is needed.
    public const bool CollisionEnabled = true;
#endif

#if EMULATIONSERVER_MAPSTORE_DISABLE_NAVMESH
    // Constant: Defines the navmesh enabled constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed navmesh enabled value used anywhere this rule or protocol value is needed.
    public const bool NavmeshEnabled = false;
#else
    // Constant: Defines the navmesh enabled constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed navmesh enabled value used anywhere this rule or protocol value is needed.
    public const bool NavmeshEnabled = true;
#endif

    // Method: BuildRequiredFlags
    // Purpose: Builds or writes build required flags output for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the map store tile data flags required flags { get; } = value produced by this operation.
    // Notes: This keeps the operation scoped to MapStoreRuntimeFeatures so callers do not duplicate validation, protocol, or persistence rules.
    public static MapStoreTileDataFlags RequiredFlags { get; } = BuildRequiredFlags();

    // Method: BuildRequiredKinds
    // Purpose: Builds or writes build required kinds output for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the I read only list required kinds { get; } = value produced by this operation.
    // Notes: This keeps the operation scoped to MapStoreRuntimeFeatures so callers do not duplicate validation, protocol, or persistence rules.
    public static IReadOnlyList<MapStoreDataKind> RequiredKinds { get; } = BuildRequiredKinds();

    // Method: IsEnabled
    // Purpose: Validates or evaluates is enabled rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - kind: Kind value supplied by the caller for this operation.
    // Returns: Returns true when is enabled succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to MapStoreRuntimeFeatures so callers do not duplicate validation, protocol, or persistence rules.
    public static bool IsEnabled(MapStoreDataKind kind)
    {
        return kind switch
        {
            MapStoreDataKind.Terrain => TerrainEnabled,
            MapStoreDataKind.Liquid => LiquidEnabled,
            MapStoreDataKind.Collision => CollisionEnabled,
            MapStoreDataKind.Navmesh => NavmeshEnabled,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown mapstore data kind."),
        };
    }

    // Method: FormatPolicy
    // Purpose: Executes the format policy operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to MapStoreRuntimeFeatures so callers do not duplicate validation, protocol, or persistence rules.
    public static string FormatPolicy()
    {
        return $"terrain={(TerrainEnabled ? "required" : "compile-disabled")}, " +
               $"liquid={(LiquidEnabled ? "required" : "compile-disabled")}, " +
               $"collision/vmaps={(CollisionEnabled ? "required" : "compile-disabled")}, " +
               $"navmesh/mmaps={(NavmeshEnabled ? "required" : "compile-disabled")}";
    }

    // Method: BuildRequiredFlags
    // Purpose: Builds or writes build required flags output for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the map store tile data flags value produced by this operation.
    // Notes: This keeps the operation scoped to MapStoreRuntimeFeatures so callers do not duplicate validation, protocol, or persistence rules.
    private static MapStoreTileDataFlags BuildRequiredFlags()
    {
        MapStoreTileDataFlags flags = MapStoreTileDataFlags.None;

        if (TerrainEnabled)
        {
            flags |= MapStoreTileDataFlags.Terrain;
        }

        if (LiquidEnabled)
        {
            flags |= MapStoreTileDataFlags.Liquid;
        }

        if (CollisionEnabled)
        {
            flags |= MapStoreTileDataFlags.Collision;
        }

        if (NavmeshEnabled)
        {
            flags |= MapStoreTileDataFlags.Navmesh;
        }

        return flags;
    }

    // Method: BuildRequiredKinds
    // Purpose: Builds or writes build required kinds output for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to MapStoreRuntimeFeatures so callers do not duplicate validation, protocol, or persistence rules.
    private static IReadOnlyList<MapStoreDataKind> BuildRequiredKinds()
    {
        List<MapStoreDataKind> kinds = [];

        if (TerrainEnabled)
        {
            kinds.Add(MapStoreDataKind.Terrain);
        }

        if (LiquidEnabled)
        {
            kinds.Add(MapStoreDataKind.Liquid);
        }

        if (CollisionEnabled)
        {
            kinds.Add(MapStoreDataKind.Collision);
        }

        if (NavmeshEnabled)
        {
            kinds.Add(MapStoreDataKind.Navmesh);
        }

        return kinds;
    }
}
