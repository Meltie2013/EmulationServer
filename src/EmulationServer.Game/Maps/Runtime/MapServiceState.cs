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
  * This file belongs to the map service runtime, grid ownership, service state transitions, and health reporting portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Game.Maps.Runtime;

/**
  * Defines the allowed map service state values used to keep state and protocol decisions explicit.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public enum MapServiceState
{
    /**
      * Represents the offline value for MapServiceState.
      */
    Offline = 0,
    /**
      * Represents the starting value for MapServiceState.
      */
    Starting = 1,
    /**
      * Represents the online value for MapServiceState.
      */
    Online = 2,
    /**
      * Represents the restart requested value for MapServiceState.
      */
    RestartRequested = 3,
    /**
      * Represents the draining players value for MapServiceState.
      */
    DrainingPlayers = 4,
    /**
      * Represents the saving players value for MapServiceState.
      */
    SavingPlayers = 5,
    /**
      * Represents the unloading objects value for MapServiceState.
      */
    UnloadingObjects = 6,
    /**
      * Represents the reloading data value for MapServiceState.
      */
    ReloadingData = 7,
    /**
      * Represents the respawning objects value for MapServiceState.
      */
    RespawningObjects = 8,
    /**
      * Represents the stopping value for MapServiceState.
      */
    Stopping = 9,
    /**
      * Represents the faulted value for MapServiceState.
      */
    Faulted = 10,
}
