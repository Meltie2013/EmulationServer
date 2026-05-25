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
  * Handles reload commands from in-game chat.
  */
public sealed class ReloadCommand : IChatCommand
{
    public string Name => "reload";

    public IReadOnlyList<string> Aliases { get; } = [];

    public uint RequiredPermission => RbacPermissionIds.CommandReload;

    public string Description => "Reloads runtime data.";

    public string Syntax => ".reload";

    public async Task<string> ExecuteAsync(ChatCommandContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);

        string[] parts = CommandArgumentParser.Split(context.Arguments);
        if (parts.Length == 0)
        {
            return GetHelp(context);
        }

        if (!string.Equals(parts[0], "rbac", StringComparison.OrdinalIgnoreCase))
        {
            return GetHelp(context, $"Unknown reload command '{parts[0]}'.");
        }

        if (!context.Session.HasPermission(RbacPermissionIds.CommandReloadRbac))
        {
            return "You do not have permission to reload RBAC data.";
        }

        if (context.Dependencies.RbacCommands is not null)
        {
            return await context.Dependencies.RbacCommands.ReloadRbacAsync(cancellationToken);
        }

        await context.Session.ReloadPermissionsAsync(cancellationToken);
        return $"RBAC data was reloaded for account '{context.Session.AccountName}'.";
    }

    private static string GetHelp(ChatCommandContext context, string? prefix = null)
    {
        string command = context.Session.HasPermission(RbacPermissionIds.CommandReloadRbac)
            ? "Reload commands:\n  .reload rbac"
            : "No reload commands are available to your account.";

        return string.IsNullOrWhiteSpace(prefix) ? command : $"{prefix}\n{command}";
    }
}
