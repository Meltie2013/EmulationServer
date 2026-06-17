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
// File: src/MapServer/Configuration/MapServerSettings.cs
// Purpose: Contains map server settings code for the map server runtime, world-map ownership, and grid/tile coordination.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Game.Maps.Runtime;
using EmulationServer.Network.Configuration;

using EmulationServer.Shared.Logging.Configuration;

namespace EmulationServer.MapServer.Configuration;

// Type: MapServerSettings
// Purpose: Provides map server settings behavior for the map server runtime, world-map ownership, and grid/tile coordination.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class MapServerSettings
{

    public LoggingSettings Logging { get; init; } = new();

    public InternalNetworkSettings InternalNetwork { get; init; } = new();

    public MapRuntimeSettings MapServices { get; init; } = new();

    // Method: Validate
    // Purpose: Validates or evaluates validate rules for the map server runtime, world-map ownership, and grid/tile coordination.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapServerSettings so callers do not duplicate validation, protocol, or persistence rules.
    public void Validate()
    {
        Logging.Validate();
        InternalNetwork.Validate();
        MapServices.Validate();
    }
}
