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

namespace EmulationServer.Game.Commands;

/**
  * Carries parsed command input and shared registry data into a command handler.
  * The context keeps command files small and avoids adding handler-specific methods to the session contract.
  */
public sealed class ChatCommandContext
{
    public ChatCommandContext(
        IInGameCommandSession session,
        string rawText,
        string commandName,
        string arguments,
        InGameCommandRegistry registry,
        InGameCommandDependencies dependencies)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
        RawText = rawText ?? string.Empty;
        CommandName = commandName ?? string.Empty;
        Arguments = arguments ?? string.Empty;
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        Dependencies = dependencies ?? InGameCommandDependencies.Empty;
    }

    /**
      * Session that issued the command.
      */
    public IInGameCommandSession Session { get; }

    /**
      * Original command text after the chat prefix was recognized.
      */
    public string RawText { get; }

    /**
      * Parsed command token without the dot prefix.
      */
    public string CommandName { get; }

    /**
      * Remaining text after the command token.
      */
    public string Arguments { get; }

    /**
      * Command registry used by meta commands such as help.
      */
    public InGameCommandRegistry Registry { get; }

    /**
      * Runtime command dependencies supplied by the server executable.
      */
    public InGameCommandDependencies Dependencies { get; }
}
