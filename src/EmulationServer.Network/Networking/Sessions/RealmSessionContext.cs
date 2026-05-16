
using System.Net;
using System.Net.Sockets;

namespace EmulationServer.Network.Networking.Sessions;

public sealed class RealmSessionContext
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;

    public RealmSessionContext(Guid sessionId, TcpClient client, NetworkStream stream)
    {
        Id = sessionId;
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));

        RemoteEndPoint = _client.Client.RemoteEndPoint?.ToString() ?? "unknown endpoint";
        RemoteAddress = (_client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "0.0.0.0";
    }

    public Guid Id { get; }

    public string RemoteEndPoint { get; }

    public string RemoteAddress { get; }

    public async ValueTask<byte> ReadByteAsync(CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[1];
        await ReadExactlyAsync(buffer, cancellationToken);
        return buffer[0];
    }

    public async ValueTask<byte[]> ReadBytesAsync(int length, CancellationToken cancellationToken)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Read length cannot be negative.");
        }

        byte[] buffer = new byte[length];
        await ReadExactlyAsync(buffer, cancellationToken);
        return buffer;
    }

    public async ValueTask ReadExactlyAsync(byte[] buffer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        int offset = 0;
        while (offset < buffer.Length)
        {
            int received = await _stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken);
            if (received == 0)
            {
                throw new EndOfStreamException($"Client disconnected from {RemoteEndPoint}.");
            }

            offset += received;
        }
    }

    public ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        return _stream.WriteAsync(data, cancellationToken);
    }
}
