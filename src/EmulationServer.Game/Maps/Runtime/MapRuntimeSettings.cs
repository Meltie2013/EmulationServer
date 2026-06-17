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
// File: src/EmulationServer.Game/Maps/Runtime/MapRuntimeSettings.cs
// Purpose: Contains map runtime settings code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Maps.Runtime;

// Type: MapRuntimeSettings
// Purpose: Provides map runtime settings behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class MapRuntimeSettings
{

    // Property: Gets or sets the enabled value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: enabled value exposed by the owning type.
    public bool Enabled { get; init; } = true;

    // Method: FromMilliseconds
    // Purpose: Executes the from milliseconds operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the time span tick interval { get; init; } = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to MapRuntimeSettings so callers do not duplicate validation, protocol, or persistence rules.
    public TimeSpan TickInterval { get; init; } = TimeSpan.FromMilliseconds(100);

    // Method: FromSeconds
    // Purpose: Executes the from seconds operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the time span status report interval { get; init; } = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to MapRuntimeSettings so callers do not duplicate validation, protocol, or persistence rules.
    public TimeSpan StatusReportInterval { get; init; } = TimeSpan.FromSeconds(15);

    // Property: Gets or sets the log ticks value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: log ticks value exposed by the owning type.
    public bool LogTicks { get; init; }

    // Property: Gets or sets the data directory value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: data directory value exposed by the owning type.
    public string DataDirectory { get; init; } = "Data";

    // Property: Gets or sets the DBC directory value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: DBC directory value exposed by the owning type.
    public string DbcDirectory { get; init; } = "dbc";

    // Property: Gets or sets the maps directory value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: maps directory value exposed by the owning type.
    public string MapsDirectory { get; init; } = "mapstore";

    // Property: Gets or sets the load DBC stores value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: load DBC stores value exposed by the owning type.
    public bool LoadDbcStores { get; init; } = true;

    // Property: Gets or sets the required DBC files value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: required DBC files value exposed by the owning type.
    public IReadOnlyList<string> RequiredDbcFiles { get; init; } = [];

    // Property: Gets or sets the services value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: services value exposed by the owning type.
    public IReadOnlyList<MapServiceDefinition> Services { get; init; } = [];

    // Method: Validate
    // Purpose: Validates or evaluates validate rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapRuntimeSettings so callers do not duplicate validation, protocol, or persistence rules.
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

        if (string.IsNullOrWhiteSpace(DataDirectory))
        {
            throw new InvalidOperationException("Map-service data directory is required when map services are enabled.");
        }

        if (LoadDbcStores && string.IsNullOrWhiteSpace(DbcDirectory))
        {
            throw new InvalidOperationException("Map-service DBC directory is required when DBC loading is enabled.");
        }

        if (string.IsNullOrWhiteSpace(MapsDirectory))
        {
            throw new InvalidOperationException("Map-service mapstore directory is required when map services are enabled.");
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
