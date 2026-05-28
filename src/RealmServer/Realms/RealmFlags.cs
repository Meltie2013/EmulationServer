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

namespace EmulationServer.RealmServer.Realms;

/**
  * Defines realm-list flags used by the authentication server protocol.
  */
[Flags]
public enum RealmFlags : byte
{
    /**
      * No special realm-list flags are applied.
      */
    None = 0x00,

    /**
      * Marks the realm as invalid and excludes it from normal realm-list loading in MaNGOS.
      */
    Invalid = 0x01,

    /**
      * Marks the realm as offline in the client realm list.
      */
    Offline = 0x02,

    /**
      * Requests the client to display the realm build/version when supported by that client generation.
      */
    SpecifyBuild = 0x04,

    /**
      * Marks the realm as suitable for new players.
      */
    NewPlayers = 0x20,

    /**
      * Marks the realm as recommended.
      */
    Recommended = 0x40,

    /**
      * Marks the realm as full.
      */
    Full = 0x80,
}
