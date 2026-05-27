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
  * File overview: src/RealmServer/Configuration/RealmListSettings.cs
  * Documents the RealmListSettings source file in the realm authentication, realm-list handling, and external client login services area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.RealmServer.Configuration;

/**
  * Owns the realm list visibility settings used by RealmServer when building client realm-list packets.
  * These settings separate configured realm loading from public realm-list visibility so realms can remain configured at startup without being advertised before WorldServer has registered.
  */
public sealed class RealmListSettings
{
    /**
      * Gets whether configured realms must receive at least one WorldServer status packet before they are advertised to clients.
      */
    public bool RequireWorldServerStatus { get; init; } = true;

    /**
      * Gets whether realms with stale WorldServer status should be hidden from client realm lists.
      */
    public bool HideStaleRealms { get; init; } = true;

    /**
      * Gets how long a realm may go without a fresh WorldServer status update before it is considered stale.
      */
    public TimeSpan StaleRealmTimeout { get; init; } = TimeSpan.FromMinutes(5);

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      */
    public void Validate()
    {
        if (HideStaleRealms && StaleRealmTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Realm list stale realm timeout must be greater than zero when stale realm hiding is enabled.");
        }
    }
}
