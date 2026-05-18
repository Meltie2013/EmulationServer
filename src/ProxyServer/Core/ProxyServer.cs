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
  * This file belongs to the server startup, shutdown, and dependency orchestration portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.ProxyServer.Core;

/**
  * Represents the proxy server component in the server startup, shutdown, and dependency orchestration area.
  * It owns the server startup, shutdown, and dependency wiring for this process.
  */
public sealed class ProxyServer : IAsyncDisposable
{
    /**
      * Stores the dependency monitor dependency or runtime value for ProxyServer.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly ProxyDependencyMonitor _dependencyMonitor;
    /**
      * Stores the host dependency or runtime value for ProxyServer.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly EmulationServerHost _host;

    /**
      * Creates a new ProxyServer instance and stores the dependencies required by the component.
      * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
      */
    public ProxyServer(ProxyServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _dependencyMonitor = new ProxyDependencyMonitor(settings.DependencyPolicy);
        _host = new EmulationServerHost(nameof(ProxyServer), settings.InternalNetwork, _dependencyMonitor.CreateCallbacks());
    }

    /**
      * Starts the component and prepares the runtime state required before it can accept work.
      * The method is part of ProxyServer and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
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

    /**
      * Stops the component and releases runtime resources in a controlled order.
      * The method is part of ProxyServer and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _dependencyMonitor.StopAsync(cancellationToken);
        await _host.StopAsync(cancellationToken);
    }

    /**
      * Releases owned resources and ensures background work is stopped safely.
      * The method is part of ProxyServer and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        await _dependencyMonitor.DisposeAsync();
        await _host.DisposeAsync();
    }
}
