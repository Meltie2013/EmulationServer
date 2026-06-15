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

namespace EmulationServer.Game.Data.Maps;

/**
  * Carries the result of resolving a world coordinate to AreaTable identifiers.
  * ZoneId is always the parent/root AreaTable id and AreaId is the exact sub-area when terrain data can provide it.
  */
public readonly record struct WorldAreaLookupResult(uint ZoneId, uint AreaId, string Source)
{
    public bool IsResolved => ZoneId != 0 && AreaId != 0;
}
