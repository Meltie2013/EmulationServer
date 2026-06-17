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
// File: src/EmulationServer.Game/Data/Dbc/Items/ItemDisplayInfoDbcRecord.cs
// Purpose: Contains item display info DBC record code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Data.Dbc.Items;

// Type: ItemDisplayInfoDbcRecord
// Purpose: Represents item display info DBC record data passed through the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - Id: ID identifier used to select the exact record, object, or runtime owner.
// - ModelName1: Model name1 value supplied by the caller for this operation.
// - ModelName2: Model name2 value supplied by the caller for this operation.
// - ModelTexture1: Model texture1 value supplied by the caller for this operation.
// - ModelTexture2: Model texture2 value supplied by the caller for this operation.
// - InventoryIcon1: Inventory icon1 value supplied by the caller for this operation.
// - InventoryIcon2: Inventory icon2 value supplied by the caller for this operation.
// - GeosetGroup1: Geoset group1 value supplied by the caller for this operation.
// - GeosetGroup2: Geoset group2 value supplied by the caller for this operation.
// - GeosetGroup3: Geoset group3 value supplied by the caller for this operation.
// - SpellVisualId: Spell visual ID identifier used to select the exact record, object, or runtime owner.
// - GroupSoundIndex: Group sound index value supplied by the caller for this operation.
// - HelmetGeosetVis1: Helmet geoset vis1 value supplied by the caller for this operation.
// - HelmetGeosetVis2: Helmet geoset vis2 value supplied by the caller for this operation.
// - Textures: Textures value supplied by the caller for this operation.
// - ItemVisual: Item visual value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
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
