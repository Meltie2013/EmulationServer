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
// File: src/EmulationServer.Game/Maps/Runtime/MapPlayerRuntimeLogger.cs
// Purpose: Contains map player runtime logger code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Globalization;

using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Game.Maps.Runtime;

// Type: MapPlayerRuntimeLogger
// Purpose: Provides map player runtime logger behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class MapPlayerRuntimeLogger
{

    // Method: LogPlayerEntered
    // Purpose: Executes the log player entered operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - category: Category value supplied by the caller for this operation.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - player: Player value supplied by the caller for this operation.
    // - activePlayerCount: Active player count value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapPlayerRuntimeLogger so callers do not duplicate validation, protocol, or persistence rules.
    public static void LogPlayerEntered(string category, string remoteServerName, MapPlayerRuntimeState player, int activePlayerCount)
    {
        ArgumentNullException.ThrowIfNull(player);

        string displayName = FormatPlayerName(player);
        Logger.Write(LogType.SYSTEM, $"{category} player '{displayName}' ({player.Guid}) entered map {player.Map} from {remoteServerName}. Active players={activePlayerCount}.", category);
        Logger.Write(LogType.SYSTEM, $"{category} player '{displayName}' ({player.Guid}) entered zone {player.Zone} on map {player.Map} from {remoteServerName}.", category);
    }

    // Method: LogPlayerLeft
    // Purpose: Executes the log player left operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - category: Category value supplied by the caller for this operation.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - player: Player value supplied by the caller for this operation.
    // - activePlayerCount: Active player count value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapPlayerRuntimeLogger so callers do not duplicate validation, protocol, or persistence rules.
    public static void LogPlayerLeft(string category, string remoteServerName, MapPlayerRuntimeState player, int activePlayerCount)
    {
        ArgumentNullException.ThrowIfNull(player);

        string displayName = FormatPlayerName(player);
        Logger.Write(LogType.SYSTEM, $"{category} player '{displayName}' ({player.Guid}) left zone {player.Zone} on map {player.Map} from {remoteServerName}.", category);
        Logger.Write(LogType.SYSTEM, $"{category} player '{displayName}' ({player.Guid}) left map {player.Map} from {remoteServerName}. Active players={activePlayerCount}.", category);
    }

    // Method: LogPlayerTransition
    // Purpose: Executes the log player transition operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - category: Category value supplied by the caller for this operation.
    // - remoteServerName: Remote server name value supplied by the caller for this operation.
    // - previousPlayer: Previous player value supplied by the caller for this operation.
    // - currentPlayer: Current player value supplied by the caller for this operation.
    // - activePlayerCount: Active player count value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to MapPlayerRuntimeLogger so callers do not duplicate validation, protocol, or persistence rules.
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
            Logger.Write(LogType.SYSTEM, $"{category} player '{displayName}' ({currentPlayer.Guid}) left zone {previousPlayer.Zone} on map {previousPlayer.Map} from {remoteServerName}.", category);
        }

        if (mapChanged)
        {
            Logger.Write(LogType.SYSTEM, $"{category} player '{displayName}' ({currentPlayer.Guid}) left map {previousPlayer.Map} from {remoteServerName}. Active players={activePlayerCount}.", category);
            Logger.Write(LogType.SYSTEM, $"{category} player '{displayName}' ({currentPlayer.Guid}) entered map {currentPlayer.Map} from {remoteServerName}. Active players={activePlayerCount}.", category);
        }

        if (mapChanged || zoneChanged)
        {
            Logger.Write(LogType.SYSTEM, $"{category} player '{displayName}' ({currentPlayer.Guid}) entered zone {currentPlayer.Zone} on map {currentPlayer.Map} from {remoteServerName}.", category);
        }
    }

    // Method: FormatPlayerName
    // Purpose: Executes the format player name operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to MapPlayerRuntimeLogger so callers do not duplicate validation, protocol, or persistence rules.
    private static string FormatPlayerName(MapPlayerRuntimeState player)
    {
        return FormatPlayerName(player, null);
    }

    // Method: FormatPlayerName
    // Purpose: Executes the format player name operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - fallbackPlayer: Fallback player value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to MapPlayerRuntimeLogger so callers do not duplicate validation, protocol, or persistence rules.
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
