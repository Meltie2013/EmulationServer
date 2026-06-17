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
// File: src/WorldServer/Networking/Packets/WorldOpcode.cs
// Purpose: Contains world opcode code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.WorldServer.Networking.Packets;

// Type: WorldOpcode
// Purpose: Defines the allowed world opcode values used by the world server gameplay, session, and character runtime layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public enum WorldOpcode : ushort
{

    // Enum Value: Defines the CMSG CHAR CREATE enum value.
    // Value: explicit expression 0x036.
    CMSG_CHAR_CREATE = 0x036,

    // Enum Value: Defines the CMSG CHAR ENUM enum value.
    // Value: explicit expression 0x037.
    CMSG_CHAR_ENUM = 0x037,

    // Enum Value: Defines the CMSG CHAR DELETE enum value.
    // Value: explicit expression 0x038.
    CMSG_CHAR_DELETE = 0x038,

    // Enum Value: Defines the SMSG CHAR CREATE enum value.
    // Value: explicit expression 0x03A.
    SMSG_CHAR_CREATE = 0x03A,

    // Enum Value: Defines the SMSG CHAR ENUM enum value.
    // Value: explicit expression 0x03B.
    SMSG_CHAR_ENUM = 0x03B,

    // Enum Value: Defines the SMSG CHAR DELETE enum value.
    // Value: explicit expression 0x03C.
    SMSG_CHAR_DELETE = 0x03C,

    // Enum Value: Defines the CMSG PLAYER LOGIN enum value.
    // Value: explicit expression 0x03D.
    CMSG_PLAYER_LOGIN = 0x03D,

    // Enum Value: Defines the SMSG NEW WORLD enum value.
    // Value: explicit expression 0x03E.
    SMSG_NEW_WORLD = 0x03E,

    // Enum Value: Defines the SMSG TRANSFER PENDING enum value.
    // Value: explicit expression 0x03F.
    SMSG_TRANSFER_PENDING = 0x03F,

    // Enum Value: Defines the SMSG TRANSFER ABORTED enum value.
    // Value: explicit expression 0x040.
    SMSG_TRANSFER_ABORTED = 0x040,

    // Enum Value: Defines the SMSG CHARACTER LOGIN FAILED enum value.
    // Value: explicit expression 0x041.
    SMSG_CHARACTER_LOGIN_FAILED = 0x041,

    // Enum Value: Defines the SMSG LOGIN SETTIMESPEED enum value.
    // Value: explicit expression 0x042.
    SMSG_LOGIN_SETTIMESPEED = 0x042,

    // Enum Value: Defines the CMSG SERVERTIME enum value.
    // Value: explicit expression 0x048.
    CMSG_SERVERTIME = 0x048,

    // Enum Value: Defines the SMSG SERVERTIME enum value.
    // Value: explicit expression 0x049.
    SMSG_SERVERTIME = 0x049,

    // Enum Value: Defines the CMSG PLAYER LOGOUT enum value.
    // Value: explicit expression 0x04A.
    CMSG_PLAYER_LOGOUT = 0x04A,

    // Enum Value: Defines the CMSG LOGOUT REQUEST enum value.
    // Value: explicit expression 0x04B.
    CMSG_LOGOUT_REQUEST = 0x04B,

    // Enum Value: Defines the SMSG LOGOUT RESPONSE enum value.
    // Value: explicit expression 0x04C.
    SMSG_LOGOUT_RESPONSE = 0x04C,

    // Enum Value: Defines the SMSG LOGOUT COMPLETE enum value.
    // Value: explicit expression 0x04D.
    SMSG_LOGOUT_COMPLETE = 0x04D,

    // Enum Value: Defines the CMSG LOGOUT CANCEL enum value.
    // Value: explicit expression 0x04E.
    CMSG_LOGOUT_CANCEL = 0x04E,

    // Enum Value: Defines the SMSG LOGOUT CANCEL ACK enum value.
    // Value: explicit expression 0x04F.
    SMSG_LOGOUT_CANCEL_ACK = 0x04F,

    // Enum Value: Defines the CMSG NAME QUERY enum value.
    // Value: explicit expression 0x050.
    CMSG_NAME_QUERY = 0x050,

    // Enum Value: Defines the SMSG NAME QUERY RESPONSE enum value.
    // Value: explicit expression 0x051.
    SMSG_NAME_QUERY_RESPONSE = 0x051,

    // Enum Value: Defines the CMSG ITEM QUERY SINGLE enum value.
    // Value: explicit expression 0x056.
    CMSG_ITEM_QUERY_SINGLE = 0x056,

    // Enum Value: Defines the SMSG ITEM QUERY SINGLE RESPONSE enum value.
    // Value: explicit expression 0x058.
    SMSG_ITEM_QUERY_SINGLE_RESPONSE = 0x058,

    // Enum Value: Defines the CMSG GAMEOBJECT QUERY enum value.
    // Value: explicit expression 0x05E.
    CMSG_GAMEOBJECT_QUERY = 0x05E,

    // Enum Value: Defines the SMSG GAMEOBJECT QUERY RESPONSE enum value.
    // Value: explicit expression 0x05F.
    SMSG_GAMEOBJECT_QUERY_RESPONSE = 0x05F,

    // Enum Value: Defines the CMSG CREATURE QUERY enum value.
    // Value: explicit expression 0x060.
    CMSG_CREATURE_QUERY = 0x060,

    // Enum Value: Defines the SMSG CREATURE QUERY RESPONSE enum value.
    // Value: explicit expression 0x061.
    SMSG_CREATURE_QUERY_RESPONSE = 0x061,

    // Enum Value: Defines the CMSG WHO enum value.
    // Value: explicit expression 0x062.
    CMSG_WHO = 0x062,

    // Enum Value: Defines the SMSG WHO enum value.
    // Value: explicit expression 0x063.
    SMSG_WHO = 0x063,

    // Enum Value: Defines the CMSG MESSAGECHAT enum value.
    // Value: explicit expression 0x095.
    CMSG_MESSAGECHAT = 0x095,

    // Enum Value: Defines the SMSG MESSAGECHAT enum value.
    // Value: explicit expression 0x096.
    SMSG_MESSAGECHAT = 0x096,

    // Enum Value: Defines the CMSG JOIN CHANNEL enum value.
    // Value: explicit expression 0x097.
    CMSG_JOIN_CHANNEL = 0x097,

    // Enum Value: Defines the CMSG LEAVE CHANNEL enum value.
    // Value: explicit expression 0x098.
    CMSG_LEAVE_CHANNEL = 0x098,

    // Enum Value: Defines the SMSG CHANNEL NOTIFY enum value.
    // Value: explicit expression 0x099.
    SMSG_CHANNEL_NOTIFY = 0x099,

    // Enum Value: Defines the CMSG CHANNEL LIST enum value.
    // Value: explicit expression 0x09A.
    CMSG_CHANNEL_LIST = 0x09A,

    // Enum Value: Defines the SMSG CHANNEL LIST enum value.
    // Value: explicit expression 0x09B.
    SMSG_CHANNEL_LIST = 0x09B,

    // Enum Value: Defines the CMSG CHANNEL PASSWORD enum value.
    // Value: explicit expression 0x09C.
    CMSG_CHANNEL_PASSWORD = 0x09C,

    // Enum Value: Defines the SMSG UPDATE OBJECT enum value.
    // Value: explicit expression 0x0A9.
    SMSG_UPDATE_OBJECT = 0x0A9,

    // Enum Value: Defines the SMSG DESTROY OBJECT enum value.
    // Value: explicit expression 0x0AA.
    SMSG_DESTROY_OBJECT = 0x0AA,

    // Enum Value: Defines the CMSG AREATRIGGER enum value.
    // Value: explicit expression 0x0B4.
    CMSG_AREATRIGGER = 0x0B4,

    // Enum Value: Defines the MSG MOVE START FORWARD enum value.
    // Value: explicit expression 0x0B5.
    MSG_MOVE_START_FORWARD = 0x0B5,

    // Enum Value: Defines the MSG MOVE START BACKWARD enum value.
    // Value: explicit expression 0x0B6.
    MSG_MOVE_START_BACKWARD = 0x0B6,

    // Enum Value: Defines the MSG MOVE STOP enum value.
    // Value: explicit expression 0x0B7.
    MSG_MOVE_STOP = 0x0B7,

    // Enum Value: Defines the MSG MOVE START STRAFE LEFT enum value.
    // Value: explicit expression 0x0B8.
    MSG_MOVE_START_STRAFE_LEFT = 0x0B8,

    // Enum Value: Defines the MSG MOVE START STRAFE RIGHT enum value.
    // Value: explicit expression 0x0B9.
    MSG_MOVE_START_STRAFE_RIGHT = 0x0B9,

    // Enum Value: Defines the MSG MOVE STOP STRAFE enum value.
    // Value: explicit expression 0x0BA.
    MSG_MOVE_STOP_STRAFE = 0x0BA,

    // Enum Value: Defines the MSG MOVE JUMP enum value.
    // Value: explicit expression 0x0BB.
    MSG_MOVE_JUMP = 0x0BB,

    // Enum Value: Defines the MSG MOVE START TURN LEFT enum value.
    // Value: explicit expression 0x0BC.
    MSG_MOVE_START_TURN_LEFT = 0x0BC,

    // Enum Value: Defines the MSG MOVE START TURN RIGHT enum value.
    // Value: explicit expression 0x0BD.
    MSG_MOVE_START_TURN_RIGHT = 0x0BD,

    // Enum Value: Defines the MSG MOVE STOP TURN enum value.
    // Value: explicit expression 0x0BE.
    MSG_MOVE_STOP_TURN = 0x0BE,

    // Enum Value: Defines the MSG MOVE START PITCH UP enum value.
    // Value: explicit expression 0x0BF.
    MSG_MOVE_START_PITCH_UP = 0x0BF,

    // Enum Value: Defines the MSG MOVE START PITCH DOWN enum value.
    // Value: explicit expression 0x0C0.
    MSG_MOVE_START_PITCH_DOWN = 0x0C0,

    // Enum Value: Defines the MSG MOVE STOP PITCH enum value.
    // Value: explicit expression 0x0C1.
    MSG_MOVE_STOP_PITCH = 0x0C1,

    // Enum Value: Defines the MSG MOVE SET RUN MODE enum value.
    // Value: explicit expression 0x0C2.
    MSG_MOVE_SET_RUN_MODE = 0x0C2,

    // Enum Value: Defines the MSG MOVE SET WALK MODE enum value.
    // Value: explicit expression 0x0C3.
    MSG_MOVE_SET_WALK_MODE = 0x0C3,

    // Enum Value: Defines the MSG MOVE TOGGLE LOGGING enum value.
    // Value: explicit expression 0x0C4.
    MSG_MOVE_TOGGLE_LOGGING = 0x0C4,

    // Enum Value: Defines the MSG MOVE TELEPORT enum value.
    // Value: explicit expression 0x0C5.
    MSG_MOVE_TELEPORT = 0x0C5,

    // Enum Value: Defines the MSG MOVE TELEPORT CHEAT enum value.
    // Value: explicit expression 0x0C6.
    MSG_MOVE_TELEPORT_CHEAT = 0x0C6,

    // Enum Value: Defines the MSG MOVE TELEPORT ACK enum value.
    // Value: explicit expression 0x0C7.
    MSG_MOVE_TELEPORT_ACK = 0x0C7,

    // Enum Value: Defines the MSG MOVE TOGGLE FALL LOGGING enum value.
    // Value: explicit expression 0x0C8.
    MSG_MOVE_TOGGLE_FALL_LOGGING = 0x0C8,

    // Enum Value: Defines the MSG MOVE FALL LAND enum value.
    // Value: explicit expression 0x0C9.
    MSG_MOVE_FALL_LAND = 0x0C9,

    // Enum Value: Defines the MSG MOVE START SWIM enum value.
    // Value: explicit expression 0x0CA.
    MSG_MOVE_START_SWIM = 0x0CA,

    // Enum Value: Defines the MSG MOVE STOP SWIM enum value.
    // Value: explicit expression 0x0CB.
    MSG_MOVE_STOP_SWIM = 0x0CB,

    // Enum Value: Defines the MSG MOVE SET RUN SPEED CHEAT enum value.
    // Value: explicit expression 0x0CC.
    MSG_MOVE_SET_RUN_SPEED_CHEAT = 0x0CC,

    // Enum Value: Defines the MSG MOVE SET RUN SPEED enum value.
    // Value: explicit expression 0x0CD.
    MSG_MOVE_SET_RUN_SPEED = 0x0CD,

    // Enum Value: Defines the MSG MOVE SET RUN BACK SPEED CHEAT enum value.
    // Value: explicit expression 0x0CE.
    MSG_MOVE_SET_RUN_BACK_SPEED_CHEAT = 0x0CE,

    // Enum Value: Defines the MSG MOVE SET RUN BACK SPEED enum value.
    // Value: explicit expression 0x0CF.
    MSG_MOVE_SET_RUN_BACK_SPEED = 0x0CF,

    // Enum Value: Defines the MSG MOVE SET WALK SPEED CHEAT enum value.
    // Value: explicit expression 0x0D0.
    MSG_MOVE_SET_WALK_SPEED_CHEAT = 0x0D0,

    // Enum Value: Defines the MSG MOVE SET WALK SPEED enum value.
    // Value: explicit expression 0x0D1.
    MSG_MOVE_SET_WALK_SPEED = 0x0D1,

    // Enum Value: Defines the MSG MOVE SET SWIM SPEED CHEAT enum value.
    // Value: explicit expression 0x0D2.
    MSG_MOVE_SET_SWIM_SPEED_CHEAT = 0x0D2,

    // Enum Value: Defines the MSG MOVE SET SWIM SPEED enum value.
    // Value: explicit expression 0x0D3.
    MSG_MOVE_SET_SWIM_SPEED = 0x0D3,

    // Enum Value: Defines the MSG MOVE SET SWIM BACK SPEED CHEAT enum value.
    // Value: explicit expression 0x0D4.
    MSG_MOVE_SET_SWIM_BACK_SPEED_CHEAT = 0x0D4,

    // Enum Value: Defines the MSG MOVE SET SWIM BACK SPEED enum value.
    // Value: explicit expression 0x0D5.
    MSG_MOVE_SET_SWIM_BACK_SPEED = 0x0D5,

    // Enum Value: Defines the MSG MOVE SET ALL SPEED CHEAT enum value.
    // Value: explicit expression 0x0D6.
    MSG_MOVE_SET_ALL_SPEED_CHEAT = 0x0D6,

    // Enum Value: Defines the MSG MOVE SET TURN RATE CHEAT enum value.
    // Value: explicit expression 0x0D7.
    MSG_MOVE_SET_TURN_RATE_CHEAT = 0x0D7,

    // Enum Value: Defines the MSG MOVE SET TURN RATE enum value.
    // Value: explicit expression 0x0D8.
    MSG_MOVE_SET_TURN_RATE = 0x0D8,

    // Enum Value: Defines the MSG MOVE TOGGLE COLLISION CHEAT enum value.
    // Value: explicit expression 0x0D9.
    MSG_MOVE_TOGGLE_COLLISION_CHEAT = 0x0D9,

    // Enum Value: Defines the MSG MOVE SET FACING enum value.
    // Value: explicit expression 0x0DA.
    MSG_MOVE_SET_FACING = 0x0DA,

    // Enum Value: Defines the MSG MOVE SET PITCH enum value.
    // Value: explicit expression 0x0DB.
    MSG_MOVE_SET_PITCH = 0x0DB,

    // Enum Value: Defines the MSG MOVE WORLDPORT ACK enum value.
    // Value: explicit expression 0x0DC.
    MSG_MOVE_WORLDPORT_ACK = 0x0DC,

    // Enum Value: Defines the SMSG MONSTER MOVE enum value.
    // Value: explicit expression 0x0DD.
    SMSG_MONSTER_MOVE = 0x0DD,

    // Enum Value: Defines the SMSG MOVE WATER WALK enum value.
    // Value: explicit expression 0x0DE.
    SMSG_MOVE_WATER_WALK = 0x0DE,

    // Enum Value: Defines the SMSG MOVE LAND WALK enum value.
    // Value: explicit expression 0x0DF.
    SMSG_MOVE_LAND_WALK = 0x0DF,

    // Enum Value: Defines the MSG MOVE SET RAW POSITION ACK enum value.
    // Value: explicit expression 0x0E0.
    MSG_MOVE_SET_RAW_POSITION_ACK = 0x0E0,

    // Enum Value: Defines the CMSG MOVE SET RAW POSITION enum value.
    // Value: explicit expression 0x0E1.
    CMSG_MOVE_SET_RAW_POSITION = 0x0E1,

    // Enum Value: Defines the SMSG FORCE RUN SPEED CHANGE enum value.
    // Value: explicit expression 0x0E2.
    SMSG_FORCE_RUN_SPEED_CHANGE = 0x0E2,

    // Enum Value: Defines the CMSG FORCE RUN SPEED CHANGE ACK enum value.
    // Value: explicit expression 0x0E3.
    CMSG_FORCE_RUN_SPEED_CHANGE_ACK = 0x0E3,

    // Enum Value: Defines the SMSG FORCE RUN BACK SPEED CHANGE enum value.
    // Value: explicit expression 0x0E4.
    SMSG_FORCE_RUN_BACK_SPEED_CHANGE = 0x0E4,

    // Enum Value: Defines the CMSG FORCE RUN BACK SPEED CHANGE ACK enum value.
    // Value: explicit expression 0x0E5.
    CMSG_FORCE_RUN_BACK_SPEED_CHANGE_ACK = 0x0E5,

    // Enum Value: Defines the SMSG FORCE SWIM SPEED CHANGE enum value.
    // Value: explicit expression 0x0E6.
    SMSG_FORCE_SWIM_SPEED_CHANGE = 0x0E6,

    // Enum Value: Defines the CMSG FORCE SWIM SPEED CHANGE ACK enum value.
    // Value: explicit expression 0x0E7.
    CMSG_FORCE_SWIM_SPEED_CHANGE_ACK = 0x0E7,

    // Enum Value: Defines the SMSG FORCE MOVE ROOT enum value.
    // Value: explicit expression 0x0E8.
    SMSG_FORCE_MOVE_ROOT = 0x0E8,

    // Enum Value: Defines the CMSG FORCE MOVE ROOT ACK enum value.
    // Value: explicit expression 0x0E9.
    CMSG_FORCE_MOVE_ROOT_ACK = 0x0E9,

    // Enum Value: Defines the SMSG FORCE MOVE UNROOT enum value.
    // Value: explicit expression 0x0EA.
    SMSG_FORCE_MOVE_UNROOT = 0x0EA,

    // Enum Value: Defines the CMSG FORCE MOVE UNROOT ACK enum value.
    // Value: explicit expression 0x0EB.
    CMSG_FORCE_MOVE_UNROOT_ACK = 0x0EB,

    // Enum Value: Defines the MSG MOVE ROOT enum value.
    // Value: explicit expression 0x0EC.
    MSG_MOVE_ROOT = 0x0EC,

    // Enum Value: Defines the MSG MOVE UNROOT enum value.
    // Value: explicit expression 0x0ED.
    MSG_MOVE_UNROOT = 0x0ED,

    // Enum Value: Defines the MSG MOVE HEARTBEAT enum value.
    // Value: explicit expression 0x0EE.
    MSG_MOVE_HEARTBEAT = 0x0EE,

    // Enum Value: Defines the SMSG MOVE KNOCK BACK enum value.
    // Value: explicit expression 0x0EF.
    SMSG_MOVE_KNOCK_BACK = 0x0EF,

    // Enum Value: Defines the CMSG MOVE KNOCK BACK ACK enum value.
    // Value: explicit expression 0x0F0.
    CMSG_MOVE_KNOCK_BACK_ACK = 0x0F0,

    // Enum Value: Defines the MSG MOVE KNOCK BACK enum value.
    // Value: explicit expression 0x0F1.
    MSG_MOVE_KNOCK_BACK = 0x0F1,

    // Enum Value: Defines the SMSG MOVE FEATHER FALL enum value.
    // Value: explicit expression 0x0F2.
    SMSG_MOVE_FEATHER_FALL = 0x0F2,

    // Enum Value: Defines the SMSG MOVE NORMAL FALL enum value.
    // Value: explicit expression 0x0F3.
    SMSG_MOVE_NORMAL_FALL = 0x0F3,

    // Enum Value: Defines the SMSG MOVE SET HOVER enum value.
    // Value: explicit expression 0x0F4.
    SMSG_MOVE_SET_HOVER = 0x0F4,

    // Enum Value: Defines the SMSG MOVE UNSET HOVER enum value.
    // Value: explicit expression 0x0F5.
    SMSG_MOVE_UNSET_HOVER = 0x0F5,

    // Enum Value: Defines the CMSG MOVE HOVER ACK enum value.
    // Value: explicit expression 0x0F6.
    CMSG_MOVE_HOVER_ACK = 0x0F6,

    // Enum Value: Defines the MSG MOVE HOVER enum value.
    // Value: explicit expression 0x0F7.
    MSG_MOVE_HOVER = 0x0F7,

    // Enum Value: Defines the CMSG OPENING CINEMATIC enum value.
    // Value: explicit expression 0x0F9.
    CMSG_OPENING_CINEMATIC = 0x0F9,

    // Enum Value: Defines the CMSG NEXT CINEMATIC CAMERA enum value.
    // Value: explicit expression 0x0FB.
    CMSG_NEXT_CINEMATIC_CAMERA = 0x0FB,

    // Enum Value: Defines the CMSG COMPLETE CINEMATIC enum value.
    // Value: explicit expression 0x0FC.
    CMSG_COMPLETE_CINEMATIC = 0x0FC,

    // Enum Value: Defines the SMSG TUTORIAL FLAGS enum value.
    // Value: explicit expression 0x0FD.
    SMSG_TUTORIAL_FLAGS = 0x0FD,

    // Enum Value: Defines the CMSG TUTORIAL FLAG enum value.
    // Value: explicit expression 0x0FE.
    CMSG_TUTORIAL_FLAG = 0x0FE,

    // Enum Value: Defines the CMSG TUTORIAL CLEAR enum value.
    // Value: explicit expression 0x0FF.
    CMSG_TUTORIAL_CLEAR = 0x0FF,

    // Enum Value: Defines the CMSG TUTORIAL RESET enum value.
    // Value: explicit expression 0x100.
    CMSG_TUTORIAL_RESET = 0x100,

    // Enum Value: Defines the CMSG STANDSTATECHANGE enum value.
    // Value: explicit expression 0x101.
    CMSG_STANDSTATECHANGE = 0x101,

    // Enum Value: Defines the CMSG AUTOEQUIP ITEM enum value.
    // Value: explicit expression 0x10A.
    CMSG_AUTOEQUIP_ITEM = 0x10A,

    // Enum Value: Defines the CMSG AUTOSTORE BAG ITEM enum value.
    // Value: explicit expression 0x10B.
    CMSG_AUTOSTORE_BAG_ITEM = 0x10B,

    // Enum Value: Defines the CMSG SWAP ITEM enum value.
    // Value: explicit expression 0x10C.
    CMSG_SWAP_ITEM = 0x10C,

    // Enum Value: Defines the CMSG SWAP INV ITEM enum value.
    // Value: explicit expression 0x10D.
    CMSG_SWAP_INV_ITEM = 0x10D,

    // Enum Value: Defines the CMSG SPLIT ITEM enum value.
    // Value: explicit expression 0x10E.
    CMSG_SPLIT_ITEM = 0x10E,

    // Enum Value: Defines the CMSG AUTOEQUIP ITEM SLOT enum value.
    // Value: explicit expression 0x10F.
    CMSG_AUTOEQUIP_ITEM_SLOT = 0x10F,

    // Enum Value: Defines the CMSG DESTROYITEM enum value.
    // Value: explicit expression 0x111.
    CMSG_DESTROYITEM = 0x111,

    // Enum Value: Defines the SMSG INVENTORY CHANGE FAILURE enum value.
    // Value: explicit expression 0x112.
    SMSG_INVENTORY_CHANGE_FAILURE = 0x112,

    // Enum Value: Defines the SMSG OPEN CONTAINER enum value.
    // Value: explicit expression 0x113.
    SMSG_OPEN_CONTAINER = 0x113,

    // Enum Value: Defines the SMSG INITIALIZE FACTIONS enum value.
    // Value: explicit expression 0x122.
    SMSG_INITIALIZE_FACTIONS = 0x122,

    // Enum Value: Defines the CMSG SET ACTION BUTTON enum value.
    // Value: explicit expression 0x128.
    CMSG_SET_ACTION_BUTTON = 0x128,

    // Enum Value: Defines the SMSG ACTION BUTTONS enum value.
    // Value: explicit expression 0x129.
    SMSG_ACTION_BUTTONS = 0x129,

    // Enum Value: Defines the SMSG INITIAL SPELLS enum value.
    // Value: explicit expression 0x12A.
    SMSG_INITIAL_SPELLS = 0x12A,

    // Enum Value: Defines the SMSG BINDPOINTUPDATE enum value.
    // Value: explicit expression 0x155.
    SMSG_BINDPOINTUPDATE = 0x155,

    // Enum Value: Defines the CMSG BANKER ACTIVATE enum value.
    // Value: explicit expression 0x1B7.
    CMSG_BANKER_ACTIVATE = 0x1B7,

    // Enum Value: Defines the SMSG SHOW BANK enum value.
    // Value: explicit expression 0x1B8.
    SMSG_SHOW_BANK = 0x1B8,

    // Enum Value: Defines the CMSG BUY BANK SLOT enum value.
    // Value: explicit expression 0x1B9.
    CMSG_BUY_BANK_SLOT = 0x1B9,

    // Enum Value: Defines the SMSG BUY BANK SLOT RESULT enum value.
    // Value: explicit expression 0x1BA.
    SMSG_BUY_BANK_SLOT_RESULT = 0x1BA,

    // Enum Value: Defines the SMSG NOTIFICATION enum value.
    // Value: explicit expression 0x1CB.
    SMSG_NOTIFICATION = 0x1CB,

    // Enum Value: Defines the CMSG PLAYED TIME enum value.
    // Value: explicit expression 0x1CC.
    CMSG_PLAYED_TIME = 0x1CC,

    // Enum Value: Defines the SMSG PLAYED TIME enum value.
    // Value: explicit expression 0x1CD.
    SMSG_PLAYED_TIME = 0x1CD,

    // Enum Value: Defines the CMSG QUERY TIME enum value.
    // Value: explicit expression 0x1CE.
    CMSG_QUERY_TIME = 0x1CE,

    // Enum Value: Defines the SMSG QUERY TIME RESPONSE enum value.
    // Value: explicit expression 0x1CF.
    SMSG_QUERY_TIME_RESPONSE = 0x1CF,

    // Enum Value: Defines the CMSG PING enum value.
    // Value: explicit expression 0x1DC.
    CMSG_PING = 0x1DC,

    // Enum Value: Defines the SMSG PONG enum value.
    // Value: explicit expression 0x1DD.
    SMSG_PONG = 0x1DD,

    // Enum Value: Defines the CMSG ZONEUPDATE enum value.
    // Value: explicit expression 0x1F4.
    CMSG_ZONEUPDATE = 0x1F4,

    // Enum Value: Defines the SMSG AUTH CHALLENGE enum value.
    // Value: explicit expression 0x1EC.
    SMSG_AUTH_CHALLENGE = 0x1EC,

    // Enum Value: Defines the CMSG AUTH SESSION enum value.
    // Value: explicit expression 0x1ED.
    CMSG_AUTH_SESSION = 0x1ED,

    // Enum Value: Defines the SMSG AUTH RESPONSE enum value.
    // Value: explicit expression 0x1EE.
    SMSG_AUTH_RESPONSE = 0x1EE,

    // Enum Value: Defines the SMSG ACCOUNT DATA TIMES enum value.
    // Value: explicit expression 0x209.
    SMSG_ACCOUNT_DATA_TIMES = 0x209,

    // Enum Value: Defines the CMSG REQUEST ACCOUNT DATA enum value.
    // Value: explicit expression 0x20A.
    CMSG_REQUEST_ACCOUNT_DATA = 0x20A,

    // Enum Value: Defines the CMSG UPDATE ACCOUNT DATA enum value.
    // Value: explicit expression 0x20B.
    CMSG_UPDATE_ACCOUNT_DATA = 0x20B,

    // Enum Value: Defines the SMSG UPDATE ACCOUNT DATA enum value.
    // Value: explicit expression 0x20C.
    SMSG_UPDATE_ACCOUNT_DATA = 0x20C,

    // Enum Value: Defines the SMSG SET REST START enum value.
    // Value: explicit expression 0x21E.
    SMSG_SET_REST_START = 0x21E,

    // Enum Value: Defines the SMSG LOGIN VERIFY WORLD enum value.
    // Value: explicit expression 0x236.
    SMSG_LOGIN_VERIFY_WORLD = 0x236,

    // Enum Value: Defines the CMSG SET ACTIONBAR TOGGLES enum value.
    // Value: explicit expression 0x2BF.
    CMSG_SET_ACTIONBAR_TOGGLES = 0x2BF,

    // Enum Value: Defines the CMSG ITEM NAME QUERY enum value.
    // Value: explicit expression 0x2C4.
    CMSG_ITEM_NAME_QUERY = 0x2C4,

    // Enum Value: Defines the SMSG ITEM NAME QUERY RESPONSE enum value.
    // Value: explicit expression 0x2C5.
    SMSG_ITEM_NAME_QUERY_RESPONSE = 0x2C5,

    // Enum Value: Defines the SMSG ADDON INFO enum value.
    // Value: explicit expression 0x2EF.
    SMSG_ADDON_INFO = 0x2EF,

    // Enum Value: Defines the SMSG MOTD enum value.
    // Value: explicit expression 0x33D.
    SMSG_MOTD = 0x33D,
}
