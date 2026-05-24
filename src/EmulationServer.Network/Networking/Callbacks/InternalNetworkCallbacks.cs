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

using EmulationServer.Network.Networking.Peers;
using EmulationServer.Network.Networking.Sessions;


/**
 * File overview: src/EmulationServer.Network/Networking/Callbacks/InternalNetworkCallbacks.cs
 * Documents the InternalNetworkCallbacks source file in the internal server networking, packet framing, and peer/session lifecycle area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Network.Networking.Callbacks;

/**
 * Owns the internal network callbacks behavior for the internal server networking, packet framing, and peer/session lifecycle layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public sealed class InternalNetworkCallbacks
{
    /**
      * Gets or stores the empty value used by InternalNetworkCallbacks.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public static InternalNetworkCallbacks Empty { get; } = new();

    public Func<InternalServerSession, string, CancellationToken, Task>? ServerAuthenticatedAsync { get; init; }

    public Func<InternalServerSession, string, string, CancellationToken, Task>? PacketReceivedAsync { get; init; }

    public Func<InternalServerSession, string, CancellationToken, Task>? ServerDisconnectedAsync { get; init; }

    public Func<InternalPeerConnection, string, CancellationToken, Task>? PeerAuthenticatedAsync { get; init; }

    public Func<InternalPeerConnection, string, string, CancellationToken, Task>? PeerPacketReceivedAsync { get; init; }

    public Func<InternalPeerConnection, string, CancellationToken, Task>? PeerDisconnectedAsync { get; init; }

    public Func<string, TimeSpan, CancellationToken, Task>? PeerReconnectTimedOutAsync { get; init; }

    public Func<string, string, CancellationToken, Task>? ShutdownRequestedAsync { get; init; }

    /**
     * Performs the notify server authenticated operation for the internal server networking, packet framing, and peer/session lifecycle workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: session, remoteServerName, cancellationToken.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
     */
    public Task NotifyServerAuthenticatedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        return ServerAuthenticatedAsync?.Invoke(session, remoteServerName, cancellationToken) ?? Task.CompletedTask;
    }

    /**
     * Performs the notify packet received operation for the internal server networking, packet framing, and peer/session lifecycle workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: session, remoteServerName, packet, cancellationToken.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
     */
    public Task NotifyPacketReceivedAsync(
        InternalServerSession session,
        string remoteServerName,
        string packet,
        CancellationToken cancellationToken)
    {
        return PacketReceivedAsync?.Invoke(session, remoteServerName, packet, cancellationToken) ?? Task.CompletedTask;
    }

    /**
     * Performs the notify server disconnected operation for the internal server networking, packet framing, and peer/session lifecycle workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: session, remoteServerName, cancellationToken.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
     */
    public Task NotifyServerDisconnectedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        return ServerDisconnectedAsync?.Invoke(session, remoteServerName, cancellationToken) ?? Task.CompletedTask;
    }

    /**
     * Performs the notify peer authenticated operation for the internal server networking, packet framing, and peer/session lifecycle workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: connection, remoteServerName, cancellationToken.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
     */
    public Task NotifyPeerAuthenticatedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        return PeerAuthenticatedAsync?.Invoke(connection, remoteServerName, cancellationToken) ?? Task.CompletedTask;
    }

    /**
     * Performs the notify peer packet received operation for the internal server networking, packet framing, and peer/session lifecycle workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: connection, remoteServerName, packet, cancellationToken.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
     */
    public Task NotifyPeerPacketReceivedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        string packet,
        CancellationToken cancellationToken)
    {
        return PeerPacketReceivedAsync?.Invoke(connection, remoteServerName, packet, cancellationToken) ?? Task.CompletedTask;
    }

    /**
     * Performs the notify peer disconnected operation for the internal server networking, packet framing, and peer/session lifecycle workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: connection, remoteServerName, cancellationToken.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
     */
    public Task NotifyPeerDisconnectedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        return PeerDisconnectedAsync?.Invoke(connection, remoteServerName, cancellationToken) ?? Task.CompletedTask;
    }

    /**
     * Performs the notify peer reconnect timed out operation for the internal server networking, packet framing, and peer/session lifecycle workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: remoteServerName, reconnectTimeout, cancellationToken.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
     */
    public Task NotifyPeerReconnectTimedOutAsync(
        string remoteServerName,
        TimeSpan reconnectTimeout,
        CancellationToken cancellationToken)
    {
        return PeerReconnectTimedOutAsync?.Invoke(remoteServerName, reconnectTimeout, cancellationToken) ?? Task.CompletedTask;
    }

    /**
     * Performs the notify shutdown requested operation for the internal server networking, packet framing, and peer/session lifecycle workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: sourceServerName, reason, cancellationToken.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
     */
    public Task NotifyShutdownRequestedAsync(
        string sourceServerName,
        string reason,
        CancellationToken cancellationToken)
    {
        return ShutdownRequestedAsync?.Invoke(sourceServerName, reason, cancellationToken) ?? Task.CompletedTask;
    }
}
