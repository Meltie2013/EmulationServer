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
// File: src/EmulationServer.Network/Networking/Socket/InternalSocketListener.cs
// Purpose: Contains internal socket listener code for the packet serialization, socket transport, and protocol framing layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Net;
using System.Net.Sockets;

using EmulationServer.Network.Configuration;
using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Sessions;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Network.Networking.Socket;

// Type: InternalSocketListener
// Purpose: Provides internal socket listener behavior for the packet serialization, socket transport, and protocol framing layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class InternalSocketListener
{

    // Field: Stores the tcp listener state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current tcp listener backing value maintained by the owning type.
    private readonly TcpListener _tcpListener;

    private readonly InternalSessionManager _sessionManager = new();

    // Field: Stores the settings state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current settings backing value maintained by the owning type.
    private readonly InternalNetworkSettings _settings;

    // Field: Stores the callbacks state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current callbacks backing value maintained by the owning type.
    private readonly InternalNetworkCallbacks _callbacks;

    // Field: Stores the started state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current started backing value maintained by the owning type.
    private int _started;

    // Field: Stores the stopping state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current stopping backing value maintained by the owning type.
    private int _stopping;

    // Constructor: InternalSocketListener
    // Purpose: Initializes a new InternalSocketListener instance with dependencies and values required by the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - settings: Settings values that control how this operation should run.
    // - callbacks: Callbacks value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to InternalSocketListener so callers do not duplicate validation, protocol, or persistence rules.
    public InternalSocketListener(
        InternalNetworkSettings settings,
        InternalNetworkCallbacks? callbacks = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _settings = settings;
        _callbacks = callbacks ?? InternalNetworkCallbacks.Empty;
        _tcpListener = new TcpListener(settings.GetBindAddress(), settings.Port);
    }

    // Method: StartAsync
    // Purpose: Controls the start lifecycle step for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalSocketListener so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _started, 1) == 1)
        {
            throw new InvalidOperationException($"{_settings.ServerName} internal network listener has already been started.");
        }

        try
        {
            _tcpListener.Start(_settings.Backlog);

            IPEndPoint? endPoint = _tcpListener.LocalEndpoint as IPEndPoint;

            Logger.Write(LogType.NETWORK, $"{_settings.ServerName} internal listener started on {endPoint?.Address}:{endPoint?.Port}", "InternalSocketListener");
            await AcceptLoopAsync(cancellationToken);
        }
        finally
        {
            await StopAsync(CancellationToken.None);
        }
    }

    // Method: StopAsync
    // Purpose: Controls the stop lifecycle step for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalSocketListener so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _stopping, 1) == 1)
        {
            return;
        }

        Logger.Write(LogType.WARNING, $"Stopping {_settings.ServerName} internal network listener...", "InternalSocketListener");
        _tcpListener.Stop();

        Logger.Write(LogType.NETWORK, $"Disconnecting {_settings.ServerName} internal sessions...", "InternalSocketListener");
        await _sessionManager.DisconnectAllAsync();

        Logger.Write(LogType.NETWORK, $"Waiting up to {_settings.ShutdownGracePeriod.TotalSeconds:0.##} second(s) for {_settings.ServerName} internal sessions to stop...",
            "InternalSocketListener");
        await _sessionManager.WaitForAllSessionsAsync(_settings.ShutdownGracePeriod, cancellationToken);

        Logger.Write(LogType.NETWORK, $"{_settings.ServerName} internal network listener stopped.", "InternalSocketListener");
    }

    // Method: AcceptLoopAsync
    // Purpose: Handles accept loop work for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalSocketListener so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && !IsStopping)
        {
            TcpClient client;

            try
            {
                client = await _tcpListener.AcceptTcpClientAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException) when (IsStopping)
            {
                break;
            }
            catch (SocketException) when (IsStopping)
            {
                break;
            }

            if (IsStopping)
            {
                client.Dispose();
                break;
            }

            ConfigureClient(client, _settings);

            Logger.Write(LogType.NETWORK, $"{_settings.ServerName} accepted internal connection from {client.Client.RemoteEndPoint}", "InternalSocketListener");

            InternalServerSession session = new(_settings, client, _callbacks);

            if (!_sessionManager.TryAddSession(session))
            {
                await session.DisconnectAsync();
                continue;
            }

            _ = ProcessSessionAsync(session, cancellationToken);
        }
    }

    // Method: ProcessSessionAsync
    // Purpose: Executes the process session operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - session: Session value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalSocketListener so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task ProcessSessionAsync(InternalServerSession session, CancellationToken cancellationToken)
    {
        try
        {
            await session.ProcessAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.CRITICAL, exception.ToString(), "InternalSocketListener");
        }
        finally
        {
            _sessionManager.CompleteSession(session);
        }
    }

    // Method: ConfigureClient
    // Purpose: Executes the configure client operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - client: Client value supplied by the caller for this operation.
    // - settings: Settings values that control how this operation should run.
    // Returns: none.
    // Notes: This keeps the operation scoped to InternalSocketListener so callers do not duplicate validation, protocol, or persistence rules.
    private static void ConfigureClient(TcpClient client, InternalNetworkSettings settings)
    {
        TcpSocketOptions.ConfigureClient(client, settings);
    }

    // Method: Read
    // Purpose: Retrieves read data for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - _stopping: Stopping value supplied by the caller for this operation.
    // Returns: Returns the bool is stopping => volatile. value produced by this operation.
    // Notes: This keeps the operation scoped to InternalSocketListener so callers do not duplicate validation, protocol, or persistence rules.
    private bool IsStopping => Volatile.Read(ref _stopping) == 1;
}
