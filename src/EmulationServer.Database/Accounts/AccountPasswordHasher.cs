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
// File: src/EmulationServer.Database/Accounts/AccountPasswordHasher.cs
// Purpose: Contains account password hasher code for the database persistence, repository, and MySQL connectivity layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Security.Cryptography;
using System.Text;

namespace EmulationServer.Database.Accounts;

// Type: AccountPasswordHasher
// Purpose: Provides account password hasher behavior for the database persistence, repository, and MySQL connectivity layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class AccountPasswordHasher
{

    // Method: ComputeShaPassHash
    // Purpose: Calculates compute sha pass hash values for the database persistence, repository, and MySQL connectivity layer.
    // Parameters:
    // - username: Username value supplied by the caller for this operation.
    // - password: Password value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to AccountPasswordHasher so callers do not duplicate validation, protocol, or persistence rules.
    public static string ComputeShaPassHash(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username is required.");
        }

        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Password is required.");
        }

        string normalized = $"{username.Trim().ToUpperInvariant()}:{password.ToUpperInvariant()}";
        byte[] digest = SHA1.HashData(Encoding.UTF8.GetBytes(normalized));

        return Convert.ToHexString(digest).ToLowerInvariant();
    }
}
