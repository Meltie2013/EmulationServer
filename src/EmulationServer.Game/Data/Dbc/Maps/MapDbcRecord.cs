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
// File: src/EmulationServer.Game/Data/Dbc/Maps/MapDbcRecord.cs
// Purpose: Contains map DBC record code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Data.Dbc.Maps;

// Type: MapDbcRecord
// Purpose: Represents map DBC record data passed through the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - Id: ID identifier used to select the exact record, object, or runtime owner.
// - InternalName: Internal name value supplied by the caller for this operation.
// - InstanceType: Instance type value supplied by the caller for this operation.
// - IsBattleground: Is battleground value supplied by the caller for this operation.
// - Name: Name value supplied by the caller for this operation.
// - MinLevel: Min level value supplied by the caller for this operation.
// - MaxLevel: Max level value supplied by the caller for this operation.
// - MaxPlayers: Max players value supplied by the caller for this operation.
// - ParentMapId: Parent map ID identifier used to select the exact record, object, or runtime owner.
// - LoadingScreenId: Loading screen ID identifier used to select the exact record, object, or runtime owner.
// - RaidOffset: Raid offset value supplied by the caller for this operation.
// - ContinentName: Continent name value supplied by the caller for this operation.
// - BattlefieldMapIconScale: Battlefield map icon scale value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record MapDbcRecord(
    int Id,
    string InternalName,
    int InstanceType,
    bool IsBattleground,
    string Name,
    int MinLevel,
    int MaxLevel,
    int MaxPlayers,
    int ParentMapId,
    int LoadingScreenId,
    int RaidOffset,
    string ContinentName,
    float BattlefieldMapIconScale)
{

    // Method: IsDefined
    // Purpose: Validates or evaluates is defined rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - MapInstanceType: Map instance type value supplied by the caller for this operation.
    // - InstanceType: Instance type value supplied by the caller for this operation.
    // Returns: Returns the map instance type type => enum. value produced by this operation.
    // Notes: This keeps the operation scoped to MapDbcRecord so callers do not duplicate validation, protocol, or persistence rules.
    public MapInstanceType Type => Enum.IsDefined(typeof(MapInstanceType), InstanceType)
        ? (MapInstanceType)InstanceType
        : MapInstanceType.Unknown;

    // Method: InstanceType
    // Purpose: Executes the instance type operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the bool is world map => value produced by this operation.
    // Notes: This keeps the operation scoped to MapDbcRecord so callers do not duplicate validation, protocol, or persistence rules.
    public bool IsWorldMap => InstanceType == (int)MapInstanceType.World;

    // Method: InstanceType
    // Purpose: Executes the instance type operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the bool is instance map => value produced by this operation.
    // Notes: This keeps the operation scoped to MapDbcRecord so callers do not duplicate validation, protocol, or persistence rules.
    public bool IsInstanceMap => InstanceType != (int)MapInstanceType.World;

    // Method: IsNullOrWhiteSpace
    // Purpose: Validates or evaluates is null or white space rules for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - Name: Name value supplied by the caller for this operation.
    // Returns: Returns the string display name => string. value produced by this operation.
    // Notes: This keeps the operation scoped to MapDbcRecord so callers do not duplicate validation, protocol, or persistence rules.
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? InternalName : Name;
}
