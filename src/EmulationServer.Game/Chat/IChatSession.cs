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

using EmulationServer.Game.Players;

/**
  * File overview: src/EmulationServer.Game/Chat/IChatSession.cs
  * Documents the IChatSession source file in the chat channel normalization, language handling, and message routing area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Chat;

/**
  * Defines the contract for chat session behavior in the chat channel normalization, language handling, and message routing layer.
  * Implementations are expected to keep caller-facing behavior stable because other servers depend on this shape across shared game and network workflows.
  */
public interface IChatSession
{
    /**
      * Exposes the current player value required by chat session callers.
      * The property keeps implementations aligned on the data the shared workflow needs to read without tying callers to a concrete session or service type.
      */
    PlayerLoginRecord? CurrentPlayer { get; }

    /**
      * Requires the current player value and throws when the implementing session cannot provide it.
      * Callers use the contract method so gameplay, database, and network code can depend on behavior rather than a concrete implementation.
      */
    PlayerLoginRecord RequireCurrentPlayer();

    /**
      * Determines whether the implementing session is currently in chat channel.
      * Callers use the contract method so gameplay, database, and network code can depend on behavior rather than a concrete implementation.
      */
    bool IsInChatChannel(string channelName);

    /**
      * Joins the implementing session to the chat channel state or channel.
      * Callers use the contract method so gameplay, database, and network code can depend on behavior rather than a concrete implementation.
      */
    void JoinChatChannel(string channelName);

    /**
      * Removes the implementing session from the chat channel state or channel.
      * Callers use the contract method so gameplay, database, and network code can depend on behavior rather than a concrete implementation.
      */
    void LeaveChatChannel(string channelName);
}
