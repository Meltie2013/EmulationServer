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
// File: src/EmulationServer.Network/Networking/Socket/TcpSocketOptions.cs
// Purpose: Contains TCP socket options code for the packet serialization, socket transport, and protocol framing layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Net.Sockets;

using EmulationServer.Network.Configuration;

namespace EmulationServer.Network.Networking.Socket;

// Type: TcpSocketOptions
// Purpose: Provides TCP socket options behavior for the packet serialization, socket transport, and protocol framing layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class TcpSocketOptions
{
    // Method: ConfigureClient
    // Purpose: Executes the configure client operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - client: Client value supplied by the caller for this operation.
    // - settings: Settings values that control how this operation should run.
    // Returns: none.
    // Notes: This keeps the operation scoped to TcpSocketOptions so callers do not duplicate validation, protocol, or persistence rules.
    public static void ConfigureClient(TcpClient client, InternalNetworkSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        ConfigureClient(
            client,
            settings.ReceiveBufferSize,
            settings.SendBufferSize,
            settings.KeepAlive,
            settings.KeepAliveTimeSeconds,
            settings.KeepAliveIntervalSeconds);
    }

    // Method: ConfigureClient
    // Purpose: Executes the configure client operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - client: Client value supplied by the caller for this operation.
    // - receiveBufferSize: Receive buffer size value supplied by the caller for this operation.
    // - sendBufferSize: Send buffer size value supplied by the caller for this operation.
    // - keepAlive: Keep alive value supplied by the caller for this operation.
    // - keepAliveTimeSeconds: Keep alive time seconds value supplied by the caller for this operation.
    // - keepAliveIntervalSeconds: Keep alive interval seconds value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to TcpSocketOptions so callers do not duplicate validation, protocol, or persistence rules.
    public static void ConfigureClient(
        TcpClient client,
        int receiveBufferSize,
        int sendBufferSize,
        bool keepAlive,
        int keepAliveTimeSeconds,
        int keepAliveIntervalSeconds)
    {
        ArgumentNullException.ThrowIfNull(client);

        client.NoDelay = true;
        client.ReceiveBufferSize = receiveBufferSize;
        client.SendBufferSize = sendBufferSize;

        if (!keepAlive)
        {
            return;
        }

        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        TrySetTcpKeepAliveOption(client, SocketOptionName.TcpKeepAliveTime, keepAliveTimeSeconds);
        TrySetTcpKeepAliveOption(client, SocketOptionName.TcpKeepAliveInterval, keepAliveIntervalSeconds);
    }

    // Method: TrySetTcpKeepAliveOption
    // Purpose: Executes the try set TCP keep alive option operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - client: Client value supplied by the caller for this operation.
    // - optionName: Option name value supplied by the caller for this operation.
    // - valueSeconds: Value seconds value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to TcpSocketOptions so callers do not duplicate validation, protocol, or persistence rules.
    private static void TrySetTcpKeepAliveOption(TcpClient client, SocketOptionName optionName, int valueSeconds)
    {
        if (valueSeconds <= 0)
        {
            return;
        }

        try
        {
            client.Client.SetSocketOption(SocketOptionLevel.Tcp, optionName, valueSeconds);
        }
        catch (SocketException)
        {

        }
        catch (ObjectDisposedException)
        {

        }
    }
}
