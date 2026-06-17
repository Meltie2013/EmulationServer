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
// File: src/EmulationServer.Game/Commands/BankCommand.cs
// Purpose: Contains bank command code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Database.Accounts;

namespace EmulationServer.Game.Commands;

// Type: BankCommand
// Purpose: Provides bank command behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class BankCommand : IChatCommand
{
    // Property: Gets or sets the name value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: name value exposed by the owning type.
    public string Name => "bank";

    // Property: Gets or sets the aliases value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: aliases value exposed by the owning type.
    public IReadOnlyList<string> Aliases { get; } = [];

    // Property: Gets or sets the required permission value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: required permission value exposed by the owning type.
    public uint RequiredPermission => RbacPermissionIds.CommandBank;

    // Property: Gets or sets the description value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: description value exposed by the owning type.
    public string Description => "Opens your character bank for inventory testing.";

    // Property: Gets or sets the syntax value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: syntax value exposed by the owning type.
    public string Syntax => ".bank";

    // Method: ExecuteAsync
    // Purpose: Controls the execute lifecycle step for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - context: Context value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to BankCommand so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task<string> ExecuteAsync(ChatCommandContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Session.RequireCurrentPlayer();
        await context.Session.OpenBankAsync(cancellationToken);
        return "Bank window opened.";
    }
}
