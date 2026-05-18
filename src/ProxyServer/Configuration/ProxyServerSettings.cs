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

using EmulationServer.Network.Configuration;

using EmulationServer.Shared.Logging.Configuration;
/**
  * File overview: src/ProxyServer/Configuration/ProxyServerSettings.cs
  * This file belongs to the server configuration loading and strongly typed settings portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.ProxyServer.Configuration;

/**
  * Represents the proxy server settings component in the server configuration loading and strongly typed settings area.
  * It keeps configuration values grouped by responsibility and prevents unrelated server code from reading raw INI keys.
  */
public sealed class ProxyServerSettings
{
    /**
      * Gets or stores the logging value used by ProxyServerSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public LoggingSettings Logging { get; init; } = new();

    /**
      * Gets or stores the internal network value used by ProxyServerSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public InternalNetworkSettings InternalNetwork { get; init; } = new();

    /**
      * Gets or stores the dependency policy value used by ProxyServerSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public ProxyDependencySettings DependencyPolicy { get; init; } = new();

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of ProxyServerSettings and keeps this workflow isolated from the caller.
      */
    public void Validate()
    {
        Logging.Validate();
        InternalNetwork.Validate();
        DependencyPolicy.Validate();
    }
}
