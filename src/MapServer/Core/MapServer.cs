using System.Globalization;

using EmulationServer.Core.Servers;
using EmulationServer.MapServer.Configuration;
using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Peers;
using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.MapServer.Core;

public sealed class MapServer : IAsyncDisposable
{
    private readonly EmulationServerHost _host;
    private int _worldCapacityLimit;

    public MapServer(MapServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _host = new EmulationServerHost(nameof(MapServer), settings.InternalNetwork, CreateCallbacks());
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _host.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return _host.StopAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        await _host.DisposeAsync();
    }

    private InternalNetworkCallbacks CreateCallbacks()
    {
        return new InternalNetworkCallbacks
        {
            PeerPacketReceivedAsync = OnPeerPacketReceivedAsync,
        };
    }

    private Task OnPeerPacketReceivedAsync(
        InternalPeerConnection connection,
        string remoteServerName,
        string packet,
        CancellationToken cancellationToken)
    {
        string[] parts = packet.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 3 || !string.Equals(parts[0], InternalProtocol.WorldCapacity, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int capacityLimit) || capacityLimit <= 0)
        {
            Logger.Write(LogType.WARNING, $"MapServer received invalid WorldServer capacity packet from {remoteServerName}: {packet}", nameof(MapServer));
            return Task.CompletedTask;
        }

        Volatile.Write(ref _worldCapacityLimit, capacityLimit);
        Logger.Write(LogType.NETWORK, $"MapServer received WorldServer capacity limit from {remoteServerName}: {parts[1]}={capacityLimit}.", nameof(MapServer));

        return Task.CompletedTask;
    }
}
