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

/**
  * File overview: src/WorldServer/Networking/Socket/WorldClientSocketListener.cs
  * Documents the WorldClientSocketListener source file in the world client socket listening and connection acceptance area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.WorldServer.Networking.Socket;

/**
  * Owns the world client socket listener behavior for the world client socket listening and connection acceptance layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class WorldClientSocketListener : IAsyncDisposable
{
    /**
      * Holds the private settings state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly WorldClientSettings _settings;
    private readonly Func<TcpClient, WorldClientSession> _sessionFactory;
    private readonly ConcurrentDictionary<Guid, WorldClientSession> _sessions = new();
    private readonly ConcurrentDictionary<Guid, Task> _sessionTasks = new();
    /**
      * Holds the private shutdown state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly CancellationTokenSource _shutdown = new();

    /**
      * Holds the private listener state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private TcpListener? _listener;
    /**
      * Holds the private accept task state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private Task? _acceptTask;
    /**
      * Holds the private disposed state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private bool _disposed;

    /**
      * Initializes a new WorldClientSocketListener instance with the dependencies required by the world client socket listening and connection acceptance workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: settings, sessionFactory.
      */
    public WorldClientSocketListener(WorldClientSettings settings, Func<TcpClient, WorldClientSession> sessionFactory)
    {
        _settings = settings ?? throw new ArgumentNullException();
        _settings.Validate();
        _sessionFactory = sessionFactory ?? throw new ArgumentNullException();
    }

    /**
      * Starts the start workflow and prepares the component to accept runtime work.
      * Startup is ordered so validation and dependency setup finish before services are announced as available.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_listener is not null)
        {
            throw new InvalidOperationException("World client socket listener is already running.");
        }

        IPAddress bindAddress = _settings.GetBindAddress();
        _listener = new TcpListener(bindAddress, _settings.Port);
        _listener.Start(_settings.Backlog);

        Logger.Write(LogType.SUCCESS, $"WorldServer listening for WoW clients on {bindAddress}:{_settings.Port}.", "WorldClientSocketListener");
        _acceptTask = AcceptLoopAsync(cancellationToken);
        return _acceptTask;
    }

    /**
      * Stops the stop workflow and releases owned runtime resources in a controlled order.
      * Shutdown logic is centralized to avoid dangling connections, incomplete saves, or partially registered services.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Performs the accept loop operation for the world client socket listening and connection acceptance workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: serverCancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Performs the run session operation for the world client socket listening and connection acceptance workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: session, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Performs the configure client operation for the world client socket listening and connection acceptance workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: client, settings.
      */
    private static void ConfigureClient(TcpClient client, WorldClientSettings settings)
    {
        client.NoDelay = true;
        client.ReceiveBufferSize = settings.ReceiveBufferSize;
        client.SendBufferSize = settings.SendBufferSize;

        if (!settings.KeepAlive)
        {
            return;
        }

        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        TrySetTcpKeepAliveOption(client, SocketOptionName.TcpKeepAliveTime, settings.KeepAliveTimeSeconds);
        TrySetTcpKeepAliveOption(client, SocketOptionName.TcpKeepAliveInterval, settings.KeepAliveIntervalSeconds);
    }

    /**
      * Tries to resolve the set tcp keep alive option value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: client, optionName, valueSeconds.
      */
    private static void TrySetTcpKeepAliveOption(TcpClient client, SocketOptionName optionName, int valueSeconds)
    {
        if (valueSeconds <= 0)
        {
            return;
        }

        try
        {
            client.Client.SetSocketOption(SocketOptionLevel.Tcp, optionName, valueSeconds);
        }
        catch (SocketException)
        {
            // Some platforms do not expose per-socket TCP keep-alive tuning. KeepAlive itself is still enabled.
        }
        catch (ObjectDisposedException)
        {
            // The socket is already closed.
        }
    }

    /**
      * Stops the dispose workflow and releases owned runtime resources in a controlled order.
      * Shutdown logic is centralized to avoid dangling connections, incomplete saves, or partially registered services.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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
