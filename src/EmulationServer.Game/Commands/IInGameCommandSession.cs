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

using EmulationServer.Game.Players;

/**
 * File overview: src/EmulationServer.Game/Commands/IInGameCommandSession.cs
 * Documents the IInGameCommandSession source file in the in-game command parsing and command session access area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Game.Commands;

/**
 * Defines the contract for in game command session behavior in the in-game command parsing and command session access layer.
 * Implementations are expected to keep caller-facing behavior stable because other servers depend on this shape across shared game and network workflows.
 */
public interface IInGameCommandSession
{
    /**
     * Exposes the account gm level value required by in game command session callers.
     * The property keeps implementations aligned on the data the shared workflow needs to read without tying callers to a concrete session or service type.
     */
    byte AccountGmLevel { get; }

    /**
     * Exposes the active player count value required by in game command session callers.
     * The property keeps implementations aligned on the data the shared workflow needs to read without tying callers to a concrete session or service type.
     */
    int ActivePlayerCount { get; }

    /**
     * Exposes the message of the day value required by in game command session callers.
     * The property keeps implementations aligned on the data the shared workflow needs to read without tying callers to a concrete session or service type.
     */
    string MessageOfTheDay { get; }

    /**
     * Requires the current player value and throws when the implementing session cannot provide it.
     * Callers use the contract method so gameplay, database, and network code can depend on behavior rather than a concrete implementation.
     */
    PlayerLoginRecord RequireCurrentPlayer();
}
