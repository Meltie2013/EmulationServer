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
  * File overview: src/EmulationServer.Game/Chat/ChatLanguage.cs
  * Documents the ChatLanguage source file in the chat channel normalization, language handling, and message routing area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Chat;

/**
  * Lists the supported chat language values used by the chat channel normalization, language handling, and message routing layer.
  * Numeric values are part of the project contract and should only be changed when the related client packet, DBC value, or database schema is updated as well.
  */
public enum ChatLanguage : uint
{
    /**
      * Represents the universal value for chat language handling.
      */
    Universal = 0,
    /**
      * Represents the orcish value for chat language handling.
      */
    Orcish = 1,
    /**
      * Represents the darnassian value for chat language handling.
      */
    Darnassian = 2,
    /**
      * Represents the taurahe value for chat language handling.
      */
    Taurahe = 3,
    /**
      * Represents the dwarvish value for chat language handling.
      */
    Dwarvish = 6,
    /**
      * Represents the common value for chat language handling.
      */
    Common = 7,
    /**
      * Represents the demonic value for chat language handling.
      */
    Demonic = 8,
    /**
      * Represents the titan value for chat language handling.
      */
    Titan = 9,
    /**
      * Represents the thalassian value for chat language handling.
      */
    Thalassian = 10,
    /**
      * Represents the draconic value for chat language handling.
      */
    Draconic = 11,
    /**
      * Represents the kalimag value for chat language handling.
      */
    Kalimag = 12,
    /**
      * Represents the gnomish value for chat language handling.
      */
    Gnomish = 13,
    /**
      * Represents the troll value for chat language handling.
      */
    Troll = 14,
    /**
      * Represents the gutterspeak value for chat language handling.
      */
    Gutterspeak = 33,
}
