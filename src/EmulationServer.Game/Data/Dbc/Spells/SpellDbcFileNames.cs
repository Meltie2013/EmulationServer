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
// File: src/EmulationServer.Game/Data/Dbc/Spells/SpellDbcFileNames.cs
// Purpose: Contains spell DBC file names code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Data.Dbc.Spells;

// Type: SpellDbcFileNames
// Purpose: Provides spell DBC file names behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class SpellDbcFileNames
{

    // Constant: Defines the skill line constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed skill line value used anywhere this rule or protocol value is needed.
    public const string SkillLine = "SkillLine.dbc";

    // Constant: Defines the skill line ability constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed skill line ability value used anywhere this rule or protocol value is needed.
    public const string SkillLineAbility = "SkillLineAbility.dbc";

    // Constant: Defines the skill race class info constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed skill race class info value used anywhere this rule or protocol value is needed.
    public const string SkillRaceClassInfo = "SkillRaceClassInfo.dbc";

    // Constant: Defines the spell constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed spell value used anywhere this rule or protocol value is needed.
    public const string Spell = "Spell.dbc";

    // Constant: Defines the spell cast times constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed spell cast times value used anywhere this rule or protocol value is needed.
    public const string SpellCastTimes = "SpellCastTimes.dbc";

    // Constant: Defines the spell duration constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed spell duration value used anywhere this rule or protocol value is needed.
    public const string SpellDuration = "SpellDuration.dbc";

    // Constant: Defines the spell icon constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed spell icon value used anywhere this rule or protocol value is needed.
    public const string SpellIcon = "SpellIcon.dbc";

    // Constant: Defines the spell range constant used by the game-domain data, player state, DBC, and world-template layer.
    // Value: fixed spell range value used anywhere this rule or protocol value is needed.
    public const string SpellRange = "SpellRange.dbc";

    // Property: Gets or sets the core spell DBC files value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: core spell DBC files value exposed by the owning type.
    public static IReadOnlyList<string> CoreSpellDbcFiles { get; } =
    [
        SkillLine,
        SkillLineAbility,
        SkillRaceClassInfo,
        Spell,
        SpellCastTimes,
        SpellDuration,
        SpellIcon,
        SpellRange,
    ];
}
