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
// File: src/EmulationServer.Game/Characters/CharacterEquipmentDisplay.cs
// Purpose: Contains character equipment display code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Characters;

// Type: CharacterEquipmentDisplay
// Purpose: Represents character equipment display data passed through the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - DisplayId: Display ID identifier used to select the exact record, object, or runtime owner.
// - InventoryType: Inventory type value supplied by the caller for this operation.
// - EnchantmentId: Enchantment ID identifier used to select the exact record, object, or runtime owner.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record CharacterEquipmentDisplay(uint DisplayId, byte InventoryType, uint EnchantmentId);
