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
  * File overview: src/EmulationServer.Game/Maps/Runtime/MapServiceSnapshot.cs
  * Documents the MapServiceSnapshot source file in the runtime map-player state tracking area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Maps.Runtime;

/**
  * Captures the observable runtime health of a map service for status packets and command output.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  * Positional fields carried by this record: OwnerServerName, Kind, MapId, InstanceId, Name, State, Tick, ActivePlayers, ActiveGrids, LastTickMilliseconds, AverageTickMilliseconds, LoadPercent, StartedUtc, LastTickUtc.
  */
public sealed record MapServiceSnapshot(
    string OwnerServerName,
    MapServiceKind Kind,
    int MapId,
    long InstanceId,
    string Name,
    MapServiceState State,
    long Tick,
    int ActivePlayers,
    int ActiveGrids,
    double LastTickMilliseconds,
    double AverageTickMilliseconds,
    double LoadPercent,
    DateTimeOffset StartedUtc,
    DateTimeOffset LastTickUtc);
