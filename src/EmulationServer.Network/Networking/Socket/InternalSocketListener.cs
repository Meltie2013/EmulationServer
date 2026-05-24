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

using System.Net;
using System.Net.Sockets;

using EmulationServer.Network.Configuration;
using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Sessions;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

/**
  * File overview: src/EmulationServer.Network/Networking/Socket/InternalSocketListener.cs
  * Documents the InternalSocketListener source file in the internal server networking, packet framing, and peer/session lifecycle area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Network.Networking.Socket;

/**
  * Owns the internal socket listener behavior for the internal server networking, packet framing, and peer/session lifecycle layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class InternalSocketListener
{
    /**
      * Holds the private tcp listener state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly TcpListener _tcpListener;
    /**
      * Holds the private session manager state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly InternalSessionManager _sessionManager = new();
    /**
      * Holds the private settings state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly InternalNetworkSettings _settings;
    /**
      * Holds the private callbacks state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly InternalNetworkCallbacks _callbacks;

    /**
      * Holds the private started state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private int _started;
    /**
      * Holds the private stopping state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private int _stopping;

    /**
      * Initializes a new InternalSocketListener instance with the dependencies required by the internal server networking, packet framing, and peer/session lifecycle workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: settings, callbacks.
      */
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

    /**
      * Starts the start workflow and prepares the component to accept runtime work.
      * Startup is ordered so validation and dependency setup finish before services are announced as available.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Stops the stop workflow and releases owned runtime resources in a controlled order.
      * Shutdown logic is centralized to avoid dangling connections, incomplete saves, or partially registered services.
      * Inputs used by this operation: cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
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

    /**
      * Accepts an incoming connection or request and transfers it into managed server state.
      * The method is part of InternalSocketListener and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
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

    /**
      * Processes incoming data and dispatches it to the correct subsystem handler.
      * The method is part of InternalSocketListener and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
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

    /**
      * Applies configuration to shared runtime services before they are used by the server.
      * The method is part of InternalSocketListener and keeps this workflow isolated from the caller.
      */
    private static void ConfigureClient(TcpClient client, InternalNetworkSettings settings)
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
      * Gets or stores the is stopping value used by InternalSocketListener.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    private bool IsStopping => Volatile.Read(ref _stopping) == 1;
}
