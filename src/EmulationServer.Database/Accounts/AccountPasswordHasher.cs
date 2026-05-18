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

using System.Security.Cryptography;
using System.Text;

namespace EmulationServer.Database.Accounts;

public static class AccountPasswordHasher
{
    public static string ComputeShaPassHash(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username is required.", nameof(username));
        }

        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Password is required.", nameof(password));
        }

        string normalized = $"{username.Trim().ToUpperInvariant()}:{password.ToUpperInvariant()}";
        byte[] digest = SHA1.HashData(Encoding.UTF8.GetBytes(normalized));

        return Convert.ToHexString(digest).ToLowerInvariant();
    }
}
