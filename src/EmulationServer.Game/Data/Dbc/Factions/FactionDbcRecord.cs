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

namespace EmulationServer.Game.Data.Dbc.Factions;

/**
  * Represents one Faction.dbc row used for faction and reputation defaults.
  */
public sealed record FactionDbcRecord(
    int Id,
    int ReputationIndex,
    IReadOnlyList<int> ReputationRaceMasks,
    IReadOnlyList<int> ReputationClassMasks,
    IReadOnlyList<int> ReputationBases,
    IReadOnlyList<int> ReputationFlags,
    int ParentFactionId,
    string Name,
    string Description);
