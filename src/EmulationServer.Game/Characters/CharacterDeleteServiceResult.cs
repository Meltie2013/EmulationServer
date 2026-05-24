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
 * File overview: src/EmulationServer.Game/Characters/CharacterDeleteServiceResult.cs
 * Documents the CharacterDeleteServiceResult source file in the character creation, listing, and identity transfer models area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Game.Characters;

/**
 * Lists the supported character delete service result values used by the character creation, listing, and identity transfer models layer.
 * Numeric values are part of the project contract and should only be changed when the related client packet, DBC value, or database schema is updated as well.
 */
public enum CharacterDeleteServiceResult
{
    /**
     * Represents the success value for character delete service result handling.
     */
    Success,
    /**
     * Represents the failed value for character delete service result handling.
     */
    Failed,
    /**
     * Represents the security mismatch value for character delete service result handling.
     */
    SecurityMismatch,
}
