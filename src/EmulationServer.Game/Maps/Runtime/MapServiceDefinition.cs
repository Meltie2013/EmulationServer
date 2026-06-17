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
// File: src/EmulationServer.Game/Maps/Runtime/MapServiceDefinition.cs
// Purpose: Contains map service definition code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Maps.Runtime;

// Type: MapServiceDefinition
// Purpose: Provides map service definition behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class MapServiceDefinition
{

    // Property: Gets or sets the map ID value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: map ID value exposed by the owning type.
    public int MapId { get; init; }

    // Property: Gets or sets the instance ID value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: instance ID value exposed by the owning type.
    public long InstanceId { get; init; }

    // Property: Gets or sets the name value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: name value exposed by the owning type.
    public string Name { get; init; } = string.Empty;

    // Property: Gets or sets the kind value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: kind value exposed by the owning type.
    public MapServiceKind Kind { get; init; }

    // Method: FromMilliseconds
    // Purpose: Executes the from milliseconds operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the time span tick interval { get; init; } = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to MapServiceDefinition so callers do not duplicate validation, protocol, or persistence rules.
    public TimeSpan TickInterval { get; init; } = TimeSpan.FromMilliseconds(100);

    // Property: Gets or sets the log ticks value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: log ticks value exposed by the owning type.
    public bool LogTicks { get; init; }

    // Method: Validate
    // Purpose: Validates or evaluates validate rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapServiceDefinition so callers do not duplicate validation, protocol, or persistence rules.
    public void Validate()
    {
        if (MapId < 0)
        {
            throw new InvalidOperationException("Map service map id must be greater than or equal to zero.");
        }

        if (InstanceId < 0)
        {
            throw new InvalidOperationException("Map service instance id must be greater than or equal to zero.");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException($"Map service {MapId} requires a display name.");
        }

        if (TickInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"Map service '{Name}' tick interval must be greater than zero.");
        }
    }
}
