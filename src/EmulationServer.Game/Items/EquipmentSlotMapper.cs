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
// File: src/EmulationServer.Game/Items/EquipmentSlotMapper.cs
// Purpose: Contains equipment slot mapper code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Items;

// Type: EquipmentSlotMapper
// Purpose: Provides equipment slot mapper behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class EquipmentSlotMapper
{
    // Constant: Defines the no equipment slot constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed no equipment slot value used anywhere this rule or protocol value is needed.
    public const int NoEquipmentSlot = -1;

    // Method: FromInventoryType
    // Purpose: Executes the from inventory type operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - inventoryType: Inventory type value supplied by the caller for this operation.
    // Returns: Returns the int value produced by this operation.
    // Notes: This keeps the operation scoped to EquipmentSlotMapper so callers do not duplicate validation, protocol, or persistence rules.
    public static int FromInventoryType(byte inventoryType)
    {

        return inventoryType switch
        {
            1 => 0,
            2 => 1,
            3 => 2,
            4 => 3,
            5 => 4,
            6 => 5,
            7 => 6,
            8 => 7,
            9 => 8,
            10 => 9,
            11 => 10,
            12 => 12,
            13 => 15,
            14 => 16,
            15 => 17,
            16 => 14,
            17 => 15,
            19 => 18,
            20 => 4,
            21 => 15,
            22 => 16,
            23 => 16,
            25 => 17,
            26 => 17,
            28 => 17,
            _ => NoEquipmentSlot,
        };
    }
}
