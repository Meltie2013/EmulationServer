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
// File: src/ProxyServer/Core/ProxyServer.cs
// Purpose: Contains proxy server code for the proxy server gateway, internal routing, and public connection coordination.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Core.Servers;
using EmulationServer.ProxyServer.Configuration;

namespace EmulationServer.ProxyServer.Core;

// Type: ProxyServer
// Purpose: Provides proxy server behavior for the proxy server gateway, internal routing, and public connection coordination.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class ProxyServer : IAsyncDisposable
{

    // Field: Stores the dependency monitor state used by the proxy server gateway, internal routing, and public connection coordination.
    // Value: current dependency monitor backing value maintained by the owning type.
    private readonly ProxyDependencyMonitor _dependencyMonitor;

    // Field: Stores the host state used by the proxy server gateway, internal routing, and public connection coordination.
    // Value: current host backing value maintained by the owning type.
    private readonly EmulationServerHost _host;

    // Constructor: ProxyServer
    // Purpose: Initializes a new ProxyServer instance with dependencies and values required by the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - settings: Settings values that control how this operation should run.
    // Returns: none.
    // Notes: This keeps the operation scoped to ProxyServer so callers do not duplicate validation, protocol, or persistence rules.
    public ProxyServer(ProxyServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _dependencyMonitor = new ProxyDependencyMonitor(settings.DependencyPolicy);
        _host = new EmulationServerHost("ProxyServer", settings.InternalNetwork, _dependencyMonitor.CreateCallbacks());
    }

    // Method: StartAsync
    // Purpose: Controls the start lifecycle step for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to ProxyServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: StopAsync
    // Purpose: Controls the stop lifecycle step for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to ProxyServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _dependencyMonitor.StopAsync(cancellationToken);
        await _host.StopAsync(cancellationToken);
    }

    // Method: DisposeAsync
    // Purpose: Controls the dispose lifecycle step for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters: none.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to ProxyServer so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        await _dependencyMonitor.DisposeAsync();
        await _host.DisposeAsync();
    }
}
