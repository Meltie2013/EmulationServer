
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace EmulationServer.Network.Networking.Protocol;

public static class InternalProtocol
{
    public const int MaximumAuthenticationLineLength = 512;
    public const int MaximumPacketLineLength = 1024;

    public const string AuthenticationChallenge = "AUTH_CHALLENGE";
    public const string AuthenticationResponse = "AUTH_RESPONSE";
    public const string AuthenticationAccepted = "AUTH_ACCEPTED";
    public const string AuthenticationRejected = "AUTH_REJECTED";
    public const string Ping = "PING";
    public const string Pong = "PONG";
    public const string ShutdownRequest = "SHUTDOWN_REQUEST";
    public const string WorldCapacity = "WORLD_CAPACITY";

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

    public static bool RegistrationKeysMatch(string expected, string actual)
    {
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
        byte[] actualBytes = Encoding.UTF8.GetBytes(actual);

        return expectedBytes.Length == actualBytes.Length &&
            CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
