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

/**
  * File overview: src/WorldServer/Networking/Packets/WorldOpcode.cs
  * Documents the WorldOpcode source file in the World of Warcraft packet opcode, reader, writer, and builder support area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.WorldServer.Networking.Packets;

/**
  * Lists the supported world opcode values used by the World of Warcraft packet opcode, reader, writer, and builder support layer.
  * Numeric values are part of the project contract and should only be changed when the related client packet, DBC value, or database schema is updated as well.
  */
public enum WorldOpcode : ushort
{
    /**
      * Represents the create value for world opcode handling.
      */
    CMSG_CHAR_CREATE = 0x036,
    /**
      * Represents the enum value for world opcode handling.
      */
    CMSG_CHAR_ENUM = 0x037,
    /**
      * Represents the delete value for world opcode handling.
      */
    CMSG_CHAR_DELETE = 0x038,
    /**
      * Represents the create value for world opcode handling.
      */
    SMSG_CHAR_CREATE = 0x03A,
    /**
      * Represents the enum value for world opcode handling.
      */
    SMSG_CHAR_ENUM = 0x03B,
    /**
      * Represents the delete value for world opcode handling.
      */
    SMSG_CHAR_DELETE = 0x03C,
    /**
      * Represents the login value for world opcode handling.
      */
    CMSG_PLAYER_LOGIN = 0x03D,
    /**
      * Represents the world value for world opcode handling.
      */
    SMSG_NEW_WORLD = 0x03E,
    /**
      * Represents the pending value for world opcode handling.
      */
    SMSG_TRANSFER_PENDING = 0x03F,
    /**
      * Represents the aborted value for world opcode handling.
      */
    SMSG_TRANSFER_ABORTED = 0x040,
    /**
      * Represents the failed value for world opcode handling.
      */
    SMSG_CHARACTER_LOGIN_FAILED = 0x041,
    /**
      * Represents the settimespeed value for world opcode handling.
      */
    SMSG_LOGIN_SETTIMESPEED = 0x042,
    /**
      * Represents the servertime value for world opcode handling.
      */
    CMSG_SERVERTIME = 0x048,
    /**
      * Represents the servertime value for world opcode handling.
      */
    SMSG_SERVERTIME = 0x049,
    /**
      * Represents the logout value for world opcode handling.
      */
    CMSG_PLAYER_LOGOUT = 0x04A,
    /**
      * Represents the request value for world opcode handling.
      */
    CMSG_LOGOUT_REQUEST = 0x04B,
    /**
      * Represents the response value for world opcode handling.
      */
    SMSG_LOGOUT_RESPONSE = 0x04C,
    /**
      * Represents the complete value for world opcode handling.
      */
    SMSG_LOGOUT_COMPLETE = 0x04D,
    /**
      * Represents the cancel value for world opcode handling.
      */
    CMSG_LOGOUT_CANCEL = 0x04E,
    /**
      * Represents the ack value for world opcode handling.
      */
    SMSG_LOGOUT_CANCEL_ACK = 0x04F,
    /**
      * Represents the query value for world opcode handling.
      */
    CMSG_NAME_QUERY = 0x050,
    /**
      * Represents the response value for world opcode handling.
      */
    SMSG_NAME_QUERY_RESPONSE = 0x051,
    /**
      * Represents the single value for world opcode handling.
      */
    CMSG_ITEM_QUERY_SINGLE = 0x056,
    /**
      * Represents the response value for world opcode handling.
      */
    SMSG_ITEM_QUERY_SINGLE_RESPONSE = 0x058,
    /**
      * Represents the who value for world opcode handling.
      */
    CMSG_WHO = 0x062,
    /**
      * Represents the who value for world opcode handling.
      */
    SMSG_WHO = 0x063,
    /**
      * Represents the messagechat value for world opcode handling.
      */
    CMSG_MESSAGECHAT = 0x095,
    /**
      * Represents the messagechat value for world opcode handling.
      */
    SMSG_MESSAGECHAT = 0x096,
    /**
      * Represents the channel value for world opcode handling.
      */
    CMSG_JOIN_CHANNEL = 0x097,
    /**
      * Represents the channel value for world opcode handling.
      */
    CMSG_LEAVE_CHANNEL = 0x098,
    /**
      * Represents the notify value for world opcode handling.
      */
    SMSG_CHANNEL_NOTIFY = 0x099,
    /**
      * Represents the list value for world opcode handling.
      */
    CMSG_CHANNEL_LIST = 0x09A,
    /**
      * Represents the list value for world opcode handling.
      */
    SMSG_CHANNEL_LIST = 0x09B,
    /**
      * Represents the password value for world opcode handling.
      */
    CMSG_CHANNEL_PASSWORD = 0x09C,
    /**
      * Represents the object value for world opcode handling.
      */
    SMSG_UPDATE_OBJECT = 0x0A9,
    /**
      * Represents the object value for world opcode handling.
      */
    SMSG_DESTROY_OBJECT = 0x0AA,
    /**
      * Represents the areatrigger value for world opcode handling.
      */
    CMSG_AREATRIGGER = 0x0B4,
    /**
      * Represents the forward value for world opcode handling.
      */
    MSG_MOVE_START_FORWARD = 0x0B5,
    /**
      * Represents the backward value for world opcode handling.
      */
    MSG_MOVE_START_BACKWARD = 0x0B6,
    /**
      * Represents the stop value for world opcode handling.
      */
    MSG_MOVE_STOP = 0x0B7,
    /**
      * Represents the left value for world opcode handling.
      */
    MSG_MOVE_START_STRAFE_LEFT = 0x0B8,
    /**
      * Represents the right value for world opcode handling.
      */
    MSG_MOVE_START_STRAFE_RIGHT = 0x0B9,
    /**
      * Represents the strafe value for world opcode handling.
      */
    MSG_MOVE_STOP_STRAFE = 0x0BA,
    /**
      * Represents the jump value for world opcode handling.
      */
    MSG_MOVE_JUMP = 0x0BB,
    /**
      * Represents the left value for world opcode handling.
      */
    MSG_MOVE_START_TURN_LEFT = 0x0BC,
    /**
      * Represents the right value for world opcode handling.
      */
    MSG_MOVE_START_TURN_RIGHT = 0x0BD,
    /**
      * Represents the turn value for world opcode handling.
      */
    MSG_MOVE_STOP_TURN = 0x0BE,
    /**
      * Represents the up value for world opcode handling.
      */
    MSG_MOVE_START_PITCH_UP = 0x0BF,
    /**
      * Represents the down value for world opcode handling.
      */
    MSG_MOVE_START_PITCH_DOWN = 0x0C0,
    /**
      * Represents the pitch value for world opcode handling.
      */
    MSG_MOVE_STOP_PITCH = 0x0C1,
    /**
      * Represents the mode value for world opcode handling.
      */
    MSG_MOVE_SET_RUN_MODE = 0x0C2,
    /**
      * Represents the mode value for world opcode handling.
      */
    MSG_MOVE_SET_WALK_MODE = 0x0C3,
    /**
      * Represents the logging value for world opcode handling.
      */
    MSG_MOVE_TOGGLE_LOGGING = 0x0C4,
    /**
      * Represents the teleport value for world opcode handling.
      */
    MSG_MOVE_TELEPORT = 0x0C5,
    /**
      * Represents the cheat value for world opcode handling.
      */
    MSG_MOVE_TELEPORT_CHEAT = 0x0C6,
    /**
      * Represents the ack value for world opcode handling.
      */
    MSG_MOVE_TELEPORT_ACK = 0x0C7,
    /**
      * Represents the logging value for world opcode handling.
      */
    MSG_MOVE_TOGGLE_FALL_LOGGING = 0x0C8,
    /**
      * Represents the land value for world opcode handling.
      */
    MSG_MOVE_FALL_LAND = 0x0C9,
    /**
      * Represents the swim value for world opcode handling.
      */
    MSG_MOVE_START_SWIM = 0x0CA,
    /**
      * Represents the swim value for world opcode handling.
      */
    MSG_MOVE_STOP_SWIM = 0x0CB,
    /**
      * Represents the cheat value for world opcode handling.
      */
    MSG_MOVE_SET_RUN_SPEED_CHEAT = 0x0CC,
    /**
      * Represents the speed value for world opcode handling.
      */
    MSG_MOVE_SET_RUN_SPEED = 0x0CD,
    /**
      * Represents the cheat value for world opcode handling.
      */
    MSG_MOVE_SET_RUN_BACK_SPEED_CHEAT = 0x0CE,
    /**
      * Represents the speed value for world opcode handling.
      */
    MSG_MOVE_SET_RUN_BACK_SPEED = 0x0CF,
    /**
      * Represents the cheat value for world opcode handling.
      */
    MSG_MOVE_SET_WALK_SPEED_CHEAT = 0x0D0,
    /**
      * Represents the speed value for world opcode handling.
      */
    MSG_MOVE_SET_WALK_SPEED = 0x0D1,
    /**
      * Represents the cheat value for world opcode handling.
      */
    MSG_MOVE_SET_SWIM_SPEED_CHEAT = 0x0D2,
    /**
      * Represents the speed value for world opcode handling.
      */
    MSG_MOVE_SET_SWIM_SPEED = 0x0D3,
    /**
      * Represents the cheat value for world opcode handling.
      */
    MSG_MOVE_SET_SWIM_BACK_SPEED_CHEAT = 0x0D4,
    /**
      * Represents the speed value for world opcode handling.
      */
    MSG_MOVE_SET_SWIM_BACK_SPEED = 0x0D5,
    /**
      * Represents the cheat value for world opcode handling.
      */
    MSG_MOVE_SET_ALL_SPEED_CHEAT = 0x0D6,
    /**
      * Represents the cheat value for world opcode handling.
      */
    MSG_MOVE_SET_TURN_RATE_CHEAT = 0x0D7,
    /**
      * Represents the rate value for world opcode handling.
      */
    MSG_MOVE_SET_TURN_RATE = 0x0D8,
    /**
      * Represents the cheat value for world opcode handling.
      */
    MSG_MOVE_TOGGLE_COLLISION_CHEAT = 0x0D9,
    /**
      * Represents the facing value for world opcode handling.
      */
    MSG_MOVE_SET_FACING = 0x0DA,
    /**
      * Represents the pitch value for world opcode handling.
      */
    MSG_MOVE_SET_PITCH = 0x0DB,
    /**
      * Represents the ack value for world opcode handling.
      */
    MSG_MOVE_WORLDPORT_ACK = 0x0DC,
    /**
      * Represents the move value for world opcode handling.
      */
    SMSG_MONSTER_MOVE = 0x0DD,
    /**
      * Represents the walk value for world opcode handling.
      */
    SMSG_MOVE_WATER_WALK = 0x0DE,
    /**
      * Represents the walk value for world opcode handling.
      */
    SMSG_MOVE_LAND_WALK = 0x0DF,
    /**
      * Represents the ack value for world opcode handling.
      */
    MSG_MOVE_SET_RAW_POSITION_ACK = 0x0E0,
    /**
      * Represents the position value for world opcode handling.
      */
    CMSG_MOVE_SET_RAW_POSITION = 0x0E1,
    /**
      * Represents the change value for world opcode handling.
      */
    SMSG_FORCE_RUN_SPEED_CHANGE = 0x0E2,
    /**
      * Represents the ack value for world opcode handling.
      */
    CMSG_FORCE_RUN_SPEED_CHANGE_ACK = 0x0E3,
    /**
      * Represents the change value for world opcode handling.
      */
    SMSG_FORCE_RUN_BACK_SPEED_CHANGE = 0x0E4,
    /**
      * Represents the ack value for world opcode handling.
      */
    CMSG_FORCE_RUN_BACK_SPEED_CHANGE_ACK = 0x0E5,
    /**
      * Represents the change value for world opcode handling.
      */
    SMSG_FORCE_SWIM_SPEED_CHANGE = 0x0E6,
    /**
      * Represents the ack value for world opcode handling.
      */
    CMSG_FORCE_SWIM_SPEED_CHANGE_ACK = 0x0E7,
    /**
      * Represents the root value for world opcode handling.
      */
    SMSG_FORCE_MOVE_ROOT = 0x0E8,
    /**
      * Represents the ack value for world opcode handling.
      */
    CMSG_FORCE_MOVE_ROOT_ACK = 0x0E9,
    /**
      * Represents the unroot value for world opcode handling.
      */
    SMSG_FORCE_MOVE_UNROOT = 0x0EA,
    /**
      * Represents the ack value for world opcode handling.
      */
    CMSG_FORCE_MOVE_UNROOT_ACK = 0x0EB,
    /**
      * Represents the root value for world opcode handling.
      */
    MSG_MOVE_ROOT = 0x0EC,
    /**
      * Represents the unroot value for world opcode handling.
      */
    MSG_MOVE_UNROOT = 0x0ED,
    /**
      * Represents the heartbeat value for world opcode handling.
      */
    MSG_MOVE_HEARTBEAT = 0x0EE,
    /**
      * Represents the back value for world opcode handling.
      */
    SMSG_MOVE_KNOCK_BACK = 0x0EF,
    /**
      * Represents the ack value for world opcode handling.
      */
    CMSG_MOVE_KNOCK_BACK_ACK = 0x0F0,
    /**
      * Represents the back value for world opcode handling.
      */
    MSG_MOVE_KNOCK_BACK = 0x0F1,
    /**
      * Represents the fall value for world opcode handling.
      */
    SMSG_MOVE_FEATHER_FALL = 0x0F2,
    /**
      * Represents the fall value for world opcode handling.
      */
    SMSG_MOVE_NORMAL_FALL = 0x0F3,
    /**
      * Represents the hover value for world opcode handling.
      */
    SMSG_MOVE_SET_HOVER = 0x0F4,
    /**
      * Represents the hover value for world opcode handling.
      */
    SMSG_MOVE_UNSET_HOVER = 0x0F5,
    /**
      * Represents the ack value for world opcode handling.
      */
    CMSG_MOVE_HOVER_ACK = 0x0F6,
    /**
      * Represents the hover value for world opcode handling.
      */
    MSG_MOVE_HOVER = 0x0F7,
    /**
      * Represents the cinematic value for world opcode handling.
      */
    CMSG_OPENING_CINEMATIC = 0x0F9,
    /**
      * Represents the camera value for world opcode handling.
      */
    CMSG_NEXT_CINEMATIC_CAMERA = 0x0FB,
    /**
      * Represents the cinematic value for world opcode handling.
      */
    CMSG_COMPLETE_CINEMATIC = 0x0FC,
    /**
      * Represents the flags value for world opcode handling.
      */
    SMSG_TUTORIAL_FLAGS = 0x0FD,
    /**
      * Represents the flag value for world opcode handling.
      */
    CMSG_TUTORIAL_FLAG = 0x0FE,
    /**
      * Represents the clear value for world opcode handling.
      */
    CMSG_TUTORIAL_CLEAR = 0x0FF,
    /**
      * Represents the reset value for world opcode handling.
      */
    CMSG_TUTORIAL_RESET = 0x100,
    /**
      * Represents the standstatechange value for world opcode handling.
      */
    CMSG_STANDSTATECHANGE = 0x101,
    /**
      * Represents the factions value for world opcode handling.
      */
    SMSG_INITIALIZE_FACTIONS = 0x122,
    /**
      * Represents the button value for world opcode handling.
      */
    CMSG_SET_ACTION_BUTTON = 0x128,
    /**
      * Represents the buttons value for world opcode handling.
      */
    SMSG_ACTION_BUTTONS = 0x129,
    /**
      * Represents the spells value for world opcode handling.
      */
    SMSG_INITIAL_SPELLS = 0x12A,
    /**
      * Represents the bindpointupdate value for world opcode handling.
      */
    SMSG_BINDPOINTUPDATE = 0x155,
    /**
      * Represents the notification value for world opcode handling.
      */
    SMSG_NOTIFICATION = 0x1CB,
    /**
      * Represents the time value for world opcode handling.
      */
    CMSG_PLAYED_TIME = 0x1CC,
    /**
      * Represents the time value for world opcode handling.
      */
    SMSG_PLAYED_TIME = 0x1CD,
    /**
      * Represents the time value for world opcode handling.
      */
    CMSG_QUERY_TIME = 0x1CE,
    /**
      * Represents the response value for world opcode handling.
      */
    SMSG_QUERY_TIME_RESPONSE = 0x1CF,
    /**
      * Represents the ping value for world opcode handling.
      */
    CMSG_PING = 0x1DC,
    /**
      * Represents the pong value for world opcode handling.
      */
    SMSG_PONG = 0x1DD,
    /**
      * Represents the zoneupdate value for world opcode handling.
      */
    CMSG_ZONEUPDATE = 0x1F4,
    /**
      * Represents the challenge value for world opcode handling.
      */
    SMSG_AUTH_CHALLENGE = 0x1EC,
    /**
      * Represents the session value for world opcode handling.
      */
    CMSG_AUTH_SESSION = 0x1ED,
    /**
      * Represents the response value for world opcode handling.
      */
    SMSG_AUTH_RESPONSE = 0x1EE,
    /**
      * Represents the times value for world opcode handling.
      */
    SMSG_ACCOUNT_DATA_TIMES = 0x209,
    /**
      * Represents the data value for world opcode handling.
      */
    CMSG_REQUEST_ACCOUNT_DATA = 0x20A,
    /**
      * Represents the data value for world opcode handling.
      */
    CMSG_UPDATE_ACCOUNT_DATA = 0x20B,
    /**
      * Represents the data value for world opcode handling.
      */
    SMSG_UPDATE_ACCOUNT_DATA = 0x20C,
    /**
      * Represents the start value for world opcode handling.
      */
    SMSG_SET_REST_START = 0x21E,
    /**
      * Represents the world value for world opcode handling.
      */
    SMSG_LOGIN_VERIFY_WORLD = 0x236,
    /**
      * Represents the toggles value for world opcode handling.
      */
    CMSG_SET_ACTIONBAR_TOGGLES = 0x2BF,
    /**
      * Represents the query value for world opcode handling.
      */
    CMSG_ITEM_NAME_QUERY = 0x2C4,
    /**
      * Represents the response value for world opcode handling.
      */
    SMSG_ITEM_NAME_QUERY_RESPONSE = 0x2C5,
    /**
      * Represents the info value for world opcode handling.
      */
    SMSG_ADDON_INFO = 0x2EF,
    /**
      * Represents the motd value for world opcode handling.
      */
    SMSG_MOTD = 0x33D,
}
