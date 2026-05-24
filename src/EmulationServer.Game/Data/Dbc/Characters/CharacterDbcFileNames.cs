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
 * File overview: src/EmulationServer.Game/Data/Dbc/Characters/CharacterDbcFileNames.cs
 * Documents the CharacterDbcFileNames source file in the DBC loading and strongly typed client data records area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Game.Data.Dbc.Characters;

/**
  * Defines character-related DBC filenames used by WorldServer startup validation.
  */
public static class CharacterDbcFileNames
{
    /**
     * Defines the constant value for char base info.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    public const string CharBaseInfo = "CharBaseInfo.dbc";
    /**
     * Defines the constant value for char hair geosets.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    public const string CharHairGeosets = "CharHairGeosets.dbc";
    /**
     * Defines the constant value for char sections.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    public const string CharSections = "CharSections.dbc";
    /**
     * Defines the constant value for char start outfit.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    public const string CharStartOutfit = "CharStartOutfit.dbc";
    /**
     * Defines the constant value for character facial hair styles.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    public const string CharacterFacialHairStyles = "CharacterFacialHairStyles.dbc";
    /**
     * Defines the constant value for chr classes.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    public const string ChrClasses = "ChrClasses.dbc";
    /**
     * Defines the constant value for chr races.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    public const string ChrRaces = "ChrRaces.dbc";

    /**
      * Contains the character DBC files required before character create/list work can be reliable.
      */
    public static IReadOnlyList<string> CoreCharacterDbcFiles { get; } =
    [
        CharBaseInfo,
        CharHairGeosets,
        CharSections,
        CharStartOutfit,
        CharacterFacialHairStyles,
        ChrClasses,
        ChrRaces,
    ];
}
