using EmulationServer.Network.Networking.Peers;
using EmulationServer.Network.Networking.Sessions;

namespace EmulationServer.Network.Networking.Callbacks;

public sealed class InternalNetworkCallbacks
{
    public static InternalNetworkCallbacks Empty { get; } = new();

    public Func<InternalServerSession, string, CancellationToken, Task>? ServerAuthenticatedAsync { get; init; }

    public Func<InternalServerSession, string, string, CancellationToken, Task>? PacketReceivedAsync { get; init; }

    public Func<InternalServerSession, string, CancellationToken, Task>? ServerDisconnectedAsync { get; init; }

    public Func<InternalPeerConnection, string, CancellationToken, Task>? PeerAuthenticatedAsync { get; init; }

    public Func<InternalPeerConnection, string, string, CancellationToken, Task>? PeerPacketReceivedAsync { get; init; }

    public Func<InternalPeerConnection, string, CancellationToken, Task>? PeerDisconnectedAsync { get; init; }

    public Func<string, string, CancellationToken, Task>? ShutdownRequestedAsync { get; init; }

    public Task NotifyServerAuthenticatedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        return ServerAuthenticatedAsync?.Invoke(session, remoteServerName, cancellationToken) ?? Task.CompletedTask;
    }

    public Task NotifyPacketReceivedAsync(
        InternalServerSession session,
        string remoteServerName,
        string packet,
        CancellationToken cancellationToken)
    {
        return PacketReceivedAsync?.Invoke(session, remoteServerName, packet, cancellationToken) ?? Task.CompletedTask;
    }

    public Task NotifyServerDisconnectedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        return ServerDisconnectedAsync?.Invoke(session, remoteServerName, cancellationToken) ?? Task.CompletedTask;
    }

    public Task NotifyPeerAuthenticatedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        return PeerAuthenticatedAsync?.Invoke(connection, remoteServerName, cancellationToken) ?? Task.CompletedTask;
    }

    public Task NotifyPeerPacketReceivedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        string packet,
        CancellationToken cancellationToken)
    {
        return PeerPacketReceivedAsync?.Invoke(connection, remoteServerName, packet, cancellationToken) ?? Task.CompletedTask;
    }

    public Task NotifyPeerDisconnectedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        return PeerDisconnectedAsync?.Invoke(connection, remoteServerName, cancellationToken) ?? Task.CompletedTask;
    }

    public Task NotifyShutdownRequestedAsync(
        string sourceServerName,
        string reason,
        CancellationToken cancellationToken)
    {
        return ShutdownRequestedAsync?.Invoke(sourceServerName, reason, cancellationToken) ?? Task.CompletedTask;
    }
}
