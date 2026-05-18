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
using EmulationServer.Network.Networking.Health;
using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.WorldServer.Configuration;

/**
  * File overview: src/WorldServer/Internal/WorldRealmStatusReporter.cs
  * This file belongs to the project runtime logic and supporting data models portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.WorldServer.Internal;

/**
  * Represents the world realm status reporter component in the project runtime logic and supporting data models area.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public sealed class WorldRealmStatusReporter : IAsyncDisposable
{
    /**
      * Stores the settings dependency or runtime value for WorldRealmStatusReporter.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly RealmStatusSettings _settings;
    /**
      * Stores the registration key dependency or runtime value for WorldRealmStatusReporter.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly string _registrationKey;
    /**
      * Stores the send lock dependency or runtime value for WorldRealmStatusReporter.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    /**
      * Stores the max connections dependency or runtime value for WorldRealmStatusReporter.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly int _maxConnections;
    /**
      * Stores the latency report interval dependency or runtime value for WorldRealmStatusReporter.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly TimeSpan _latencyReportInterval;
    /**
      * Stores the ping timeout dependency or runtime value for WorldRealmStatusReporter.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly TimeSpan _pingTimeout;

    /**
      * Stores the stop cancellation dependency or runtime value for WorldRealmStatusReporter.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private CancellationTokenSource? _stopCancellation;
    /**
      * Stores the report task dependency or runtime value for WorldRealmStatusReporter.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private Task? _reportTask;
    /**
      * Stores the client dependency or runtime value for WorldRealmStatusReporter.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private TcpClient? _client;
    /**
      * Stores the stream dependency or runtime value for WorldRealmStatusReporter.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private NetworkStream? _stream;
    /**
      * Stores the started dependency or runtime value for WorldRealmStatusReporter.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private int _started;
    /**
      * Stores the active connections dependency or runtime value for WorldRealmStatusReporter.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private int _activeConnections;

    /**
      * Creates a new WorldRealmStatusReporter instance and stores the dependencies required by the component.
      * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
      */
    public WorldRealmStatusReporter(
        RealmStatusSettings settings,
        string registrationKey,
        int maxConnections,
        TimeSpan latencyReportInterval,
        TimeSpan pingTimeout)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        if (string.IsNullOrWhiteSpace(registrationKey))
        {
            throw new ArgumentException("Registration key is required.", nameof(registrationKey));
        }

        if (maxConnections <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxConnections), "WorldServer max connections must be greater than zero.");
        }

        if (latencyReportInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(latencyReportInterval), "Latency report interval must be greater than zero.");
        }

        if (pingTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(pingTimeout), "Ping timeout must be greater than zero.");
        }

        _settings.Validate();
        _registrationKey = registrationKey;
        _maxConnections = maxConnections;
        _latencyReportInterval = latencyReportInterval;
        _pingTimeout = pingTimeout;
    }

    /**
      * Starts the component and prepares the runtime state required before it can accept work.
      * The method is part of WorldRealmStatusReporter and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_settings.Enabled)
        {
            Logger.Write(LogType.NETWORK, "WorldServer realm status reporting is disabled.", nameof(WorldRealmStatusReporter));
            return Task.CompletedTask;
        }

        if (Interlocked.Exchange(ref _started, 1) == 1)
        {
            throw new InvalidOperationException("WorldServer realm status reporter has already been started.");
        }

        _stopCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _reportTask = Task.Run(() => RunAsync(_stopCancellation.Token), CancellationToken.None);

        Logger.Write(LogType.NETWORK, $"WorldServer realm status reporter started for realm {_settings.RealmId}.", nameof(WorldRealmStatusReporter));

        return Task.CompletedTask;
    }

    /**
      * Stops the component and releases runtime resources in a controlled order.
      * The method is part of WorldRealmStatusReporter and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _started, 0) == 0)
        {
            return;
        }

        try
        {
            if (_stream is not null)
            {
                await SendRealmStatusAsync(false, 0, cancellationToken);
            }
        }
        catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
        {
            Logger.Write(LogType.WARNING, $"Unable to send offline realm status before shutdown: {exception.Message}", nameof(WorldRealmStatusReporter));
        }

        if (_stopCancellation is not null)
        {
            await _stopCancellation.CancelAsync();
        }

        if (_reportTask is not null)
        {
            try
            {
                await _reportTask;
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
        }

        CleanupConnection();

        _stopCancellation?.Dispose();
        _stopCancellation = null;
        _reportTask = null;

        Logger.Write(LogType.NETWORK, "WorldServer realm status reporter stopped.", nameof(WorldRealmStatusReporter));
    }

    /**
      * Releases owned resources and ensures background work is stopped safely.
      * The method is part of WorldRealmStatusReporter and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        _sendLock.Dispose();
    }

    /**
      * Runs the main loop for this component until cancellation or shutdown is requested.
      * The method is part of WorldRealmStatusReporter and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndAuthenticateAsync(cancellationToken);

                NetworkStream stream = _stream ?? throw new IOException("RealmServer connection is not available.");
                await using InternalLatencyMonitor latencyMonitor = new(
                    "WorldServer",
                    "RealmServer",
                    stream,
                    _sendLock,
                    _latencyReportInterval,
                    _pingTimeout);

                latencyMonitor.Start(cancellationToken);

                Task receiveTask = ProcessRealmServerPacketsAsync(latencyMonitor, cancellationToken);
                Task statusTask = SendRealmStatusLoopAsync(cancellationToken);

                Task completedTask = await Task.WhenAny(receiveTask, statusTask);
                await completedTask;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                Logger.Write(LogType.WARNING, $"WorldServer could not update RealmServer status: {exception.Message}", nameof(WorldRealmStatusReporter));
                CleanupConnection();

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    /**
      * Sends a protocol message or status update to a connected peer.
      * The method is part of WorldRealmStatusReporter and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task SendRealmStatusLoopAsync(CancellationToken cancellationToken)
    {
        await SendRealmStatusAsync(true, Volatile.Read(ref _activeConnections), cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(_settings.UpdateInterval, cancellationToken);
            await SendRealmStatusAsync(true, Volatile.Read(ref _activeConnections), cancellationToken);
        }
    }

    /**
      * Processes incoming data and dispatches it to the correct subsystem handler.
      * The method is part of WorldRealmStatusReporter and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task ProcessRealmServerPacketsAsync(InternalLatencyMonitor latencyMonitor, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            NetworkStream stream = _stream ?? throw new IOException("RealmServer connection is not available.");

            string? line = await InternalProtocol.ReadLineAsync(
                stream,
                InternalProtocol.MaximumPacketLineLength,
                cancellationToken);

            if (line is null)
            {
                throw new IOException("RealmServer disconnected from WorldServer realm status reporter.");
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            await ProcessRealmServerPacketAsync(line, latencyMonitor, cancellationToken);
        }
    }

    /**
      * Processes incoming data and dispatches it to the correct subsystem handler.
      * The method is part of WorldRealmStatusReporter and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task ProcessRealmServerPacketAsync(string line, InternalLatencyMonitor latencyMonitor, CancellationToken cancellationToken)
    {
        string[] parts = line.Split(' ', 3, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            return;
        }

        if (parts.Length >= 2 && string.Equals(parts[0], InternalProtocol.Ping, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.TRACE, "WorldServer received PING packet from RealmServer.", nameof(WorldRealmStatusReporter));
            await latencyMonitor.RespondToPingAsync(parts[1], cancellationToken);
            return;
        }

        if (parts.Length >= 2 && string.Equals(parts[0], InternalProtocol.Pong, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.TRACE, "WorldServer received PONG packet from RealmServer.", nameof(WorldRealmStatusReporter));
            latencyMonitor.RecordPong(parts[1]);
            return;
        }

        if (parts.Length >= 2 && string.Equals(parts[0], InternalProtocol.ShutdownRequest, StringComparison.OrdinalIgnoreCase))
        {
            string reason = parts.Length == 3 ? parts[2] : "No reason provided.";
            Logger.Write(LogType.WARNING, $"WorldServer received shutdown request from {parts[1]}: {reason}", nameof(WorldRealmStatusReporter));
            return;
        }

        Logger.Write(LogType.DEBUG, $"WorldServer received RealmServer internal packet: {line}", nameof(WorldRealmStatusReporter));
    }

    /**
      * Creates or restores an internal network connection to the target server.
      * The method is part of WorldRealmStatusReporter and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task ConnectAndAuthenticateAsync(CancellationToken cancellationToken)
    {
        CleanupConnection();

        _client = new TcpClient
        {
            NoDelay = true,
            ReceiveBufferSize = 8192,
            SendBufferSize = 8192,
        };

        Logger.Write(LogType.NETWORK, $"WorldServer connecting to RealmServer internal listener at {_settings.RealmServerHost}:{_settings.RealmServerPort}...", nameof(WorldRealmStatusReporter));

        await _client.ConnectAsync(_settings.RealmServerHost, _settings.RealmServerPort, cancellationToken);
        _stream = _client.GetStream();

        string? challenge = await InternalProtocol.ReadLineAsync(
            _stream,
            InternalProtocol.MaximumAuthenticationLineLength,
            cancellationToken);

        if (challenge is null)
        {
            throw new InvalidOperationException("RealmServer disconnected before authentication challenge.");
        }

        string[] challengeParts = challenge.Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (challengeParts.Length != 2 || !string.Equals(challengeParts[0], InternalProtocol.AuthenticationChallenge, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("RealmServer sent an invalid authentication challenge.");
        }

        await InternalProtocol.WriteLineAsync(
            _stream,
            _sendLock,
            $"{InternalProtocol.AuthenticationResponse} WorldServer {_registrationKey}",
            cancellationToken);

        string? response = await InternalProtocol.ReadLineAsync(
            _stream,
            InternalProtocol.MaximumAuthenticationLineLength,
            cancellationToken);

        if (response is null)
        {
            throw new InvalidOperationException("RealmServer disconnected before accepting authentication.");
        }

        if (!response.StartsWith($"{InternalProtocol.AuthenticationAccepted} ", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("RealmServer rejected WorldServer authentication.");
        }

        Logger.Write(LogType.NETWORK, "WorldServer authenticated with RealmServer internal listener.", nameof(WorldRealmStatusReporter));
    }

    /**
      * Sends a protocol message or status update to a connected peer.
      * The method is part of WorldRealmStatusReporter and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task SendRealmStatusAsync(bool online, int activeConnections, CancellationToken cancellationToken)
    {
        if (_stream is null)
        {
            return;
        }

        int safeActiveConnections = Math.Max(0, activeConnections);
        int safeMaxConnections = Math.Max(1, _maxConnections);
        string state = online ? "online" : "offline";

        string packet = $"REALM_STATUS {_settings.RealmId} {state} {safeActiveConnections} {safeMaxConnections}";

        await InternalProtocol.WriteLineAsync(
            _stream,
            _sendLock,
            packet,
            cancellationToken);

        Logger.Write(LogType.NETWORK, $"WorldServer sent realm status: {packet}", nameof(WorldRealmStatusReporter));
    }

    /**
      * Performs the cleanup connection operation for WorldRealmStatusReporter.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
    private void CleanupConnection()
    {
        try
        {
            _stream?.Dispose();
        }
        catch
        {
            // Ignore cleanup exceptions.
        }

        try
        {
            _client?.Dispose();
        }
        catch
        {
            // Ignore cleanup exceptions.
        }

        _stream = null;
        _client = null;
    }
}
