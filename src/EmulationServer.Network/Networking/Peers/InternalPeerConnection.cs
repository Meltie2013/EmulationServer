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
 * Documents the InternalPeerConnection source file in the internal server networking, packet framing, and peer/session lifecycle area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Network.Networking.Peers;

/**
 * Owns the internal peer connection behavior for the internal server networking, packet framing, and peer/session lifecycle layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public sealed class InternalPeerConnection
{
    /**
     * Holds the private stream state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly NetworkStream _stream;
    /**
     * Holds the private send lock state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly SemaphoreSlim _sendLock;

    /**
     * Initializes a new InternalPeerConnection instance with the dependencies required by the internal server networking, packet framing, and peer/session lifecycle workflow.
     * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
     * Inputs used by this operation: localServerName, peer, stream, sendLock.
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
