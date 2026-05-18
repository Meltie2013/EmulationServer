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

namespace EmulationServer.Network.Configuration;

public sealed class RealmSocketListenerSettings
{
    public string BindAddress { get; init; } = "0.0.0.0";

    public int Port { get; init; } = 3724;

    public int Backlog { get; init; } = 128;


    public TimeSpan ShutdownGracePeriod { get; init; } = TimeSpan.FromSeconds(15);

    public IPAddress GetBindAddress()
    {
        if (!IPAddress.TryParse(BindAddress, out IPAddress? ipAddress))
        {
            throw new InvalidOperationException($"Invalid realm bind address: '{BindAddress}'.");
        }

        return ipAddress;
    }

    public void Validate()
    {
        _ = GetBindAddress();

        if (Port is < IPEndPoint.MinPort or > IPEndPoint.MaxPort)
        {
            throw new InvalidOperationException($"Invalid realm port: {Port}. Valid range is 0-65535.");
        }

        if (Backlog <= 0)
        {
            throw new InvalidOperationException("Realm listener backlog must be greater than zero.");
        }


        if (ShutdownGracePeriod < TimeSpan.Zero)
        {
            throw new InvalidOperationException("Realm shutdown grace period cannot be negative.");
        }
    }
}
