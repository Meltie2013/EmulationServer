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
// File: src/EmulationServer.Game/Players/PlayerLoginRecord.cs
// Purpose: Contains player login record code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Players;

// Type: PlayerLoginRecord
// Purpose: Represents player login record data passed through the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - Guid: GUID identifier used to select the exact record, object, or runtime owner.
// - AccountId: Account ID identifier used to select the exact record, object, or runtime owner.
// - Name: Name value supplied by the caller for this operation.
// - Race: Race value supplied by the caller for this operation.
// - Class: Class value supplied by the caller for this operation.
// - Gender: Gender value supplied by the caller for this operation.
// - Level: Level value supplied by the caller for this operation.
// - Experience: Experience value supplied by the caller for this operation.
// - Zone: Zone value supplied by the caller for this operation.
// - Map: Map value supplied by the caller for this operation.
// - PositionX: Position X value supplied by the caller for this operation.
// - PositionY: Position Y value supplied by the caller for this operation.
// - PositionZ: Position Z value supplied by the caller for this operation.
// - Orientation: Orientation value supplied by the caller for this operation.
// - Money: Money value supplied by the caller for this operation.
// - PlayerBytes: Player bytes value supplied by the caller for this operation.
// - PlayerBytes2: Player bytes2 value supplied by the caller for this operation.
// - PlayerFlags: Player flags value supplied by the caller for this operation.
// - AtLogin: At login value supplied by the caller for this operation.
// - Cinematic: Cinematic value supplied by the caller for this operation.
// - TotalTime: Total time value supplied by the caller for this operation.
// - LevelTime: Level time value supplied by the caller for this operation.
// - Stats: Stats value supplied by the caller for this operation.
// - NextLevelExperience: Next level experience value supplied by the caller for this operation.
// - Inventory: Inventory value supplied by the caller for this operation.
// - Spells: Spells value supplied by the caller for this operation.
// - ActionButtons: Action buttons value supplied by the caller for this operation.
// - uintTutorialFlags: Uint tutorial flags value supplied by the caller for this operation.
// - Reputations: Reputations value supplied by the caller for this operation.
// - Skills: Skills value supplied by the caller for this operation.
// - Faction: Faction value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
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

    // Method: ToClientGuid
    // Purpose: Executes the to client GUID operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - Guid: GUID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the ulong client GUID => character guid. value produced by this operation.
    // Notes: This keeps the operation scoped to PlayerLoginRecord so callers do not duplicate validation, protocol, or persistence rules.
    public ulong ClientGuid => CharacterGuid.ToClientGuid(Guid);
}
