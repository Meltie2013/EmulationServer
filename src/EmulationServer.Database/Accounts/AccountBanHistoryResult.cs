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

/**
  * File overview: src/EmulationServer.Database/Accounts/AccountBanHistoryResult.cs
  * Documents the account ban lookup result used when an administrator asks for ban history.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Database.Accounts;

/**
  * Describes whether an account exists and includes every known ban history row for display.
  * This avoids treating an existing account with no bans the same as a missing account.
  */
public sealed record AccountBanHistoryResult(
    bool AccountExists,
    string Username,
    IReadOnlyList<AccountBanRecord> Bans);
