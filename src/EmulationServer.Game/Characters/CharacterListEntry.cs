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
// File: src/EmulationServer.Game/Characters/CharacterListEntry.cs
// Purpose: Contains character list entry code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Characters;

// Type: CharacterListEntry
// Purpose: Represents character list entry data passed through the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - Guid: GUID identifier used to select the exact record, object, or runtime owner.
// - Name: Name value supplied by the caller for this operation.
// - Race: Race value supplied by the caller for this operation.
// - Class: Class value supplied by the caller for this operation.
// - Gender: Gender value supplied by the caller for this operation.
// - Level: Level value supplied by the caller for this operation.
// - Zone: Zone value supplied by the caller for this operation.
// - Map: Map value supplied by the caller for this operation.
// - PositionX: Position X value supplied by the caller for this operation.
// - PositionY: Position Y value supplied by the caller for this operation.
// - PositionZ: Position Z value supplied by the caller for this operation.
// - GuildId: Guild ID identifier used to select the exact record, object, or runtime owner.
// - PlayerFlags: Player flags value supplied by the caller for this operation.
// - AtLogin: At login value supplied by the caller for this operation.
// - PlayerBytes: Player bytes value supplied by the caller for this operation.
// - PlayerBytes2: Player bytes2 value supplied by the caller for this operation.
// - Equipment: Equipment value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record CharacterListEntry(
    uint Guid,
    string Name,
    byte Race,
    byte Class,
    byte Gender,
    byte Level,
    uint Zone,
    uint Map,
    float PositionX,
    float PositionY,
    float PositionZ,
    uint GuildId,
    uint PlayerFlags,
    uint AtLogin,
    uint PlayerBytes,
    uint PlayerBytes2,
    IReadOnlyList<CharacterEquipmentDisplay> Equipment);
