
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
        Logger.Write(LogType.WARNING, $"RealmServer internal server '{remoteServerName}' disconnected. Realm status will stay unchanged until another status packet is received.", nameof(RealmInternalPacketHandler));
        return Task.CompletedTask;
    }

    private void HandleRealmStatusPacket(string remoteServerName, string[] parts)
    {
        if (parts.Length < 3)
        {
            Logger.Write(LogType.WARNING, $"Invalid REALM_STATUS packet from '{remoteServerName}'. Expected: REALM_STATUS <RealmId> <online|offline> [population].", nameof(RealmInternalPacketHandler));
            return;
        }

        if (!uint.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint realmId))
        {
            Logger.Write(LogType.WARNING, $"Invalid REALM_STATUS realm id from '{remoteServerName}': '{parts[1]}'.", nameof(RealmInternalPacketHandler));
            return;
        }

        bool online = parts[2].ToLowerInvariant() switch
        {
            "online" => true,
            "up" => true,
            "1" => true,
            "true" => true,

            "offline" => false,
            "down" => false,
            "0" => false,
            "false" => false,

            _ => false,
        };

        float? population = null;
        if (parts.Length >= 4)
        {
            if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedPopulation))
            {
                Logger.Write(LogType.WARNING, $"Invalid REALM_STATUS population from '{remoteServerName}': '{parts[3]}'.", nameof(RealmInternalPacketHandler));
                return;
            }

            population = parsedPopulation;
        }

        if (!_realmStore.TrySetRealmStatus(realmId, online, population))
        {
            Logger.Write(LogType.WARNING, $"REALM_STATUS packet from '{remoteServerName}' referenced unknown realm id {realmId}.", nameof(RealmInternalPacketHandler));
            return;
        }

        Logger.Write(LogType.NETWORK, $"Realm {realmId} status updated by '{remoteServerName}': {(online ? "online" : "offline")}{(population.HasValue ? $", population {population.Value:0.00}" : string.Empty)}.", nameof(RealmInternalPacketHandler));
    }
}
