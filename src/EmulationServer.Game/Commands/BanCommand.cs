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
  * Handles administrator ban commands from in-game chat.
  */
public sealed class BanCommand : IChatCommand
{
    public string Name => "ban";

    public IReadOnlyList<string> Aliases { get; } = [];

    public uint RequiredPermission => RbacPermissionIds.CommandBan;

    public string Description => "Bans accounts.";

    public string Syntax => ".ban";

    public async Task<string> ExecuteAsync(ChatCommandContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);

        IInGameAccountCommandExecutor? accountCommands = context.Dependencies.AccountCommands;
        if (accountCommands is null)
        {
            return "Ban commands are not configured on this server.";
        }

        string[] parts = CommandArgumentParser.Split(context.Arguments);
        if (parts.Length == 0)
        {
            return GetHelp(context);
        }

        if (!string.Equals(parts[0], "account", StringComparison.OrdinalIgnoreCase))
        {
            return GetHelp(context, $"Unknown ban command '{parts[0]}'.");
        }

        if (!context.Session.HasPermission(RbacPermissionIds.CommandBanAccount))
        {
            return "You do not have permission to use that ban command.";
        }

        if (parts.Length < 2)
        {
            return "Usage: .ban account #username";
        }

        AccountCommandResult result = await accountCommands.BanAccountAsync(parts[1], context.Session.AccountName, cancellationToken);
        return result.Message;
    }

    private static string GetHelp(ChatCommandContext context, string? prefix = null)
    {
        string command = context.Session.HasPermission(RbacPermissionIds.CommandBanAccount)
            ? "Ban commands:\n  .ban account #username"
            : "No ban commands are available to your account.";

        return string.IsNullOrWhiteSpace(prefix) ? command : $"{prefix}\n{command}";
    }
}
