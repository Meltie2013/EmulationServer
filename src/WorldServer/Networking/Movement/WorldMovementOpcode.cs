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

using EmulationServer.WorldServer.Networking.Packets;

/**
  * File overview: src/WorldServer/Networking/Movement/WorldMovementOpcode.cs
  * Documents the WorldMovementOpcode source file in the world movement opcode parsing and server-side movement state updates area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.WorldServer.Networking.Movement;

/**
  * Owns the world movement opcode behavior for the world movement opcode parsing and server-side movement state updates layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public static class WorldMovementOpcode
{
    /**
      * Determines whether movement opcode for the world movement opcode parsing and server-side movement state updates workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: opcode.
      */
    public static bool IsMovementOpcode(WorldOpcode opcode)
    {
        return opcode is
            WorldOpcode.MSG_MOVE_START_FORWARD or
            WorldOpcode.MSG_MOVE_START_BACKWARD or
            WorldOpcode.MSG_MOVE_STOP or
            WorldOpcode.MSG_MOVE_START_STRAFE_LEFT or
            WorldOpcode.MSG_MOVE_START_STRAFE_RIGHT or
            WorldOpcode.MSG_MOVE_STOP_STRAFE or
            WorldOpcode.MSG_MOVE_JUMP or
            WorldOpcode.MSG_MOVE_START_TURN_LEFT or
            WorldOpcode.MSG_MOVE_START_TURN_RIGHT or
            WorldOpcode.MSG_MOVE_STOP_TURN or
            WorldOpcode.MSG_MOVE_START_PITCH_UP or
            WorldOpcode.MSG_MOVE_START_PITCH_DOWN or
            WorldOpcode.MSG_MOVE_STOP_PITCH or
            WorldOpcode.MSG_MOVE_SET_RUN_MODE or
            WorldOpcode.MSG_MOVE_SET_WALK_MODE or
            WorldOpcode.MSG_MOVE_TOGGLE_LOGGING or
            WorldOpcode.MSG_MOVE_TELEPORT or
            WorldOpcode.MSG_MOVE_TELEPORT_CHEAT or
            WorldOpcode.MSG_MOVE_TELEPORT_ACK or
            WorldOpcode.MSG_MOVE_TOGGLE_FALL_LOGGING or
            WorldOpcode.MSG_MOVE_FALL_LAND or
            WorldOpcode.MSG_MOVE_START_SWIM or
            WorldOpcode.MSG_MOVE_STOP_SWIM or
            WorldOpcode.MSG_MOVE_SET_RUN_SPEED_CHEAT or
            WorldOpcode.MSG_MOVE_SET_RUN_SPEED or
            WorldOpcode.MSG_MOVE_SET_RUN_BACK_SPEED_CHEAT or
            WorldOpcode.MSG_MOVE_SET_RUN_BACK_SPEED or
            WorldOpcode.MSG_MOVE_SET_WALK_SPEED_CHEAT or
            WorldOpcode.MSG_MOVE_SET_WALK_SPEED or
            WorldOpcode.MSG_MOVE_SET_SWIM_SPEED_CHEAT or
            WorldOpcode.MSG_MOVE_SET_SWIM_SPEED or
            WorldOpcode.MSG_MOVE_SET_SWIM_BACK_SPEED_CHEAT or
            WorldOpcode.MSG_MOVE_SET_SWIM_BACK_SPEED or
            WorldOpcode.MSG_MOVE_SET_ALL_SPEED_CHEAT or
            WorldOpcode.MSG_MOVE_SET_TURN_RATE_CHEAT or
            WorldOpcode.MSG_MOVE_SET_TURN_RATE or
            WorldOpcode.MSG_MOVE_TOGGLE_COLLISION_CHEAT or
            WorldOpcode.MSG_MOVE_SET_FACING or
            WorldOpcode.MSG_MOVE_SET_PITCH or
            WorldOpcode.MSG_MOVE_WORLDPORT_ACK or
            WorldOpcode.MSG_MOVE_SET_RAW_POSITION_ACK or
            WorldOpcode.CMSG_MOVE_SET_RAW_POSITION or
            WorldOpcode.CMSG_FORCE_RUN_SPEED_CHANGE_ACK or
            WorldOpcode.CMSG_FORCE_RUN_BACK_SPEED_CHANGE_ACK or
            WorldOpcode.CMSG_FORCE_SWIM_SPEED_CHANGE_ACK or
            WorldOpcode.CMSG_FORCE_MOVE_ROOT_ACK or
            WorldOpcode.CMSG_FORCE_MOVE_UNROOT_ACK or
            WorldOpcode.MSG_MOVE_ROOT or
            WorldOpcode.MSG_MOVE_UNROOT or
            WorldOpcode.MSG_MOVE_HEARTBEAT or
            WorldOpcode.CMSG_MOVE_KNOCK_BACK_ACK or
            WorldOpcode.MSG_MOVE_KNOCK_BACK or
            WorldOpcode.CMSG_MOVE_HOVER_ACK or
            WorldOpcode.MSG_MOVE_HOVER;
    }

    /**
      * Determines whether movement info at payload start exists for the world movement opcode parsing and server-side movement state updates workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: opcode.
      */
    public static bool HasMovementInfoAtPayloadStart(WorldOpcode opcode)
    {
        return opcode is
            WorldOpcode.MSG_MOVE_START_FORWARD or
            WorldOpcode.MSG_MOVE_START_BACKWARD or
            WorldOpcode.MSG_MOVE_STOP or
            WorldOpcode.MSG_MOVE_START_STRAFE_LEFT or
            WorldOpcode.MSG_MOVE_START_STRAFE_RIGHT or
            WorldOpcode.MSG_MOVE_STOP_STRAFE or
            WorldOpcode.MSG_MOVE_JUMP or
            WorldOpcode.MSG_MOVE_START_TURN_LEFT or
            WorldOpcode.MSG_MOVE_START_TURN_RIGHT or
            WorldOpcode.MSG_MOVE_STOP_TURN or
            WorldOpcode.MSG_MOVE_START_PITCH_UP or
            WorldOpcode.MSG_MOVE_START_PITCH_DOWN or
            WorldOpcode.MSG_MOVE_STOP_PITCH or
            WorldOpcode.MSG_MOVE_SET_RUN_MODE or
            WorldOpcode.MSG_MOVE_SET_WALK_MODE or
            WorldOpcode.MSG_MOVE_TELEPORT or
            WorldOpcode.MSG_MOVE_TELEPORT_CHEAT or
            WorldOpcode.MSG_MOVE_TELEPORT_ACK or
            WorldOpcode.MSG_MOVE_FALL_LAND or
            WorldOpcode.MSG_MOVE_START_SWIM or
            WorldOpcode.MSG_MOVE_STOP_SWIM or
            WorldOpcode.MSG_MOVE_SET_FACING or
            WorldOpcode.MSG_MOVE_SET_PITCH or
            WorldOpcode.MSG_MOVE_ROOT or
            WorldOpcode.MSG_MOVE_UNROOT or
            WorldOpcode.MSG_MOVE_HEARTBEAT or
            WorldOpcode.MSG_MOVE_KNOCK_BACK or
            WorldOpcode.MSG_MOVE_HOVER;
    }

    /**
      * Determines whether ack header before movement info exists for the world movement opcode parsing and server-side movement state updates workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: opcode.
      */
    public static bool HasAckHeaderBeforeMovementInfo(WorldOpcode opcode)
    {
        return opcode is
            WorldOpcode.CMSG_FORCE_RUN_SPEED_CHANGE_ACK or
            WorldOpcode.CMSG_FORCE_RUN_BACK_SPEED_CHANGE_ACK or
            WorldOpcode.CMSG_FORCE_SWIM_SPEED_CHANGE_ACK or
            WorldOpcode.CMSG_FORCE_MOVE_ROOT_ACK or
            WorldOpcode.CMSG_FORCE_MOVE_UNROOT_ACK or
            WorldOpcode.CMSG_MOVE_KNOCK_BACK_ACK or
            WorldOpcode.CMSG_MOVE_HOVER_ACK;
    }
}
