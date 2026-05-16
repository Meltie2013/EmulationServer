
using EmulationServer.Core.Servers;
using EmulationServer.CharacterServer.Configuration;

namespace EmulationServer.CharacterServer.Core;

public sealed class CharacterServer : IAsyncDisposable
{
    private readonly EmulationServerHost _host;

    public CharacterServer(CharacterServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _host = new EmulationServerHost(nameof(CharacterServer), settings.Database, settings.InternalNetwork);
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
