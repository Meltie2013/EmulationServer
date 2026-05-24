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
  * File overview: src/EmulationServer.Game/Data/Dbc/Maps/MapDbcRecord.cs
  * Documents the MapDbcRecord source file in the DBC loading and strongly typed client data records area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Data.Dbc.Maps;

/**
  * Represents one Map.dbc row with the fields needed by WorldServer, MapServer, and InstanceServer.
  * Positional fields carried by this record: Id, InternalName, InstanceType, IsBattleground, Name, MinLevel, MaxLevel, MaxPlayers, ParentMapId, LoadingScreenId, RaidOffset, ContinentName, BattlefieldMapIconScale.
  */
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
    /**
      * Returns the map type as a named enum when the DBC value matches a known map kind.
      */
    public MapInstanceType Type => Enum.IsDefined(typeof(MapInstanceType), InstanceType)
        ? (MapInstanceType)InstanceType
        : MapInstanceType.Unknown;

    /**
      * Indicates whether this map is a persistent outdoor world service candidate.
      */
    public bool IsWorldMap => InstanceType == (int)MapInstanceType.World;

    /**
      * Indicates whether this map should normally be hosted by InstanceServer instead of the shared outdoor MapServer.
      */
    public bool IsInstanceMap => InstanceType != (int)MapInstanceType.World;

    /**
      * Returns the best available human-readable name for logging and status output.
      */
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? InternalName : Name;
}
