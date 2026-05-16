
using EmulationServer.Core.Servers;
using EmulationServer.InstanceServer.Configuration;

namespace EmulationServer.InstanceServer.Core;

public sealed class InstanceServer : IAsyncDisposable
{
    private readonly EmulationServerHost _host;

    public InstanceServer(InstanceServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _host = new EmulationServerHost(nameof(InstanceServer), settings.InternalNetwork);
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
