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
// File: src/EmulationServer.Network/Configuration/InternalPeerSettings.cs
// Purpose: Contains internal peer settings code for the packet serialization, socket transport, and protocol framing layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Network.Networking.Protocol;

namespace EmulationServer.Network.Configuration;

// Type: InternalPeerSettings
// Purpose: Provides internal peer settings behavior for the packet serialization, socket transport, and protocol framing layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class InternalPeerSettings
{

    // Property: Gets or sets the name value used by the packet serialization, socket transport, and protocol framing layer.
    // Value: name value exposed by the owning type.
    public string Name { get; init; } = string.Empty;

    // Property: Gets or sets the host value used by the packet serialization, socket transport, and protocol framing layer.
    // Value: host value exposed by the owning type.
    public string Host { get; init; } = "127.0.0.1";

    // Property: Gets or sets the port value used by the packet serialization, socket transport, and protocol framing layer.
    // Value: port value exposed by the owning type.
    public int Port { get; init; }

    // Property: Gets or sets the enabled value used by the packet serialization, socket transport, and protocol framing layer.
    // Value: enabled value exposed by the owning type.
    public bool Enabled { get; init; } = true;

    // Method: FromSeconds
    // Purpose: Executes the from seconds operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters: none.
    // Returns: Returns the time span reconnect delay { get; init; } = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to InternalPeerSettings so callers do not duplicate validation, protocol, or persistence rules.
    public TimeSpan ReconnectDelay { get; init; } = TimeSpan.FromSeconds(5);

    // Method: FromSeconds
    // Purpose: Executes the from seconds operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters: none.
    // Returns: Returns the time span reconnect timeout { get; init; } = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to InternalPeerSettings so callers do not duplicate validation, protocol, or persistence rules.
    public TimeSpan ReconnectTimeout { get; init; } = TimeSpan.FromSeconds(120);

    // Method: Validate
    // Purpose: Validates or evaluates validate rules for the packet serialization, socket transport, and protocol framing layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to InternalPeerSettings so callers do not duplicate validation, protocol, or persistence rules.
    public void Validate()
    {
        if (!InternalProtocol.IsValidServerName(Name))
        {
            throw new InvalidOperationException($"Invalid internal peer name: '{Name}'.");
        }

        if (string.IsNullOrWhiteSpace(Host))
        {
            throw new InvalidOperationException($"Internal peer '{Name}' host is required.");
        }

        if (Port is < 1 or > 65535)
        {
            throw new InvalidOperationException($"Invalid internal peer '{Name}' port: {Port}. Valid range is 1-65535.");
        }

        if (ReconnectDelay <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"Internal peer '{Name}' reconnect delay must be greater than zero.");
        }

        if (ReconnectTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"Internal peer '{Name}' reconnect timeout must be greater than zero.");
        }

        if (ReconnectTimeout < ReconnectDelay)
        {
            throw new InvalidOperationException($"Internal peer '{Name}' reconnect timeout must be greater than or equal to the reconnect delay.");
        }
    }
}
