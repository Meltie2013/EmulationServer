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
// File: src/WorldServer/Networking/Movement/WorldMovementDiagnostics.cs
// Purpose: Contains world movement diagnostics code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Collections.Concurrent;
using System.Globalization;

using EmulationServer.Game.Movement;
using EmulationServer.Game.Players;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;
using EmulationServer.WorldServer.Networking.Packets;

namespace EmulationServer.WorldServer.Networking.Movement;

// Type: WorldMovementDiagnostics
// Purpose: Provides world movement diagnostics behavior for the world server gameplay, session, and character runtime layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class WorldMovementDiagnostics
{
    // Constant: Defines the enabled environment variable constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed enabled environment variable value used anywhere this rule or protocol value is needed.
    private const string EnabledEnvironmentVariable = "EMULATIONSERVER_MOVEMENT_DIAGNOSTICS";
    // Constant: Defines the incoming environment variable constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed incoming environment variable value used anywhere this rule or protocol value is needed.
    private const string IncomingEnvironmentVariable = "EMULATIONSERVER_MOVEMENT_DIAGNOSTICS_INCOMING";
    // Constant: Defines the outgoing environment variable constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed outgoing environment variable value used anywhere this rule or protocol value is needed.
    private const string OutgoingEnvironmentVariable = "EMULATIONSERVER_MOVEMENT_DIAGNOSTICS_OUTGOING";
    // Constant: Defines the map route environment variable constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed map route environment variable value used anywhere this rule or protocol value is needed.
    private const string MapRouteEnvironmentVariable = "EMULATIONSERVER_MOVEMENT_DIAGNOSTICS_MAP_ROUTE";

    // Method: FromSeconds
    // Purpose: Executes the from seconds operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the time span incoming movement trace interval = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to WorldMovementDiagnostics so callers do not duplicate validation, protocol, or persistence rules.
    private static readonly TimeSpan IncomingMovementTraceInterval = TimeSpan.FromSeconds(1);
    // Method: FromMilliseconds
    // Purpose: Executes the from milliseconds operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the time span outgoing position trace interval = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to WorldMovementDiagnostics so callers do not duplicate validation, protocol, or persistence rules.
    private static readonly TimeSpan OutgoingPositionTraceInterval = TimeSpan.FromMilliseconds(500);
    // Method: FromSeconds
    // Purpose: Executes the from seconds operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the time span map route trace interval = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to WorldMovementDiagnostics so callers do not duplicate validation, protocol, or persistence rules.
    private static readonly TimeSpan MapRouteTraceInterval = TimeSpan.FromSeconds(2);
    // Method: FromMilliseconds
    // Purpose: Executes the from milliseconds operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the time span slow map route warning threshold = time span. value produced by this operation.
    // Notes: This keeps the operation scoped to WorldMovementDiagnostics so callers do not duplicate validation, protocol, or persistence rules.
    private static readonly TimeSpan SlowMapRouteWarningThreshold = TimeSpan.FromMilliseconds(75);

    // Method: IsEnabled
    // Purpose: Validates or evaluates is enabled rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - EnabledEnvironmentVariable: Enabled environment variable value supplied by the caller for this operation.
    // Returns: Returns the bool diagnostics enabled = value produced by this operation.
    // Notes: This keeps the operation scoped to WorldMovementDiagnostics so callers do not duplicate validation, protocol, or persistence rules.
    private static readonly bool DiagnosticsEnabled = IsEnabled(Environment.GetEnvironmentVariable(EnabledEnvironmentVariable));
    // Method: IsDisabled
    // Purpose: Validates or evaluates is disabled rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - IncomingEnvironmentVariable: Incoming environment variable value supplied by the caller for this operation.
    // Returns: Returns the bool incoming diagnostics enabled = diagnostics enabled && ! value produced by this operation.
    // Notes: This keeps the operation scoped to WorldMovementDiagnostics so callers do not duplicate validation, protocol, or persistence rules.
    private static readonly bool IncomingDiagnosticsEnabled = DiagnosticsEnabled && !IsDisabled(Environment.GetEnvironmentVariable(IncomingEnvironmentVariable));
    // Method: IsDisabled
    // Purpose: Validates or evaluates is disabled rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - OutgoingEnvironmentVariable: Outgoing environment variable value supplied by the caller for this operation.
    // Returns: Returns the bool outgoing diagnostics enabled = diagnostics enabled && ! value produced by this operation.
    // Notes: This keeps the operation scoped to WorldMovementDiagnostics so callers do not duplicate validation, protocol, or persistence rules.
    private static readonly bool OutgoingDiagnosticsEnabled = DiagnosticsEnabled && !IsDisabled(Environment.GetEnvironmentVariable(OutgoingEnvironmentVariable));
    // Method: IsDisabled
    // Purpose: Validates or evaluates is disabled rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - MapRouteEnvironmentVariable: Map route environment variable value supplied by the caller for this operation.
    // Returns: Returns the bool map route diagnostics enabled = diagnostics enabled && ! value produced by this operation.
    // Notes: This keeps the operation scoped to WorldMovementDiagnostics so callers do not duplicate validation, protocol, or persistence rules.
    private static readonly bool MapRouteDiagnosticsEnabled = DiagnosticsEnabled && !IsDisabled(Environment.GetEnvironmentVariable(MapRouteEnvironmentVariable));

    private static readonly ConcurrentDictionary<string, long> LastLogTicksByKey = new(StringComparer.Ordinal);
    // Field: Stores the enabled banner logged state used by the world server gameplay, session, and character runtime layer.
    // Value: current enabled banner logged backing value maintained by the owning type.
    private static int _enabledBannerLogged;

    // Property: Gets or sets the enabled value used by the world server gameplay, session, and character runtime layer.
    // Value: enabled value exposed by the owning type.
    public static bool Enabled => DiagnosticsEnabled;

    // Method: LogEnabledOnce
    // Purpose: Executes the log enabled once operation for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldMovementDiagnostics so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: LogIncomingMovement
    // Purpose: Executes the log incoming movement operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - opcode: Opcode value supplied by the caller for this operation.
    // - payloadLength: Payload length value supplied by the caller for this operation.
    // - player: Player value supplied by the caller for this operation.
    // - movement: Movement value supplied by the caller for this operation.
    // - previousMovement: Previous movement value supplied by the caller for this operation.
    // - remoteEndPoint: Remote end point value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldMovementDiagnostics so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: LogOutgoingPositionPacket
    // Purpose: Executes the log outgoing position packet operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - opcode: Opcode value supplied by the caller for this operation.
    // - payload: Payload bytes or structured payload consumed by this operation.
    // - targetPlayer: Target player value supplied by the caller for this operation.
    // - targetMovement: Target movement value supplied by the caller for this operation.
    // - remoteEndPoint: Remote end point value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldMovementDiagnostics so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: LogSkippedSelfMovementBroadcast
    // Purpose: Executes the log skipped self movement broadcast operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - sourcePlayer: Source player value supplied by the caller for this operation.
    // - recipientPlayer: Recipient player value supplied by the caller for this operation.
    // - movement: Movement value supplied by the caller for this operation.
    // - sourceRemoteEndPoint: Source remote end point value supplied by the caller for this operation.
    // - recipientRemoteEndPoint: Recipient remote end point value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldMovementDiagnostics so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: LogMapServiceMovementRoute
    // Purpose: Executes the log map service movement route operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - ownerServerName: Owner server name value supplied by the caller for this operation.
    // - movement: Movement value supplied by the caller for this operation.
    // - routeStartedUtc: Route started utc value supplied by the caller for this operation.
    // - elapsed: Elapsed value supplied by the caller for this operation.
    // - remoteEndPoint: Remote end point value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldMovementDiagnostics so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: IsEnabled
    // Purpose: Validates or evaluates is enabled rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns true when is enabled succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldMovementDiagnostics so callers do not duplicate validation, protocol, or persistence rules.
    private static bool IsEnabled(string? value)
    {
        return value is not null &&
            (value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
             value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
             value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
             value.Equals("on", StringComparison.OrdinalIgnoreCase));
    }

    // Method: IsDisabled
    // Purpose: Validates or evaluates is disabled rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns true when is disabled succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldMovementDiagnostics so callers do not duplicate validation, protocol, or persistence rules.
    private static bool IsDisabled(string? value)
    {
        return value is not null &&
            (value.Equals("0", StringComparison.OrdinalIgnoreCase) ||
             value.Equals("false", StringComparison.OrdinalIgnoreCase) ||
             value.Equals("no", StringComparison.OrdinalIgnoreCase) ||
             value.Equals("off", StringComparison.OrdinalIgnoreCase));
    }

    // Method: IsPositionAffectingServerOpcode
    // Purpose: Validates or evaluates is position affecting server opcode rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - opcode: Opcode value supplied by the caller for this operation.
    // Returns: Returns true when is position affecting server opcode succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldMovementDiagnostics so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: IsForceOrTransferOpcode
    // Purpose: Validates or evaluates is force or transfer opcode rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - opcode: Opcode value supplied by the caller for this operation.
    // Returns: Returns true when is force or transfer opcode succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldMovementDiagnostics so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: TryReadPackedGuid
    // Purpose: Attempts to retrieve or parse try read packed GUID data without treating normal misses as failures.
    // Parameters:
    // - payload: Payload bytes or structured payload consumed by this operation.
    // - guid: Guid identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns true when try read packed GUID succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldMovementDiagnostics so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ShouldLog
    // Purpose: Validates or evaluates should log rules for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - key: Key value supplied by the caller for this operation.
    // - interval: Interval value supplied by the caller for this operation.
    // Returns: Returns true when should log succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldMovementDiagnostics so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: CalculateDistance
    // Purpose: Calculates calculate distance values for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - previous: Previous value supplied by the caller for this operation.
    // - current: Current value supplied by the caller for this operation.
    // Returns: Returns the double value produced by this operation.
    // Notes: This keeps the operation scoped to WorldMovementDiagnostics so callers do not duplicate validation, protocol, or persistence rules.
    private static double CalculateDistance(MovementPosition previous, MovementPosition current)
    {
        double deltaX = current.X - previous.X;
        double deltaY = current.Y - previous.Y;
        double deltaZ = current.Z - previous.Z;
        return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY) + (deltaZ * deltaZ));
    }

    // Method: Format
    // Purpose: Executes the format operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to WorldMovementDiagnostics so callers do not duplicate validation, protocol, or persistence rules.
    private static string Format(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    // Method: Format
    // Purpose: Executes the format operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to WorldMovementDiagnostics so callers do not duplicate validation, protocol, or persistence rules.
    private static string Format(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
