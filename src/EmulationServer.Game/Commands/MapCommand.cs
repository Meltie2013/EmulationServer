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
  * Handles administrator map service commands from in-game chat.
  */
public sealed class MapCommand : IChatCommand
{
    public string Name => "map";

    public IReadOnlyList<string> Aliases { get; } = [];

    public uint RequiredPermission => RbacPermissionIds.CommandMap;

    public string Description => "Controls MapServer and InstanceServer map services.";

    public string Syntax => ".map";

    public async Task<string> ExecuteAsync(ChatCommandContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);

        IInGameMapCommandExecutor? mapCommands = context.Dependencies.MapCommands;
        if (mapCommands is null)
        {
            return "Map commands are not configured on this server.";
        }

        string[] parts = CommandArgumentParser.Split(context.Arguments);
        if (parts.Length == 0)
        {
            return GetHelp(context);
        }

        string action = parts[0].ToLowerInvariant();
        uint permissionId = action switch
        {
            "info" => RbacPermissionIds.CommandMapInfo,
            "restart" => RbacPermissionIds.CommandMapRestart,
            "shutdown" => RbacPermissionIds.CommandMapShutdown,
            "start" => RbacPermissionIds.CommandMapStart,
            _ => 0
        };

        if (permissionId == 0)
        {
            return GetHelp(context, $"Unknown map command '{parts[0]}'.");
        }

        if (!context.Session.HasPermission(permissionId))
        {
            return "You do not have permission to use that map command.";
        }

        if (parts.Length < 2)
        {
            return $"Usage: .map {action} #mapid";
        }

        if (!CommandArgumentParser.TryParseMapId(parts[1], out int mapId))
        {
            return "Map ID must be a non-negative number. Example: .map info #0";
        }

        TimeSpan delay = TimeSpan.Zero;
        if (parts.Length > 2)
        {
            if (action is not ("restart" or "shutdown"))
            {
                return $"Usage: .map {action} #mapid";
            }

            if (!CommandArgumentParser.TryParseDuration(parts[2], out delay))
            {
                return "Timer must be 0, seconds, or values using s/m/h/d/w such as 30s, 5m, or 1h.";
            }
        }

        return await mapCommands.ExecuteMapCommandAsync(action, mapId, delay, context.Session.AccountName, cancellationToken);
    }

    private static string GetHelp(ChatCommandContext context, string? prefix = null)
    {
        string[] lines =
        [
            "Map commands:",
            context.Session.HasPermission(RbacPermissionIds.CommandMapInfo) ? "  .map info #mapid" : string.Empty,
            context.Session.HasPermission(RbacPermissionIds.CommandMapRestart) ? "  .map restart #mapid #timer" : string.Empty,
            context.Session.HasPermission(RbacPermissionIds.CommandMapShutdown) ? "  .map shutdown #mapid #timer" : string.Empty,
            context.Session.HasPermission(RbacPermissionIds.CommandMapStart) ? "  .map start #mapid" : string.Empty,
        ];

        string help = string.Join('\n', lines.Where(line => !string.IsNullOrWhiteSpace(line)));
        return string.IsNullOrWhiteSpace(prefix) ? help : $"{prefix}\n{help}";
    }
}
