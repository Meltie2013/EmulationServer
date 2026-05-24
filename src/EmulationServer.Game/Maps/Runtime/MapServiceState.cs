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
 * File overview: src/EmulationServer.Game/Maps/Runtime/MapServiceState.cs
 * Documents the MapServiceState source file in the runtime map-player state tracking area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Game.Maps.Runtime;

/**
 * Lists the supported map service state values used by the runtime map-player state tracking layer.
 * Numeric values are part of the project contract and should only be changed when the related client packet, DBC value, or database schema is updated as well.
 */
public enum MapServiceState
{
    /**
     * Represents the offline value for map service state handling.
     */
    Offline = 0,
    /**
     * Represents the starting value for map service state handling.
     */
    Starting = 1,
    /**
     * Represents the online value for map service state handling.
     */
    Online = 2,
    /**
     * Represents the restart requested value for map service state handling.
     */
    RestartRequested = 3,
    /**
     * Represents the draining players value for map service state handling.
     */
    DrainingPlayers = 4,
    /**
     * Represents the saving players value for map service state handling.
     */
    SavingPlayers = 5,
    /**
     * Represents the unloading objects value for map service state handling.
     */
    UnloadingObjects = 6,
    /**
     * Represents the reloading data value for map service state handling.
     */
    ReloadingData = 7,
    /**
     * Represents the respawning objects value for map service state handling.
     */
    RespawningObjects = 8,
    /**
     * Represents the stopping value for map service state handling.
     */
    Stopping = 9,
    /**
     * Represents the faulted value for map service state handling.
     */
    Faulted = 10,
}
