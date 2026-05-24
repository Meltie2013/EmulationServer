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
 * File overview: src/EmulationServer.Game/Data/Dbc/Items/ItemDisplayInfoDbcRecord.cs
 * Documents the ItemDisplayInfoDbcRecord source file in the DBC loading and strongly typed client data records area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Game.Data.Dbc.Items;

/**
  * Represents one ItemDisplayInfo.dbc row used for item appearance and character-list equipment display.
 * Positional fields carried by this record: Id, ModelName1, ModelName2, ModelTexture1, ModelTexture2, InventoryIcon1, InventoryIcon2, GeosetGroup1, GeosetGroup2, GeosetGroup3, SpellVisualId, GroupSoundIndex, HelmetGeosetVis1, HelmetGeosetVis2, Textures, ItemVisual.
  */
public sealed record ItemDisplayInfoDbcRecord(
    int Id,
    string ModelName1,
    string ModelName2,
    string ModelTexture1,
    string ModelTexture2,
    string InventoryIcon1,
    string InventoryIcon2,
    int GeosetGroup1,
    int GeosetGroup2,
    int GeosetGroup3,
    int SpellVisualId,
    int GroupSoundIndex,
    int HelmetGeosetVis1,
    int HelmetGeosetVis2,
    IReadOnlyList<string> Textures,
    int ItemVisual);
