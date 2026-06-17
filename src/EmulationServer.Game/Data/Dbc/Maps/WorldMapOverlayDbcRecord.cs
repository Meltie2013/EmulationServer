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
// File: src/EmulationServer.Game/Data/Dbc/Maps/WorldMapOverlayDbcRecord.cs
// Purpose: Contains world map overlay DBC record code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Data.Dbc.Maps;

// Type: WorldMapOverlayDbcRecord
// Purpose: Represents world map overlay DBC record data passed through the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - Id: ID identifier used to select the exact record, object, or runtime owner.
// - WorldMapAreaId: World map area ID identifier used to select the exact record, object, or runtime owner.
// - AreaTableIds: Area table ids value supplied by the caller for this operation.
// - LocationX: Location X value supplied by the caller for this operation.
// - LocationY: Location Y value supplied by the caller for this operation.
// - TextureName: Texture name value supplied by the caller for this operation.
// - TextureWidth: Texture width value supplied by the caller for this operation.
// - TextureHeight: Texture height value supplied by the caller for this operation.
// - OffsetX: Offset X value supplied by the caller for this operation.
// - OffsetY: Offset Y value supplied by the caller for this operation.
// - HitRectTop: Hit rect top value supplied by the caller for this operation.
// - HitRectLeft: Hit rect left value supplied by the caller for this operation.
// - HitRectBottom: Hit rect bottom value supplied by the caller for this operation.
// - HitRectRight: Hit rect right value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
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
