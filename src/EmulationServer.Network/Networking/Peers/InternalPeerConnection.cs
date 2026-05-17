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
