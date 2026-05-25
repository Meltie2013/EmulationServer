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
  * Parses in-game chat command text, resolves the matching handler, checks RBAC permissions, and executes the command.
  * Command behavior is owned by individual command files; this service only handles common routing and validation.
  */
public sealed class InGameCommandService
{
    private readonly InGameCommandRegistry _registry;
    private readonly InGameCommandDependencies _dependencies;

    public InGameCommandService(InGameCommandDependencies? dependencies = null, InGameCommandRegistry? registry = null)
    {
        _registry = registry ?? InGameCommandRegistry.CreateDefault();
        _dependencies = dependencies ?? InGameCommandDependencies.Empty;
    }

    /**
      * Executes an in-game command and returns the system message text that should be sent back to the player.
      */
    public async Task<string> ExecuteAsync(IInGameCommandSession session, string commandText, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        string normalized = NormalizeCommandText(commandText);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "Command text is empty.";
        }

        string[] parts = normalized.Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        string commandName = parts[0];
        string arguments = parts.Length > 1 ? parts[1] : string.Empty;

        if (!_registry.TryGetCommand(commandName, out IChatCommand command))
        {
            return $"Unknown command '{commandName}'. Type .help for available commands.";
        }

        if (!session.HasPermission(command.RequiredPermission))
        {
            return "You do not have permission to use that command.";
        }

        ChatCommandContext context = new(session, normalized, commandName, arguments, _registry, _dependencies);
        return await command.ExecuteAsync(context, cancellationToken);
    }

    /**
      * Removes the leading chat command prefix before token parsing.
      */
    private static string NormalizeCommandText(string commandText)
    {
        string normalized = (commandText ?? string.Empty).Trim();
        return normalized.StartsWith(".", StringComparison.Ordinal) ? normalized[1..].Trim() : normalized;
    }
}
