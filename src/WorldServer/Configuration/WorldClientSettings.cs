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
  * File overview: src/WorldServer/Configuration/WorldClientSettings.cs
  * Documents the WorldClientSettings source file in the world server configuration and startup settings area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.WorldServer.Configuration;

/**
  * Owns the world client settings behavior for the world server configuration and startup settings layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class WorldClientSettings
{
    /**
      * Exposes the bind address value to callers that need this runtime or configuration data.
      * The property keeps the public surface strongly typed and documents which part of the server workflow owns the value.
      */
    public string BindAddress { get; init; } = "127.0.0.1";
    /**
      * Exposes the port value to callers that need this runtime or configuration data.
      * The property keeps the public surface strongly typed and documents which part of the server workflow owns the value.
      */
    public ushort Port { get; init; } = 8085;
    /**
      * Exposes the backlog value to callers that need this runtime or configuration data.
      * The property keeps the public surface strongly typed and documents which part of the server workflow owns the value.
      */
    public int Backlog { get; init; } = 128;
    /**
      * Exposes the receive buffer size value to callers that need this runtime or configuration data.
      * The property keeps the public surface strongly typed and documents which part of the server workflow owns the value.
      */
    public int ReceiveBufferSize { get; init; } = 65536;
    /**
      * Exposes the send buffer size value to callers that need this runtime or configuration data.
      * The property keeps the public surface strongly typed and documents which part of the server workflow owns the value.
      */
    public int SendBufferSize { get; init; } = 65536;
    /**
      * Exposes the keep alive value to callers that need this runtime or configuration data.
      * The property keeps the public surface strongly typed and documents which part of the server workflow owns the value.
      */
    public bool KeepAlive { get; init; } = true;
    /**
      * Exposes the keep alive time seconds value to callers that need this runtime or configuration data.
      * The property keeps the public surface strongly typed and documents which part of the server workflow owns the value.
      */
    public int KeepAliveTimeSeconds { get; init; } = 30;
    /**
      * Exposes the keep alive interval seconds value to callers that need this runtime or configuration data.
      * The property keeps the public surface strongly typed and documents which part of the server workflow owns the value.
      */
    public int KeepAliveIntervalSeconds { get; init; } = 10;
    /**
      * Exposes the shutdown grace period value to callers that need this runtime or configuration data.
      * The property keeps the public surface strongly typed and documents which part of the server workflow owns the value.
      */
    public TimeSpan ShutdownGracePeriod { get; init; } = TimeSpan.FromSeconds(15);
    /**
      * Exposes the maximum packet size value to callers that need this runtime or configuration data.
      * The property keeps the public surface strongly typed and documents which part of the server workflow owns the value.
      */
    public int MaximumPacketSize { get; init; } = 0x8000;

    /**
      * Validates validate state before it is used by another server component.
      * Validation failures are raised as close to the source as possible so configuration, packet, and data problems are easier to diagnose.
      */
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BindAddress))
        {
            throw new InvalidOperationException("World client bind address is required.");
        }

        _ = GetBindAddress();

        if (Port == 0)
        {
            throw new InvalidOperationException("World client port must be greater than zero.");
        }

        if (Backlog <= 0)
        {
            throw new InvalidOperationException("World client backlog must be greater than zero.");
        }

        if (ReceiveBufferSize <= 0)
        {
            throw new InvalidOperationException("World client receive buffer size must be greater than zero.");
        }

        if (SendBufferSize <= 0)
        {
            throw new InvalidOperationException("World client send buffer size must be greater than zero.");
        }

        if (KeepAliveTimeSeconds < 0)
        {
            throw new InvalidOperationException("World client keep-alive time cannot be negative.");
        }

        if (KeepAliveIntervalSeconds < 0)
        {
            throw new InvalidOperationException("World client keep-alive interval cannot be negative.");
        }

        if (ShutdownGracePeriod < TimeSpan.Zero)
        {
            throw new InvalidOperationException("World client shutdown grace period cannot be negative.");
        }

        if (MaximumPacketSize < 6)
        {
            throw new InvalidOperationException("World client maximum packet size must be at least 6 bytes.");
        }
    }

    /**
      * Resolves the bind address value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      */
    public IPAddress GetBindAddress()
    {
        if (!IPAddress.TryParse(BindAddress, out IPAddress? address))
        {
            throw new InvalidOperationException($"Invalid World client bind address: {BindAddress}");
        }

        return address;
    }
}
