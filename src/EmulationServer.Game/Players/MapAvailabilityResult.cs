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
// File: src/EmulationServer.Game/Players/MapAvailabilityResult.cs
// Purpose: Contains map availability result code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Players;

// Type: MapAvailabilityResult
// Purpose: Represents map availability result data passed through the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - IsAvailable: Is available value supplied by the caller for this operation.
// - Reason: Reason value supplied by the caller for this operation.
// - OwnerServerName: Owner server name value supplied by the caller for this operation.
// - RequiresInstanceServer: Requires instance server value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record MapAvailabilityResult(bool IsAvailable, string Reason, string OwnerServerName, bool RequiresInstanceServer)
{

    // Method: Available
    // Purpose: Executes the available operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // - requiresInstanceServer: Requires instance server value supplied by the caller for this operation.
    // Returns: Returns the map availability result value produced by this operation.
    // Notes: This keeps the operation scoped to MapAvailabilityResult so callers do not duplicate validation, protocol, or persistence rules.
    public static MapAvailabilityResult Available(string ownerServerName, bool requiresInstanceServer = false)
    {
        return new MapAvailabilityResult(true, string.Empty, ownerServerName, requiresInstanceServer);
    }

    // Method: Unavailable
    // Purpose: Executes the unavailable operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - reason: Reason value supplied by the caller for this operation.
    // - requiresInstanceServer: Requires instance server value supplied by the caller for this operation.
    // Returns: Returns the map availability result value produced by this operation.
    // Notes: This keeps the operation scoped to MapAvailabilityResult so callers do not duplicate validation, protocol, or persistence rules.
    public static MapAvailabilityResult Unavailable(string reason, bool requiresInstanceServer = false)
    {
        return new MapAvailabilityResult(false, reason, string.Empty, requiresInstanceServer);
    }
}
