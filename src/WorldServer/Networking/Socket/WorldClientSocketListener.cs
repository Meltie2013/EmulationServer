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
// File: src/WorldServer/Networking/Socket/WorldClientSocketListener.cs
// Purpose: Contains world client socket listener code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

using EmulationServer.Network.Networking.Socket;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.WorldServer.Configuration;
using EmulationServer.WorldServer.Networking.Sessions;

namespace EmulationServer.WorldServer.Networking.Socket;

// Type: WorldClientSocketListener
// Purpose: Provides world client socket listener behavior for the world server gameplay, session, and character runtime layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class WorldClientSocketListener : IAsyncDisposable
{

    // Field: Stores the settings state used by the world server gameplay, session, and character runtime layer.
    // Value: current settings backing value maintained by the owning type.
    private readonly WorldClientSettings _settings;
    // Field: Stores the TCP client state used by the world server gameplay, session, and character runtime layer.
    // Value: current TCP client backing value maintained by the owning type.
    private readonly Func<TcpClient, WorldClientSession> _sessionFactory;
    // Field: Stores the connection gate state used by the world server gameplay, session, and character runtime layer.
    // Value: current connection gate backing value maintained by the owning type.
    private readonly Func<bool>? _connectionGate;
    private readonly ConcurrentDictionary<Guid, WorldClientSession> _sessions = new();
    private readonly ConcurrentDictionary<Guid, Task> _sessionTasks = new();

    private readonly CancellationTokenSource _shutdown = new();

    // Field: Stores the listener state used by the world server gameplay, session, and character runtime layer.
    // Value: current listener backing value maintained by the owning type.
    private TcpListener? _listener;

    // Field: Stores the accept task state used by the world server gameplay, session, and character runtime layer.
    // Value: current accept task backing value maintained by the owning type.
    private Task? _acceptTask;

    // Field: Stores the next dependency warning utc state used by the world server gameplay, session, and character runtime layer.
    // Value: current next dependency warning utc backing value maintained by the owning type.
    private DateTimeOffset _nextDependencyWarningUtc;

    // Field: Stores the disposed state used by the world server gameplay, session, and character runtime layer.
    // Value: current disposed backing value maintained by the owning type.
    private bool _disposed;

    // Constructor: WorldClientSocketListener
    // Purpose: Initializes a new WorldClientSocketListener instance with dependencies and values required by the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - settings: Settings values that control how this operation should run.
    // - sessionFactory: Session factory value supplied by the caller for this operation.
    // - connectionGate: Connection gate value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldClientSocketListener so callers do not duplicate validation, protocol, or persistence rules.
    public WorldClientSocketListener(
        WorldClientSettings settings,
        Func<TcpClient, WorldClientSession> sessionFactory,
        Func<bool>? connectionGate = null)
    {
        _settings = settings ?? throw new ArgumentNullException();
        _settings.Validate();
        _sessionFactory = sessionFactory ?? throw new ArgumentNullException();
        _connectionGate = connectionGate;
    }

    // Method: StartAsync
    // Purpose: Controls the start lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSocketListener so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_listener is not null)
        {
            throw new InvalidOperationException("World client socket listener is already running.");
        }

        IPAddress bindAddress = _settings.GetBindAddress();
        _listener = new TcpListener(bindAddress, _settings.Port);
        _listener.Start(_settings.Backlog);

        Logger.Write(LogType.NETWORK, $"WorldServer listening for WoW clients on {bindAddress}:{_settings.Port}.", "WorldClientSocketListener");
        _acceptTask = AcceptLoopAsync(cancellationToken);
        return _acceptTask;
    }

    // Method: StopAsync
    // Purpose: Controls the stop lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSocketListener so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_shutdown.IsCancellationRequested)
        {
            await _shutdown.CancelAsync();
        }

        try
        {
            _listener?.Stop();
        }
        catch
        {

        }

        foreach (WorldClientSession session in _sessions.Values)
        {
            await session.DisconnectAsync();
        }

        Task[] tasks = [.. _sessionTasks.Values];
        if (tasks.Length > 0)
        {
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_settings.ShutdownGracePeriod);

            try
            {
                await Task.WhenAll(tasks).WaitAsync(timeout.Token);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                Logger.Write(LogType.WARNING, "World client sessions did not all close before the shutdown grace period expired.", "WorldClientSocketListener");
            }
        }

        if (_acceptTask is not null)
        {
            try
            {
                await _acceptTask.WaitAsync(CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException)
            {
            }
        }
    }

    // Method: AcceptLoopAsync
    // Purpose: Handles accept loop work for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - serverCancellationToken: Server cancellation token value supplied by the caller for this operation.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSocketListener so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task AcceptLoopAsync(CancellationToken serverCancellationToken)
    {
        using CancellationTokenSource linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            serverCancellationToken,
            _shutdown.Token);
        CancellationToken cancellationToken = linkedCancellation.Token;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client = await _listener!.AcceptTcpClientAsync(cancellationToken);
                ConfigureClient(client, _settings);

                if (_connectionGate is not null && !_connectionGate())
                {
                    LogDependencyGateRejection(client);
                    client.Dispose();
                    continue;
                }

                WorldClientSession session = _sessionFactory(client);
                _sessions[session.Id] = session;

                Task task = RunSessionAsync(session, cancellationToken);
                _sessionTasks[session.Id] = task;

                Logger.Write(LogType.NETWORK, $"Accepted WoW client from {session.RemoteEndPoint}.", "WorldClientSocketListener");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (SocketException exception) when (cancellationToken.IsCancellationRequested)
        {
            Logger.Write(LogType.TRACE, $"World client listener stopped: {exception.Message}", "WorldClientSocketListener");
        }
    }

    // Method: RunSessionAsync
    // Purpose: Controls the run session lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - session: Session value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSocketListener so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task RunSessionAsync(WorldClientSession session, CancellationToken cancellationToken)
    {
        try
        {
            await session.ProcessAsync(cancellationToken);
        }
        finally
        {
            _sessions.TryRemove(session.Id, out _);
            _sessionTasks.TryRemove(session.Id, out _);
            await session.DisposeAsync();
        }
    }

    // Method: ConfigureClient
    // Purpose: Executes the configure client operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - client: Client value supplied by the caller for this operation.
    // - settings: Settings values that control how this operation should run.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldClientSocketListener so callers do not duplicate validation, protocol, or persistence rules.
    private static void ConfigureClient(TcpClient client, WorldClientSettings settings)
    {
        TcpSocketOptions.ConfigureClient(
            client,
            settings.ReceiveBufferSize,
            settings.SendBufferSize,
            settings.KeepAlive,
            settings.KeepAliveTimeSeconds,
            settings.KeepAliveIntervalSeconds);
    }

    // Method: LogDependencyGateRejection
    // Purpose: Executes the log dependency gate rejection operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - client: Client value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldClientSocketListener so callers do not duplicate validation, protocol, or persistence rules.
    private void LogDependencyGateRejection(TcpClient client)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (now < _nextDependencyWarningUtc)
        {
            return;
        }

        _nextDependencyWarningUtc = now.AddSeconds(10);
        Logger.Write(
            LogType.WARNING,
            $"Rejected WoW client from {client.Client.RemoteEndPoint} because WorldServer public dependencies are not online.",
            "WorldClientSocketListener");
    }

    // Method: DisposeAsync
    // Purpose: Controls the dispose lifecycle step for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to WorldClientSocketListener so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await StopAsync(CancellationToken.None);
        _shutdown.Dispose();
    }
}
