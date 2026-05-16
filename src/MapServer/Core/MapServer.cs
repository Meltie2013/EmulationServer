
using EmulationServer.Core.Servers;
using EmulationServer.MapServer.Configuration;

namespace EmulationServer.MapServer.Core;

public sealed class MapServer : IAsyncDisposable
{
    private readonly EmulationServerHost _host;

    public MapServer(MapServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _host = new EmulationServerHost(nameof(MapServer), settings.InternalNetwork);
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
