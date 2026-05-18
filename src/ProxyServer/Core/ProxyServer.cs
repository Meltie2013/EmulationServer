//
// Copyright (C) 2026 Emulation Server Project
//
// This program is free software. You can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation. either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY. Without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//

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
