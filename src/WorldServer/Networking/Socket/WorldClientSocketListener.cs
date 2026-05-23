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

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.WorldServer.Configuration;
using EmulationServer.WorldServer.Networking.Sessions;

namespace EmulationServer.WorldServer.Networking.Socket;

public sealed class WorldClientSocketListener : IAsyncDisposable
{
    private readonly WorldClientSettings _settings;
    private readonly Func<TcpClient, WorldClientSession> _sessionFactory;
    private readonly ConcurrentDictionary<Guid, WorldClientSession> _sessions = new();
    private readonly ConcurrentDictionary<Guid, Task> _sessionTasks = new();
    private readonly CancellationTokenSource _shutdown = new();

    private TcpListener? _listener;
    private Task? _acceptTask;
    private bool _disposed;

    public WorldClientSocketListener(WorldClientSettings settings, Func<TcpClient, WorldClientSession> sessionFactory)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _settings.Validate();
        _sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_listener is not null)
        {
            throw new InvalidOperationException("World client socket listener is already running.");
        }

        IPAddress bindAddress = _settings.GetBindAddress();
        _listener = new TcpListener(bindAddress, _settings.Port);
        _listener.Start(_settings.Backlog);

        Logger.Write(LogType.SUCCESS, $"WorldServer listening for WoW clients on {bindAddress}:{_settings.Port}.", nameof(WorldClientSocketListener));
        _acceptTask = AcceptLoopAsync(cancellationToken);
        return _acceptTask;
    }

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
            // Ignore shutdown races.
        }

        foreach (WorldClientSession session in _sessions.Values)
        {
            await session.DisconnectAsync();
        }

        Task[] tasks = _sessionTasks.Values.ToArray();
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
                Logger.Write(LogType.WARNING, "World client sessions did not all close before the shutdown grace period expired.", nameof(WorldClientSocketListener));
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
                ConfigureClient(client);

                WorldClientSession session = _sessionFactory(client);
                _sessions[session.Id] = session;

                Task task = RunSessionAsync(session, cancellationToken);
                _sessionTasks[session.Id] = task;

                Logger.Write(LogType.NETWORK, $"Accepted WoW client from {session.RemoteEndPoint}.", nameof(WorldClientSocketListener));
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
            Logger.Write(LogType.TRACE, $"World client listener stopped: {exception.Message}", nameof(WorldClientSocketListener));
        }
    }

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

    private static void ConfigureClient(TcpClient client)
    {
        client.NoDelay = true;
        client.ReceiveBufferSize = 8192;
        client.SendBufferSize = 8192;
    }

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
