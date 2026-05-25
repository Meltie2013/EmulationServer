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
  * Handles administrator account management commands from in-game chat.
  */
public sealed class AccountCommand : IChatCommand
{
    public string Name => "account";

    public IReadOnlyList<string> Aliases { get; } = [];

    public uint RequiredPermission => RbacPermissionIds.CommandAccount;

    public string Description => "Manages accounts and direct RBAC account permissions.";

    public string Syntax => ".account";

    public async Task<string> ExecuteAsync(ChatCommandContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);

        IInGameAccountCommandExecutor? accountCommands = context.Dependencies.AccountCommands;
        if (accountCommands is null)
        {
            return "Account commands are not configured on this server.";
        }

        string[] parts = CommandArgumentParser.Split(context.Arguments);
        if (parts.Length == 0)
        {
            return GetHelp(context);
        }

        string action = parts[0].ToLowerInvariant();
        return action switch
        {
            "create" => await ExecuteCreateAsync(context, accountCommands, parts, cancellationToken),
            "delete" => await ExecuteDeleteAsync(context, accountCommands, parts, cancellationToken),
            "remove" => await ExecuteRemovePermissionAsync(context, accountCommands, parts, cancellationToken),
            "set" => await ExecuteSetPermissionAsync(context, accountCommands, parts, cancellationToken),
            _ => GetHelp(context, $"Unknown account command '{parts[0]}'.")
        };
    }

    private static async Task<string> ExecuteCreateAsync(
        ChatCommandContext context,
        IInGameAccountCommandExecutor accountCommands,
        string[] parts,
        CancellationToken cancellationToken)
    {
        if (!context.Session.HasPermission(RbacPermissionIds.CommandAccountCreate))
        {
            return PermissionDenied();
        }

        if (parts.Length < 3)
        {
            return "Usage: .account create #username #password";
        }

        AccountCommandResult result = await accountCommands.CreateAccountAsync(parts[1], parts[2], cancellationToken);
        return result.Message;
    }

    private static async Task<string> ExecuteDeleteAsync(
        ChatCommandContext context,
        IInGameAccountCommandExecutor accountCommands,
        string[] parts,
        CancellationToken cancellationToken)
    {
        if (!context.Session.HasPermission(RbacPermissionIds.CommandAccountDelete))
        {
            return PermissionDenied();
        }

        if (parts.Length < 2)
        {
            return "Usage: .account delete #username";
        }

        AccountCommandResult result = await accountCommands.DeleteAccountAsync(parts[1], cancellationToken);
        return result.Message;
    }

    private static async Task<string> ExecuteRemovePermissionAsync(
        ChatCommandContext context,
        IInGameAccountCommandExecutor accountCommands,
        string[] parts,
        CancellationToken cancellationToken)
    {
        if (!context.Session.HasPermission(RbacPermissionIds.CommandAccountRemovePermission))
        {
            return PermissionDenied();
        }

        if (parts.Length < 4 || !string.Equals(parts[1], "permission", StringComparison.OrdinalIgnoreCase))
        {
            return "Usage: .account remove permission #username #permissionid";
        }

        if (!CommandArgumentParser.TryParseUnsignedId(parts[3], out uint permissionId))
        {
            return "Permission id must be a non-negative number. Example: .account remove permission Admin 195";
        }

        AccountCommandResult result = await accountCommands.RemovePermissionAsync(parts[2], permissionId, cancellationToken);
        return result.Message;
    }

    private static async Task<string> ExecuteSetPermissionAsync(
        ChatCommandContext context,
        IInGameAccountCommandExecutor accountCommands,
        string[] parts,
        CancellationToken cancellationToken)
    {
        if (!context.Session.HasPermission(RbacPermissionIds.CommandAccountSetPermission))
        {
            return PermissionDenied();
        }

        if (parts.Length < 4 || !string.Equals(parts[1], "permission", StringComparison.OrdinalIgnoreCase))
        {
            return "Usage: .account set permission #username #permissionid";
        }

        if (!CommandArgumentParser.TryParseUnsignedId(parts[3], out uint permissionId))
        {
            return "Permission id must be a non-negative number. Example: .account set permission Admin 195";
        }

        AccountCommandResult result = await accountCommands.SetPermissionAsync(parts[2], permissionId, cancellationToken);
        return result.Message;
    }

    private static string GetHelp(ChatCommandContext context, string? prefix = null)
    {
        string[] lines =
        [
            "Account commands:",
            context.Session.HasPermission(RbacPermissionIds.CommandAccountCreate) ? "  .account create #username #password" : string.Empty,
            context.Session.HasPermission(RbacPermissionIds.CommandAccountDelete) ? "  .account delete #username" : string.Empty,
            context.Session.HasPermission(RbacPermissionIds.CommandAccountRemovePermission) ? "  .account remove permission #username #permissionid" : string.Empty,
            context.Session.HasPermission(RbacPermissionIds.CommandAccountSetPermission) ? "  .account set permission #username #permissionid" : string.Empty,
        ];

        string help = string.Join('\n', lines.Where(line => !string.IsNullOrWhiteSpace(line)));
        return string.IsNullOrWhiteSpace(prefix) ? help : $"{prefix}\n{help}";
    }

    private static string PermissionDenied()
    {
        return "You do not have permission to use that account command.";
    }
}
