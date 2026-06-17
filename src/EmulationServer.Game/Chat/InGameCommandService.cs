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
// File: src/EmulationServer.Game/Chat/InGameCommandService.cs
// Purpose: Contains in game command service code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Commands;

// Type: InGameCommandService
// Purpose: Provides in game command service behavior for the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - dependencies: Dependencies value supplied by the caller for this operation.
// - registry: Registry value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class InGameCommandService(InGameCommandDependencies? dependencies = null, InGameCommandRegistry? registry = null)
{
    // Method: CreateDefault
    // Purpose: Applies create default changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the in game command registry registry = registry ?? in game command registry. value produced by this operation.
    // Notes: This keeps the operation scoped to InGameCommandService so callers do not duplicate validation, protocol, or persistence rules.
    private readonly InGameCommandRegistry _registry = registry ?? InGameCommandRegistry.CreateDefault();
    // Field: Stores the dependencies state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current dependencies backing value maintained by the owning type.
    private readonly InGameCommandDependencies _dependencies = dependencies ?? InGameCommandDependencies.Empty;

    // Method: ExecuteAsync
    // Purpose: Controls the execute lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - session: Session value supplied by the caller for this operation.
    // - commandText: Command text value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to InGameCommandService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: NormalizeCommandText
    // Purpose: Converts incoming data into normalize command text form for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - commandText: Command text value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to InGameCommandService so callers do not duplicate validation, protocol, or persistence rules.
    private static string NormalizeCommandText(string commandText)
    {
        string normalized = (commandText ?? string.Empty).Trim();
        return normalized.StartsWith('.') ? normalized[1..].Trim() : normalized;
    }
}
