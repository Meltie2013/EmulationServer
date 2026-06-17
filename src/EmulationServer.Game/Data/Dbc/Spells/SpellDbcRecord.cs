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
// File: src/EmulationServer.Game/Data/Dbc/Spells/SpellDbcRecord.cs
// Purpose: Contains spell DBC record code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Data.Dbc.Spells;

// Type: SpellDbcRecord
// Purpose: Represents spell DBC record data passed through the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - Id: ID identifier used to select the exact record, object, or runtime owner.
// - School: School value supplied by the caller for this operation.
// - Category: Category value supplied by the caller for this operation.
// - DispelType: Dispel type value supplied by the caller for this operation.
// - Mechanic: Mechanic value supplied by the caller for this operation.
// - Attributes: Attributes value supplied by the caller for this operation.
// - AttributesEx: Attributes ex value supplied by the caller for this operation.
// - CastingTimeIndex: Casting time index value supplied by the caller for this operation.
// - DurationIndex: Duration index value supplied by the caller for this operation.
// - PowerType: Power type value supplied by the caller for this operation.
// - ManaCost: Mana cost value supplied by the caller for this operation.
// - RangeIndex: Range index value supplied by the caller for this operation.
// - SpellIconId: Spell icon ID identifier used to select the exact record, object, or runtime owner.
// - Name: Name value supplied by the caller for this operation.
// - NameSubText: Name sub text value supplied by the caller for this operation.
// - Description: Description value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record SpellDbcRecord(
    int Id,
    int School,
    int Category,
    int DispelType,
    int Mechanic,
    int Attributes,
    int AttributesEx,
    int CastingTimeIndex,
    int DurationIndex,
    int PowerType,
    int ManaCost,
    int RangeIndex,
    int SpellIconId,
    string Name,
    string NameSubText,
    string Description);
