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

using System.Net;

/**
  * File overview: src/EmulationServer.Network/Configuration/RealmSocketListenerSettings.cs
  * This file belongs to the server configuration loading and strongly typed settings portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Network.Configuration;

/**
  * Represents the realm socket listener settings component in the server configuration loading and strongly typed settings area.
  * It keeps configuration values grouped by responsibility and prevents unrelated server code from reading raw INI keys.
  */
public sealed class RealmSocketListenerSettings
{
    /**
      * Gets or stores the bind address value used by RealmSocketListenerSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string BindAddress { get; init; } = "0.0.0.0";

    /**
      * Gets or stores the port value used by RealmSocketListenerSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int Port { get; init; } = 3724;

    /**
      * Gets or stores the backlog value used by RealmSocketListenerSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int Backlog { get; init; } = 128;


    /**
      * Gets or stores the shutdown grace period value used by RealmSocketListenerSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public TimeSpan ShutdownGracePeriod { get; init; } = TimeSpan.FromSeconds(15);

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of RealmSocketListenerSettings and keeps this workflow isolated from the caller.
      */
    public IPAddress GetBindAddress()
    {
        if (!IPAddress.TryParse(BindAddress, out IPAddress? ipAddress))
        {
            throw new InvalidOperationException($"Invalid realm bind address: '{BindAddress}'.");
        }

        return ipAddress;
    }

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of RealmSocketListenerSettings and keeps this workflow isolated from the caller.
      */
    public void Validate()
    {
        _ = GetBindAddress();

        if (Port is < IPEndPoint.MinPort or > IPEndPoint.MaxPort)
        {
            throw new InvalidOperationException($"Invalid realm port: {Port}. Valid range is 0-65535.");
        }

        if (Backlog <= 0)
        {
            throw new InvalidOperationException("Realm listener backlog must be greater than zero.");
        }


        if (ShutdownGracePeriod < TimeSpan.Zero)
        {
            throw new InvalidOperationException("Realm shutdown grace period cannot be negative.");
        }
    }
}
