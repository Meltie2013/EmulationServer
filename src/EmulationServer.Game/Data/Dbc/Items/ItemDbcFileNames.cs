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
// File: src/EmulationServer.Game/Data/Dbc/Items/ItemDbcFileNames.cs
// Purpose: Contains item DBC file names code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Data.Dbc.Items;

// Type: ItemDbcFileNames
// Purpose: Provides item DBC file names behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class ItemDbcFileNames
{

    // Constant: Defines the item bag family constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed item bag family value used anywhere this rule or protocol value is needed.
    public const string ItemBagFamily = "ItemBagFamily.dbc";

    // Constant: Defines the item class constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed item class value used anywhere this rule or protocol value is needed.
    public const string ItemClass = "ItemClass.dbc";

    // Constant: Defines the item display info constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed item display info value used anywhere this rule or protocol value is needed.
    public const string ItemDisplayInfo = "ItemDisplayInfo.dbc";

    // Constant: Defines the durability costs constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed durability costs value used anywhere this rule or protocol value is needed.
    public const string DurabilityCosts = "DurabilityCosts.dbc";

    // Constant: Defines the durability quality constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed durability quality value used anywhere this rule or protocol value is needed.
    public const string DurabilityQuality = "DurabilityQuality.dbc";

    // Constant: Defines the item random properties constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed item random properties value used anywhere this rule or protocol value is needed.
    public const string ItemRandomProperties = "ItemRandomProperties.dbc";

    // Constant: Defines the item set constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed item set value used anywhere this rule or protocol value is needed.
    public const string ItemSet = "ItemSet.dbc";

    // Constant: Defines the item sub class constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed item sub class value used anywhere this rule or protocol value is needed.
    public const string ItemSubClass = "ItemSubClass.dbc";

    // Constant: Defines the spell item enchantment constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed spell item enchantment value used anywhere this rule or protocol value is needed.
    public const string SpellItemEnchantment = "SpellItemEnchantment.dbc";

    // Property: Gets or sets the core item DBC files value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: core item DBC files value exposed by the owning type.
    public static IReadOnlyList<string> CoreItemDbcFiles { get; } =
    [
        DurabilityCosts,
        DurabilityQuality,
        ItemBagFamily,
        ItemClass,
        ItemDisplayInfo,
        ItemRandomProperties,
        ItemSet,
        ItemSubClass,
        SpellItemEnchantment,
    ];
}
