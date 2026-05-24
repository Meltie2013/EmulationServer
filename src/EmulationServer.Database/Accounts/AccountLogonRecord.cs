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
 * File overview: src/EmulationServer.Database/Accounts/AccountLogonRecord.cs
 * Documents the AccountLogonRecord source file in the database access, account persistence, and MySQL connectivity area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Database.Accounts;

/**
  * Represents immutable account logon record data passed between parts of the server.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
 * Positional fields carried by this record: Id, Username, ShaPassHash, GmLevel, Locked, LastIp, Verifier, Salt.
  */
public sealed record AccountLogonRecord(
    uint Id,
    string Username,
    string ShaPassHash,
    byte GmLevel,
    bool Locked,
    string LastIp,
    string? Verifier,
    string? Salt);
