
using EmulationServer.Core.Servers;
using EmulationServer.WorldServer.Configuration;

namespace EmulationServer.WorldServer.Core;

public sealed class WorldServer : IAsyncDisposable
{
    private readonly EmulationServerHost _host;

    public WorldServer(WorldServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _host = new EmulationServerHost(nameof(WorldServer), settings.Database, settings.InternalNetwork);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _host.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return _host.StopAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        await _host.DisposeAsync();
    }
}
