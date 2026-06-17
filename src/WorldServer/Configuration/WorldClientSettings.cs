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
// File: src/WorldServer/Configuration/WorldClientSettings.cs
// Purpose: Contains world client settings code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Net;

namespace EmulationServer.WorldServer.Configuration;

// Type: WorldClientSettings
// Purpose: Provides world client settings behavior for the world server gameplay, session, and character runtime layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class WorldClientSettings
{

    // Property: Gets or sets the bind address value used by the world server gameplay, session, and character runtime layer.
    // Value: bind address value exposed by the owning type.
    public string BindAddress { get; init; } = "127.0.0.1";

    // Property: Gets or sets the port value used by the world server gameplay, session, and character runtime layer.
    // Value: port value exposed by the owning type.
    public ushort Port { get; init; } = 8085;

    // Property: Gets or sets the backlog value used by the world server gameplay, session, and character runtime layer.
    // Value: backlog value exposed by the owning type.
    public int Backlog { get; init; } = 128;

    // Property: Gets or sets the receive buffer size value used by the world server gameplay, session, and character runtime layer.
    // Value: receive buffer size value exposed by the owning type.
    public int ReceiveBufferSize { get; init; } = 65536;

    // Property: Gets or sets the send buffer size value used by the world server gameplay, session, and character runtime layer.
    // Value: send buffer size value exposed by the owning type.
    public int SendBufferSize { get; init; } = 65536;

    // Property: Gets or sets the keep alive value used by the world server gameplay, session, and character runtime layer.
    // Value: keep alive value exposed by the owning type.
    public bool KeepAlive { get; init; } = true;

    // Property: Gets or sets the keep alive time seconds value used by the world server gameplay, session, and character runtime layer.
    // Value: keep alive time seconds value exposed by the owning type.
    public int KeepAliveTimeSeconds { get; init; } = 30;

    // Property: Gets or sets the keep alive interval seconds value used by the world server gameplay, session, and character runtime layer.
    // Value: keep alive interval seconds value exposed by the owning type.
    public int KeepAliveIntervalSeconds { get; init; } = 10;

    // Method: FromSeconds
    // Purpose: Executes the from seconds operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the time span shutdown grace period { get; init; } = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSettings so callers do not duplicate validation, protocol, or persistence rules.
    public TimeSpan ShutdownGracePeriod { get; init; } = TimeSpan.FromSeconds(15);

    // Property: Gets or sets the maximum packet size value used by the world server gameplay, session, and character runtime layer.
    // Value: maximum packet size value exposed by the owning type.
    public int MaximumPacketSize { get; init; } = 0x8000;

    // Method: Validate
    // Purpose: Validates or evaluates validate rules for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldClientSettings so callers do not duplicate validation, protocol, or persistence rules.
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BindAddress))
        {
            throw new InvalidOperationException("World client bind address is required.");
        }

        _ = GetBindAddress();

        if (Port == 0)
        {
            throw new InvalidOperationException("World client port must be greater than zero.");
        }

        if (Backlog <= 0)
        {
            throw new InvalidOperationException("World client backlog must be greater than zero.");
        }

        if (ReceiveBufferSize <= 0)
        {
            throw new InvalidOperationException("World client receive buffer size must be greater than zero.");
        }

        if (SendBufferSize <= 0)
        {
            throw new InvalidOperationException("World client send buffer size must be greater than zero.");
        }

        if (KeepAliveTimeSeconds < 0)
        {
            throw new InvalidOperationException("World client keep-alive time cannot be negative.");
        }

        if (KeepAliveIntervalSeconds < 0)
        {
            throw new InvalidOperationException("World client keep-alive interval cannot be negative.");
        }

        if (ShutdownGracePeriod < TimeSpan.Zero)
        {
            throw new InvalidOperationException("World client shutdown grace period cannot be negative.");
        }

        if (MaximumPacketSize < 6)
        {
            throw new InvalidOperationException("World client maximum packet size must be at least 6 bytes.");
        }
    }

    // Method: GetBindAddress
    // Purpose: Retrieves get bind address data for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the IP address value produced by this operation.
    // Notes: This keeps the operation scoped to WorldClientSettings so callers do not duplicate validation, protocol, or persistence rules.
    public IPAddress GetBindAddress()
    {
        if (!IPAddress.TryParse(BindAddress, out IPAddress? address))
        {
            throw new InvalidOperationException($"Invalid World client bind address: {BindAddress}");
        }

        return address;
    }
}
