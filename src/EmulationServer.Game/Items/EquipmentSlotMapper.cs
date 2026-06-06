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

namespace EmulationServer.Game.Items;

/**
  * Converts item inventory type values into Vanilla character equipment slot indexes.
  * The same mapping is used during starter item creation and character equipment display loading.
  */
public static class EquipmentSlotMapper
{
    public const int NoEquipmentSlot = -1;

    public static int FromInventoryType(byte inventoryType)
    {
        // CharStartOutfit.dbc stores item inventory type values, not character
        // equipment slot indexes. These are the Vanilla equipment slots used by
        // character_inventory and SMSG_CHAR_ENUM.
        return inventoryType switch
        {
            1 => 0,   // Head
            2 => 1,   // Neck
            3 => 2,   // Shoulders
            4 => 3,   // Shirt/body
            5 => 4,   // Chest
            6 => 5,   // Waist
            7 => 6,   // Legs
            8 => 7,   // Feet
            9 => 8,   // Wrists
            10 => 9,  // Hands
            11 => 10, // First finger
            12 => 12, // First trinket
            13 => 15, // One-hand weapon
            14 => 16, // Shield
            15 => 17, // Ranged
            16 => 14, // Back
            17 => 15, // Two-hand weapon
            19 => 18, // Tabard
            20 => 4,  // Robe/chest
            21 => 15, // Main hand
            22 => 16, // Off hand
            23 => 16, // Held in off hand
            25 => 17, // Thrown
            26 => 17, // Ranged right
            28 => 17, // Relic
            _ => NoEquipmentSlot,
        };
    }
}
