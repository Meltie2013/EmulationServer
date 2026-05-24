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


/**
 * File overview: src/ProxyServer/Core/ProxyServer.cs
 * Documents the ProxyServer source file in the proxy startup, service discovery, and client-routing support area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.ProxyServer.Core;

/**
 * Owns the proxy server behavior for the proxy startup, service discovery, and client-routing support layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public sealed class ProxyServer : IAsyncDisposable
{
    /**
     * Holds the private dependency monitor state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly ProxyDependencyMonitor _dependencyMonitor;
    /**
     * Holds the private host state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly EmulationServerHost _host;

    /**
     * Initializes a new ProxyServer instance with the dependencies required by the proxy startup, service discovery, and client-routing support workflow.
     * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
     * Inputs used by this operation: settings.
     */
    public ProxyServer(ProxyServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _dependencyMonitor = new ProxyDependencyMonitor(settings.DependencyPolicy);
        _host = new EmulationServerHost(nameof(ProxyServer), settings.InternalNetwork, _dependencyMonitor.CreateCallbacks());
    }

    /**
     * Starts the start workflow and prepares the component to accept runtime work.
     * Startup is ordered so validation and dependency setup finish before services are announced as available.
     * Inputs used by this operation: cancellationToken.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
     */
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Task hostTask = _host.StartAsync(cancellationToken);

        try
        {
            await _host.StartupCompleted.WaitAsync(cancellationToken);
            await _dependencyMonitor.StartAsync(cancellationToken);

            await hostTask;
        }
        finally
        {
            await _dependencyMonitor.StopAsync(CancellationToken.None);
        }
    }

    /**
     * Stops the stop workflow and releases owned runtime resources in a controlled order.
     * Shutdown logic is centralized to avoid dangling connections, incomplete saves, or partially registered services.
     * Inputs used by this operation: cancellationToken.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
     */
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _dependencyMonitor.StopAsync(cancellationToken);
        await _host.StopAsync(cancellationToken);
    }

    /**
     * Stops the dispose workflow and releases owned runtime resources in a controlled order.
     * Shutdown logic is centralized to avoid dangling connections, incomplete saves, or partially registered services.
     * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
     */
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        await _dependencyMonitor.DisposeAsync();
        await _host.DisposeAsync();
    }
}
