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


/**
 * File overview: src/EmulationServer.Game/Maps/Runtime/MapServiceControlResult.cs
 * Documents the MapServiceControlResult source file in the runtime map-player state tracking area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Game.Maps.Runtime;

/**
  * Represents immutable map service control result data passed between parts of the server.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
 * Positional fields carried by this record: OwnerServerName, Kind, MapId, InstanceId, ResultCode, State, Message.
  */
public sealed record MapServiceControlResult(
    string OwnerServerName,
    MapServiceKind Kind,
    int MapId,
    long InstanceId,
    MapServiceControlResultCode ResultCode,
    MapServiceState State,
    string Message)
{
    /**
     * Performs the from snapshot operation for the runtime map-player state tracking workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: snapshot, resultCode, message.
     */
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
