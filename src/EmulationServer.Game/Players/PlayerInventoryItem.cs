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
// File: src/EmulationServer.Game/Players/PlayerInventoryItem.cs
// Purpose: Contains player inventory item code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Players;

// Type: PlayerInventoryItem
// Purpose: Represents player inventory item data passed through the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - ItemGuid: Item GUID identifier used to select the exact record, object, or runtime owner.
// - OwnerGuid: Owner GUID identifier used to select the exact record, object, or runtime owner.
// - TemplateEntry: Template entry value supplied by the caller for this operation.
// - BagGuid: Bag GUID identifier used to select the exact record, object, or runtime owner.
// - Slot: Slot value supplied by the caller for this operation.
// - InstanceData: Instance data value supplied by the caller for this operation.
// - InventoryType: Inventory type value supplied by the caller for this operation.
// - DisplayId: Display ID identifier used to select the exact record, object, or runtime owner.
// - EnchantmentId: Enchantment ID identifier used to select the exact record, object, or runtime owner.
// - ContainerSlots: Container slots value supplied by the caller for this operation.
// - MaxDurability: Max durability value supplied by the caller for this operation.
// - StackCount: Stack count value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record PlayerInventoryItem(
    uint ItemGuid,
    uint OwnerGuid,
    uint TemplateEntry,
    uint BagGuid,
    byte Slot,
    string InstanceData,
    byte InventoryType,
    uint DisplayId,
    uint EnchantmentId,
    byte ContainerSlots,
    uint MaxDurability,
    uint StackCount)
{

    // Property: Gets or sets the is equipped value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: is equipped value exposed by the owning type.
    public bool IsEquipped => BagGuid == 0 && Slot < 19;

    // Property: Gets or sets the is container value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: is container value exposed by the owning type.
    public bool IsContainer => ContainerSlots > 0;
}
