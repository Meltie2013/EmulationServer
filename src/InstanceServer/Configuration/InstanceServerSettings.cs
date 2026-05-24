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

using EmulationServer.Game.Maps.Runtime;
using EmulationServer.Network.Configuration;

using EmulationServer.Shared.Logging.Configuration;

/**
 * File overview: src/InstanceServer/Configuration/InstanceServerSettings.cs
 * Documents the InstanceServerSettings source file in the instance service startup and internal server coordination area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.InstanceServer.Configuration;

/**
 * Owns the instance server settings behavior for the instance service startup and internal server coordination layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public sealed class InstanceServerSettings
{
    /**
      * Gets or stores the logging value used by InstanceServerSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public LoggingSettings Logging { get; init; } = new();

    /**
      * Gets or stores the internal network value used by InstanceServerSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public InternalNetworkSettings InternalNetwork { get; init; } = new();

    /**
      * Gets or stores the instance services value used by InstanceServerSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public MapRuntimeSettings InstanceServices { get; init; } = new();

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of InstanceServerSettings and keeps this workflow isolated from the caller.
      */
    public void Validate()
    {
        Logging.Validate();
        InternalNetwork.Validate();
        InstanceServices.Validate();
    }
}
