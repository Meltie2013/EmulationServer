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
// File: src/ProxyServer/Configuration/ProxyDependencySettings.cs
// Purpose: Contains proxy dependency settings code for the proxy server gateway, internal routing, and public connection coordination.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.ProxyServer.Configuration;

// Type: ProxyDependencySettings
// Purpose: Provides proxy dependency settings behavior for the proxy server gateway, internal routing, and public connection coordination.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class ProxyDependencySettings
{

    // Method: string
    // Purpose: Executes the string operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - OrdinalIgnoreCase: Ordinal ignore case value supplied by the caller for this operation.
    // Returns: Returns the I read only set critical servers { get; init; } = new hash set< value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencySettings so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlySet<string> CriticalServers { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "WorldServer",
    };

    // Method: string
    // Purpose: Executes the string operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - OrdinalIgnoreCase: Ordinal ignore case value supplied by the caller for this operation.
    // Returns: Returns the I read only set non critical servers { get; init; } = new hash set< value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencySettings so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlySet<string> NonCriticalServers { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "MapServer",
        "InstanceServer",
    };

    // Method: FromSeconds
    // Purpose: Executes the from seconds operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters: none.
    // Returns: Returns the time span critical server packet timeout { get; init; } = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencySettings so callers do not duplicate validation, protocol, or persistence rules.
    public TimeSpan CriticalServerPacketTimeout { get; init; } = TimeSpan.FromSeconds(45);

    // Method: FromSeconds
    // Purpose: Executes the from seconds operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters: none.
    // Returns: Returns the time span non critical reconnect report interval { get; init; } = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencySettings so callers do not duplicate validation, protocol, or persistence rules.
    public TimeSpan NonCriticalReconnectReportInterval { get; init; } = TimeSpan.FromSeconds(30);

    // Method: FromSeconds
    // Purpose: Executes the from seconds operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters: none.
    // Returns: Returns the time span non critical reconnect timeout { get; init; } = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencySettings so callers do not duplicate validation, protocol, or persistence rules.
    public TimeSpan NonCriticalReconnectTimeout { get; init; } = TimeSpan.FromSeconds(120);

    // Property: Gets or sets the health logging enabled value used by the proxy server gateway, internal routing, and public connection coordination.
    // Value: health logging enabled value exposed by the owning type.
    public bool HealthLoggingEnabled { get; init; } = true;

    // Method: FromSeconds
    // Purpose: Executes the from seconds operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters: none.
    // Returns: Returns the time span health report interval { get; init; } = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencySettings so callers do not duplicate validation, protocol, or persistence rules.
    public TimeSpan HealthReportInterval { get; init; } = TimeSpan.FromSeconds(30);

    // Method: FromSeconds
    // Purpose: Executes the from seconds operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters: none.
    // Returns: Returns the time span health status stale timeout { get; init; } = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencySettings so callers do not duplicate validation, protocol, or persistence rules.
    public TimeSpan HealthStatusStaleTimeout { get; init; } = TimeSpan.FromSeconds(45);

    // Method: FromMilliseconds
    // Purpose: Executes the from milliseconds operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters: none.
    // Returns: Returns the time span degraded latency threshold { get; init; } = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencySettings so callers do not duplicate validation, protocol, or persistence rules.
    public TimeSpan DegradedLatencyThreshold { get; init; } = TimeSpan.FromMilliseconds(150);

    // Method: FromMilliseconds
    // Purpose: Executes the from milliseconds operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters: none.
    // Returns: Returns the time span unhealthy latency threshold { get; init; } = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencySettings so callers do not duplicate validation, protocol, or persistence rules.
    public TimeSpan UnhealthyLatencyThreshold { get; init; } = TimeSpan.FromMilliseconds(500);

    // Property: Gets or sets the degraded load percent value used by the proxy server gateway, internal routing, and public connection coordination.
    // Value: degraded load percent value exposed by the owning type.
    public double DegradedLoadPercent { get; init; } = 70d;

    // Property: Gets or sets the unhealthy load percent value used by the proxy server gateway, internal routing, and public connection coordination.
    // Value: unhealthy load percent value exposed by the owning type.
    public double UnhealthyLoadPercent { get; init; } = 90d;

    // Method: FromMilliseconds
    // Purpose: Executes the from milliseconds operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters: none.
    // Returns: Returns the time span degraded average tick threshold { get; init; } = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencySettings so callers do not duplicate validation, protocol, or persistence rules.
    public TimeSpan DegradedAverageTickThreshold { get; init; } = TimeSpan.FromMilliseconds(50);

    // Method: FromMilliseconds
    // Purpose: Executes the from milliseconds operation for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters: none.
    // Returns: Returns the time span unhealthy average tick threshold { get; init; } = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to ProxyDependencySettings so callers do not duplicate validation, protocol, or persistence rules.
    public TimeSpan UnhealthyAverageTickThreshold { get; init; } = TimeSpan.FromMilliseconds(200);

    // Property: Gets or sets the degraded ping miss count value used by the proxy server gateway, internal routing, and public connection coordination.
    // Value: degraded ping miss count value exposed by the owning type.
    public int DegradedPingMissCount { get; init; } = 1;

    // Property: Gets or sets the unhealthy ping miss count value used by the proxy server gateway, internal routing, and public connection coordination.
    // Value: unhealthy ping miss count value exposed by the owning type.
    public int UnhealthyPingMissCount { get; init; } = 3;

    // Method: Validate
    // Purpose: Validates or evaluates validate rules for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to ProxyDependencySettings so callers do not duplicate validation, protocol, or persistence rules.
    public void Validate()
    {
        if (CriticalServers.Count == 0)
        {
            throw new InvalidOperationException("Proxy dependency policy requires at least one critical server.");
        }

        if (CriticalServerPacketTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Proxy critical server packet timeout must be greater than zero.");
        }

        if (NonCriticalReconnectReportInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Proxy non-critical reconnect report interval must be greater than zero.");
        }

        if (NonCriticalReconnectTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Proxy non-critical reconnect timeout must be greater than zero.");
        }

        if (HealthReportInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Proxy health report interval must be greater than zero.");
        }

        if (HealthStatusStaleTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Proxy health stale timeout must be greater than zero.");
        }

        if (DegradedLatencyThreshold <= TimeSpan.Zero || UnhealthyLatencyThreshold <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Proxy health latency thresholds must be greater than zero.");
        }

        if (UnhealthyLatencyThreshold <= DegradedLatencyThreshold)
        {
            throw new InvalidOperationException("Proxy unhealthy latency threshold must be greater than degraded latency threshold.");
        }

        if (DegradedLoadPercent is < 0d or > 100d || UnhealthyLoadPercent is < 0d or > 100d || UnhealthyLoadPercent <= DegradedLoadPercent)
        {
            throw new InvalidOperationException("Proxy health load thresholds must be between 0 and 100, and unhealthy must be greater than degraded.");
        }

        if (DegradedAverageTickThreshold <= TimeSpan.Zero || UnhealthyAverageTickThreshold <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Proxy health average tick thresholds must be greater than zero.");
        }

        if (UnhealthyAverageTickThreshold <= DegradedAverageTickThreshold)
        {
            throw new InvalidOperationException("Proxy unhealthy average tick threshold must be greater than degraded average tick threshold.");
        }

        if (DegradedPingMissCount < 1 || UnhealthyPingMissCount < 1 || UnhealthyPingMissCount < DegradedPingMissCount)
        {
            throw new InvalidOperationException("Proxy health ping miss thresholds must be positive, and unhealthy must be greater than or equal to degraded.");
        }

        foreach (string serverName in CriticalServers)
        {
            ValidateServerName(serverName);
        }

        foreach (string serverName in NonCriticalServers)
        {
            ValidateServerName(serverName);

            if (CriticalServers.Contains(serverName))
            {
                throw new InvalidOperationException($"Server '{serverName}' cannot be both critical and non-critical.");
            }
        }
    }

    // Method: ValidateServerName
    // Purpose: Validates or evaluates validate server name rules for the proxy server gateway, internal routing, and public connection coordination.
    // Parameters:
    // - serverName: Server name value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to ProxyDependencySettings so callers do not duplicate validation, protocol, or persistence rules.
    private static void ValidateServerName(string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
        {
            throw new InvalidOperationException("Proxy dependency settings contain an empty server name.");
        }
    }
}
