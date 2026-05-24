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
  * File overview: src/EmulationServer.Game/Data/Dbc/Spells/SpellDbcFileNames.cs
  * Documents the SpellDbcFileNames source file in the DBC loading and strongly typed client data records area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Data.Dbc.Spells;

/**
  * Defines spell-related DBC filenames needed by character startup skills and future spell validation.
  */
public static class SpellDbcFileNames
{
    /**
      * Defines the constant value for skill line.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const string SkillLine = "SkillLine.dbc";
    /**
      * Defines the constant value for skill line ability.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const string SkillLineAbility = "SkillLineAbility.dbc";
    /**
      * Defines the constant value for skill race class info.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const string SkillRaceClassInfo = "SkillRaceClassInfo.dbc";
    /**
      * Defines the constant value for spell.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const string Spell = "Spell.dbc";
    /**
      * Defines the constant value for spell cast times.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const string SpellCastTimes = "SpellCastTimes.dbc";
    /**
      * Defines the constant value for spell duration.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const string SpellDuration = "SpellDuration.dbc";
    /**
      * Defines the constant value for spell icon.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const string SpellIcon = "SpellIcon.dbc";
    /**
      * Defines the constant value for spell range.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    public const string SpellRange = "SpellRange.dbc";

    /**
      * Exposes the core spell dbc files value to callers that need this runtime or configuration data.
      * The property keeps the public surface strongly typed and documents which part of the server workflow owns the value.
      */
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
