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
// File: src/EmulationServer.Game/Maps/Runtime/MapServiceState.cs
// Purpose: Contains map service state code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Maps.Runtime;

// Type: MapServiceState
// Purpose: Defines the allowed map service state values used by the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public enum MapServiceState
{

    // Enum Value: Defines the offline enum value.
    // Value: explicit expression 0.
    Offline = 0,

    // Enum Value: Defines the starting enum value.
    // Value: explicit expression 1.
    Starting = 1,

    // Enum Value: Defines the online enum value.
    // Value: explicit expression 2.
    Online = 2,

    // Enum Value: Defines the restart requested enum value.
    // Value: explicit expression 3.
    RestartRequested = 3,

    // Enum Value: Defines the draining players enum value.
    // Value: explicit expression 4.
    DrainingPlayers = 4,

    // Enum Value: Defines the saving players enum value.
    // Value: explicit expression 5.
    SavingPlayers = 5,

    // Enum Value: Defines the unloading objects enum value.
    // Value: explicit expression 6.
    UnloadingObjects = 6,

    // Enum Value: Defines the reloading data enum value.
    // Value: explicit expression 7.
    ReloadingData = 7,

    // Enum Value: Defines the respawning objects enum value.
    // Value: explicit expression 8.
    RespawningObjects = 8,

    // Enum Value: Defines the stopping enum value.
    // Value: explicit expression 9.
    Stopping = 9,

    // Enum Value: Defines the faulted enum value.
    // Value: explicit expression 10.
    Faulted = 10,
}
