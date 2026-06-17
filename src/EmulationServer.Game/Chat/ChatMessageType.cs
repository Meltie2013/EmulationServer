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
// File: src/EmulationServer.Game/Chat/ChatMessageType.cs
// Purpose: Contains chat message type code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Chat;

// Type: ChatMessageType
// Purpose: Defines the allowed chat message type values used by the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public enum ChatMessageType : uint
{

    // Enum Value: Defines the say enum value.
    // Value: explicit expression 0.
    Say = 0,

    // Enum Value: Defines the party enum value.
    // Value: explicit expression 1.
    Party = 1,

    // Enum Value: Defines the raid enum value.
    // Value: explicit expression 2.
    Raid = 2,

    // Enum Value: Defines the guild enum value.
    // Value: explicit expression 3.
    Guild = 3,

    // Enum Value: Defines the officer enum value.
    // Value: explicit expression 4.
    Officer = 4,

    // Enum Value: Defines the yell enum value.
    // Value: explicit expression 5.
    Yell = 5,

    // Enum Value: Defines the whisper enum value.
    // Value: explicit expression 6.
    Whisper = 6,

    // Enum Value: Defines the whisper inform enum value.
    // Value: explicit expression 7.
    WhisperInform = 7,

    // Enum Value: Defines the emote enum value.
    // Value: explicit expression 8.
    Emote = 8,

    // Enum Value: Defines the text emote enum value.
    // Value: explicit expression 9.
    TextEmote = 9,

    // Enum Value: Defines the system enum value.
    // Value: explicit expression 10.
    System = 10,

    // Enum Value: Defines the raid leader enum value.
    // Value: explicit expression 11.
    RaidLeader = 11,

    // Enum Value: Defines the raid warning enum value.
    // Value: explicit expression 12.
    RaidWarning = 12,

    // Enum Value: Defines the channel enum value.
    // Value: explicit expression 17.
    Channel = 17,
}
