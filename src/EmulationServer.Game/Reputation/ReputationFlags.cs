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

namespace EmulationServer.Game.Reputation;

/**
  * Defines the client reputation state flags used by Faction.dbc and character_reputation.
  */
[Flags]
public enum ReputationFlags : uint
{
    None = 0x00,
    Visible = 0x01,
    AtWar = 0x02,
    Hidden = 0x04,
    InvisibleForced = 0x08,
    PeaceForced = 0x10,
    Inactive = 0x20,
    Rival = 0x40,
    Special = 0x80,
}
