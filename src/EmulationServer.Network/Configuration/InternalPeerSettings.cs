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

using EmulationServer.Network.Networking.Protocol;

/**
  * File overview: src/EmulationServer.Network/Configuration/InternalPeerSettings.cs
  * This file belongs to the server configuration loading and strongly typed settings portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Network.Configuration;

/**
  * Represents the internal peer settings component in the server configuration loading and strongly typed settings area.
  * It keeps configuration values grouped by responsibility and prevents unrelated server code from reading raw INI keys.
  */
public sealed class InternalPeerSettings
{
    /**
      * Gets or stores the name value used by InternalPeerSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string Name { get; init; } = string.Empty;

    /**
      * Gets or stores the host value used by InternalPeerSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string Host { get; init; } = "127.0.0.1";

    /**
      * Gets or stores the port value used by InternalPeerSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int Port { get; init; }

    /**
      * Gets or stores the enabled value used by InternalPeerSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public bool Enabled { get; init; } = true;

    /**
      * Gets or stores the reconnect delay value used by InternalPeerSettings.
      * This delay controls how long the connector waits between reconnect attempts while the peer is unavailable.
      */
    public TimeSpan ReconnectDelay { get; init; } = TimeSpan.FromSeconds(5);

    /**
      * Gets or stores the reconnect timeout value used by InternalPeerSettings.
      * Once a peer has been seen at least once, the connector will only attempt to reconnect for this window before returning to passive wait mode.
      */
    public TimeSpan ReconnectTimeout { get; init; } = TimeSpan.FromSeconds(120);

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of InternalPeerSettings and keeps this workflow isolated from the caller.
      */
    public void Validate()
    {
        if (!InternalProtocol.IsValidServerName(Name))
        {
            throw new InvalidOperationException($"Invalid internal peer name: '{Name}'.");
        }

        if (string.IsNullOrWhiteSpace(Host))
        {
            throw new InvalidOperationException($"Internal peer '{Name}' host is required.");
        }

        if (Port is < 1 or > 65535)
        {
            throw new InvalidOperationException($"Invalid internal peer '{Name}' port: {Port}. Valid range is 1-65535.");
        }

        if (ReconnectDelay <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"Internal peer '{Name}' reconnect delay must be greater than zero.");
        }

        if (ReconnectTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"Internal peer '{Name}' reconnect timeout must be greater than zero.");
        }

        if (ReconnectTimeout < ReconnectDelay)
        {
            throw new InvalidOperationException($"Internal peer '{Name}' reconnect timeout must be greater than or equal to the reconnect delay.");
        }
    }
}
