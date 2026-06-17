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
// File: src/EmulationServer.Network/Networking/Callbacks/InternalNetworkCallbacks.cs
// Purpose: Contains internal network callbacks code for the packet serialization, socket transport, and protocol framing layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Network.Networking.Peers;
using EmulationServer.Network.Networking.Sessions;

namespace EmulationServer.Network.Networking.Callbacks;

// Type: InternalNetworkCallbacks
// Purpose: Provides internal network callbacks behavior for the packet serialization, socket transport, and protocol framing layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class InternalNetworkCallbacks
{

    public static InternalNetworkCallbacks Empty { get; } = new();

    // Property: Gets or sets the internal server session value used by the packet serialization, socket transport, and protocol framing layer.
    // Value: internal server session value exposed by the owning type.
    public Func<InternalServerSession, string, CancellationToken, Task>? ServerAuthenticatedAsync { get; init; }

    // Property: Gets or sets the internal server session value used by the packet serialization, socket transport, and protocol framing layer.
    // Value: internal server session value exposed by the owning type.
    public Func<InternalServerSession, string, string, CancellationToken, Task>? PacketReceivedAsync { get; init; }

    // Property: Gets or sets the internal server session value used by the packet serialization, socket transport, and protocol framing layer.
    // Value: internal server session value exposed by the owning type.
    public Func<InternalServerSession, string, CancellationToken, Task>? ServerDisconnectedAsync { get; init; }

    // Property: Gets or sets the internal peer connection value used by the packet serialization, socket transport, and protocol framing layer.
    // Value: internal peer connection value exposed by the owning type.
    public Func<InternalPeerConnection, string, CancellationToken, Task>? PeerAuthenticatedAsync { get; init; }

    // Property: Gets or sets the internal peer connection value used by the packet serialization, socket transport, and protocol framing layer.
    // Value: internal peer connection value exposed by the owning type.
    public Func<InternalPeerConnection, string, string, CancellationToken, Task>? PeerPacketReceivedAsync { get; init; }

    // Property: Gets or sets the internal peer connection value used by the packet serialization, socket transport, and protocol framing layer.
    // Value: internal peer connection value exposed by the owning type.
    public Func<InternalPeerConnection, string, CancellationToken, Task>? PeerDisconnectedAsync { get; init; }

    // Property: Gets or sets the string value used by the packet serialization, socket transport, and protocol framing layer.
    // Value: string value exposed by the owning type.
    public Func<string, TimeSpan, CancellationToken, Task>? PeerReconnectTimedOutAsync { get; init; }

    // Property: Gets or sets the string value used by the packet serialization, socket transport, and protocol framing layer.
    // Value: string value exposed by the owning type.
    public Action<string, TimeSpan>? LatencyMeasured { get; init; }

    // Property: Gets or sets the string value used by the packet serialization, socket transport, and protocol framing layer.
    // Value: string value exposed by the owning type.
    public Action<string, TimeSpan>? PingTimedOut { get; init; }

    // Property: Gets or sets the string value used by the packet serialization, socket transport, and protocol framing layer.
    // Value: string value exposed by the owning type.
    public Func<string, string, CancellationToken, Task>? ShutdownRequestedAsync { get; init; }

    // Method: NotifyServerAuthenticatedAsync
    // Purpose: Executes the notify server authenticated operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - session: Session value supplied by the caller for this operation.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalNetworkCallbacks so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public Task NotifyServerAuthenticatedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        return ServerAuthenticatedAsync?.Invoke(session, remoteServerName, cancellationToken) ?? Task.CompletedTask;
    }

    // Method: NotifyPacketReceivedAsync
    // Purpose: Executes the notify packet received operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - session: Session value supplied by the caller for this operation.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalNetworkCallbacks so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public Task NotifyPacketReceivedAsync(
        InternalServerSession session,
        string remoteServerName,
        string packet,
        CancellationToken cancellationToken)
    {
        return PacketReceivedAsync?.Invoke(session, remoteServerName, packet, cancellationToken) ?? Task.CompletedTask;
    }

    // Method: NotifyServerDisconnectedAsync
    // Purpose: Executes the notify server disconnected operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - session: Session value supplied by the caller for this operation.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalNetworkCallbacks so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public Task NotifyServerDisconnectedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        return ServerDisconnectedAsync?.Invoke(session, remoteServerName, cancellationToken) ?? Task.CompletedTask;
    }

    // Method: NotifyPeerAuthenticatedAsync
    // Purpose: Executes the notify peer authenticated operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalNetworkCallbacks so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public Task NotifyPeerAuthenticatedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        return PeerAuthenticatedAsync?.Invoke(connection, remoteServerName, cancellationToken) ?? Task.CompletedTask;
    }

    // Method: NotifyPeerPacketReceivedAsync
    // Purpose: Executes the notify peer packet received operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - packet: Packet bytes or structured payload consumed by this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalNetworkCallbacks so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public Task NotifyPeerPacketReceivedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        string packet,
        CancellationToken cancellationToken)
    {
        return PeerPacketReceivedAsync?.Invoke(connection, remoteServerName, packet, cancellationToken) ?? Task.CompletedTask;
    }

    // Method: NotifyPeerDisconnectedAsync
    // Purpose: Executes the notify peer disconnected operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - connection: Database connection used to execute this operation without opening unnecessary additional state.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalNetworkCallbacks so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public Task NotifyPeerDisconnectedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        return PeerDisconnectedAsync?.Invoke(connection, remoteServerName, cancellationToken) ?? Task.CompletedTask;
    }

    // Method: NotifyPeerReconnectTimedOutAsync
    // Purpose: Executes the notify peer reconnect timed out operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - reconnectTimeout: Reconnect timeout value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalNetworkCallbacks so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public Task NotifyPeerReconnectTimedOutAsync(
        string remoteServerName,
        TimeSpan reconnectTimeout,
        CancellationToken cancellationToken)
    {
        return PeerReconnectTimedOutAsync?.Invoke(remoteServerName, reconnectTimeout, cancellationToken) ?? Task.CompletedTask;
    }

    // Method: NotifyLatencyMeasured
    // Purpose: Executes the notify latency measured operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - latency: Latency value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to InternalNetworkCallbacks so callers do not duplicate validation, protocol, or persistence rules.
    public void NotifyLatencyMeasured(string remoteServerName, TimeSpan latency)
    {
        LatencyMeasured?.Invoke(remoteServerName, latency);
    }

    // Method: NotifyPingTimedOut
    // Purpose: Executes the notify ping timed out operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - elapsed: Elapsed value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to InternalNetworkCallbacks so callers do not duplicate validation, protocol, or persistence rules.
    public void NotifyPingTimedOut(string remoteServerName, TimeSpan elapsed)
    {
        PingTimedOut?.Invoke(remoteServerName, elapsed);
    }

    // Method: NotifyShutdownRequestedAsync
    // Purpose: Executes the notify shutdown requested operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - sourceServerName: Source server name value supplied by the caller for this operation.
    // - reason: Reason value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalNetworkCallbacks so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public Task NotifyShutdownRequestedAsync(
        string sourceServerName,
        string reason,
        CancellationToken cancellationToken)
    {
        return ShutdownRequestedAsync?.Invoke(sourceServerName, reason, cancellationToken) ?? Task.CompletedTask;
    }
}
