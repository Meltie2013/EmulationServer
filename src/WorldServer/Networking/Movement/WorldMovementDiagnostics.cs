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

using System.Collections.Concurrent;
using System.Globalization;

using EmulationServer.Game.Movement;
using EmulationServer.Game.Players;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.WorldServer.Networking.Packets;

/**
  * File overview: src/WorldServer/Networking/Movement/WorldMovementDiagnostics.cs
  * Provides targeted movement and position packet diagnostics for rubber-banding investigations.
  * The diagnostics are disabled by default so normal movement traffic does not become noisy or slower.
  */

namespace EmulationServer.WorldServer.Networking.Movement;

/**
  * Provides opt-in diagnostics for movement-sensitive world traffic.
  * Enable with EMULATIONSERVER_MOVEMENT_DIAGNOSTICS=true when investigating rubber-banding or unexpected position correction packets.
  */
public static class WorldMovementDiagnostics
{
    private const string EnabledEnvironmentVariable = "EMULATIONSERVER_MOVEMENT_DIAGNOSTICS";
    private const string IncomingEnvironmentVariable = "EMULATIONSERVER_MOVEMENT_DIAGNOSTICS_INCOMING";
    private const string OutgoingEnvironmentVariable = "EMULATIONSERVER_MOVEMENT_DIAGNOSTICS_OUTGOING";
    private const string MapRouteEnvironmentVariable = "EMULATIONSERVER_MOVEMENT_DIAGNOSTICS_MAP_ROUTE";

    private static readonly TimeSpan IncomingMovementTraceInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan OutgoingPositionTraceInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan MapRouteTraceInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan SlowMapRouteWarningThreshold = TimeSpan.FromMilliseconds(75);

    private static readonly bool DiagnosticsEnabled = IsEnabled(Environment.GetEnvironmentVariable(EnabledEnvironmentVariable));
    private static readonly bool IncomingDiagnosticsEnabled = DiagnosticsEnabled && !IsDisabled(Environment.GetEnvironmentVariable(IncomingEnvironmentVariable));
    private static readonly bool OutgoingDiagnosticsEnabled = DiagnosticsEnabled && !IsDisabled(Environment.GetEnvironmentVariable(OutgoingEnvironmentVariable));
    private static readonly bool MapRouteDiagnosticsEnabled = DiagnosticsEnabled && !IsDisabled(Environment.GetEnvironmentVariable(MapRouteEnvironmentVariable));

    private static readonly ConcurrentDictionary<string, long> LastLogTicksByKey = new(StringComparer.Ordinal);
    private static int _enabledBannerLogged;

    /**
      * Gets whether the diagnostic system is enabled for the current WorldServer process.
      */
    public static bool Enabled => DiagnosticsEnabled;

    /**
      * Emits one startup line when movement diagnostics are enabled.
      */
    public static void LogEnabledOnce()
    {
        if (!DiagnosticsEnabled || Interlocked.Exchange(ref _enabledBannerLogged, 1) == 1)
        {
            return;
        }

        Logger.Write(LogType.NOTICE,
            $"Movement diagnostics enabled. Toggle with {EnabledEnvironmentVariable}=true/false. Incoming={IncomingDiagnosticsEnabled}, outgoing={OutgoingDiagnosticsEnabled}, map-route={MapRouteDiagnosticsEnabled}.",
            "MovementDiagnostics");
    }

    /**
      * Logs a throttled snapshot of incoming client movement state.
      * Suspicious jumps, backwards client time, and map/zone changes are logged immediately.
      */
    public static void LogIncomingMovement(
        WorldOpcode opcode,
        int payloadLength,
        PlayerLoginRecord player,
        PlayerMovementState movement,
        PlayerMovementState? previousMovement,
        string remoteEndPoint)
    {
        if (!IncomingDiagnosticsEnabled)
        {
            return;
        }

        bool hasPrevious = previousMovement is not null;
        bool mapOrZoneChanged = hasPrevious && (previousMovement!.Map != movement.Map || previousMovement.Zone != movement.Zone);
        double distance = hasPrevious ? CalculateDistance(previousMovement!.Position, movement.Position) : 0.0d;
        long clientDelta = hasPrevious ? unchecked((long)movement.ClientTime - previousMovement!.ClientTime) : 0L;
        double serverDeltaMs = hasPrevious ? (movement.LastUpdatedUtc - previousMovement!.LastUpdatedUtc).TotalMilliseconds : 0.0d;
        bool suspicious = mapOrZoneChanged || distance > 25.0d || clientDelta < 0L || serverDeltaMs > 750.0d;
        TimeSpan throttle = suspicious ? TimeSpan.Zero : IncomingMovementTraceInterval;

        if (!ShouldLog($"in:{player.Guid}", throttle))
        {
            return;
        }

        LogType logType = suspicious ? LogType.WARNING : LogType.TRACE;
        Logger.Write(logType,
            $"MovementDiag IN player='{player.Name}' guid={player.Guid} remote={remoteEndPoint} opcode={opcode} payload={payloadLength} map={movement.Map} zone={movement.Zone} pos=({Format(movement.PositionX)}, {Format(movement.PositionY)}, {Format(movement.PositionZ)}, o={Format(movement.Orientation)}) flags=0x{(uint)movement.Flags:X8} clientTime={movement.ClientTime} deltaDist={Format(distance)} clientDeltaMs={clientDelta} serverDeltaMs={Format(serverDeltaMs)}.",
            "MovementDiagnostics");
    }

    /**
      * Logs server-to-client packets that can affect movement, position, speed, or world transfer state.
      * A movement packet whose packed source GUID equals the receiving player's GUID is logged as a possible self-echo.
      */
    public static void LogOutgoingPositionPacket(
        WorldOpcode opcode,
        ReadOnlySpan<byte> payload,
        PlayerLoginRecord? targetPlayer,
        PlayerMovementState? targetMovement,
        string remoteEndPoint)
    {
        if (!OutgoingDiagnosticsEnabled || !IsPositionAffectingServerOpcode(opcode))
        {
            return;
        }

        bool hasPackedSourceGuid = TryReadPackedGuid(payload, out ulong packedSourceGuid);
        bool selfMovementEcho = targetPlayer is not null && hasPackedSourceGuid && packedSourceGuid == targetPlayer.ClientGuid && WorldMovementOpcode.IsMovementOpcode(opcode);
        bool forceOrTransfer = IsForceOrTransferOpcode(opcode);
        TimeSpan throttle = selfMovementEcho || forceOrTransfer ? TimeSpan.Zero : OutgoingPositionTraceInterval;
        string key = targetPlayer is null ? $"out:0:{(ushort)opcode}" : $"out:{targetPlayer.Guid}:{(ushort)opcode}";

        if (!ShouldLog(key, throttle))
        {
            return;
        }

        string playerText = targetPlayer is null ? "none" : $"'{targetPlayer.Name}' guid={targetPlayer.Guid} clientGuid=0x{targetPlayer.ClientGuid:X16}";
        string movementText = targetMovement is null
            ? "movement=none"
            : $"movement=map={targetMovement.Map} zone={targetMovement.Zone} pos=({Format(targetMovement.PositionX)}, {Format(targetMovement.PositionY)}, {Format(targetMovement.PositionZ)}, o={Format(targetMovement.Orientation)})";
        string sourceGuidText = hasPackedSourceGuid ? $" packedSourceGuid=0x{packedSourceGuid:X16}" : string.Empty;
        string reason = selfMovementEcho ? " POSSIBLE_SELF_MOVEMENT_ECHO" : string.Empty;
        LogType logType = selfMovementEcho || forceOrTransfer ? LogType.WARNING : LogType.TRACE;

        Logger.Write(logType,
            $"MovementDiag OUT{reason} target={playerText} remote={remoteEndPoint} opcode={opcode} payload={payload.Length}{sourceGuidText} {movementText}.",
            "MovementDiagnostics");
    }

    /**
      * Logs when the broadcast fanout detects another registered session for the same player GUID/client GUID.
      */
    public static void LogSkippedSelfMovementBroadcast(
        PlayerLoginRecord sourcePlayer,
        PlayerLoginRecord recipientPlayer,
        PlayerMovementState movement,
        string sourceRemoteEndPoint,
        string recipientRemoteEndPoint)
    {
        if (!OutgoingDiagnosticsEnabled)
        {
            return;
        }

        if (!ShouldLog($"self-broadcast:{sourcePlayer.Guid}:{recipientRemoteEndPoint}", TimeSpan.FromSeconds(1)))
        {
            return;
        }

        Logger.Write(LogType.WARNING,
            $"MovementDiag skipped same-player movement broadcast: source='{sourcePlayer.Name}' guid={sourcePlayer.Guid} sourceRemote={sourceRemoteEndPoint} recipient='{recipientPlayer.Name}' guid={recipientPlayer.Guid} recipientRemote={recipientRemoteEndPoint} opcode={(WorldOpcode)movement.Opcode} pos=({Format(movement.PositionX)}, {Format(movement.PositionY)}, {Format(movement.PositionZ)}, o={Format(movement.Orientation)}). This can indicate a stale duplicate world session registration.",
            "MovementDiagnostics");
    }

    /**
      * Logs movement telemetry sent from WorldServer to the owning Map/Instance service.
      */
    public static void LogMapServiceMovementRoute(
        PlayerLoginRecord player,
        string ownerServerName,
        PlayerMovementState movement,
        DateTimeOffset routeStartedUtc,
        TimeSpan elapsed,
        string remoteEndPoint)
    {
        if (!MapRouteDiagnosticsEnabled)
        {
            return;
        }

        TimeSpan movementAge = routeStartedUtc - movement.LastUpdatedUtc;
        bool slow = elapsed >= SlowMapRouteWarningThreshold || movementAge >= SlowMapRouteWarningThreshold;
        TimeSpan throttle = slow ? TimeSpan.Zero : MapRouteTraceInterval;

        if (!ShouldLog($"map-route:{player.Guid}", throttle))
        {
            return;
        }

        Logger.Write(slow ? LogType.WARNING : LogType.TRACE,
            $"MovementDiag MAPROUTE player='{player.Name}' guid={player.Guid} owner={ownerServerName} remote={remoteEndPoint} opcode={(WorldOpcode)movement.Opcode} map={movement.Map} zone={movement.Zone} pos=({Format(movement.PositionX)}, {Format(movement.PositionY)}, {Format(movement.PositionZ)}, o={Format(movement.Orientation)}) queuedAgeMs={Format(movementAge.TotalMilliseconds)} routeElapsedMs={Format(elapsed.TotalMilliseconds)}.",
            "MovementDiagnostics");
    }

    private static bool IsEnabled(string? value)
    {
        return value is not null &&
            (value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
             value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
             value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
             value.Equals("on", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsDisabled(string? value)
    {
        return value is not null &&
            (value.Equals("0", StringComparison.OrdinalIgnoreCase) ||
             value.Equals("false", StringComparison.OrdinalIgnoreCase) ||
             value.Equals("no", StringComparison.OrdinalIgnoreCase) ||
             value.Equals("off", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsPositionAffectingServerOpcode(WorldOpcode opcode)
    {
        return WorldMovementOpcode.IsMovementOpcode(opcode) || opcode is
            WorldOpcode.SMSG_UPDATE_OBJECT or
            WorldOpcode.SMSG_NEW_WORLD or
            WorldOpcode.SMSG_TRANSFER_PENDING or
            WorldOpcode.SMSG_TRANSFER_ABORTED or
            WorldOpcode.SMSG_LOGIN_VERIFY_WORLD or
            WorldOpcode.SMSG_MONSTER_MOVE or
            WorldOpcode.SMSG_MOVE_WATER_WALK or
            WorldOpcode.SMSG_MOVE_LAND_WALK or
            WorldOpcode.SMSG_FORCE_RUN_SPEED_CHANGE or
            WorldOpcode.SMSG_FORCE_RUN_BACK_SPEED_CHANGE or
            WorldOpcode.SMSG_FORCE_SWIM_SPEED_CHANGE or
            WorldOpcode.SMSG_FORCE_MOVE_ROOT or
            WorldOpcode.SMSG_FORCE_MOVE_UNROOT or
            WorldOpcode.SMSG_MOVE_KNOCK_BACK or
            WorldOpcode.SMSG_MOVE_FEATHER_FALL or
            WorldOpcode.SMSG_MOVE_NORMAL_FALL or
            WorldOpcode.SMSG_MOVE_SET_HOVER or
            WorldOpcode.SMSG_MOVE_UNSET_HOVER;
    }

    private static bool IsForceOrTransferOpcode(WorldOpcode opcode)
    {
        return opcode is
            WorldOpcode.SMSG_NEW_WORLD or
            WorldOpcode.SMSG_TRANSFER_PENDING or
            WorldOpcode.SMSG_TRANSFER_ABORTED or
            WorldOpcode.SMSG_FORCE_RUN_SPEED_CHANGE or
            WorldOpcode.SMSG_FORCE_RUN_BACK_SPEED_CHANGE or
            WorldOpcode.SMSG_FORCE_SWIM_SPEED_CHANGE or
            WorldOpcode.SMSG_FORCE_MOVE_ROOT or
            WorldOpcode.SMSG_FORCE_MOVE_UNROOT or
            WorldOpcode.SMSG_MOVE_KNOCK_BACK;
    }

    private static bool TryReadPackedGuid(ReadOnlySpan<byte> payload, out ulong guid)
    {
        guid = 0UL;
        if (payload.IsEmpty)
        {
            return false;
        }

        byte mask = payload[0];
        int offset = 1;
        for (int index = 0; index < 8; index++)
        {
            if ((mask & (1 << index)) == 0)
            {
                continue;
            }

            if (offset >= payload.Length)
            {
                guid = 0UL;
                return false;
            }

            guid |= (ulong)payload[offset] << (index * 8);
            offset++;
        }

        return true;
    }

    private static bool ShouldLog(string key, TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
        {
            return true;
        }

        long nowTicks = DateTimeOffset.UtcNow.UtcTicks;
        while (true)
        {
            long previousTicks = LastLogTicksByKey.GetOrAdd(key, 0L);
            if (previousTicks != 0L && nowTicks - previousTicks < interval.Ticks)
            {
                return false;
            }

            if (LastLogTicksByKey.TryUpdate(key, nowTicks, previousTicks))
            {
                return true;
            }
        }
    }

    private static double CalculateDistance(MovementPosition previous, MovementPosition current)
    {
        double deltaX = current.X - previous.X;
        double deltaY = current.Y - previous.Y;
        double deltaZ = current.Z - previous.Z;
        return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY) + (deltaZ * deltaZ));
    }

    private static string Format(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string Format(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
