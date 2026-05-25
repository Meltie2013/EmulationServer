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
  * File overview: src/EmulationServer.Game/Players/PlayerInventoryItem.cs
  * Documents the PlayerInventoryItem source file in the logged-in player state, persistence models, and gameplay records area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Players;

/**
  * Carries immutable player inventory item data for the logged-in player state, persistence models, and gameplay records layer.
  * Records in this project are used as explicit transfer models so packet parsing, database repositories, and runtime systems can pass strongly typed values without mutating shared state.
  * Positional fields carried by this record: ItemGuid, OwnerGuid, TemplateEntry, BagGuid, Slot, InstanceData, InventoryType, DisplayId, EnchantmentId, ContainerSlots, MaxDurability, StackCount.
  */
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
    /**
      * Stores the default is equipped value used when the caller does not supply an override.
      * Centralizing the default keeps configuration and packet behavior consistent across the server process.
      */
    public bool IsEquipped => BagGuid == 0 && Slot < 19;

    /**
      * True when the item has container slots and should be created with TYPEID_CONTAINER.
      */
    public bool IsContainer => ContainerSlots > 0;
}
