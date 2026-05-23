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

using System.Net;

namespace EmulationServer.WorldServer.Configuration;

public sealed class WorldClientSettings
{
    public string BindAddress { get; init; } = "127.0.0.1";
    public ushort Port { get; init; } = 8085;
    public int Backlog { get; init; } = 128;
    public int ReceiveBufferSize { get; init; } = 65536;
    public int SendBufferSize { get; init; } = 65536;
    public bool KeepAlive { get; init; } = true;
    public int KeepAliveTimeSeconds { get; init; } = 30;
    public int KeepAliveIntervalSeconds { get; init; } = 10;
    public TimeSpan ShutdownGracePeriod { get; init; } = TimeSpan.FromSeconds(15);
    public int MaximumPacketSize { get; init; } = 0x8000;

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

    public IPAddress GetBindAddress()
    {
        if (!IPAddress.TryParse(BindAddress, out IPAddress? address))
        {
            throw new InvalidOperationException($"Invalid World client bind address: {BindAddress}");
        }

        return address;
    }
}
