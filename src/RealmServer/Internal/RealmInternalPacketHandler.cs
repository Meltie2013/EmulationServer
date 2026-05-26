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
  * Documents the RealmInternalPacketHandler source file in the realm authentication, realm-list handling, and external client login services area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.RealmServer.Internal;

/**
  * Owns the realm internal packet handler behavior for the realm authentication, realm-list handling, and external client login services layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class RealmInternalPacketHandler
{
    /**
      * Defines the constant value for realm status packet.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const string RealmStatusPacket = "REALM_STATUS";

    /**
      * Holds the private realm store state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly ConfiguredRealmStore _realmStore;

    /**
      * Holds the private sync root state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly object _syncRoot = new();

    private readonly Dictionary<string, HashSet<uint>> _realmsByServerName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<uint, Dictionary<uint, byte>>> _pendingCharacterCounts = new(StringComparer.OrdinalIgnoreCase);

    /**
      * Initializes a new RealmInternalPacketHandler instance with the dependencies required by the realm authentication, realm-list handling, and external client login services workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: realmStore.
      */
    public RealmInternalPacketHandler(ConfiguredRealmStore realmStore)
    {
        _realmStore = realmStore ?? throw new ArgumentNullException();
    }

    /**
      * Creates the callbacks result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
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
      * Handles the on server authenticated event for the realm authentication, realm-list handling, and external client login services workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: session, remoteServerName, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    private Task OnServerAuthenticatedAsync(
        InternalServerSession session,
        string remoteServerName,
        CancellationToken cancellationToken)
    {
        Logger.Write(LogType.NETWORK, $"RealmServer accepted internal server registration from '{remoteServerName}'.", "RealmInternalPacketHandler");
        return Task.CompletedTask;
    }

    /**
      * Handles the on packet received event for the realm authentication, realm-list handling, and external client login services workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: session, remoteServerName, packet, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
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
      * Handles the on server disconnected event for the realm authentication, realm-list handling, and external client login services workflow.
      * The handler updates local state first, then performs any required packet/database work so the component remains consistent when errors occur.
      * Inputs used by this operation: session, remoteServerName, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
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
                Logger.Write(LogType.WARNING, $"RealmServer internal server '{remoteServerName}' disconnected. No realm status mapping was registered.", "RealmInternalPacketHandler");
                return Task.CompletedTask;
            }

            realmIds = mappedRealmIds.ToList();
        }

        foreach (uint realmId in realmIds)
        {
            if (_realmStore.TrySetRealmStatus(realmId, false, 0, 1))
            {
                Logger.Write(LogType.WARNING, $"Realm {realmId} marked offline because internal server '{remoteServerName}' disconnected.", "RealmInternalPacketHandler");
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
            Logger.Write(LogType.WARNING, $"Invalid {InternalProtocol.RealmCharacterCountSnapshotBegin} packet from '{remoteServerName}'.", "RealmInternalPacketHandler");
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
            Logger.Write(LogType.WARNING, $"Invalid {InternalProtocol.RealmCharacterCountSnapshotData} packet from '{remoteServerName}'.", "RealmInternalPacketHandler");
            return;
        }

        lock (_syncRoot)
        {
            if (!_pendingCharacterCounts.TryGetValue(remoteServerName, out Dictionary<uint, Dictionary<uint, byte>>? snapshotsByRealm) ||
                !snapshotsByRealm.TryGetValue(realmId, out Dictionary<uint, byte>? characterCounts))
            {
                Logger.Write(LogType.WARNING, $"Received {InternalProtocol.RealmCharacterCountSnapshotData} from '{remoteServerName}' before snapshot begin for realm {realmId}.", "RealmInternalPacketHandler");
                return;
            }

            for (int index = 2; index < parts.Length; index++)
            {
                string[] pair = parts[index].Split(':', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (pair.Length != 2 ||
                    !uint.TryParse(pair[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint accountId) ||
                    !byte.TryParse(pair[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte count))
                {
                    Logger.Write(LogType.WARNING, $"Invalid character-count pair '{parts[index]}' from '{remoteServerName}'.", "RealmInternalPacketHandler");
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
            Logger.Write(LogType.WARNING, $"Invalid {InternalProtocol.RealmCharacterCountSnapshotEnd} packet from '{remoteServerName}'.", "RealmInternalPacketHandler");
            return;
        }

        Dictionary<uint, byte>? snapshot;
        lock (_syncRoot)
        {
            if (!_pendingCharacterCounts.TryGetValue(remoteServerName, out Dictionary<uint, Dictionary<uint, byte>>? snapshotsByRealm) ||
                !snapshotsByRealm.Remove(realmId, out snapshot))
            {
                Logger.Write(LogType.WARNING, $"Received {InternalProtocol.RealmCharacterCountSnapshotEnd} from '{remoteServerName}' before snapshot data for realm {realmId}.", "RealmInternalPacketHandler");
                return;
            }
        }

        if (snapshot is null || !_realmStore.TryReplaceRealmCharacterCounts(realmId, snapshot))
        {
            Logger.Write(LogType.WARNING, $"Character-count snapshot from '{remoteServerName}' referenced unknown realm id {realmId}.", "RealmInternalPacketHandler");
            return;
        }

        Logger.Write(LogType.NETWORK, $"Realm {realmId} character-count snapshot updated by '{remoteServerName}': {snapshot.Count} account(s).", "RealmInternalPacketHandler");
    }

    /**
      * Handles a single operation or packet and keeps the calling code focused on flow control.
      * The method is part of RealmInternalPacketHandler and keeps this workflow isolated from the caller.
      */
    private void HandleRealmStatusPacket(string remoteServerName, string[] parts)
    {
        if (parts.Length < 5)
        {
            Logger.Write(LogType.WARNING, $"Invalid REALM_STATUS packet from '{remoteServerName}'. Expected: REALM_STATUS <realmId> <online|offline> <activeConnections> <capacityLimit>.", "RealmInternalPacketHandler");
            return;
        }

        if (!uint.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint realmId))
        {
            Logger.Write(LogType.WARNING, $"Invalid REALM_STATUS realm id from '{remoteServerName}': '{parts[1]}'.", "RealmInternalPacketHandler");
            return;
        }

        if (!TryParseOnlineState(parts[2], out bool online))
        {
            Logger.Write(LogType.WARNING, $"Invalid REALM_STATUS state from '{remoteServerName}': '{parts[2]}'.", "RealmInternalPacketHandler");
            return;
        }

        if (!int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int activeConnections))
        {
            Logger.Write(LogType.WARNING, $"Invalid REALM_STATUS active connection count from '{remoteServerName}': '{parts[3]}'.", "RealmInternalPacketHandler");
            return;
        }

        if (!int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int capacityLimit))
        {
            Logger.Write(LogType.WARNING, $"Invalid REALM_STATUS capacity limit from '{remoteServerName}': '{parts[4]}'.", "RealmInternalPacketHandler");
            return;
        }

        if (!_realmStore.TrySetRealmStatus(realmId, online, activeConnections, capacityLimit))
        {
            Logger.Write(LogType.WARNING, $"REALM_STATUS packet from '{remoteServerName}' referenced unknown realm id {realmId}.", "RealmInternalPacketHandler");
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

        Logger.Write(LogType.TRACE, $"Realm {realmId} status updated by '{remoteServerName}': {(online ? "online" : "offline")}, active connections {Math.Max(0, activeConnections)}/{Math.Max(1, capacityLimit)}, population {population:0.00}.", "RealmInternalPacketHandler");
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
