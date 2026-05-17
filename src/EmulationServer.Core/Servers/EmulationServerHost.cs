
using EmulationServer.Database.Configuration;
using EmulationServer.Database.Interfaces;
using EmulationServer.Database.Services;
using EmulationServer.Network.Configuration;
using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Peers;
using EmulationServer.Network.Networking.Socket;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Core.Servers;

public sealed class EmulationServerHost : IAsyncDisposable
{
    private readonly string _serverName;
    private readonly DatabaseSettings? _databaseSettings;
    private readonly InternalNetworkSettings _internalNetworkSettings;
    private readonly IDatabaseService? _databaseService;
    private readonly InternalSocketListener _internalSocketListener;
    private readonly InternalPeerConnector _internalPeerConnector;
    private readonly CancellationTokenSource _shutdownCancellation = new();
    private readonly TaskCompletionSource<bool> _startupCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private int _shutdownRequested;

    public EmulationServerHost(
        string serverName,
        InternalNetworkSettings internalNetworkSettings,
        InternalNetworkCallbacks? callbacks = null)
        : this(serverName, null, internalNetworkSettings, callbacks)
    {
    }

    public EmulationServerHost(
        string serverName,
        DatabaseSettings? databaseSettings,
        InternalNetworkSettings internalNetworkSettings,
        InternalNetworkCallbacks? callbacks = null)
    {
        if (string.IsNullOrWhiteSpace(serverName))
        {
            throw new ArgumentException("Server name is required.", nameof(serverName));
        }

        ArgumentNullException.ThrowIfNull(internalNetworkSettings);

        databaseSettings?.Validate();
        internalNetworkSettings.Validate();

        _serverName = serverName;
        _databaseSettings = databaseSettings;
        _internalNetworkSettings = internalNetworkSettings;
        _databaseService = databaseSettings is null ? null : new MySqlDatabaseService(databaseSettings);

        InternalNetworkCallbacks hostCallbacks = CreateHostCallbacks(callbacks ?? InternalNetworkCallbacks.Empty);

        _internalSocketListener = new InternalSocketListener(internalNetworkSettings, hostCallbacks);
        _internalPeerConnector = new InternalPeerConnector(
            serverName,
            internalNetworkSettings.Peers,
            internalNetworkSettings.RegistrationKey,
            internalNetworkSettings.LatencyReportInterval,
            internalNetworkSettings.PingTimeout,
            hostCallbacks);
    }

    public Task StartupCompleted => _startupCompleted.Task;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using CancellationTokenSource linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, _shutdownCancellation.Token);

        try
        {
            Logger.Write(LogType.NOTICE, $"Starting {_serverName}...", nameof(EmulationServerHost));
            await ValidateStartupAsync(linkedCancellation.Token);

            await _internalPeerConnector.StartAsync(linkedCancellation.Token);

            if (_internalNetworkSettings.Peers.Count == 0)
            {
                Logger.Write(LogType.NETWORK, $"{_serverName} has no outgoing internal peers configured. Waiting for incoming internal server registrations...", nameof(EmulationServerHost));
            }

            Logger.Write(LogType.NETWORK, $"{_serverName} started successfully. Listening for internal server connections...", nameof(EmulationServerHost));

            _startupCompleted.TrySetResult(true);

            await _internalSocketListener.StartAsync(linkedCancellation.Token);

            Logger.Write(LogType.TRACE, $"{_serverName} stopped.", nameof(EmulationServerHost));
        }
        catch (Exception exception)
        {
            _startupCompleted.TrySetException(exception);
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _internalPeerConnector.StopAsync(cancellationToken);
        await _internalSocketListener.StopAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        _shutdownCancellation.Dispose();

        if (_databaseService is not null)
        {
            await _databaseService.DisposeAsync();
        }
    }

    private InternalNetworkCallbacks CreateHostCallbacks(InternalNetworkCallbacks callbacks)
    {
        return new InternalNetworkCallbacks
        {
            ServerAuthenticatedAsync = callbacks.ServerAuthenticatedAsync,
            PacketReceivedAsync = callbacks.PacketReceivedAsync,
            ServerDisconnectedAsync = callbacks.ServerDisconnectedAsync,
            PeerAuthenticatedAsync = callbacks.PeerAuthenticatedAsync,
            PeerPacketReceivedAsync = callbacks.PeerPacketReceivedAsync,
            PeerDisconnectedAsync = callbacks.PeerDisconnectedAsync,
            ShutdownRequestedAsync = async (sourceServerName, reason, cancellationToken) =>
            {
                await callbacks.NotifyShutdownRequestedAsync(sourceServerName, reason, cancellationToken);
                await RequestShutdownAsync(sourceServerName, reason);
            },
        };
    }

    private async Task RequestShutdownAsync(string sourceServerName, string reason)
    {
        if (Interlocked.Exchange(ref _shutdownRequested, 1) == 1)
        {
            return;
        }

        Logger.Write(LogType.WARNING, $"{_serverName} received internal shutdown request from {sourceServerName}: {reason}. Stopping server...", nameof(EmulationServerHost));
        await _shutdownCancellation.CancelAsync();
    }

    private async Task ValidateStartupAsync(CancellationToken cancellationToken)
    {
        Logger.Write(LogType.TRACE, $"Validating {_serverName} settings...", nameof(EmulationServerHost));

        _internalNetworkSettings.Validate();

        if (_databaseSettings is not null && _databaseService is not null)
        {
            _databaseSettings.Validate();

            Logger.Write(LogType.DATABASE, $"Validating {_serverName} database connection...", nameof(EmulationServerHost));
            await _databaseService.ValidateConnectionAsync(cancellationToken);

            Logger.Write(LogType.NETWORK, $"{_serverName} settings, database connection, and internal networking validated successfully.", nameof(EmulationServerHost));
            return;
        }

        Logger.Write(LogType.NETWORK, $"{_serverName} settings and internal networking validated successfully. No direct database connection is configured.", nameof(EmulationServerHost));
    }
}
