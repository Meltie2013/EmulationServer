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

using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

/**
  * File overview: src/RealmServer/Auth/Srp6Utilities.cs
  * This file belongs to the realm authentication, build validation, and realm list packet creation portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.RealmServer.Auth;

/**
  * Represents the srp6 utilities component in the realm authentication, build validation, and realm list packet creation area.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public static class Srp6Utilities
{
    public const int SaltLength = 32;
    public const int PublicKeyLength = 32;
    public const int SessionKeyLength = 40;
    public const int ProofLength = 20;

    private const string ModulusHex = "894B645E89E1535BBDAD5B8B290650530801B18EBFBF5E8FAB3C82872A3E9BB7";

    public static readonly BigInteger N = FromBigEndianHex(ModulusHex);
    public static readonly BigInteger G = new(7);

    /**
      * Performs the generate random bytes operation for Srp6Utilities.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
    public static byte[] GenerateRandomBytes(int length)
    {
        byte[] bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        return bytes;
    }

    /**
      * Performs the generate private ephemeral operation for Srp6Utilities.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
    public static BigInteger GeneratePrivateEphemeral()
    {
        return FromLittleEndian(GenerateRandomBytes(19));
    }

    /**
      * Performs the generate salt operation for Srp6Utilities.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
    public static BigInteger GenerateSalt()
    {
        return FromLittleEndian(GenerateRandomBytes(SaltLength));
    }

    /**
      * Calculates a derived value from current runtime state.
      * The method is part of Srp6Utilities and keeps this workflow isolated from the caller.
      */
    public static BigInteger CalculateVerifier(BigInteger salt, string shaPassHash)
    {
        byte[] passwordDigest = Convert.FromHexString(NormalizeHex(shaPassHash));
        byte[] saltBytes = ToLittleEndian(salt, SaltLength);
        byte[] xDigest = SHA1.HashData(Concat(saltBytes, passwordDigest));

        // MaNGOS BigNumber.SetBinary treats this digest as little-endian.
        BigInteger x = FromLittleEndian(xDigest);

        return BigInteger.ModPow(G, x, N);
    }

    /**
      * Calculates a derived value from current runtime state.
      * The method is part of Srp6Utilities and keeps this workflow isolated from the caller.
      */
    public static BigInteger CalculateHostPublicEphemeral(BigInteger verifier, BigInteger hostPrivateEphemeral)
    {
        BigInteger gMod = BigInteger.ModPow(G, hostPrivateEphemeral, N);
        return PositiveMod((verifier * 3) + gMod, N);
    }

    /**
      * Calculates a derived value from current runtime state.
      * The method is part of Srp6Utilities and keeps this workflow isolated from the caller.
      */
    public static BigInteger CalculateScrambler(BigInteger clientPublicEphemeral, BigInteger hostPublicEphemeral)
    {
        byte[] digest = SHA1.HashData(Concat(
            ToLittleEndian(clientPublicEphemeral, PublicKeyLength),
            ToLittleEndian(hostPublicEphemeral, PublicKeyLength)));

        return FromLittleEndian(digest);
    }

    /**
      * Calculates a derived value from current runtime state.
      * The method is part of Srp6Utilities and keeps this workflow isolated from the caller.
      */
    public static BigInteger CalculateSessionSecret(
        BigInteger clientPublicEphemeral,
        BigInteger verifier,
        BigInteger scrambler,
        BigInteger hostPrivateEphemeral)
    {
        BigInteger value = PositiveMod(clientPublicEphemeral * BigInteger.ModPow(verifier, scrambler, N), N);
        return BigInteger.ModPow(value, hostPrivateEphemeral, N);
    }

    /**
      * Performs the hash session key operation for Srp6Utilities.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
    public static byte[] HashSessionKey(BigInteger sessionSecret)
    {
        byte[] secret = ToLittleEndian(sessionSecret, PublicKeyLength);
        byte[] even = new byte[16];
        byte[] odd = new byte[16];

        for (int index = 0; index < 16; index++)
        {
            even[index] = secret[index * 2];
            odd[index] = secret[(index * 2) + 1];
        }

        byte[] evenHash = SHA1.HashData(even);
        byte[] oddHash = SHA1.HashData(odd);
        byte[] sessionKey = new byte[SessionKeyLength];

        for (int index = 0; index < ProofLength; index++)
        {
            sessionKey[index * 2] = evenHash[index];
            sessionKey[(index * 2) + 1] = oddHash[index];
        }

        return sessionKey;
    }

    /**
      * Calculates a derived value from current runtime state.
      * The method is part of Srp6Utilities and keeps this workflow isolated from the caller.
      */
    public static byte[] CalculateClientProof(
        string username,
        BigInteger salt,
        BigInteger clientPublicEphemeral,
        BigInteger hostPublicEphemeral,
        byte[] sessionKey)
    {
        byte[] nHash = SHA1.HashData(ToLittleEndian(N, PublicKeyLength));
        byte[] gHash = SHA1.HashData(ToLittleEndian(G));
        byte[] nXorG = new byte[ProofLength];

        for (int index = 0; index < ProofLength; index++)
        {
            nXorG[index] = (byte)(nHash[index] ^ gHash[index]);
        }

        byte[] usernameHash = SHA1.HashData(Encoding.UTF8.GetBytes(username.ToUpperInvariant()));

        return SHA1.HashData(Concat(
            nXorG,
            usernameHash,
            ToLittleEndian(salt, SaltLength),
            ToLittleEndian(clientPublicEphemeral, PublicKeyLength),
            ToLittleEndian(hostPublicEphemeral, PublicKeyLength),
            sessionKey));
    }

    /**
      * Calculates a derived value from current runtime state.
      * The method is part of Srp6Utilities and keeps this workflow isolated from the caller.
      */
    public static byte[] CalculateHostProof(BigInteger clientPublicEphemeral, byte[] clientProof, byte[] sessionKey)
    {
        return SHA1.HashData(Concat(
            ToLittleEndian(clientPublicEphemeral, PublicKeyLength),
            clientProof,
            sessionKey));
    }

    /**
      * Performs the from little endian operation for Srp6Utilities.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
    public static BigInteger FromLittleEndian(ReadOnlySpan<byte> bytes)
    {
        return new BigInteger(bytes, isUnsigned: true, isBigEndian: false);
    }

    /**
      * Performs the from big endian operation for Srp6Utilities.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
    public static BigInteger FromBigEndian(ReadOnlySpan<byte> bytes)
    {
        return new BigInteger(bytes, isUnsigned: true, isBigEndian: true);
    }

    /**
      * Performs the from big endian hex operation for Srp6Utilities.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
    public static BigInteger FromBigEndianHex(string hex)
    {
        return FromBigEndian(Convert.FromHexString(NormalizeHex(hex)));
    }

    /**
      * Performs the to little endian operation for Srp6Utilities.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
    public static byte[] ToLittleEndian(BigInteger value, int length = 0)
    {
        byte[] bytes = value.ToByteArray(isUnsigned: true, isBigEndian: false);

        if (length == 0)
        {
            return bytes;
        }

        if (bytes.Length == length)
        {
            return bytes;
        }

        byte[] result = new byte[length];
        Array.Copy(bytes, result, Math.Min(bytes.Length, result.Length));
        return result;
    }

    /**
      * Performs the to big endian hex operation for Srp6Utilities.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
    public static string ToBigEndianHex(BigInteger value, int minimumBytes = 0)
    {
        byte[] bytes = value.ToByteArray(isUnsigned: true, isBigEndian: true);

        if (minimumBytes > 0 && bytes.Length < minimumBytes)
        {
            byte[] padded = new byte[minimumBytes];
            Array.Copy(bytes, 0, padded, minimumBytes - bytes.Length, bytes.Length);
            bytes = padded;
        }

        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /**
      * Performs the is valid stored srp value operation for Srp6Utilities.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    public static bool IsValidStoredSrpValue(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex) || hex.Length != SaltLength * 2)
        {
            return false;
        }

        return hex.All(Uri.IsHexDigit);
    }

    /**
      * Performs the fixed time equals operation for Srp6Utilities.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    public static bool FixedTimeEquals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        return CryptographicOperations.FixedTimeEquals(left, right);
    }

    /**
      * Performs the positive mod operation for Srp6Utilities.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
    private static BigInteger PositiveMod(BigInteger value, BigInteger modulus)
    {
        BigInteger result = value % modulus;
        return result.Sign < 0 ? result + modulus : result;
    }

    /**
      * Performs the concat operation for Srp6Utilities.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
    private static byte[] Concat(params byte[][] arrays)
    {
        int length = arrays.Sum(array => array.Length);
        byte[] result = new byte[length];
        int offset = 0;

        foreach (byte[] array in arrays)
        {
            Buffer.BlockCopy(array, 0, result, offset, array.Length);
            offset += array.Length;
        }

        return result;
    }

    /**
      * Performs the normalize hex operation for Srp6Utilities.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
    private static string NormalizeHex(string hex)
    {
        string normalized = hex.Trim();
        if (normalized.Length % 2 != 0)
        {
            normalized = "0" + normalized;
        }

        _ = ulong.TryParse("0", NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);
        return normalized;
    }
}
