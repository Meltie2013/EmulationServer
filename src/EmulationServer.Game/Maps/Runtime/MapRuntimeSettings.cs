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

using EmulationServer.Game.Data.Maps;

/**
  * File overview: src/EmulationServer.Game/Maps/Runtime/MapRuntimeSettings.cs
  * This file belongs to the map service runtime, grid ownership, service state transitions, and health reporting portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Game.Maps.Runtime;

/**
  * Represents the map runtime settings component in the map service runtime, grid ownership, service state transitions, and health reporting area.
  * It keeps configuration values grouped by responsibility and prevents unrelated server code from reading raw INI keys.
  */
public sealed class MapRuntimeSettings
{
    /**
      * Gets or stores the enabled value used by MapRuntimeSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public bool Enabled { get; init; } = true;

    /**
      * Gets or stores the tick interval value used by MapRuntimeSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public TimeSpan TickInterval { get; init; } = TimeSpan.FromMilliseconds(100);

    /**
      * Gets or stores the status report interval value used by MapRuntimeSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public TimeSpan StatusReportInterval { get; init; } = TimeSpan.FromSeconds(15);

    /**
      * Gets or stores the log ticks value used by MapRuntimeSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public bool LogTicks { get; init; }

    /**
      * Gets or stores the data directory value used by MapRuntimeSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string DataDirectory { get; init; } = "Data";

    /**
      * Gets or stores the dbc directory value used by MapRuntimeSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string DbcDirectory { get; init; } = "dbc";

    /**
      * Gets or stores the maps directory value used by MapRuntimeSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string MapsDirectory { get; init; } = "maps";

    /**
      * Gets or stores the load dbc stores value used by MapRuntimeSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public bool LoadDbcStores { get; init; } = true;

    /**
      * Gets or stores the load map tiles value used by MapRuntimeSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public bool LoadMapTiles { get; init; } = true;

    /**
      * Gets or stores the grid loading mode value used by MapRuntimeSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public MapGridLoadingMode GridLoadingMode { get; init; } = MapGridLoadingMode.OnDemand;

    /**
      * Gets or stores the keep loaded grids value used by MapRuntimeSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public bool KeepLoadedGrids { get; init; }

    /**
      * Gets or stores the grid idle unload delay value used by MapRuntimeSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public TimeSpan GridIdleUnloadDelay { get; init; } = TimeSpan.FromMinutes(5);

    /**
      * Gets or stores the startup grids value used by MapRuntimeSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public IReadOnlyList<MapTileKey> StartupGrids { get; init; } = [];

    /**
      * Gets or stores the required dbc files value used by MapRuntimeSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public IReadOnlyList<string> RequiredDbcFiles { get; init; } = [];

    /**
      * Gets or stores the services value used by MapRuntimeSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public IReadOnlyList<MapServiceDefinition> Services { get; init; } = [];

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of MapRuntimeSettings and keeps this workflow isolated from the caller.
      */
    public void Validate()
    {
        if (TickInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Map service tick interval must be greater than zero.");
        }

        if (StatusReportInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Map service status report interval must be greater than zero.");
        }

        if (GridIdleUnloadDelay < TimeSpan.Zero)
        {
            throw new InvalidOperationException("Map service grid idle unload delay cannot be negative.");
        }

        if (!Enabled)
        {
            return;
        }

        if (Services.Count == 0)
        {
            throw new InvalidOperationException("At least one map service must be configured when map services are enabled.");
        }

        if (LoadDbcStores && RequiredDbcFiles.Count == 0)
        {
            throw new InvalidOperationException("At least one required DBC file must be configured when map-service DBC loading is enabled.");
        }

        if ((LoadDbcStores || LoadMapTiles) && string.IsNullOrWhiteSpace(DataDirectory))
        {
            throw new InvalidOperationException("Map-service data directory is required when game data loading is enabled.");
        }

        if (LoadDbcStores && string.IsNullOrWhiteSpace(DbcDirectory))
        {
            throw new InvalidOperationException("Map-service DBC directory is required when DBC loading is enabled.");
        }

        if (LoadMapTiles && string.IsNullOrWhiteSpace(MapsDirectory))
        {
            throw new InvalidOperationException("Map-service maps directory is required when map tile loading is enabled.");
        }

        HashSet<(MapServiceKind Kind, int MapId, long InstanceId)> serviceKeys = [];
        foreach (MapServiceDefinition service in Services)
        {
            service.Validate();

            if (!serviceKeys.Add((service.Kind, service.MapId, service.InstanceId)))
            {
                throw new InvalidOperationException($"Duplicate map service registration for kind={service.Kind}, map={service.MapId}, instance={service.InstanceId}.");
            }
        }

        foreach (string requiredDbcFile in RequiredDbcFiles)
        {
            if (string.IsNullOrWhiteSpace(requiredDbcFile))
            {
                throw new InvalidOperationException("Required DBC file list cannot contain empty entries.");
            }
        }
    }
}
