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
// File: src/EmulationServer.Game/Data/Dbc/Characters/CharacterDbcFileNames.cs
// Purpose: Contains character DBC file names code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Data.Dbc.Characters;

// Type: CharacterDbcFileNames
// Purpose: Provides character DBC file names behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class CharacterDbcFileNames
{

    // Constant: Defines the char base info constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed char base info value used anywhere this rule or protocol value is needed.
    public const string CharBaseInfo = "CharBaseInfo.dbc";

    // Constant: Defines the char hair geosets constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed char hair geosets value used anywhere this rule or protocol value is needed.
    public const string CharHairGeosets = "CharHairGeosets.dbc";

    // Constant: Defines the char sections constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed char sections value used anywhere this rule or protocol value is needed.
    public const string CharSections = "CharSections.dbc";

    // Constant: Defines the char start outfit constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed char start outfit value used anywhere this rule or protocol value is needed.
    public const string CharStartOutfit = "CharStartOutfit.dbc";

    // Constant: Defines the character facial hair styles constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed character facial hair styles value used anywhere this rule or protocol value is needed.
    public const string CharacterFacialHairStyles = "CharacterFacialHairStyles.dbc";

    // Constant: Defines the chr classes constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed chr classes value used anywhere this rule or protocol value is needed.
    public const string ChrClasses = "ChrClasses.dbc";

    // Constant: Defines the chr races constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed chr races value used anywhere this rule or protocol value is needed.
    public const string ChrRaces = "ChrRaces.dbc";

    // Property: Gets or sets the core character DBC files value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: core character DBC files value exposed by the owning type.
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
