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
  * File overview: src/EmulationServer.Game/Chat/ChatMessageType.cs
  * Documents the ChatMessageType source file in the chat channel normalization, language handling, and message routing area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Chat;

/**
  * Lists the supported chat message type values used by the chat channel normalization, language handling, and message routing layer.
  * Numeric values are part of the project contract and should only be changed when the related client packet, DBC value, or database schema is updated as well.
  */
public enum ChatMessageType : uint
{
    /**
      * Represents the say value for chat message type handling.
      */
    Say = 0,
    /**
      * Represents the party value for chat message type handling.
      */
    Party = 1,
    /**
      * Represents the raid value for chat message type handling.
      */
    Raid = 2,
    /**
      * Represents the guild value for chat message type handling.
      */
    Guild = 3,
    /**
      * Represents the officer value for chat message type handling.
      */
    Officer = 4,
    /**
      * Represents the yell value for chat message type handling.
      */
    Yell = 5,
    /**
      * Represents the whisper value for chat message type handling.
      */
    Whisper = 6,
    /**
      * Represents the whisper inform value for chat message type handling.
      */
    WhisperInform = 7,
    /**
      * Represents the emote value for chat message type handling.
      */
    Emote = 8,
    /**
      * Represents the text emote value for chat message type handling.
      */
    TextEmote = 9,
    /**
      * Represents the system value for chat message type handling.
      */
    System = 10,
    /**
      * Represents the raid leader value for chat message type handling.
      */
    RaidLeader = 11,
    /**
      * Represents the raid warning value for chat message type handling.
      */
    RaidWarning = 12,
    /**
      * Represents the channel value for chat message type handling.
      */
    Channel = 17,
}
