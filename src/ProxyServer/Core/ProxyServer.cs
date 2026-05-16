
using EmulationServer.Core.Servers;
using EmulationServer.ProxyServer.Configuration;

namespace EmulationServer.ProxyServer.Core;

public sealed class ProxyServer : IAsyncDisposable
{
    private readonly EmulationServerHost _host;

    public ProxyServer(ProxyServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _host = new EmulationServerHost(nameof(ProxyServer), settings.InternalNetwork);
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
