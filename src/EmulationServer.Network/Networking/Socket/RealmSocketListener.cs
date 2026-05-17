
using System.Net;
using System.Net.Sockets;
using System.Threading;

using EmulationServer.Network.Configuration;
using EmulationServer.Network.Networking.Sessions;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Network.Networking.Socket;

public sealed class RealmSocketListener
{
    private readonly TcpListener _tcpListener;
    private readonly SessionManager _sessionManager = new();
    private readonly RealmSocketListenerSettings _settings;
    private readonly Func<IRealmSessionProcessor>? _sessionProcessorFactory;

    private int _started;
    private int _stopping;

    public RealmSocketListener(RealmSocketListenerSettings settings, Func<IRealmSessionProcessor>? sessionProcessorFactory = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _settings = settings;
        _sessionProcessorFactory = sessionProcessorFactory;
        _tcpListener = new TcpListener(settings.GetBindAddress(), settings.Port);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _started, 1) == 1)
        {
            throw new InvalidOperationException("RealmSocketListener has already been started.");
        }

        try
        {
            _tcpListener.Start(_settings.Backlog);

            IPEndPoint? endPoint = _tcpListener.LocalEndpoint as IPEndPoint;

            Logger.Write(LogType.NETWORK, $"RealmServer network listener started on {endPoint?.Address}:{endPoint?.Port}", nameof(RealmSocketListener));
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

        Logger.Write(LogType.WARNING, "Stopping RealmServer network listener...", nameof(RealmSocketListener));
        _tcpListener.Stop();

        Logger.Write(LogType.NETWORK, "Disconnecting all sessions...", nameof(RealmSocketListener));
        await _sessionManager.DisconnectAllAsync();

        Logger.Write(LogType.NETWORK, $"Waiting up to {_settings.ShutdownGracePeriod.TotalSeconds:0.##} second(s) for sessions to stop...",
            nameof(RealmSocketListener));
        await _sessionManager.WaitForAllSessionsAsync(_settings.ShutdownGracePeriod, cancellationToken);

        Logger.Write(LogType.NETWORK, "RealmServer network listener stopped.", nameof(RealmSocketListener));
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


            ConfigureClient(client);

            Logger.Write(LogType.NETWORK, $"Accepted connection from {client.Client.RemoteEndPoint}", nameof(RealmSocketListener));

            RealmSession session = new(client, _sessionProcessorFactory?.Invoke());

            if (!_sessionManager.TryAddSession(session))
            {
                await session.DisconnectAsync();
                continue;
            }

            _ = ProcessSessionAsync(session, cancellationToken);
        }
    }

    private async Task ProcessSessionAsync(RealmSession session, CancellationToken cancellationToken)
    {
        try
        {
            await session.ProcessAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.CRITICAL, exception.ToString(), nameof(RealmSocketListener));
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
