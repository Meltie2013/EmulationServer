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
  * Opens the current player's bank UI for inventory and bank-slot testing.
  */
public sealed class BankCommand : IChatCommand
{
    public string Name => "bank";

    public IReadOnlyList<string> Aliases { get; } = [];

    public uint RequiredPermission => RbacPermissionIds.CommandBank;

    public string Description => "Opens your character bank for inventory testing.";

    public string Syntax => ".bank";

    public async Task<string> ExecuteAsync(ChatCommandContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Session.RequireCurrentPlayer();
        await context.Session.OpenBankAsync(cancellationToken);
        return "Bank window opened.";
    }
}
