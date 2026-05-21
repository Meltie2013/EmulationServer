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
  * File overview: src/EmulationServer.Game/Data/Dbc/Maps/WorldMapAreaDbcRecord.cs
  * This file exposes strongly typed WorldMapArea.dbc rows for map-display area bounds and area-table linking.
  */

namespace EmulationServer.Game.Data.Dbc.Maps;

/**
  * Represents one WorldMapArea.dbc row and links a world-map rectangle to AreaTable data.
  */
public sealed record WorldMapAreaDbcRecord(
    int Id,
    int WorldMapContinentId,
    int AreaTableId,
    string AreaName,
    float LocationLeft,
    float LocationRight,
    float LocationTop,
    float LocationBottom);
