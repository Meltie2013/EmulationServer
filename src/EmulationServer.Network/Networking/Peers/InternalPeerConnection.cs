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
// File: src/EmulationServer.Network/Networking/Peers/InternalPeerConnection.cs
// Purpose: Contains internal peer connection code for the packet serialization, socket transport, and protocol framing layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Net.Sockets;

using EmulationServer.Network.Configuration;
using EmulationServer.Network.Networking.Protocol;

namespace EmulationServer.Network.Networking.Peers;

// Type: InternalPeerConnection
// Purpose: Provides internal peer connection behavior for the packet serialization, socket transport, and protocol framing layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class InternalPeerConnection
{

    // Field: Stores the stream state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current stream backing value maintained by the owning type.
    private readonly NetworkStream _stream;

    // Field: Stores the send lock state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current send lock backing value maintained by the owning type.
    private readonly SemaphoreSlim _sendLock;

    // Constructor: InternalPeerConnection
    // Purpose: Initializes a new InternalPeerConnection instance with dependencies and values required by the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - localServerName: Local server name value supplied by the caller for this operation.
    // - peer: Peer value supplied by the caller for this operation.
    // - stream: Stream value supplied by the caller for this operation.
    // - sendLock: Send lock value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to InternalPeerConnection so callers do not duplicate validation, protocol, or persistence rules.
    internal InternalPeerConnection(
        string localServerName,
        InternalPeerSettings peer,
        NetworkStream stream,
        SemaphoreSlim sendLock)
    {
        if (string.IsNullOrWhiteSpace(localServerName))
        {
            throw new ArgumentException("Local server name is required.");
        }

        LocalServerName = localServerName;
        Peer = peer ?? throw new ArgumentNullException();
        _stream = stream ?? throw new ArgumentNullException();
        _sendLock = sendLock ?? throw new ArgumentNullException();
    }

    // Property: Gets or sets the local server name value used by the packet serialization, socket transport, and protocol framing layer.
    // Value: local server name value exposed by the owning type.
    public string LocalServerName { get; }

    // Property: Gets or sets the peer value used by the packet serialization, socket transport, and protocol framing layer.
    // Value: peer value exposed by the owning type.
    public InternalPeerSettings Peer { get; }

    // Property: Gets or sets the remote server name value used by the packet serialization, socket transport, and protocol framing layer.
    // Value: remote server name value exposed by the owning type.
    public string RemoteServerName => Peer.Name;

    // Method: SendPacketAsync
    // Purpose: Handles send packet work for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalPeerConnection so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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
