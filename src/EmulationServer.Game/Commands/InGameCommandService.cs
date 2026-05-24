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

using System.Globalization;

using EmulationServer.Game.Players;

/**
  * File overview: src/EmulationServer.Game/Commands/InGameCommandService.cs
  * Documents the InGameCommandService source file in the in-game command parsing and command session access area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Commands;

/**
  * Owns the in game command service behavior for the in-game command parsing and command session access layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class InGameCommandService
{
    /**
      * Performs the execute operation for the in-game command parsing and command session access workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: session, commandText, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public Task<string> ExecuteAsync(IInGameCommandSession session, string commandText, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        PlayerLoginRecord player = session.RequireCurrentPlayer();
        if (session.AccountGmLevel == 0)
        {
            return Task.FromResult("You do not have permission to use commands.");
        }

        string normalized = commandText.Trim();
        if (normalized.StartsWith(".", StringComparison.Ordinal))
        {
            normalized = normalized[1..].Trim();
        }

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Task.FromResult("Command text is empty.");
        }

        string[] parts = normalized.Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        string command = parts[0].ToLowerInvariant();

        return Task.FromResult(command switch
        {
            "help" or "commands" => "Available commands: .help, .where, .gps, .online, .motd",
            "where" or "gps" => string.Create(CultureInfo.InvariantCulture, $"{player.Name}: map={player.Map}, zone={player.Zone}, x={player.PositionX:0.###}, y={player.PositionY:0.###}, z={player.PositionZ:0.###}, o={player.Orientation:0.###}"),
            "online" => string.Create(CultureInfo.InvariantCulture, $"Active world players: {session.ActivePlayerCount}"),
            "motd" => session.MessageOfTheDay,
            _ => $"Unknown command '{command}'. Type .help for available commands.",
        });
    }
}
