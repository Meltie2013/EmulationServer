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
  * This file centralizes the DBC files used by character validation and character creation.
  */

namespace EmulationServer.Game.Data.Dbc.Characters;

/**
  * Defines character-related DBC filenames used by WorldServer startup validation.
  */
public static class CharacterDbcFileNames
{
    public const string CharBaseInfo = "CharBaseInfo.dbc";
    public const string CharHairGeosets = "CharHairGeosets.dbc";
    public const string CharSections = "CharSections.dbc";
    public const string CharStartOutfit = "CharStartOutfit.dbc";
    public const string CharacterFacialHairStyles = "CharacterFacialHairStyles.dbc";
    public const string ChrClasses = "ChrClasses.dbc";
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
