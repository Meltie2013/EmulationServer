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
// File: src/RealmServer/Auth/Srp6Utilities.cs
// Purpose: Contains srp6 utilities code for the realm server authentication, realm-list, and account connection layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace EmulationServer.RealmServer.Auth;

// Type: Srp6Utilities
// Purpose: Provides srp6 utilities behavior for the realm server authentication, realm-list, and account connection layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class Srp6Utilities
{

    // Constant: Defines the salt length constant used by the realm server authentication, realm-list, and account connection layer.
    // Value: fixed salt length value used anywhere this rule or protocol value is needed.
    public const int SaltLength = 32;

    // Constant: Defines the public key length constant used by the realm server authentication, realm-list, and account connection layer.
    // Value: fixed public key length value used anywhere this rule or protocol value is needed.
    public const int PublicKeyLength = 32;

    // Constant: Defines the session key length constant used by the realm server authentication, realm-list, and account connection layer.
    // Value: fixed session key length value used anywhere this rule or protocol value is needed.
    public const int SessionKeyLength = 40;

    // Constant: Defines the proof length constant used by the realm server authentication, realm-list, and account connection layer.
    // Value: fixed proof length value used anywhere this rule or protocol value is needed.
    public const int ProofLength = 20;

    // Constant: Defines the modulus hex constant used by the realm server authentication, realm-list, and account connection layer.
    // Value: fixed modulus hex value used anywhere this rule or protocol value is needed.
    private const string ModulusHex = "894B645E89E1535BBDAD5B8B290650530801B18EBFBF5E8FAB3C82872A3E9BB7";

    // Method: FromBigEndianHex
    // Purpose: Executes the from big endian hex operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - ModulusHex: Modulus hex value supplied by the caller for this operation.
    // Returns: Returns the big integer N = value produced by this operation.
    // Notes: This keeps the operation scoped to Srp6Utilities so callers do not duplicate validation, protocol, or persistence rules.
    public static readonly BigInteger N = FromBigEndianHex(ModulusHex);

    public static readonly BigInteger G = new(7);

    // Method: GenerateRandomBytes
    // Purpose: Executes the generate random bytes operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - length: Length value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to Srp6Utilities so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] GenerateRandomBytes(int length)
    {
        byte[] bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        return bytes;
    }

    // Method: GeneratePrivateEphemeral
    // Purpose: Executes the generate private ephemeral operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: Returns the big integer value produced by this operation.
    // Notes: This keeps the operation scoped to Srp6Utilities so callers do not duplicate validation, protocol, or persistence rules.
    public static BigInteger GeneratePrivateEphemeral()
    {
        return FromLittleEndian(GenerateRandomBytes(19));
    }

    // Method: GenerateSalt
    // Purpose: Executes the generate salt operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: Returns the big integer value produced by this operation.
    // Notes: This keeps the operation scoped to Srp6Utilities so callers do not duplicate validation, protocol, or persistence rules.
    public static BigInteger GenerateSalt()
    {
        return FromLittleEndian(GenerateRandomBytes(SaltLength));
    }

    // Method: CalculateVerifier
    // Purpose: Calculates calculate verifier values for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - salt: Salt value supplied by the caller for this operation.
    // - shaPassHash: Sha pass hash value supplied by the caller for this operation.
    // Returns: Returns the big integer value produced by this operation.
    // Notes: This keeps the operation scoped to Srp6Utilities so callers do not duplicate validation, protocol, or persistence rules.
    public static BigInteger CalculateVerifier(BigInteger salt, string shaPassHash)
    {
        byte[] passwordDigest = Convert.FromHexString(NormalizeHex(shaPassHash));
        byte[] saltBytes = ToLittleEndian(salt, SaltLength);
        byte[] xDigest = SHA1.HashData(Concat(saltBytes, passwordDigest));

        BigInteger x = FromLittleEndian(xDigest);

        return BigInteger.ModPow(G, x, N);
    }

    // Method: CalculateHostPublicEphemeral
    // Purpose: Calculates calculate host public ephemeral values for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - verifier: Verifier value supplied by the caller for this operation.
    // - hostPrivateEphemeral: Host private ephemeral value supplied by the caller for this operation.
    // Returns: Returns the big integer value produced by this operation.
    // Notes: This keeps the operation scoped to Srp6Utilities so callers do not duplicate validation, protocol, or persistence rules.
    public static BigInteger CalculateHostPublicEphemeral(BigInteger verifier, BigInteger hostPrivateEphemeral)
    {
        BigInteger gMod = BigInteger.ModPow(G, hostPrivateEphemeral, N);
        return PositiveMod((verifier * 3) + gMod, N);
    }

    // Method: CalculateScrambler
    // Purpose: Calculates calculate scrambler values for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - clientPublicEphemeral: Client public ephemeral value supplied by the caller for this operation.
    // - hostPublicEphemeral: Host public ephemeral value supplied by the caller for this operation.
    // Returns: Returns the big integer value produced by this operation.
    // Notes: This keeps the operation scoped to Srp6Utilities so callers do not duplicate validation, protocol, or persistence rules.
    public static BigInteger CalculateScrambler(BigInteger clientPublicEphemeral, BigInteger hostPublicEphemeral)
    {
        byte[] digest = SHA1.HashData(Concat(
            ToLittleEndian(clientPublicEphemeral, PublicKeyLength),
            ToLittleEndian(hostPublicEphemeral, PublicKeyLength)));

        return FromLittleEndian(digest);
    }

    // Method: CalculateSessionSecret
    // Purpose: Calculates calculate session secret values for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - clientPublicEphemeral: Client public ephemeral value supplied by the caller for this operation.
    // - verifier: Verifier value supplied by the caller for this operation.
    // - scrambler: Scrambler value supplied by the caller for this operation.
    // - hostPrivateEphemeral: Host private ephemeral value supplied by the caller for this operation.
    // Returns: Returns the big integer value produced by this operation.
    // Notes: This keeps the operation scoped to Srp6Utilities so callers do not duplicate validation, protocol, or persistence rules.
    public static BigInteger CalculateSessionSecret(
        BigInteger clientPublicEphemeral,
        BigInteger verifier,
        BigInteger scrambler,
        BigInteger hostPrivateEphemeral)
    {
        BigInteger value = PositiveMod(clientPublicEphemeral * BigInteger.ModPow(verifier, scrambler, N), N);
        return BigInteger.ModPow(value, hostPrivateEphemeral, N);
    }

    // Method: HashSessionKey
    // Purpose: Validates or evaluates hash session key rules for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - sessionSecret: Session secret value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to Srp6Utilities so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: CalculateClientProof
    // Purpose: Calculates calculate client proof values for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - username: Username value supplied by the caller for this operation.
    // - salt: Salt value supplied by the caller for this operation.
    // - clientPublicEphemeral: Client public ephemeral value supplied by the caller for this operation.
    // - hostPublicEphemeral: Host public ephemeral value supplied by the caller for this operation.
    // - bytesessionKey: Bytesession key value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to Srp6Utilities so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: CalculateHostProof
    // Purpose: Calculates calculate host proof values for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - clientPublicEphemeral: Client public ephemeral value supplied by the caller for this operation.
    // - byteclientProof: Byteclient proof value supplied by the caller for this operation.
    // - bytesessionKey: Bytesession key value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to Srp6Utilities so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] CalculateHostProof(BigInteger clientPublicEphemeral, byte[] clientProof, byte[] sessionKey)
    {
        return SHA1.HashData(Concat(
            ToLittleEndian(clientPublicEphemeral, PublicKeyLength),
            clientProof,
            sessionKey));
    }

    // Method: FromLittleEndian
    // Purpose: Executes the from little endian operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - bytes: Bytes value supplied by the caller for this operation.
    // Returns: Returns the big integer value produced by this operation.
    // Notes: This keeps the operation scoped to Srp6Utilities so callers do not duplicate validation, protocol, or persistence rules.
    public static BigInteger FromLittleEndian(ReadOnlySpan<byte> bytes)
    {
        return new BigInteger(bytes, isUnsigned: true, isBigEndian: false);
    }

    // Method: FromBigEndian
    // Purpose: Executes the from big endian operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - bytes: Bytes value supplied by the caller for this operation.
    // Returns: Returns the big integer value produced by this operation.
    // Notes: This keeps the operation scoped to Srp6Utilities so callers do not duplicate validation, protocol, or persistence rules.
    public static BigInteger FromBigEndian(ReadOnlySpan<byte> bytes)
    {
        return new BigInteger(bytes, isUnsigned: true, isBigEndian: true);
    }

    // Method: FromBigEndianHex
    // Purpose: Executes the from big endian hex operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - hex: Hex value supplied by the caller for this operation.
    // Returns: Returns the big integer value produced by this operation.
    // Notes: This keeps the operation scoped to Srp6Utilities so callers do not duplicate validation, protocol, or persistence rules.
    public static BigInteger FromBigEndianHex(string hex)
    {
        return FromBigEndian(Convert.FromHexString(NormalizeHex(hex)));
    }

    // Method: ToLittleEndian
    // Purpose: Executes the to little endian operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // - length: Length value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to Srp6Utilities so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ToBigEndianHex
    // Purpose: Executes the to big endian hex operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // - minimumBytes: Minimum bytes value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to Srp6Utilities so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: IsValidStoredSrpValue
    // Purpose: Validates or evaluates is valid stored srp value rules for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - hex: Hex value supplied by the caller for this operation.
    // Returns: Returns true when is valid stored srp value succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to Srp6Utilities so callers do not duplicate validation, protocol, or persistence rules.
    public static bool IsValidStoredSrpValue(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex) || hex.Length != SaltLength * 2)
        {
            return false;
        }

        return hex.All(Uri.IsHexDigit);
    }

    // Method: FixedTimeEquals
    // Purpose: Executes the fixed time equals operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - left: Left value supplied by the caller for this operation.
    // - right: Right value supplied by the caller for this operation.
    // Returns: Returns true when fixed time equals succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to Srp6Utilities so callers do not duplicate validation, protocol, or persistence rules.
    public static bool FixedTimeEquals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        return CryptographicOperations.FixedTimeEquals(left, right);
    }

    // Method: PositiveMod
    // Purpose: Executes the positive mod operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // - modulus: Modulus value supplied by the caller for this operation.
    // Returns: Returns the big integer value produced by this operation.
    // Notes: This keeps the operation scoped to Srp6Utilities so callers do not duplicate validation, protocol, or persistence rules.
    private static BigInteger PositiveMod(BigInteger value, BigInteger modulus)
    {
        BigInteger result = value % modulus;
        return result.Sign < 0 ? result + modulus : result;
    }

    // Method: Concat
    // Purpose: Executes the concat operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - bytearrays: Bytearrays value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to Srp6Utilities so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: NormalizeHex
    // Purpose: Converts incoming data into normalize hex form for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - hex: Hex value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to Srp6Utilities so callers do not duplicate validation, protocol, or persistence rules.
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
