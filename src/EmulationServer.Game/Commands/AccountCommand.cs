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
// File: src/EmulationServer.Game/Commands/AccountCommand.cs
// Purpose: Contains account command code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Database.Accounts;

namespace EmulationServer.Game.Commands;

// Type: AccountCommand
// Purpose: Provides account command behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class AccountCommand : IChatCommand
{
    // Property: Gets or sets the name value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: name value exposed by the owning type.
    public string Name => "account";

    // Property: Gets or sets the aliases value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: aliases value exposed by the owning type.
    public IReadOnlyList<string> Aliases { get; } = [];

    // Property: Gets or sets the required permission value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: required permission value exposed by the owning type.
    public uint RequiredPermission => RbacPermissionIds.CommandAccount;

    // Property: Gets or sets the description value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: description value exposed by the owning type.
    public string Description => "Manages accounts and direct RBAC account permissions.";

    // Property: Gets or sets the syntax value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: syntax value exposed by the owning type.
    public string Syntax => ".account";

    // Method: ExecuteAsync
    // Purpose: Controls the execute lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - context: Context value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to AccountCommand so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: ExecuteCreateAsync
    // Purpose: Controls the execute create lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - context: Context value supplied by the caller for this operation.
    // - accountCommands: Account commands value supplied by the caller for this operation.
    // - stringparts: Stringparts value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to AccountCommand so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: ExecuteDeleteAsync
    // Purpose: Controls the execute delete lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - context: Context value supplied by the caller for this operation.
    // - accountCommands: Account commands value supplied by the caller for this operation.
    // - stringparts: Stringparts value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to AccountCommand so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: ExecuteRemovePermissionAsync
    // Purpose: Controls the execute remove permission lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - context: Context value supplied by the caller for this operation.
    // - accountCommands: Account commands value supplied by the caller for this operation.
    // - stringparts: Stringparts value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to AccountCommand so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: ExecuteSetPermissionAsync
    // Purpose: Controls the execute set permission lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - context: Context value supplied by the caller for this operation.
    // - accountCommands: Account commands value supplied by the caller for this operation.
    // - stringparts: Stringparts value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to AccountCommand so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: GetHelp
    // Purpose: Retrieves get help data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - context: Context value supplied by the caller for this operation.
    // - prefix: Prefix value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to AccountCommand so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: PermissionDenied
    // Purpose: Executes the permission denied operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to AccountCommand so callers do not duplicate validation, protocol, or persistence rules.
    private static string PermissionDenied()
    {
        return "You do not have permission to use that account command.";
    }
}
