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

using System.Globalization;
using EmulationServer.Network.Networking.Callbacks;
using EmulationServer.Network.Networking.Protocol;
using EmulationServer.Network.Networking.Sessions;
using EmulationServer.RealmServer.Realms;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

/**
  * File overview: src/RealmServer/Internal/RealmInternalPacketHandler.cs
  * This file belongs to the project runtime logic and supporting data models portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.RealmServer.Internal;

/**
  * Represents the realm internal packet handler component in the project runtime logic and supporting data models area.
  * It handles a specific protocol or command path and keeps higher-level server flow readable.
  */
public sealed class RealmInternalPacketHandler
{
    private const string RealmStatusPacket = "REALM_STATUS";

    /**
      * Stores the realm store dependency or runtime value for RealmInternalPacketHandler.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly ConfiguredRealmStore _realmStore;

    /**
      * Stores the sync root dependency or runtime value for RealmInternalPacketHandler.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly object _syncRoot = new();

    private readonly Dictionary<string, HashSet<uint>> _realmsByServerName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<uint, Dictionary<uint, byte>>> _pendingCharacterCounts = new(StringComparer.OrdinalIgnoreCase);

    /**
      * Creates a new RealmInternalPacketHandler instance and stores the dependencies required by the component.
      * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
      */
    public RealmInternalPacketHandler(ConfiguredRealmStore realmStore)
    {
        _realmStore = realmStore ?? throw new ArgumentNullException(nameof(realmStore));
    }

    /**
      * Creates a new object with validated defaults so callers receive a ready-to-use instance.
      * The method is part of RealmInternalPacketHandler and keeps this workflow isolated from the caller.
      */
    public InternalNetworkCallbacks CreateCallbacks()
    {
        return new InternalNetworkCallbacks
        {
            ServerAuthenticatedAsync = OnServerAuthenticatedAsync,
            PacketReceivedAsync = OnPacketReceivedAsync,
            ServerDisconnectedAsync = OnServerDisconnectedAsync,
        };
    }

    /**
      * Performs the on server authenticated async operation for RealmInternalPacketHandler.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    private Task OnServerAuthenticatedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        Logger.Write(LogType.NETWORK, $"RealmServer accepted internal server registration from '{remoteServerName}'.", nameof(RealmInternalPacketHandler));
        return Task.CompletedTask;
    }

    /**
      * Performs the on packet received async operation for RealmInternalPacketHandler.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
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

        if (string.Equals(parts[0], RealmStatusPacket, StringComparison.OrdinalIgnoreCase))
        {
            HandleRealmStatusPacket(remoteServerName, parts);
            return Task.CompletedTask;
        }

        if (string.Equals(parts[0], InternalProtocol.RealmCharacterCountSnapshotBegin, StringComparison.OrdinalIgnoreCase))
        {
            HandleCharacterCountSnapshotBegin(remoteServerName, parts);
            return Task.CompletedTask;
        }

        if (string.Equals(parts[0], InternalProtocol.RealmCharacterCountSnapshotData, StringComparison.OrdinalIgnoreCase))
        {
            HandleCharacterCountSnapshotData(remoteServerName, parts);
            return Task.CompletedTask;
        }

        if (string.Equals(parts[0], InternalProtocol.RealmCharacterCountSnapshotEnd, StringComparison.OrdinalIgnoreCase))
        {
            HandleCharacterCountSnapshotEnd(remoteServerName, parts);
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }

    /**
      * Performs the on server disconnected async operation for RealmInternalPacketHandler.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    private Task OnServerDisconnectedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        List<uint> realmIds;

        lock (_syncRoot)
        {
            _pendingCharacterCounts.Remove(remoteServerName);

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

    /**
      * Starts a new character-count snapshot from a WorldServer.
      */
    private void HandleCharacterCountSnapshotBegin(string remoteServerName, string[] parts)
    {
        if (parts.Length != 2 || !uint.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint realmId))
        {
            Logger.Write(LogType.WARNING, $"Invalid {InternalProtocol.RealmCharacterCountSnapshotBegin} packet from '{remoteServerName}'.", nameof(RealmInternalPacketHandler));
            return;
        }

        lock (_syncRoot)
        {
            if (!_pendingCharacterCounts.TryGetValue(remoteServerName, out Dictionary<uint, Dictionary<uint, byte>>? snapshotsByRealm))
            {
                snapshotsByRealm = [];
                _pendingCharacterCounts[remoteServerName] = snapshotsByRealm;
            }

            snapshotsByRealm[realmId] = [];
        }
    }

    /**
      * Adds one data chunk to the pending character-count snapshot.
      */
    private void HandleCharacterCountSnapshotData(string remoteServerName, string[] parts)
    {
        if (parts.Length < 2 || !uint.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint realmId))
        {
            Logger.Write(LogType.WARNING, $"Invalid {InternalProtocol.RealmCharacterCountSnapshotData} packet from '{remoteServerName}'.", nameof(RealmInternalPacketHandler));
            return;
        }

        lock (_syncRoot)
        {
            if (!_pendingCharacterCounts.TryGetValue(remoteServerName, out Dictionary<uint, Dictionary<uint, byte>>? snapshotsByRealm) ||
                !snapshotsByRealm.TryGetValue(realmId, out Dictionary<uint, byte>? characterCounts))
            {
                Logger.Write(LogType.WARNING, $"Received {InternalProtocol.RealmCharacterCountSnapshotData} from '{remoteServerName}' before snapshot begin for realm {realmId}.", nameof(RealmInternalPacketHandler));
                return;
            }

            for (int index = 2; index < parts.Length; index++)
            {
                string[] pair = parts[index].Split(':', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (pair.Length != 2 ||
                    !uint.TryParse(pair[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint accountId) ||
                    !byte.TryParse(pair[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte count))
                {
                    Logger.Write(LogType.WARNING, $"Invalid character-count pair '{parts[index]}' from '{remoteServerName}'.", nameof(RealmInternalPacketHandler));
                    continue;
                }

                characterCounts[accountId] = count;
            }
        }
    }

    /**
      * Completes and publishes the pending character-count snapshot for a realm.
      */
    private void HandleCharacterCountSnapshotEnd(string remoteServerName, string[] parts)
    {
        if (parts.Length != 2 || !uint.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint realmId))
        {
            Logger.Write(LogType.WARNING, $"Invalid {InternalProtocol.RealmCharacterCountSnapshotEnd} packet from '{remoteServerName}'.", nameof(RealmInternalPacketHandler));
            return;
        }

        Dictionary<uint, byte>? snapshot;
        lock (_syncRoot)
        {
            if (!_pendingCharacterCounts.TryGetValue(remoteServerName, out Dictionary<uint, Dictionary<uint, byte>>? snapshotsByRealm) ||
                !snapshotsByRealm.Remove(realmId, out snapshot))
            {
                Logger.Write(LogType.WARNING, $"Received {InternalProtocol.RealmCharacterCountSnapshotEnd} from '{remoteServerName}' before snapshot data for realm {realmId}.", nameof(RealmInternalPacketHandler));
                return;
            }
        }

        if (snapshot is null || !_realmStore.TryReplaceRealmCharacterCounts(realmId, snapshot))
        {
            Logger.Write(LogType.WARNING, $"Character-count snapshot from '{remoteServerName}' referenced unknown realm id {realmId}.", nameof(RealmInternalPacketHandler));
            return;
        }

        Logger.Write(LogType.NETWORK, $"Realm {realmId} character-count snapshot updated by '{remoteServerName}': {snapshot.Count} account(s).", nameof(RealmInternalPacketHandler));
    }

    /**
      * Handles a single operation or packet and keeps the calling code focused on flow control.
      * The method is part of RealmInternalPacketHandler and keeps this workflow isolated from the caller.
      */
    private void HandleRealmStatusPacket(string remoteServerName, string[] parts)
    {
        if (parts.Length < 5)
        {
            Logger.Write(LogType.WARNING, $"Invalid REALM_STATUS packet from '{remoteServerName}'. Expected: REALM_STATUS <realmId> <online|offline> <activeConnections> <capacityLimit>.", nameof(RealmInternalPacketHandler));
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

        if (!int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int capacityLimit))
        {
            Logger.Write(LogType.WARNING, $"Invalid REALM_STATUS capacity limit from '{remoteServerName}': '{parts[4]}'.", nameof(RealmInternalPacketHandler));
            return;
        }

        if (!_realmStore.TrySetRealmStatus(realmId, online, activeConnections, capacityLimit))
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

        float population = RealmPopulationCalculator.Calculate(activeConnections, capacityLimit);

        Logger.Write(LogType.NETWORK, $"Realm {realmId} status updated by '{remoteServerName}': {(online ? "online" : "offline")}, active connections {Math.Max(0, activeConnections)}/{Math.Max(1, capacityLimit)}, population {population:0.00}.", nameof(RealmInternalPacketHandler));
    }

    /**
      * Attempts the operation without treating a normal failure as an exceptional condition.
      * The method is part of RealmInternalPacketHandler and keeps this workflow isolated from the caller.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
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
