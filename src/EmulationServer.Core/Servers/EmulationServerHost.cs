
using EmulationServer.Database.Configuration;
using EmulationServer.Database.Interfaces;
using EmulationServer.Database.Services;
using EmulationServer.Network.Configuration;
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

    public EmulationServerHost(
        string serverName,
        InternalNetworkSettings internalNetworkSettings)
        : this(serverName, null, internalNetworkSettings)
    {
    }

    public EmulationServerHost(
        string serverName,
        DatabaseSettings? databaseSettings,
        InternalNetworkSettings internalNetworkSettings)
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
        _internalSocketListener = new InternalSocketListener(internalNetworkSettings);
        _internalPeerConnector = new InternalPeerConnector(
            serverName,
            internalNetworkSettings.Peers,
            internalNetworkSettings.RegistrationKey,
            internalNetworkSettings.LatencyReportInterval,
            internalNetworkSettings.PingTimeout);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.Write(LogType.NOTICE, $"Starting {_serverName}...", nameof(EmulationServerHost));
        await ValidateStartupAsync(cancellationToken);

        await _internalPeerConnector.StartAsync(cancellationToken);

        if (_internalNetworkSettings.Peers.Count == 0)
        {
            Logger.Write(LogType.NETWORK, $"{_serverName} has no outgoing internal peers configured. Waiting for incoming internal server registrations...", nameof(EmulationServerHost));
        }

        Logger.Write(LogType.NETWORK, $"{_serverName} started successfully. Listening for internal server connections...", nameof(EmulationServerHost));
        await _internalSocketListener.StartAsync(cancellationToken);

        Logger.Write(LogType.TRACE, $"{_serverName} stopped.", nameof(EmulationServerHost));
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _internalPeerConnector.StopAsync(cancellationToken);
        await _internalSocketListener.StopAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);

        if (_databaseService is not null)
        {
            await _databaseService.DisposeAsync();
        }
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
