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

namespace EmulationServer.Network.Networking.Peers;

public sealed class InternalPeerConnection
{
    private readonly NetworkStream _stream;
    private readonly SemaphoreSlim _sendLock;

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

    public string LocalServerName { get; }

    public InternalPeerSettings Peer { get; }

    public string RemoteServerName => Peer.Name;

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
