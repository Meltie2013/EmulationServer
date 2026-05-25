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

using EmulationServer.Database.Accounts;

namespace EmulationServer.Game.Commands;

/**
  * Shows the commands available to the current account after RBAC filtering.
  */
public sealed class HelpCommand : IChatCommand
{
    public string Name => "help";

    public IReadOnlyList<string> Aliases { get; } = ["commands"];

    public uint RequiredPermission => RbacPermissionIds.CommandHelp;

    public string Description => "Shows available chat commands or help for one command.";

    public string Syntax => ".help #command";

    public Task<string> ExecuteAsync(ChatCommandContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);

        string[] parts = CommandArgumentParser.Split(context.Arguments);
        if (parts.Length > 0)
        {
            string commandName = CommandArgumentParser.RemoveArgumentPrefix(parts[0]);
            return Task.FromResult(GetCommandHelp(context, commandName));
        }

        IReadOnlyList<IChatCommand> commands = context.Registry.GetAvailableCommands(context.Session);
        if (commands.Count == 0)
        {
            return Task.FromResult("No chat commands are available to your account.");
        }

        string[] commandLines = commands
            .Select(command => $"{command.Name} - {command.Description}")
            .ToArray();

        return Task.FromResult("Available commands:\n" + string.Join('\n', commandLines) + "\nType .help #command for command syntax.");
    }

    private static string GetCommandHelp(ChatCommandContext context, string commandName)
    {
        if (string.IsNullOrWhiteSpace(commandName))
        {
            return "Usage: .help #command";
        }

        if (!context.Registry.TryGetCommand(commandName, out IChatCommand command))
        {
            return $"Unknown command '{commandName}'.";
        }

        if (!context.Session.HasPermission(command.RequiredPermission))
        {
            return "You do not have permission to view help for that command.";
        }

        return $"{command.Name} - {command.Description}\nSyntax: {command.Syntax}";
    }
}
