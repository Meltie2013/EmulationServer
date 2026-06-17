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
// File: src/EmulationServer.Game/Maps/Runtime/MapServiceControlResult.cs
// Purpose: Contains map service control result code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Maps.Runtime;

// Type: MapServiceControlResult
// Purpose: Represents map service control result data passed through the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - OwnerServerName: Owner server name value supplied by the caller for this operation.
// - Kind: Kind value supplied by the caller for this operation.
// - MapId: Map ID identifier used to select the exact record, object, or runtime owner.
// - InstanceId: Instance ID identifier used to select the exact record, object, or runtime owner.
// - ResultCode: Result code value supplied by the caller for this operation.
// - State: State value supplied by the caller for this operation.
// - Message: Message value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record MapServiceControlResult(
    string OwnerServerName,
    MapServiceKind Kind,
    int MapId,
    long InstanceId,
    MapServiceControlResultCode ResultCode,
    MapServiceState State,
    string Message)
{

    // Method: FromSnapshot
    // Purpose: Executes the from snapshot operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - snapshot: Snapshot value supplied by the caller for this operation.
    // - resultCode: Result code value supplied by the caller for this operation.
    // - message: Message value supplied by the caller for this operation.
    // Returns: Returns the map service control result value produced by this operation.
    // Notes: This keeps the operation scoped to MapServiceControlResult so callers do not duplicate validation, protocol, or persistence rules.
    public static MapServiceControlResult FromSnapshot(
        MapServiceSnapshot snapshot,
        MapServiceControlResultCode resultCode,
        string message)
    {
        return new MapServiceControlResult(
            snapshot.OwnerServerName,
            snapshot.Kind,
            snapshot.MapId,
            snapshot.InstanceId,
            resultCode,
            snapshot.State,
            message);
    }
}
