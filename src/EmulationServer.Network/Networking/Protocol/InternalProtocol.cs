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
// File: src/EmulationServer.Network/Networking/Protocol/InternalProtocol.cs
// Purpose: Contains internal protocol code for the packet serialization, socket transport, and protocol framing layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace EmulationServer.Network.Networking.Protocol;

// Type: InternalProtocol
// Purpose: Provides internal protocol behavior for the packet serialization, socket transport, and protocol framing layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class InternalProtocol
{

    // Constant: Defines the maximum authentication line length constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed maximum authentication line length value used anywhere this rule or protocol value is needed.
    public const int MaximumAuthenticationLineLength = 512;

    // Constant: Defines the maximum packet line length constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed maximum packet line length value used anywhere this rule or protocol value is needed.
    public const int MaximumPacketLineLength = 2048;

    // Constant: Defines the authentication nonce byte length constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed authentication nonce byte length value used anywhere this rule or protocol value is needed.
    private const int AuthenticationNonceByteLength = 32;

    // Constant: Defines the maximum server name length constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed maximum server name length value used anywhere this rule or protocol value is needed.
    private const int MaximumServerNameLength = 64;

    // Constant: Defines the authentication challenge constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed authentication challenge value used anywhere this rule or protocol value is needed.
    public const string AuthenticationChallenge = "AUTH_CHALLENGE";

    // Constant: Defines the authentication response constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed authentication response value used anywhere this rule or protocol value is needed.
    public const string AuthenticationResponse = "AUTH_RESPONSE";

    // Constant: Defines the authentication accepted constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed authentication accepted value used anywhere this rule or protocol value is needed.
    public const string AuthenticationAccepted = "AUTH_ACCEPTED";

    // Constant: Defines the authentication rejected constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed authentication rejected value used anywhere this rule or protocol value is needed.
    public const string AuthenticationRejected = "AUTH_REJECTED";

    // Constant: Defines the ping constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed ping value used anywhere this rule or protocol value is needed.
    public const string Ping = "PING";

    // Constant: Defines the pong constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed pong value used anywhere this rule or protocol value is needed.
    public const string Pong = "PONG";

    // Constant: Defines the shutdown request constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed shutdown request value used anywhere this rule or protocol value is needed.
    public const string ShutdownRequest = "SHUTDOWN_REQUEST";

    // Constant: Defines the world capacity constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed world capacity value used anywhere this rule or protocol value is needed.
    public const string WorldCapacity = "WORLD_CAPACITY";

    // Constant: Defines the world health status constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed world health status value used anywhere this rule or protocol value is needed.
    public const string WorldHealthStatus = "WORLD_HEALTH_STATUS";

    // Constant: Defines the map service status constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed map service status value used anywhere this rule or protocol value is needed.
    public const string MapServiceStatus = "MAP_SERVICE_STATUS";

    // Constant: Defines the realm character count snapshot begin constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed realm character count snapshot begin value used anywhere this rule or protocol value is needed.
    public const string RealmCharacterCountSnapshotBegin = "REALM_CHARACTER_COUNT_SNAPSHOT_BEGIN";

    // Constant: Defines the realm character count snapshot data constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed realm character count snapshot data value used anywhere this rule or protocol value is needed.
    public const string RealmCharacterCountSnapshotData = "REALM_CHARACTER_COUNT_SNAPSHOT_DATA";

    // Constant: Defines the realm character count snapshot end constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed realm character count snapshot end value used anywhere this rule or protocol value is needed.
    public const string RealmCharacterCountSnapshotEnd = "REALM_CHARACTER_COUNT_SNAPSHOT_END";

    // Constant: Defines the map service command constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed map service command value used anywhere this rule or protocol value is needed.
    public const string MapServiceCommand = "MAP_SERVICE_COMMAND";

    // Constant: Defines the map service command result constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed map service command result value used anywhere this rule or protocol value is needed.
    public const string MapServiceCommandResult = "MAP_SERVICE_COMMAND_RESULT";

    // Constant: Defines the player enter world constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed player enter world value used anywhere this rule or protocol value is needed.
    public const string PlayerEnterWorld = "PLAYER_ENTER_WORLD";

    // Constant: Defines the player leave world constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed player leave world value used anywhere this rule or protocol value is needed.
    public const string PlayerLeaveWorld = "PLAYER_LEAVE_WORLD";

    // Constant: Defines the player movement constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed player movement value used anywhere this rule or protocol value is needed.
    public const string PlayerMovement = "PLAYER_MOVEMENT";

    // Constant: Defines the player client packet constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed player client packet value used anywhere this rule or protocol value is needed.
    public const string PlayerClientPacket = "PLAYER_CLIENT_PACKET";

    // Constant: Defines the game object snapshot begin constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed game object snapshot begin value used anywhere this rule or protocol value is needed.
    public const string GameObjectSnapshotBegin = "GAMEOBJECT_SNAPSHOT_BEGIN";

    // Constant: Defines the game object template snapshot constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed game object template snapshot value used anywhere this rule or protocol value is needed.
    public const string GameObjectTemplateSnapshot = "GAMEOBJECT_TEMPLATE_SNAPSHOT";

    // Constant: Defines the game object spawn snapshot constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed game object spawn snapshot value used anywhere this rule or protocol value is needed.
    public const string GameObjectSpawnSnapshot = "GAMEOBJECT_SPAWN_SNAPSHOT";

    // Constant: Defines the game object snapshot end constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed game object snapshot end value used anywhere this rule or protocol value is needed.
    public const string GameObjectSnapshotEnd = "GAMEOBJECT_SNAPSHOT_END";

    // Constant: Defines the creature snapshot begin constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed creature snapshot begin value used anywhere this rule or protocol value is needed.
    public const string CreatureSnapshotBegin = "CREATURE_SNAPSHOT_BEGIN";

    // Constant: Defines the creature template snapshot constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed creature template snapshot value used anywhere this rule or protocol value is needed.
    public const string CreatureTemplateSnapshot = "CREATURE_TEMPLATE_SNAPSHOT";

    // Constant: Defines the creature spawn snapshot constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed creature spawn snapshot value used anywhere this rule or protocol value is needed.
    public const string CreatureSpawnSnapshot = "CREATURE_SPAWN_SNAPSHOT";

    // Constant: Defines the creature snapshot end constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed creature snapshot end value used anywhere this rule or protocol value is needed.
    public const string CreatureSnapshotEnd = "CREATURE_SNAPSHOT_END";

    // Method: ReadLineAsync
    // Purpose: Retrieves read line data for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - stream: Stream value supplied by the caller for this operation.
    // - maximumLength: Maximum length value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to InternalProtocol so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public static async Task<string?> ReadLineAsync(NetworkStream stream, int maximumLength, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        byte[] singleByteBuffer = new byte[1];
        await using MemoryStream lineBuffer = new();

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

    // Method: WriteLineAsync
    // Purpose: Builds or writes write line output for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - stream: Stream value supplied by the caller for this operation.
    // - sendLock: Send lock value supplied by the caller for this operation.
    // - line: Line value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to InternalProtocol so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: CreateAuthenticationNonce
    // Purpose: Applies create authentication nonce changes for the packet serialization, socket transport, and protocol framing layer.
    // Parameters: none.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to InternalProtocol so callers do not duplicate validation, protocol, or persistence rules.
    public static string CreateAuthenticationNonce()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(AuthenticationNonceByteLength));
    }

    // Method: CreateAuthenticationProof
    // Purpose: Applies create authentication proof changes for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - registrationKey: Registration key value supplied by the caller for this operation.
    // - sourceServerName: Source server name value supplied by the caller for this operation.
    // - targetServerName: Target server name value supplied by the caller for this operation.
    // - challengeNonce: Challenge nonce value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to InternalProtocol so callers do not duplicate validation, protocol, or persistence rules.
    public static string CreateAuthenticationProof(
        string registrationKey,
        string sourceServerName,
        string targetServerName,
        string challengeNonce)
    {
        if (string.IsNullOrWhiteSpace(registrationKey))
        {
            throw new ArgumentException("Registration key is required.");
        }

        if (!IsValidServerName(sourceServerName))
        {
            throw new ArgumentException("Source server name is invalid.");
        }

        if (!IsValidServerName(targetServerName))
        {
            throw new ArgumentException("Target server name is invalid.");
        }

        if (string.IsNullOrWhiteSpace(challengeNonce))
        {
            throw new ArgumentException("Authentication challenge nonce is required.");
        }

        using HMACSHA256 hmac = new(Encoding.UTF8.GetBytes(registrationKey));
        byte[] proofInput = Encoding.UTF8.GetBytes($"{sourceServerName}\n{targetServerName}\n{challengeNonce}");
        byte[] proof = hmac.ComputeHash(proofInput);

        return Convert.ToHexString(proof);
    }

    // Method: AuthenticationProofsMatch
    // Purpose: Executes the authentication proofs match operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - registrationKey: Registration key value supplied by the caller for this operation.
    // - sourceServerName: Source server name value supplied by the caller for this operation.
    // - targetServerName: Target server name value supplied by the caller for this operation.
    // - challengeNonce: Challenge nonce value supplied by the caller for this operation.
    // - suppliedProof: Supplied proof value supplied by the caller for this operation.
    // Returns: Returns true when authentication proofs match succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to InternalProtocol so callers do not duplicate validation, protocol, or persistence rules.
    public static bool AuthenticationProofsMatch(
        string registrationKey,
        string sourceServerName,
        string targetServerName,
        string challengeNonce,
        string suppliedProof)
    {
        if (string.IsNullOrWhiteSpace(suppliedProof))
        {
            return false;
        }

        string expectedProof = CreateAuthenticationProof(
            registrationKey,
            sourceServerName,
            targetServerName,
            challengeNonce);

        byte[] expectedBytes = Encoding.ASCII.GetBytes(expectedProof);
        byte[] actualBytes = Encoding.ASCII.GetBytes(suppliedProof.Trim());

        return expectedBytes.Length == actualBytes.Length &&
            CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    // Method: IsValidServerName
    // Purpose: Validates or evaluates is valid server name rules for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - serverName: Server name value supplied by the caller for this operation.
    // Returns: Returns true when is valid server name succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to InternalProtocol so callers do not duplicate validation, protocol, or persistence rules.
    public static bool IsValidServerName(string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName) || serverName.Length > MaximumServerNameLength)
        {
            return false;
        }

        foreach (char value in serverName)
        {
            if (char.IsLetterOrDigit(value) || value is '_' or '-' or '.')
            {
                continue;
            }

            return false;
        }

        return true;
    }

    // Method: RegistrationKeysMatch
    // Purpose: Executes the registration keys match operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - expected: Expected value supplied by the caller for this operation.
    // - actual: Actual value supplied by the caller for this operation.
    // Returns: Returns true when registration keys match succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to InternalProtocol so callers do not duplicate validation, protocol, or persistence rules.
    public static bool RegistrationKeysMatch(string expected, string actual)
    {
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
        byte[] actualBytes = Encoding.UTF8.GetBytes(actual);

        return expectedBytes.Length == actualBytes.Length &&
            CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
