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
 * File overview: src/WorldServer/Database/Characters/CharacterDeleteRepositoryResult.cs
 * Documents the CharacterDeleteRepositoryResult source file in the world database repositories and persisted player/account records area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.WorldServer.Database.Characters;

/**
 * Lists the supported character delete repository result values used by the world database repositories and persisted player/account records layer.
 * Numeric values are part of the project contract and should only be changed when the related client packet, DBC value, or database schema is updated as well.
 */
public enum CharacterDeleteRepositoryResult
{
    /**
     * Represents the success value for character delete repository result handling.
     */
    Success,
    /**
     * Represents the not found value for character delete repository result handling.
     */
    NotFound,
    /**
     * Represents the account mismatch value for character delete repository result handling.
     */
    AccountMismatch,
    /**
     * Represents the online value for character delete repository result handling.
     */
    Online,
    /**
     * Represents the guild leader value for character delete repository result handling.
     */
    GuildLeader,
    /**
     * Represents the failed value for character delete repository result handling.
     */
    Failed,
}
