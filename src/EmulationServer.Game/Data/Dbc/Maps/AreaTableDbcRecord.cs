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
// File: src/EmulationServer.Game/Data/Dbc/Maps/AreaTableDbcRecord.cs
// Purpose: Contains area table DBC record code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Data.Dbc.Maps;

// Type: AreaTableDbcRecord
// Purpose: Represents area table DBC record data passed through the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - Id: ID identifier used to select the exact record, object, or runtime owner.
// - MapId: Map ID identifier used to select the exact record, object, or runtime owner.
// - ParentAreaTableId: Parent area table ID identifier used to select the exact record, object, or runtime owner.
// - AreaBit: Area bit value supplied by the caller for this operation.
// - Flags: Flags value supplied by the caller for this operation.
// - SoundPreferencesId: Sound preferences ID identifier used to select the exact record, object, or runtime owner.
// - SoundPreferencesIdUnderWater: Sound preferences ID under water value supplied by the caller for this operation.
// - SoundAmbienceId: Sound ambience ID identifier used to select the exact record, object, or runtime owner.
// - ZoneMusicId: Zone music ID identifier used to select the exact record, object, or runtime owner.
// - ZoneIntroMusicTableId: Zone intro music table ID identifier used to select the exact record, object, or runtime owner.
// - ExplorationLevel: Exploration level value supplied by the caller for this operation.
// - Name: Name value supplied by the caller for this operation.
// - FactionGroupId: Faction group ID identifier used to select the exact record, object, or runtime owner.
// - LiquidTypeId: Liquid type ID identifier used to select the exact record, object, or runtime owner.
// - MinElevation: Min elevation value supplied by the caller for this operation.
// - AmbientLightingMultiplier: Ambient lighting multiplier value supplied by the caller for this operation.
// - LightId: Light ID identifier used to select the exact record, object, or runtime owner.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record AreaTableDbcRecord(
    int Id,
    int MapId,
    int ParentAreaTableId,
    int AreaBit,
    int Flags,
    int SoundPreferencesId,
    int SoundPreferencesIdUnderWater,
    int SoundAmbienceId,
    int ZoneMusicId,
    int ZoneIntroMusicTableId,
    int ExplorationLevel,
    string Name,
    int FactionGroupId,
    int LiquidTypeId,
    float MinElevation,
    float AmbientLightingMultiplier,
    int LightId)
{

    // Property: Gets or sets the is root area value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: is root area value exposed by the owning type.
    public bool IsRootArea => ParentAreaTableId == 0;
}
