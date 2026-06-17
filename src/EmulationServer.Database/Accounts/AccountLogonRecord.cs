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
// File: src/EmulationServer.Database/Accounts/AccountLogonRecord.cs
// Purpose: Contains account logon record code for the database persistence, repository, and MySQL connectivity layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Database.Accounts;

// Type: AccountLogonRecord
// Purpose: Represents account logon record data passed through the database persistence, repository, and MySQL connectivity layer.
// Constructor values:
// - Id: ID identifier used to select the exact record, object, or runtime owner.
// - Username: Username value supplied by the caller for this operation.
// - ShaPassHash: Sha pass hash value supplied by the caller for this operation.
// - SecurityLevel: Security level value supplied by the caller for this operation.
// - Permissions: Permissions value supplied by the caller for this operation.
// - Locked: Locked value supplied by the caller for this operation.
// - LastIp: Last IP value supplied by the caller for this operation.
// - Verifier: Verifier value supplied by the caller for this operation.
// - Salt: Salt value supplied by the caller for this operation.
// - SessionKey: Session key value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record AccountLogonRecord(
    uint Id,
    string Username,
    string ShaPassHash,
    AccountSecurityLevel SecurityLevel,
    RbacPermissionSet Permissions,
    bool Locked,
    string LastIp,
    string? Verifier,
    string? Salt,
    string? SessionKey);
