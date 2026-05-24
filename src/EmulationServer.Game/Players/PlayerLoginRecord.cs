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
 * File overview: src/EmulationServer.Game/Players/PlayerLoginRecord.cs
 * Documents the PlayerLoginRecord source file in the logged-in player state, persistence models, and gameplay records area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Game.Players;

/**
 * Carries immutable player login record data for the logged-in player state, persistence models, and gameplay records layer.
 * Records in this project are used as explicit transfer models so packet parsing, database repositories, and runtime systems can pass strongly typed values without mutating shared state.
 * Positional fields carried by this record: Guid, AccountId, Name, Race, Class, Gender, Level, Experience, Zone, Map, PositionX, PositionY, PositionZ, Orientation, Money, PlayerBytes, PlayerBytes2, PlayerFlags, AtLogin, Cinematic, TotalTime, LevelTime, Stats, NextLevelExperience, Inventory, Spells, ActionButtons, TutorialFlags, Reputations, Skills, Faction.
 */
public sealed record PlayerLoginRecord(
    uint Guid,
    uint AccountId,
    string Name,
    byte Race,
    byte Class,
    byte Gender,
    byte Level,
    uint Experience,
    uint Zone,
    uint Map,
    float PositionX,
    float PositionY,
    float PositionZ,
    float Orientation,
    uint Money,
    uint PlayerBytes,
    uint PlayerBytes2,
    uint PlayerFlags,
    uint AtLogin,
    byte Cinematic,
    uint TotalTime,
    uint LevelTime,
    PlayerStats Stats,
    uint NextLevelExperience,
    IReadOnlyList<PlayerInventoryItem> Inventory,
    IReadOnlyList<PlayerSpell> Spells,
    IReadOnlyList<PlayerActionButton> ActionButtons,
    uint[] TutorialFlags,
    IReadOnlyList<PlayerReputation> Reputations,
    IReadOnlyList<PlayerSkill> Skills,
    PlayerFaction Faction)
{
    /**
     * Stores the default client guid value used when the caller does not supply an override.
     * Centralizing the default keeps configuration and packet behavior consistent across the server process.
     */
    public ulong ClientGuid => CharacterGuid.ToClientGuid(Guid);
}
