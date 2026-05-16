
using System.Net;
using System.Net.Sockets;

using EmulationServer.Network.Configuration;
using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Sessions;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Network.Networking.Socket;

public sealed class InternalSocketListener
{
    private readonly TcpListener _tcpListener;
    private readonly InternalSessionManager _sessionManager = new();
    private readonly InternalNetworkSettings _settings;
    private readonly InternalNetworkCallbacks _callbacks;

    private int _started;
    private int _stopping;

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

            Logger.Write(LogType.NETWORK, $"{_settings.ServerName} internal listener started on {endPoint?.Address}:{endPoint?.Port}", nameof(InternalSocketListener));
            await AcceptLoopAsync(cancellationToken);
        }
        finally
        {
            await StopAsync(CancellationToken.None);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _stopping, 1) == 1)
        {
            return;
        }

        Logger.Write(LogType.WARNING, $"Stopping {_settings.ServerName} internal network listener...", nameof(InternalSocketListener));
        _tcpListener.Stop();

        Logger.Write(LogType.NETWORK, $"Disconnecting {_settings.ServerName} internal sessions...", nameof(InternalSocketListener));
        await _sessionManager.DisconnectAllAsync();

        Logger.Write(LogType.NETWORK, $"Waiting up to {_settings.ShutdownGracePeriod.TotalSeconds:0.##} second(s) for {_settings.ServerName} internal sessions to stop...",
            nameof(InternalSocketListener));
        await _sessionManager.WaitForAllSessionsAsync(_settings.ShutdownGracePeriod, cancellationToken);

        Logger.Write(LogType.NETWORK, $"{_settings.ServerName} internal network listener stopped.", nameof(InternalSocketListener));
    }

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

            if (_sessionManager.Count >= _settings.MaxConnections)
            {
                Logger.Write(LogType.WARNING, $"Rejected internal connection from {client.Client.RemoteEndPoint}, {_settings.ServerName} internal listener is at maximum capacity.", nameof(InternalSocketListener));

                client.Dispose();
                continue;
            }

            ConfigureClient(client);

            Logger.Write(LogType.NETWORK, $"{_settings.ServerName} accepted internal connection from {client.Client.RemoteEndPoint}", nameof(InternalSocketListener));

            InternalServerSession session = new(_settings, client, _callbacks);

            if (!_sessionManager.TryAddSession(session))
            {
                await session.DisconnectAsync();
                continue;
            }

            _ = ProcessSessionAsync(session, cancellationToken);
        }
    }

    private async Task ProcessSessionAsync(InternalServerSession session, CancellationToken cancellationToken)
    {
        try
        {
            await session.ProcessAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.CRITICAL, exception.ToString(), nameof(InternalSocketListener));
        }
        finally
        {
            _sessionManager.CompleteSession(session);
        }
    }

    private static void ConfigureClient(TcpClient client)
    {
        client.NoDelay = true;
        client.ReceiveBufferSize = 8192;
        client.SendBufferSize = 8192;
    }

    private bool IsStopping => Volatile.Read(ref _stopping) == 1;
}
