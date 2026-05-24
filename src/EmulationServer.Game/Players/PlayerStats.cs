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
 * File overview: src/EmulationServer.Game/Players/PlayerStats.cs
 * Documents the PlayerStats source file in the logged-in player state, persistence models, and gameplay records area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Game.Players;

/**
 * Carries immutable player stats data for the logged-in player state, persistence models, and gameplay records layer.
 * Records in this project are used as explicit transfer models so packet parsing, database repositories, and runtime systems can pass strongly typed values without mutating shared state.
 * Positional fields carried by this record: Health, Power1, Power2, Power3, Power4, Power5, Strength, Agility, Stamina, Intellect, Spirit, Armor.
 */
public sealed record PlayerStats(
    uint Health,
    uint Power1,
    uint Power2,
    uint Power3,
    uint Power4,
    uint Power5,
    uint Strength,
    uint Agility,
    uint Stamina,
    uint Intellect,
    uint Spirit,
    uint Armor)
{
    /**
     * Exposes the empty value to callers that need this runtime or configuration data.
     * The property keeps the public surface strongly typed and documents which part of the server workflow owns the value.
     */
    public static PlayerStats Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}
