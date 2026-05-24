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

/**
  * File overview: src/WorldServer/Auth/WorldAuthCryptography.cs
  * Documents the WorldAuthCryptography source file in the world authentication parsing and session key handling area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.WorldServer.Auth;

/**
  * Owns the world auth cryptography behavior for the world authentication parsing and session key handling layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public static class WorldAuthCryptography
{
    /**
      * Parses parse session key input into the strongly typed server representation.
      * Parsing code performs boundary checks close to the raw packet or file data so corrupted input cannot leak deeper into gameplay systems.
      * Inputs used by this operation: sessionKeyHex.
      */
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

    /**
      * Performs the calculate vanilla world proof operation for the world authentication parsing and session key handling workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: accountName, clientSeed, serverSeed, sessionKey.
      */
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

    /**
      * Performs the proof matches operation for the world authentication parsing and session key handling workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: accountName, clientSeed, serverSeed, sessionKey, clientProof.
      */
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
