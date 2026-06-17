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
// File: src/WorldServer/Database/Accounts/WorldAccountSessionRecord.cs
// Purpose: Contains world account session record code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Database.Accounts;

namespace EmulationServer.WorldServer.Database.Accounts;

// Type: WorldAccountSessionRecord
// Purpose: Represents world account session record data passed through the world server gameplay, session, and character runtime layer.
// Constructor values:
// - Id: ID identifier used to select the exact record, object, or runtime owner.
// - Username: Username value supplied by the caller for this operation.
// - SecurityLevel: Security level value supplied by the caller for this operation.
// - Permissions: Permissions value supplied by the caller for this operation.
// - Locked: Locked value supplied by the caller for this operation.
// - SessionKey: Session key value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record WorldAccountSessionRecord(
    uint Id,
    string Username,
    AccountSecurityLevel SecurityLevel,
    RbacPermissionSet Permissions,
    bool Locked,
    string SessionKey);
