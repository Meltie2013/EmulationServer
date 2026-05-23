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

using EmulationServer.Network.Networking.Protocol;

/**
  * File overview: src/EmulationServer.Network/Configuration/InternalNetworkSettings.cs
  * This file belongs to the server configuration loading and strongly typed settings portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Network.Configuration;

/**
  * Represents the internal network settings component in the server configuration loading and strongly typed settings area.
  * It keeps configuration values grouped by responsibility and prevents unrelated server code from reading raw INI keys.
  */
public sealed class InternalNetworkSettings
{
    /**
      * Gets or stores the server name value used by InternalNetworkSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string ServerName { get; init; } = "Server";

    /**
      * Gets or stores the bind address value used by InternalNetworkSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string BindAddress { get; init; } = "127.0.0.1";

    /**
      * Gets or stores the port value used by InternalNetworkSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int Port { get; init; } = 0;

    /**
      * Gets or stores the registration key value used by InternalNetworkSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string RegistrationKey { get; init; } = string.Empty;

    /**
      * Gets or stores the backlog value used by InternalNetworkSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int Backlog { get; init; } = 128;

    /**
      * Gets or stores the receive buffer size used by InternalNetworkSettings.
      * Larger buffers reduce socket read pressure during packet bursts between internal servers.
      */
    public int ReceiveBufferSize { get; init; } = 65536;

    /**
      * Gets or stores the send buffer size used by InternalNetworkSettings.
      * Larger buffers reduce socket write pressure during packet bursts between internal servers.
      */
    public int SendBufferSize { get; init; } = 65536;

    /**
      * Gets or stores whether TCP keep-alive should be enabled for internal server sockets.
      */
    public bool KeepAlive { get; init; } = true;

    /**
      * Gets or stores how long a quiet internal TCP connection can sit before keep-alive probes start.
      */
    public int KeepAliveTimeSeconds { get; init; } = 30;

    /**
      * Gets or stores the interval between internal TCP keep-alive probes.
      */
    public int KeepAliveIntervalSeconds { get; init; } = 10;

    /**
      * Gets or stores how long an internal connection has to finish authentication.
      */
    public TimeSpan AuthenticationTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /**
      * Gets or stores the allowed inbound internal server names.
      * An empty list keeps compatibility and allows any server with a valid registration proof.
      */
    public IReadOnlyList<string> AllowedServers { get; init; } = [];

    /**
      * Gets or stores the shutdown grace period value used by InternalNetworkSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public TimeSpan ShutdownGracePeriod { get; init; } = TimeSpan.FromSeconds(15);

    /**
      * Gets or stores the latency report interval value used by InternalNetworkSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public TimeSpan LatencyReportInterval { get; init; } = TimeSpan.FromSeconds(15);

    /**
      * Gets or stores the ping timeout value used by InternalNetworkSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public TimeSpan PingTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /**
      * Gets or stores the peers value used by InternalNetworkSettings.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public IReadOnlyList<InternalPeerSettings> Peers { get; init; } = [];

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of InternalNetworkSettings and keeps this workflow isolated from the caller.
      */
    public IPAddress GetBindAddress()
    {
        if (!IPAddress.TryParse(BindAddress, out IPAddress? ipAddress))
        {
            throw new InvalidOperationException($"Invalid internal network bind address: '{BindAddress}'.");
        }

        return ipAddress;
    }

    /**
      * Validates input and throws a clear exception before invalid state reaches runtime code.
      * The method is part of InternalNetworkSettings and keeps this workflow isolated from the caller.
      */
    public void Validate()
    {
        if (!InternalProtocol.IsValidServerName(ServerName))
        {
            throw new InvalidOperationException($"Invalid internal network server name: '{ServerName}'.");
        }

        _ = GetBindAddress();

        if (Port is < IPEndPoint.MinPort or > IPEndPoint.MaxPort)
        {
            throw new InvalidOperationException($"Invalid internal network port: {Port}. Valid range is 0-65535.");
        }

        if (string.IsNullOrWhiteSpace(RegistrationKey))
        {
            throw new InvalidOperationException("Internal network registration key is required.");
        }

        if (RegistrationKey.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException("Internal network registration key cannot contain whitespace.");
        }

        if (RegistrationKey.Length > 256)
        {
            throw new InvalidOperationException("Internal network registration key cannot be longer than 256 characters.");
        }

        if (Backlog <= 0)
        {
            throw new InvalidOperationException("Internal network listener backlog must be greater than zero.");
        }

        if (ReceiveBufferSize <= 0)
        {
            throw new InvalidOperationException("Internal network receive buffer size must be greater than zero.");
        }

        if (SendBufferSize <= 0)
        {
            throw new InvalidOperationException("Internal network send buffer size must be greater than zero.");
        }

        if (KeepAliveTimeSeconds < 0)
        {
            throw new InvalidOperationException("Internal network keep-alive time cannot be negative.");
        }

        if (KeepAliveIntervalSeconds < 0)
        {
            throw new InvalidOperationException("Internal network keep-alive interval cannot be negative.");
        }

        if (AuthenticationTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Internal network authentication timeout must be greater than zero.");
        }

        foreach (string allowedServer in AllowedServers)
        {
            if (!InternalProtocol.IsValidServerName(allowedServer))
            {
                throw new InvalidOperationException($"Invalid allowed internal server name: '{allowedServer}'.");
            }
        }

        if (ShutdownGracePeriod < TimeSpan.Zero)
        {
            throw new InvalidOperationException("Internal network shutdown grace period cannot be negative.");
        }

        if (LatencyReportInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Internal network latency report interval must be greater than zero.");
        }

        if (PingTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Internal network ping timeout must be greater than zero.");
        }

        if (PingTimeout >= LatencyReportInterval)
        {
            throw new InvalidOperationException("Internal network ping timeout must be less than the latency report interval.");
        }

        HashSet<string> peerNames = new(StringComparer.OrdinalIgnoreCase);

        foreach (InternalPeerSettings peer in Peers)
        {
            peer.Validate();

            if (!peerNames.Add(peer.Name))
            {
                throw new InvalidOperationException($"Duplicate internal peer name: '{peer.Name}'.");
            }
        }
    }
}
