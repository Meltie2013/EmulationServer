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

using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

/**
  * File overview: src/EmulationServer.Game/Maps/Runtime/MapPlayerRuntimeLogger.cs
  * Documents the MapPlayerRuntimeLogger source file in the runtime map-player state tracking area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Maps.Runtime;

/**
  * Writes graceful map and zone transition logs for runtime player tracking.
  * The helper logs only confirmed state transitions so high-frequency movement packets do not spam console or file output.
  */
public static class MapPlayerRuntimeLogger
{
    /**
      * Logs a player entering the currently owned map and zone.
      */
    public static void LogPlayerEntered(string category, string remoteServerName, MapPlayerRuntimeState player, int activePlayerCount)
    {
        ArgumentNullException.ThrowIfNull(player);

        string displayName = FormatPlayerName(player);
        Logger.Write(LogType.NETWORK, $"{category} player '{displayName}' ({player.Guid}) entered map {player.Map} from {remoteServerName}. Active players={activePlayerCount}.", category);
        Logger.Write(LogType.NETWORK, $"{category} player '{displayName}' ({player.Guid}) entered zone {player.Zone} on map {player.Map} from {remoteServerName}.", category);
    }

    /**
      * Logs a player leaving the last tracked zone and map.
      */
    public static void LogPlayerLeft(string category, string remoteServerName, MapPlayerRuntimeState player, int activePlayerCount)
    {
        ArgumentNullException.ThrowIfNull(player);

        string displayName = FormatPlayerName(player);
        Logger.Write(LogType.NETWORK, $"{category} player '{displayName}' ({player.Guid}) left zone {player.Zone} on map {player.Map} from {remoteServerName}.", category);
        Logger.Write(LogType.NETWORK, $"{category} player '{displayName}' ({player.Guid}) left map {player.Map} from {remoteServerName}. Active players={activePlayerCount}.", category);
    }

    /**
      * Logs confirmed map and zone transitions after movement or zone-update routing changes the tracked state.
      */
    public static void LogPlayerTransition(string category, string remoteServerName, MapPlayerRuntimeState? previousPlayer, MapPlayerRuntimeState currentPlayer, int activePlayerCount)
    {
        ArgumentNullException.ThrowIfNull(currentPlayer);
        if (previousPlayer is null)
        {
            return;
        }

        bool mapChanged = previousPlayer.Map != currentPlayer.Map;
        bool zoneChanged = previousPlayer.Zone != currentPlayer.Zone;
        if (!mapChanged && !zoneChanged)
        {
            return;
        }

        string displayName = FormatPlayerName(currentPlayer, previousPlayer);
        if (mapChanged || zoneChanged)
        {
            Logger.Write(LogType.NETWORK, $"{category} player '{displayName}' ({currentPlayer.Guid}) left zone {previousPlayer.Zone} on map {previousPlayer.Map} from {remoteServerName}.", category);
        }

        if (mapChanged)
        {
            Logger.Write(LogType.NETWORK, $"{category} player '{displayName}' ({currentPlayer.Guid}) left map {previousPlayer.Map} from {remoteServerName}. Active players={activePlayerCount}.", category);
            Logger.Write(LogType.NETWORK, $"{category} player '{displayName}' ({currentPlayer.Guid}) entered map {currentPlayer.Map} from {remoteServerName}. Active players={activePlayerCount}.", category);
        }

        if (mapChanged || zoneChanged)
        {
            Logger.Write(LogType.NETWORK, $"{category} player '{displayName}' ({currentPlayer.Guid}) entered zone {currentPlayer.Zone} on map {currentPlayer.Map} from {remoteServerName}.", category);
        }
    }

    private static string FormatPlayerName(MapPlayerRuntimeState player)
    {
        return FormatPlayerName(player, null);
    }

    private static string FormatPlayerName(MapPlayerRuntimeState player, MapPlayerRuntimeState? fallbackPlayer)
    {
        if (!string.IsNullOrWhiteSpace(player.Name))
        {
            return player.Name;
        }

        if (!string.IsNullOrWhiteSpace(fallbackPlayer?.Name))
        {
            return fallbackPlayer.Name;
        }

        return $"guid:{player.Guid.ToString(CultureInfo.InvariantCulture)}";
    }
}
