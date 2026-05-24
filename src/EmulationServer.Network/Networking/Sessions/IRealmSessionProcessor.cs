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
 * Documents the IRealmSessionProcessor source file in the internal server networking, packet framing, and peer/session lifecycle area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Network.Networking.Sessions;

/**
 * Defines the contract for realm session processor behavior in the internal server networking, packet framing, and peer/session lifecycle layer.
 * Implementations are expected to keep caller-facing behavior stable because other servers depend on this shape across shared game and network workflows.
 */
public interface IRealmSessionProcessor
{
    /**
     * Performs the process async operation through the implementing contract.
     * Callers use the contract method so gameplay, database, and network code can depend on behavior rather than a concrete implementation.
     */
    Task ProcessAsync(RealmSessionContext context, CancellationToken cancellationToken);
}
