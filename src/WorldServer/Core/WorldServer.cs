
using EmulationServer.Core.Servers;
using EmulationServer.WorldServer.Configuration;
using EmulationServer.WorldServer.Internal;

namespace EmulationServer.WorldServer.Core;

public sealed class WorldServer : IAsyncDisposable
{
    private readonly EmulationServerHost _host;
    private readonly WorldRealmStatusReporter _realmStatusReporter;

    public WorldServer(WorldServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _host = new EmulationServerHost(nameof(WorldServer), settings.Database, settings.InternalNetwork);
        _realmStatusReporter = new WorldRealmStatusReporter(settings.RealmStatus, settings.InternalNetwork.RegistrationKey);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Task hostTask = _host.StartAsync(cancellationToken);

        try
        {
            await _host.StartupCompleted.WaitAsync(cancellationToken);

            await _realmStatusReporter.StartAsync(cancellationToken);

            await hostTask;
        }
        finally
        {
            await _realmStatusReporter.StopAsync(CancellationToken.None);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _realmStatusReporter.StopAsync(cancellationToken);
        await _host.StopAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        await _realmStatusReporter.DisposeAsync();
        await _host.DisposeAsync();
    }
}
