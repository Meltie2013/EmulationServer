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
// File: src/EmulationServer.Network/Networking/Protocol/InternalProtocolReader.cs
// Purpose: Contains internal protocol reader code for the packet serialization, socket transport, and protocol framing layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Buffers;
using System.Net.Sockets;
using System.Text;

namespace EmulationServer.Network.Networking.Protocol;

// Type: InternalProtocolReader
// Purpose: Provides internal protocol reader behavior for the packet serialization, socket transport, and protocol framing layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class InternalProtocolReader : IDisposable
{

    // Constant: Defines the default buffer size constant used by the packet serialization, socket transport, and protocol framing layer.
    // Value: fixed default buffer size value used anywhere this rule or protocol value is needed.
    private const int DefaultBufferSize = 4096;

    // Field: Stores the stream state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current stream backing value maintained by the owning type.
    private readonly NetworkStream _stream;

    // Field: Stores the buffer state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current buffer backing value maintained by the owning type.
    private readonly byte[] _buffer;

    // Field: Stores the offset state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current offset backing value maintained by the owning type.
    private int _offset;

    // Field: Stores the available state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current available backing value maintained by the owning type.
    private int _available;

    // Field: Stores the disposed state used by the packet serialization, socket transport, and protocol framing layer.
    // Value: current disposed backing value maintained by the owning type.
    private bool _disposed;

    // Constructor: InternalProtocolReader
    // Purpose: Initializes a new InternalProtocolReader instance with dependencies and values required by the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - stream: Stream value supplied by the caller for this operation.
    // - bufferSize: Buffer size value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to InternalProtocolReader so callers do not duplicate validation, protocol, or persistence rules.
    public InternalProtocolReader(NetworkStream stream, int bufferSize = DefaultBufferSize)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (bufferSize <= 0)
        {
            throw new ArgumentOutOfRangeException(null, "Internal protocol reader buffer size must be greater than zero.");
        }

        _stream = stream;
        _buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
    }

    // Method: ReadLineAsync
    // Purpose: Retrieves read line data for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - maximumLength: Maximum length value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to InternalProtocolReader so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task<string?> ReadLineAsync(int maximumLength, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (maximumLength <= 0)
        {
            throw new ArgumentOutOfRangeException(null, "Maximum line length must be greater than zero.");
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

    // Method: Dispose
    // Purpose: Controls the dispose lifecycle step for the packet serialization, socket transport, and protocol framing layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to InternalProtocolReader so callers do not duplicate validation, protocol, or persistence rules.
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ArrayPool<byte>.Shared.Return(_buffer);
    }

    // Method: DecodeLine
    // Purpose: Converts incoming data into decode line form for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - lineBuffer: Line buffer value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to InternalProtocolReader so callers do not duplicate validation, protocol, or persistence rules.
    private static string DecodeLine(MemoryStream lineBuffer)
    {
        return Encoding.UTF8.GetString(lineBuffer.GetBuffer(), 0, (int)lineBuffer.Length).Trim();
    }
}
