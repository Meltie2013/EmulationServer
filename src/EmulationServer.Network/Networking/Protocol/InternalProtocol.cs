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

using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

/**
  * File overview: src/EmulationServer.Network/Networking/Protocol/InternalProtocol.cs
  * This file belongs to the internal server-to-server protocol packet parsing and formatting portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Network.Networking.Protocol;

/**
  * Represents the internal protocol component in the internal server-to-server protocol packet parsing and formatting area.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public static class InternalProtocol
{
    public const int MaximumAuthenticationLineLength = 512;
    public const int MaximumPacketLineLength = 2048;

    public const string AuthenticationChallenge = "AUTH_CHALLENGE";
    public const string AuthenticationResponse = "AUTH_RESPONSE";
    public const string AuthenticationAccepted = "AUTH_ACCEPTED";
    public const string AuthenticationRejected = "AUTH_REJECTED";
    public const string Ping = "PING";
    public const string Pong = "PONG";
    public const string ShutdownRequest = "SHUTDOWN_REQUEST";
    public const string WorldCapacity = "WORLD_CAPACITY";
    public const string MapServiceStatus = "MAP_SERVICE_STATUS";
    public const string MapServiceCommand = "MAP_SERVICE_COMMAND";
    public const string MapServiceCommandResult = "MAP_SERVICE_COMMAND_RESULT";

    /**
      * Reads structured input from the supplied source and converts it into the project model.
      * The method is part of InternalProtocol and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public static async Task<string?> ReadLineAsync(NetworkStream stream, int maximumLength, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        byte[] singleByteBuffer = new byte[1];
        using MemoryStream lineBuffer = new();

        while (lineBuffer.Length < maximumLength)
        {
            int received = await stream.ReadAsync(singleByteBuffer.AsMemory(0, 1), cancellationToken);
            if (received == 0)
            {
                return lineBuffer.Length == 0
                    ? null
                    : Encoding.UTF8.GetString(lineBuffer.ToArray()).Trim();
            }

            byte value = singleByteBuffer[0];
            if (value == '\n')
            {
                break;
            }

            if (value != '\r')
            {
                lineBuffer.WriteByte(value);
            }
        }

        if (lineBuffer.Length >= maximumLength)
        {
            throw new InvalidOperationException($"Internal protocol packet is too long. Maximum length is {maximumLength} byte(s).");
        }

        return Encoding.UTF8.GetString(lineBuffer.ToArray()).Trim();
    }

    /**
      * Writes the supplied data to the target destination using the project protocol or file format.
      * The method is part of InternalProtocol and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public static async Task WriteLineAsync(NetworkStream stream, SemaphoreSlim sendLock, string line, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(sendLock);

        string packet = line.EndsWith('\n') ? line : $"{line}\n";
        byte[] data = Encoding.UTF8.GetBytes(packet);

        await sendLock.WaitAsync(cancellationToken);
        try
        {
            await stream.WriteAsync(data.AsMemory(0, data.Length), cancellationToken);
        }
        finally
        {
            sendLock.Release();
        }
    }

    /**
      * Performs the registration keys match operation for InternalProtocol.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    public static bool RegistrationKeysMatch(string expected, string actual)
    {
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
        byte[] actualBytes = Encoding.UTF8.GetBytes(actual);

        return expectedBytes.Length == actualBytes.Length &&
            CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
