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
 * File overview: src/EmulationServer.Game/Data/Dbc/Maps/WorldMapOverlayDbcRecord.cs
 * Documents the WorldMapOverlayDbcRecord source file in the DBC loading and strongly typed client data records area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Game.Data.Dbc.Maps;

/**
  * Represents one WorldMapOverlay.dbc row and the area ids that the overlay covers.
 * Positional fields carried by this record: Id, WorldMapAreaId, AreaTableIds, LocationX, LocationY, TextureName, TextureWidth, TextureHeight, OffsetX, OffsetY, HitRectTop, HitRectLeft, HitRectBottom, HitRectRight.
  */
public sealed record WorldMapOverlayDbcRecord(
    int Id,
    int WorldMapAreaId,
    IReadOnlyList<int> AreaTableIds,
    int LocationX,
    int LocationY,
    string TextureName,
    int TextureWidth,
    int TextureHeight,
    int OffsetX,
    int OffsetY,
    int HitRectTop,
    int HitRectLeft,
    int HitRectBottom,
    int HitRectRight);
