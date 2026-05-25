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
  * Handles server control commands from in-game chat.
  */
public sealed class ServerCommand : IChatCommand
{
    public string Name => "server";

    public IReadOnlyList<string> Aliases { get; } = [];

    public uint RequiredPermission => RbacPermissionIds.CommandServer;

    public string Description => "Schedules shutdown or restart requests for the realm and connected services.";

    public string Syntax => ".server";

    public async Task<string> ExecuteAsync(ChatCommandContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);

        IInGameServerCommandExecutor? serverCommands = context.Dependencies.ServerCommands;
        if (serverCommands is null)
        {
            return "Server commands are not configured on this server.";
        }

        string[] parts = CommandArgumentParser.Split(context.Arguments);
        if (parts.Length == 0)
        {
            return GetHelp(context);
        }

        string action = parts[0].ToLowerInvariant();
        uint permissionId = action switch
        {
            "restart" => RbacPermissionIds.CommandServerRestart,
            "shutdown" => RbacPermissionIds.CommandServerShutdown,
            _ => 0
        };

        if (permissionId == 0)
        {
            return GetHelp(context, $"Unknown server command '{parts[0]}'.");
        }

        if (!context.Session.HasPermission(permissionId))
        {
            return "You do not have permission to use that server command.";
        }

        if (parts.Length < 2)
        {
            return $"Usage: .server {action} #timer";
        }

        if (!CommandArgumentParser.TryParseDuration(parts[1], out TimeSpan delay))
        {
            return "Timer must be 0, seconds, or values using s/m/h/d/w such as 30s, 5m, or 1h.";
        }

        return action == "restart"
            ? await serverCommands.ScheduleRestartAsync(delay, context.Session.AccountName, cancellationToken)
            : await serverCommands.ScheduleShutdownAsync(delay, context.Session.AccountName, cancellationToken);
    }

    private static string GetHelp(ChatCommandContext context, string? prefix = null)
    {
        string[] lines =
        [
            "Server commands:",
            context.Session.HasPermission(RbacPermissionIds.CommandServerRestart) ? "  .server restart #restarttimer" : string.Empty,
            context.Session.HasPermission(RbacPermissionIds.CommandServerShutdown) ? "  .server shutdown #shutdowntimer" : string.Empty,
        ];

        string help = string.Join('\n', lines.Where(line => !string.IsNullOrWhiteSpace(line)));
        return string.IsNullOrWhiteSpace(prefix) ? help : $"{prefix}\n{help}";
    }
}
