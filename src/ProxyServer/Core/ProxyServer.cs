
using EmulationServer.Core.Servers;
using EmulationServer.ProxyServer.Configuration;

namespace EmulationServer.ProxyServer.Core;

public sealed class ProxyServer : IAsyncDisposable
{
    private readonly ProxyDependencyMonitor _dependencyMonitor;
    private readonly EmulationServerHost _host;

    public ProxyServer(ProxyServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _dependencyMonitor = new ProxyDependencyMonitor(settings.DependencyPolicy);
        _host = new EmulationServerHost(nameof(ProxyServer), settings.InternalNetwork, _dependencyMonitor.CreateCallbacks());
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _dependencyMonitor.StartAsync(cancellationToken);

        try
        {
            await _host.StartAsync(cancellationToken);
        }
        finally
        {
            await _dependencyMonitor.StopAsync(CancellationToken.None);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _dependencyMonitor.StopAsync(cancellationToken);
        await _host.StopAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        await _dependencyMonitor.DisposeAsync();
        await _host.DisposeAsync();
    }
}
