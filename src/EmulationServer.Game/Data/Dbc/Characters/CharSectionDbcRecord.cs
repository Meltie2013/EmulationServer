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
// File: src/EmulationServer.Game/Data/Dbc/Characters/CharSectionDbcRecord.cs
// Purpose: Contains char section DBC record code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Data.Dbc.Characters;

// Type: CharSectionDbcRecord
// Purpose: Represents char section DBC record data passed through the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - Id: ID identifier used to select the exact record, object, or runtime owner.
// - RaceId: Race ID identifier used to select the exact record, object, or runtime owner.
// - SexId: Sex ID identifier used to select the exact record, object, or runtime owner.
// - SectionType: Section type value supplied by the caller for this operation.
// - VariationIndex: Variation index value supplied by the caller for this operation.
// - ColorIndex: Color index value supplied by the caller for this operation.
// - TextureName1: Texture name1 value supplied by the caller for this operation.
// - TextureName2: Texture name2 value supplied by the caller for this operation.
// - TextureName3: Texture name3 value supplied by the caller for this operation.
// - Flags: Flags value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record CharSectionDbcRecord(
    int Id,
    int RaceId,
    int SexId,
    int SectionType,
    int VariationIndex,
    int ColorIndex,
    string TextureName1,
    string TextureName2,
    string TextureName3,
    int Flags);
