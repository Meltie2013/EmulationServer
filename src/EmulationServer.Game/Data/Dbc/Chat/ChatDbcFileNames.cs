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
 * File overview: src/EmulationServer.Game/Data/Dbc/Chat/ChatDbcFileNames.cs
 * Documents the ChatDbcFileNames source file in the DBC loading and strongly typed client data records area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Game.Data.Dbc.Chat;

/**
 * Owns the chat dbc file names behavior for the DBC loading and strongly typed client data records layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public static class ChatDbcFileNames
{
    /**
     * Defines the constant value for chat channels.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    public const string ChatChannels = "ChatChannels.dbc";
    /**
     * Defines the constant value for languages.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    public const string Languages = "Languages.dbc";

    /**
     * Exposes the core chat dbc files value to callers that need this runtime or configuration data.
     * The property keeps the public surface strongly typed and documents which part of the server workflow owns the value.
     */
    public static IReadOnlyList<string> CoreChatDbcFiles { get; } =
    [
        ChatChannels,
        Languages,
    ];
}
