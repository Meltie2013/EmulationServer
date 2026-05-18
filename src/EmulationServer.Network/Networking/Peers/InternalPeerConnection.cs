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

using System.Net.Sockets;

using EmulationServer.Network.Configuration;
using EmulationServer.Network.Networking.Protocol;

/**
  * File overview: src/EmulationServer.Network/Networking/Peers/InternalPeerConnection.cs
  * This file belongs to the project runtime logic and supporting data models portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Network.Networking.Peers;

/**
  * Represents the internal peer connection component in the project runtime logic and supporting data models area.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public sealed class InternalPeerConnection
{
    /**
      * Stores the stream dependency or runtime value for InternalPeerConnection.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly NetworkStream _stream;
    /**
      * Stores the send lock dependency or runtime value for InternalPeerConnection.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly SemaphoreSlim _sendLock;

    /**
      * Creates a new InternalPeerConnection instance and stores the dependencies required by the component.
      * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
      */
    internal InternalPeerConnection(
        string localServerName,
        InternalPeerSettings peer,
        NetworkStream stream,
        SemaphoreSlim sendLock)
    {
        if (string.IsNullOrWhiteSpace(localServerName))
        {
            throw new ArgumentException("Local server name is required.", nameof(localServerName));
        }

        LocalServerName = localServerName;
        Peer = peer ?? throw new ArgumentNullException(nameof(peer));
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _sendLock = sendLock ?? throw new ArgumentNullException(nameof(sendLock));
    }

    /**
      * Gets or stores the local server name value used by InternalPeerConnection.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string LocalServerName { get; }

    /**
      * Gets or stores the peer value used by InternalPeerConnection.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public InternalPeerSettings Peer { get; }

    /**
      * Gets or stores the remote server name value used by InternalPeerConnection.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public string RemoteServerName => Peer.Name;

    /**
      * Sends a protocol message or status update to a connected peer.
      * The method is part of InternalPeerConnection and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public Task SendPacketAsync(string packet, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(packet))
        {
            return Task.CompletedTask;
        }

        return InternalProtocol.WriteLineAsync(
            _stream,
            _sendLock,
            packet,
            cancellationToken);
    }
}
