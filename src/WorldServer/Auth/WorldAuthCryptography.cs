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

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace EmulationServer.WorldServer.Auth;

public static class WorldAuthCryptography
{
    public static byte[] ParseSessionKey(string sessionKeyHex)
    {
        if (string.IsNullOrWhiteSpace(sessionKeyHex))
        {
            return [];
        }

        string normalized = sessionKeyHex.Trim();
        if ((normalized.Length & 1) != 0)
        {
            normalized = "0" + normalized;
        }

        return Convert.FromHexString(normalized);
    }

    public static byte[] CalculateVanillaWorldProof(
        string accountName,
        uint clientSeed,
        uint serverSeed,
        ReadOnlySpan<byte> sessionKey)
    {
        byte[] accountBytes = Encoding.UTF8.GetBytes(accountName.ToUpperInvariant());
        byte[] buffer = new byte[accountBytes.Length + 4 + 4 + 4 + sessionKey.Length];
        int offset = 0;

        accountBytes.CopyTo(buffer, offset);
        offset += accountBytes.Length;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset, 4), 0);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset, 4), clientSeed);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset, 4), serverSeed);
        offset += 4;

        sessionKey.CopyTo(buffer.AsSpan(offset));

        return SHA1.HashData(buffer);
    }

    public static bool ProofMatches(
        string accountName,
        uint clientSeed,
        uint serverSeed,
        ReadOnlySpan<byte> sessionKey,
        ReadOnlySpan<byte> clientProof)
    {
        if (clientProof.Length != 20 || sessionKey.Length == 0)
        {
            return false;
        }

        byte[] expected = CalculateVanillaWorldProof(accountName, clientSeed, serverSeed, sessionKey);
        return CryptographicOperations.FixedTimeEquals(expected, clientProof);
    }
}
