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
  * Registers chat command handlers and resolves command names or aliases to a single handler.
  * The registry replaces the old switch-based command list so new commands can be added as separate files.
  */
public sealed class InGameCommandRegistry
{
    private readonly Dictionary<string, IChatCommand> _commandsByToken;
    private readonly IReadOnlyList<IChatCommand> _commands;

    public InGameCommandRegistry(IEnumerable<IChatCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        Dictionary<string, IChatCommand> commandsByToken = new(StringComparer.OrdinalIgnoreCase);
        List<IChatCommand> commandList = [];

        foreach (IChatCommand command in commands)
        {
            RegisterCommand(command, commandsByToken, commandList);
        }

        _commandsByToken = commandsByToken;
        _commands = commandList
            .OrderBy(command => command.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /**
      * Creates the built-in command registry used by WorldServer.
      */
    public static InGameCommandRegistry CreateDefault()
    {
        return new InGameCommandRegistry(
        [
            new AccountCommand(),
            new BanCommand(),
            new HelpCommand(),
            new MapCommand(),
            new ReloadCommand(),
            new ServerCommand(),
        ]);
    }

    /**
      * Attempts to resolve a command name or alias.
      */
    public bool TryGetCommand(string token, out IChatCommand command)
    {
        if (_commandsByToken.TryGetValue(token, out IChatCommand? resolved))
        {
            command = resolved;
            return true;
        }

        command = null!;
        return false;
    }

    /**
      * Returns one entry per command, excluding aliases.
      */
    public IReadOnlyList<IChatCommand> Commands => _commands;

    /**
      * Returns the commands visible to the supplied session after RBAC checks.
      */
    public IReadOnlyList<IChatCommand> GetAvailableCommands(IInGameCommandSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return _commands
            .Where(command => session.HasPermission(command.RequiredPermission))
            .ToArray();
    }

    /**
      * Registers the primary command name and every alias while rejecting duplicate tokens early during startup.
      */
    private static void RegisterCommand(
        IChatCommand command,
        Dictionary<string, IChatCommand> commandsByToken,
        List<IChatCommand> commandList)
    {
        ArgumentNullException.ThrowIfNull(command);

        string name = NormalizeToken(command.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Chat command name cannot be empty.");
        }

        AddToken(name, command, commandsByToken);
        foreach (string alias in command.Aliases)
        {
            string token = NormalizeToken(alias);
            if (!string.IsNullOrWhiteSpace(token))
            {
                AddToken(token, command, commandsByToken);
            }
        }

        commandList.Add(command);
    }

    /**
      * Adds one lookup token to the registry.
      */
    private static void AddToken(string token, IChatCommand command, Dictionary<string, IChatCommand> commandsByToken)
    {
        if (commandsByToken.TryGetValue(token, out IChatCommand? existing))
        {
            throw new InvalidOperationException($"Chat command token '{token}' is already registered by '{existing.Name}'.");
        }

        commandsByToken[token] = command;
    }

    /**
      * Normalizes command tokens so handlers do not need to care whether an alias was configured with a dot prefix.
      */
    private static string NormalizeToken(string token)
    {
        token = token.Trim();
        return token.StartsWith(".", StringComparison.Ordinal) ? token[1..].Trim() : token;
    }
}
