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

namespace EmulationServer.WorldServer.Networking.Packets;

public enum WorldOpcode : ushort
{
    CMSG_CHAR_CREATE = 0x036,
    CMSG_CHAR_ENUM = 0x037,
    CMSG_CHAR_DELETE = 0x038,
    SMSG_CHAR_CREATE = 0x03A,
    SMSG_CHAR_ENUM = 0x03B,
    SMSG_CHAR_DELETE = 0x03C,
    CMSG_PLAYER_LOGIN = 0x03D,
    SMSG_NEW_WORLD = 0x03E,
    SMSG_TRANSFER_PENDING = 0x03F,
    SMSG_TRANSFER_ABORTED = 0x040,
    SMSG_CHARACTER_LOGIN_FAILED = 0x041,
    SMSG_LOGIN_SETTIMESPEED = 0x042,
    CMSG_NAME_QUERY = 0x050,
    SMSG_NAME_QUERY_RESPONSE = 0x051,
    CMSG_ITEM_QUERY_SINGLE = 0x056,
    SMSG_ITEM_QUERY_SINGLE_RESPONSE = 0x058,
    CMSG_MESSAGECHAT = 0x095,
    SMSG_MESSAGECHAT = 0x096,
    CMSG_JOIN_CHANNEL = 0x097,
    SMSG_CHANNEL_NOTIFY = 0x099,
    CMSG_LEAVE_CHANNEL = 0x09A,
    CMSG_CHANNEL_LIST = 0x09C,
    SMSG_UPDATE_OBJECT = 0x0A9,
    SMSG_DESTROY_OBJECT = 0x0AA,
    SMSG_TUTORIAL_FLAGS = 0x0FD,
    SMSG_INITIALIZE_FACTIONS = 0x122,
    SMSG_ACTION_BUTTONS = 0x129,
    SMSG_INITIAL_SPELLS = 0x12A,
    SMSG_BINDPOINTUPDATE = 0x155,
    CMSG_ZONEUPDATE = 0x1F4,
    CMSG_PING = 0x1DC,
    SMSG_PONG = 0x1DD,
    SMSG_AUTH_CHALLENGE = 0x1EC,
    CMSG_AUTH_SESSION = 0x1ED,
    SMSG_AUTH_RESPONSE = 0x1EE,
    SMSG_ACCOUNT_DATA_TIMES = 0x209,
    CMSG_REQUEST_ACCOUNT_DATA = 0x20A,
    CMSG_UPDATE_ACCOUNT_DATA = 0x20B,
    SMSG_UPDATE_ACCOUNT_DATA = 0x20C,
    SMSG_LOGIN_VERIFY_WORLD = 0x236,
    SMSG_SET_REST_START = 0x21E,
    SMSG_ADDON_INFO = 0x2EF,
    SMSG_MOTD = 0x33D,
}
