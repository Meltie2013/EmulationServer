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
  * File overview: src/EmulationServer.Game/Data/Dbc/Maps/WorldMapContinentDbcRecord.cs
  * This file exposes strongly typed WorldMapContinent.dbc rows for continent-level map coordinate information.
  */

namespace EmulationServer.Game.Data.Dbc.Maps;

/**
  * Represents one WorldMapContinent.dbc row used to understand continent map boundaries and taxi-map extents.
  */
public sealed record WorldMapContinentDbcRecord(
    int Id,
    int MapId,
    int LeftBoundary,
    int RightBoundary,
    int TopBoundary,
    int BottomBoundary,
    float ContinentOffsetX,
    float ContinentOffsetY,
    float Scale,
    float TaxiMinX,
    float TaxiMinY,
    float TaxiMaxX,
    float TaxiMaxY);
