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
  * File overview: src/EmulationServer.Network/Networking/Sessions/IRealmSessionProcessor.cs
  * This file belongs to the network session lifecycle and packet dispatch portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Network.Networking.Sessions;

/**
  * Defines the contract for irealm session processor so implementations can be swapped without changing callers.
  * It receives input from a session and drives the next step in the protocol state machine.
  */
public interface IRealmSessionProcessor
{
    Task ProcessAsync(RealmSessionContext context, CancellationToken cancellationToken);
}
