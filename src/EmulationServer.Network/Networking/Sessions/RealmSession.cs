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

using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Network.Networking.Sessions;

public sealed class RealmSession
{
    private const int ReceiveBufferSize = 4096;

    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly IRealmSessionProcessor? _sessionProcessor;
    private readonly CancellationTokenSource _disconnectCancellation = new();
    private readonly string _remoteEndPoint;

    private int _disconnectRequested;

    public Guid Id { get; } = Guid.NewGuid();

    public RealmSession(TcpClient client, IRealmSessionProcessor? sessionProcessor = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _stream = _client.GetStream();
        _sessionProcessor = sessionProcessor;
        _remoteEndPoint = _client.Client.RemoteEndPoint?.ToString() ?? "unknown endpoint";
    }

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        Logger.Write(LogType.NETWORK, $"Started processing session for {_remoteEndPoint}", nameof(RealmSession));

        using CancellationTokenSource linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _disconnectCancellation.Token);

        try
        {
            if (_sessionProcessor is not null)
            {
                RealmSessionContext context = new(Id, _client, _stream);
                await _sessionProcessor.ProcessAsync(context, linkedCancellation.Token);
                return;
            }

            await ProcessRawDebugSessionAsync(linkedCancellation.Token);
        }
        catch (OperationCanceledException) when (linkedCancellation.Token.IsCancellationRequested)
        {
            // Expected during server shutdown or explicit session disconnect.
        }
        catch (EndOfStreamException exception)
        {
            Logger.Write(LogType.NETWORK, exception.Message, nameof(RealmSession));
        }
        catch (IOException exception)
        {
            Logger.Write(LogType.NETWORK, $"Connection closed for {_remoteEndPoint}: {exception.Message}", nameof(RealmSession));
        }
        catch (SocketException exception)
        {
            Logger.Write(LogType.NETWORK, $"Socket closed for {_remoteEndPoint}: {exception.SocketErrorCode}", nameof(RealmSession));
        }
        catch (ObjectDisposedException) when (IsDisconnectRequested)
        {
            // Expected when the socket is disposed during shutdown.
        }
        catch (Exception exception)
        {
            Logger.Write(LogType.CRITICAL, exception.ToString(), nameof(RealmSession));
        }
        finally
        {
            await DisconnectAsync();
        }
    }

    public Task DisconnectAsync()
    {
        if (Interlocked.Exchange(ref _disconnectRequested, 1) == 1)
        {
            return Task.CompletedTask;
        }

        Logger.Write(LogType.NETWORK, $"Ending session for {_remoteEndPoint}", nameof(RealmSession));

        try
        {
            _disconnectCancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Ignore; shutdown is already in progress or complete.
        }

        try
        {
            _client.Client.Shutdown(SocketShutdown.Both);
        }
        catch (SocketException)
        {
            // The remote side may have already closed/reset the connection.
        }
        catch (ObjectDisposedException)
        {
            // The socket may have already been disposed.
        }

        _stream.Dispose();
        _client.Dispose();
        _disconnectCancellation.Dispose();

        return Task.CompletedTask;
    }

    private async Task ProcessRawDebugSessionAsync(CancellationToken cancellationToken)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(ReceiveBufferSize);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                int received = await _stream.ReadAsync(buffer.AsMemory(0, ReceiveBufferSize), cancellationToken);
                if (received == 0)
                {
                    Logger.Write(LogType.NETWORK, $"Client disconnected from {_remoteEndPoint}", nameof(RealmSession));
                    break;
                }

                Logger.Write(LogType.DEBUG, $"Received {received} byte(s) from {_remoteEndPoint}", nameof(RealmSession));
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private bool IsDisconnectRequested => Volatile.Read(ref _disconnectRequested) == 1;
}
