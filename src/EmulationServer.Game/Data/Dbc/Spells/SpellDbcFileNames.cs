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

namespace EmulationServer.Game.Data.Dbc.Spells;

/**
  * Defines spell-related DBC filenames needed by character startup skills and future spell validation.
  */
public static class SpellDbcFileNames
{
    public const string SkillLine = "SkillLine.dbc";
    public const string SkillLineAbility = "SkillLineAbility.dbc";
    public const string SkillRaceClassInfo = "SkillRaceClassInfo.dbc";
    public const string Spell = "Spell.dbc";
    public const string SpellCastTimes = "SpellCastTimes.dbc";
    public const string SpellDuration = "SpellDuration.dbc";
    public const string SpellIcon = "SpellIcon.dbc";
    public const string SpellRange = "SpellRange.dbc";

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
