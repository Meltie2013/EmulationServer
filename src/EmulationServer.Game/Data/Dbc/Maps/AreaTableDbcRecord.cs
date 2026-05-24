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
  * File overview: src/EmulationServer.Game/Data/Dbc/Maps/AreaTableDbcRecord.cs
  * Documents the AreaTableDbcRecord source file in the DBC loading and strongly typed client data records area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Data.Dbc.Maps;

/**
  * Represents one AreaTable.dbc row and keeps area metadata available to map services from startup.
  * Positional fields carried by this record: Id, MapId, ParentAreaTableId, AreaBit, Flags, SoundPreferencesId, SoundPreferencesIdUnderWater, SoundAmbienceId, ZoneMusicId, ZoneIntroMusicTableId, ExplorationLevel, Name, FactionGroupId, LiquidTypeId, MinElevation, AmbientLightingMultiplier, LightId.
  */
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
    /**
      * Indicates whether this area is a top-level zone rather than a child sub-zone.
      */
    public bool IsRootArea => ParentAreaTableId == 0;
}
