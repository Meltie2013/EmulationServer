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

namespace EmulationServer.Game.Maps.Runtime;

public sealed class MapRuntimeSettings
{
    public bool Enabled { get; init; } = true;

    public TimeSpan TickInterval { get; init; } = TimeSpan.FromMilliseconds(100);

    public TimeSpan StatusReportInterval { get; init; } = TimeSpan.FromSeconds(15);

    public bool LogTicks { get; init; }

    public string DataDirectory { get; init; } = "Data";

    public string DbcDirectory { get; init; } = "dbc";

    public string MapsDirectory { get; init; } = "maps";

    public bool LoadDbcStores { get; init; } = true;

    public bool LoadMapTiles { get; init; } = true;

    public MapGridLoadingMode GridLoadingMode { get; init; } = MapGridLoadingMode.OnDemand;

    public bool KeepLoadedGrids { get; init; }

    public TimeSpan GridIdleUnloadDelay { get; init; } = TimeSpan.FromMinutes(5);

    public IReadOnlyList<MapTileKey> StartupGrids { get; init; } = [];

    public IReadOnlyList<string> RequiredDbcFiles { get; init; } = [];

    public IReadOnlyList<MapServiceDefinition> Services { get; init; } = [];

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
