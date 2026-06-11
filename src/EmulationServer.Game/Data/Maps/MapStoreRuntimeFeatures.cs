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

using EmulationServer.Shared.Data.MapStore;

/**
  * File overview: src/EmulationServer.Game/Data/Maps/MapStoreRuntimeFeatures.cs
  * Documents the compile-time runtime feature gates for extracted mapstore data.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Data.Maps;

/**
  * Defines which mapstore payload types are required by the runtime.
  * All payload types are enabled by default so production behavior is consistent across servers.
  * Disabling one requires recompiling with a dedicated compiler symbol such as EMULATIONSERVER_MAPSTORE_DISABLE_COLLISION.
  */
public static class MapStoreRuntimeFeatures
{
#if EMULATIONSERVER_MAPSTORE_DISABLE_TERRAIN
    public const bool TerrainEnabled = false;
#else
    public const bool TerrainEnabled = true;
#endif

#if EMULATIONSERVER_MAPSTORE_DISABLE_LIQUID
    public const bool LiquidEnabled = false;
#else
    public const bool LiquidEnabled = true;
#endif

#if EMULATIONSERVER_MAPSTORE_DISABLE_COLLISION
    public const bool CollisionEnabled = false;
#else
    public const bool CollisionEnabled = true;
#endif

#if EMULATIONSERVER_MAPSTORE_DISABLE_NAVMESH
    public const bool NavmeshEnabled = false;
#else
    public const bool NavmeshEnabled = true;
#endif

    /**
      * Gets the runtime data flags that must exist for each loaded tile.
      */
    public static MapStoreTileDataFlags RequiredFlags { get; } = BuildRequiredFlags();

    /**
      * Gets the ordered runtime data kinds that must be loaded for each tile.
      */
    public static IReadOnlyList<MapStoreDataKind> RequiredKinds { get; } = BuildRequiredKinds();

    /**
      * Determines if a mapstore kind is enabled in this build.
      */
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

    /**
      * Formats the active feature policy for startup diagnostics.
      */
    public static string FormatPolicy()
    {
        return $"terrain={(TerrainEnabled ? "required" : "compile-disabled")}, " +
               $"liquid={(LiquidEnabled ? "required" : "compile-disabled")}, " +
               $"collision/vmaps={(CollisionEnabled ? "required" : "compile-disabled")}, " +
               $"navmesh/mmaps={(NavmeshEnabled ? "required" : "compile-disabled")}";
    }

    /**
      * Builds the required tile flags from compile-time symbols.
      */
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

    /**
      * Builds the required tile kind list from compile-time symbols.
      */
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
