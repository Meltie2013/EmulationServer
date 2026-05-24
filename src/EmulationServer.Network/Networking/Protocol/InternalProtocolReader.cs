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

/**
 * File overview: src/EmulationServer.Network/Networking/Protocol/InternalProtocolReader.cs
 * Documents the InternalProtocolReader source file in the internal server networking, packet framing, and peer/session lifecycle area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Network.Networking.Protocol;

/**
  * Buffers incoming internal protocol bytes for a single connection.
  * This avoids one-byte socket reads while still preserving data after each newline.
  */
public sealed class InternalProtocolReader : IDisposable
{
    /**
     * Defines the constant value for default buffer size.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    private const int DefaultBufferSize = 4096;

    /**
     * Holds the private stream state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly NetworkStream _stream;
    /**
     * Holds the private buffer state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly byte[] _buffer;

    /**
     * Holds the private offset state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private int _offset;
    /**
     * Holds the private available state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private int _available;
    /**
     * Holds the private disposed state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private bool _disposed;

    /**
     * Initializes a new InternalProtocolReader instance with the dependencies required by the internal server networking, packet framing, and peer/session lifecycle workflow.
     * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
     * Inputs used by this operation: stream, bufferSize.
     */
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

    /**
     * Stops the dispose workflow and releases owned runtime resources in a controlled order.
     * Shutdown logic is centralized to avoid dangling connections, incomplete saves, or partially registered services.
     */
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ArrayPool<byte>.Shared.Return(_buffer);
    }

    /**
     * Performs the decode line operation for the internal server networking, packet framing, and peer/session lifecycle workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: lineBuffer.
     */
    private static string DecodeLine(MemoryStream lineBuffer)
    {
        return Encoding.UTF8.GetString(lineBuffer.GetBuffer(), 0, (int)lineBuffer.Length).Trim();
    }
}
