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
// File: src/EmulationServer.Database/Accounts/AccountBanRecord.cs
// Purpose: Contains account ban record code for the database persistence, repository, and MySQL connectivity layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Database.Accounts;

// Type: AccountBanRecord
// Purpose: Represents account ban record data passed through the database persistence, repository, and MySQL connectivity layer.
// Constructor values:
// - AccountId: Account ID identifier used to select the exact record, object, or runtime owner.
// - Username: Username value supplied by the caller for this operation.
// - BanDate: Ban date value supplied by the caller for this operation.
// - UnbanDate: Unban date value supplied by the caller for this operation.
// - BannedBy: Banned by value supplied by the caller for this operation.
// - BanReason: Ban reason value supplied by the caller for this operation.
// - Active: Active value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record AccountBanRecord(
    uint AccountId,
    string Username,
    ulong BanDate,
    ulong UnbanDate,
    string BannedBy,
    string BanReason,
    bool Active)
{
    // Property: Gets or sets the is permanent value used by the database persistence, repository, and MySQL connectivity layer.
    // Value: is permanent value exposed by the owning type.
    public bool IsPermanent => BanDate == UnbanDate;
}
