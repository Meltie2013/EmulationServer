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
  * File overview: src/EmulationServer.Game/WorldData/ItemTemplateStatRecord.cs
  * Documents one stat pair from item_template for world template loading and item query packets.
  */

namespace EmulationServer.Game.WorldData;

/**
  * Carries one item_template stat_type/stat_value pair.
  * Vanilla item query responses always send all ten slots so empty slots stay represented with zero values.
  */
public sealed record ItemTemplateStatRecord(byte Type, int Value);
