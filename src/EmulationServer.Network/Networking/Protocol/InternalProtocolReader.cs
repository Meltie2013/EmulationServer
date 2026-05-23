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

using System.Buffers;
using System.Net.Sockets;
using System.Text;

namespace EmulationServer.Network.Networking.Protocol;

/**
  * Buffers incoming internal protocol bytes for a single connection.
  * This avoids one-byte socket reads while still preserving data after each newline.
  */
public sealed class InternalProtocolReader : IDisposable
{
    private const int DefaultBufferSize = 4096;

    private readonly NetworkStream _stream;
    private readonly byte[] _buffer;

    private int _offset;
    private int _available;
    private bool _disposed;

    public InternalProtocolReader(NetworkStream stream, int bufferSize = DefaultBufferSize)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (bufferSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bufferSize), "Internal protocol reader buffer size must be greater than zero.");
        }

        _stream = stream;
        _buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
    }

    /**
      * Reads one newline-terminated UTF-8 protocol line while preserving buffered bytes for the next read.
      */
    public async Task<string?> ReadLineAsync(int maximumLength, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (maximumLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumLength), "Maximum line length must be greater than zero.");
        }

        using MemoryStream lineBuffer = new(Math.Min(maximumLength, _buffer.Length));

        while (true)
        {
            if (_offset >= _available)
            {
                int received = await _stream.ReadAsync(_buffer.AsMemory(0, _buffer.Length), cancellationToken);
                if (received == 0)
                {
                    return lineBuffer.Length == 0
                        ? null
                        : DecodeLine(lineBuffer);
                }

                _offset = 0;
                _available = received;
            }

            while (_offset < _available)
            {
                byte value = _buffer[_offset++];

                if (value == '\n')
                {
                    return DecodeLine(lineBuffer);
                }

                if (value == '\r')
                {
                    continue;
                }

                if (lineBuffer.Length >= maximumLength)
                {
                    throw new InvalidOperationException($"Internal protocol packet is too long. Maximum length is {maximumLength} byte(s).");
                }

                lineBuffer.WriteByte(value);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ArrayPool<byte>.Shared.Return(_buffer);
    }

    private static string DecodeLine(MemoryStream lineBuffer)
    {
        return Encoding.UTF8.GetString(lineBuffer.GetBuffer(), 0, (int)lineBuffer.Length).Trim();
    }
}
