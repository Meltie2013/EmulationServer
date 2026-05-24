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
  * File overview: src/EmulationServer.Game/Data/Dbc/Items/ItemDbcFileNames.cs
  * Documents the ItemDbcFileNames source file in the DBC loading and strongly typed client data records area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Data.Dbc.Items;

/**
  * Defines item-related DBC filenames needed by character creation and inventory display validation.
  */
public static class ItemDbcFileNames
{
    /**
      * Defines the constant value for item bag family.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const string ItemBagFamily = "ItemBagFamily.dbc";
    /**
      * Defines the constant value for item class.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const string ItemClass = "ItemClass.dbc";
    /**
      * Defines the constant value for item display info.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const string ItemDisplayInfo = "ItemDisplayInfo.dbc";
    /**
      * Defines the constant value for item random properties.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const string ItemRandomProperties = "ItemRandomProperties.dbc";
    /**
      * Defines the constant value for item set.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const string ItemSet = "ItemSet.dbc";
    /**
      * Defines the constant value for item sub class.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const string ItemSubClass = "ItemSubClass.dbc";

    /**
      * Exposes the core item dbc files value to callers that need this runtime or configuration data.
      * The property keeps the public surface strongly typed and documents which part of the server workflow owns the value.
      */
    public static IReadOnlyList<string> CoreItemDbcFiles { get; } =
    [
        ItemBagFamily,
        ItemClass,
        ItemDisplayInfo,
        ItemRandomProperties,
        ItemSet,
        ItemSubClass,
    ];
}
