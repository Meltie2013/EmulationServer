
using System.Globalization;
using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Sessions;
using EmulationServer.RealmServer.Realms;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.RealmServer.Internal;

public sealed class RealmInternalPacketHandler
{
    private const string RealmStatusPacket = "REALM_STATUS";

    private readonly ConfiguredRealmStore _realmStore;

    private readonly object _syncRoot = new();

    private readonly Dictionary<string, HashSet<uint>> _realmsByServerName = new(StringComparer.OrdinalIgnoreCase);

    public RealmInternalPacketHandler(ConfiguredRealmStore realmStore)
    {
        _realmStore = realmStore ?? throw new ArgumentNullException(nameof(realmStore));
    }

    public InternalNetworkCallbacks CreateCallbacks()
    {
        return new InternalNetworkCallbacks
        {
            ServerAuthenticatedAsync = OnServerAuthenticatedAsync,
            PacketReceivedAsync = OnPacketReceivedAsync,
            ServerDisconnectedAsync = OnServerDisconnectedAsync,
        };
    }

    private Task OnServerAuthenticatedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        Logger.Write(LogType.NETWORK, $"RealmServer accepted internal server registration from '{remoteServerName}'.", nameof(RealmInternalPacketHandler));
        return Task.CompletedTask;
    }

    private Task OnPacketReceivedAsync(
        InternalServerSession session,
        string remoteServerName,
        string packet,
        CancellationToken cancellationToken)
    {
        string[] parts = packet.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            return Task.CompletedTask;
        }

        if (!string.Equals(parts[0], RealmStatusPacket, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        HandleRealmStatusPacket(remoteServerName, parts);
        return Task.CompletedTask;
    }

    private Task OnServerDisconnectedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        List<uint> realmIds;

        lock (_syncRoot)
        {
            if (!_realmsByServerName.Remove(remoteServerName, out HashSet<uint>? mappedRealmIds))
            {
                Logger.Write(LogType.WARNING, $"RealmServer internal server '{remoteServerName}' disconnected. No realm status mapping was registered.", nameof(RealmInternalPacketHandler));
                return Task.CompletedTask;
            }

            realmIds = mappedRealmIds.ToList();
        }

        foreach (uint realmId in realmIds)
        {
            if (_realmStore.TrySetRealmStatus(realmId, false, 0, 1))
            {
                Logger.Write(LogType.WARNING, $"Realm {realmId} marked offline because internal server '{remoteServerName}' disconnected.", nameof(RealmInternalPacketHandler));
            }
        }

        return Task.CompletedTask;
    }

    private void HandleRealmStatusPacket(string remoteServerName, string[] parts)
    {
        if (parts.Length < 5)
        {
            Logger.Write(LogType.WARNING, $"Invalid REALM_STATUS packet from '{remoteServerName}'. Expected: REALM_STATUS <realmId> <online|offline> <activeConnections> <maxConnections>.", nameof(RealmInternalPacketHandler));
            return;
        }

        if (!uint.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint realmId))
        {
            Logger.Write(LogType.WARNING, $"Invalid REALM_STATUS realm id from '{remoteServerName}': '{parts[1]}'.", nameof(RealmInternalPacketHandler));
            return;
        }

        if (!TryParseOnlineState(parts[2], out bool online))
        {
            Logger.Write(LogType.WARNING, $"Invalid REALM_STATUS state from '{remoteServerName}': '{parts[2]}'.", nameof(RealmInternalPacketHandler));
            return;
        }

        if (!int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int activeConnections))
        {
            Logger.Write(LogType.WARNING, $"Invalid REALM_STATUS active connection count from '{remoteServerName}': '{parts[3]}'.", nameof(RealmInternalPacketHandler));
            return;
        }

        if (!int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int maxConnections))
        {
            Logger.Write(LogType.WARNING, $"Invalid REALM_STATUS max connection count from '{remoteServerName}': '{parts[4]}'.", nameof(RealmInternalPacketHandler));
            return;
        }

        if (!_realmStore.TrySetRealmStatus(realmId, online, activeConnections, maxConnections))
        {
            Logger.Write(LogType.WARNING, $"REALM_STATUS packet from '{remoteServerName}' referenced unknown realm id {realmId}.", nameof(RealmInternalPacketHandler));
            return;
        }

        lock (_syncRoot)
        {
            if (!_realmsByServerName.TryGetValue(remoteServerName, out HashSet<uint>? realmIds))
            {
                realmIds = [];
                _realmsByServerName[remoteServerName] = realmIds;
            }

            realmIds.Add(realmId);
        }

        float population = RealmPopulationCalculator.Calculate(activeConnections, maxConnections);

        Logger.Write(LogType.NETWORK, $"Realm {realmId} status updated by '{remoteServerName}': {(online ? "online" : "offline")}, active connections {Math.Max(0, activeConnections)}/{Math.Max(1, maxConnections)}, population {population:0.00}.", nameof(RealmInternalPacketHandler));
    }

    private static bool TryParseOnlineState(string value, out bool online)
    {
        switch (value.ToLowerInvariant())
        {
            case "online":
            case "up":
            case "1":
            case "true":
                online = true;
                return true;

            case "offline":
            case "down":
            case "0":
            case "false":
                online = false;
                return true;

            default:
                online = false;
                return false;
        }
    }
}
