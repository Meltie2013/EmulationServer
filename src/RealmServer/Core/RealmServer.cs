
using EmulationServer.Database.Interfaces;
using EmulationServer.Database.Services;
using EmulationServer.RealmServer.Configuration;
using EmulationServer.Network.Networking.Socket;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.RealmServer.Core;

public sealed class RealmServer : IAsyncDisposable
{
    private readonly RealmServerSettings _settings;
    private readonly IDatabaseService _databaseService;
    private readonly RealmSocketListener _socketListener;

    public RealmServer(RealmServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _settings = settings;
        _databaseService = new MySqlDatabaseService(settings.Database);
        _socketListener = new RealmSocketListener(settings.Socket);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.Write(LogType.NOTICE, "Starting RealmServer...", nameof(RealmServer));
        await ValidateStartupAsync(cancellationToken);

        Logger.Write(LogType.NETWORK, "RealmServer started successfully. Listening for connections...", nameof(RealmServer));
        await _socketListener.StartAsync(cancellationToken);

        Logger.Write(LogType.TRACE, "RealmServer stopped.", nameof(RealmServer));
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return _socketListener.StopAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        await _databaseService.DisposeAsync();
    }

    private async Task ValidateStartupAsync(CancellationToken cancellationToken)
    {
        Logger.Write(LogType.TRACE, "Validating RealmServer settings...", nameof(RealmServer));
        _settings.Validate();

        Logger.Write(LogType.NETWORK, "Validating database connection...", nameof(RealmServer));
        await _databaseService.ValidateConnectionAsync(cancellationToken);

        Logger.Write(LogType.NETWORK, "RealmServer settings and database connection validated successfully.", nameof(RealmServer));
    }
}
