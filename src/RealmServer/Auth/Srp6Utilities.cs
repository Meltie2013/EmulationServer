
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace EmulationServer.RealmServer.Auth;

public static class Srp6Utilities
{
    public const int SaltLength = 32;
    public const int PublicKeyLength = 32;
    public const int SessionKeyLength = 40;
    public const int ProofLength = 20;

    private const string ModulusHex = "894B645E89E1535BBDAD5B8B290650530801B18EBFBF5E8FAB3C82872A3E9BB7";

    public static readonly BigInteger N = FromBigEndianHex(ModulusHex);
    public static readonly BigInteger G = new(7);

    public static byte[] GenerateRandomBytes(int length)
    {
        byte[] bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        return bytes;
    }

    public static BigInteger GeneratePrivateEphemeral()
    {
        return FromLittleEndian(GenerateRandomBytes(19));
    }

    public static BigInteger GenerateSalt()
    {
        return FromLittleEndian(GenerateRandomBytes(SaltLength));
    }

    public static BigInteger CalculateVerifier(BigInteger salt, string shaPassHash)
    {
        byte[] passwordDigest = Convert.FromHexString(NormalizeHex(shaPassHash));
        byte[] saltBytes = ToLittleEndian(salt, SaltLength);
        byte[] xDigest = SHA1.HashData(Concat(saltBytes, passwordDigest));

        // MaNGOS BigNumber.SetBinary treats this digest as little-endian.
        BigInteger x = FromLittleEndian(xDigest);

        return BigInteger.ModPow(G, x, N);
    }

    public static BigInteger CalculateHostPublicEphemeral(BigInteger verifier, BigInteger hostPrivateEphemeral)
    {
        BigInteger gMod = BigInteger.ModPow(G, hostPrivateEphemeral, N);
        return PositiveMod((verifier * 3) + gMod, N);
    }

    public static BigInteger CalculateScrambler(BigInteger clientPublicEphemeral, BigInteger hostPublicEphemeral)
    {
        byte[] digest = SHA1.HashData(Concat(
            ToLittleEndian(clientPublicEphemeral, PublicKeyLength),
            ToLittleEndian(hostPublicEphemeral, PublicKeyLength)));

        return FromLittleEndian(digest);
    }

    public static BigInteger CalculateSessionSecret(
        BigInteger clientPublicEphemeral,
        BigInteger verifier,
        BigInteger scrambler,
        BigInteger hostPrivateEphemeral)
    {
        BigInteger value = PositiveMod(clientPublicEphemeral * BigInteger.ModPow(verifier, scrambler, N), N);
        return BigInteger.ModPow(value, hostPrivateEphemeral, N);
    }

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

    public static byte[] CalculateHostProof(BigInteger clientPublicEphemeral, byte[] clientProof, byte[] sessionKey)
    {
        return SHA1.HashData(Concat(
            ToLittleEndian(clientPublicEphemeral, PublicKeyLength),
            clientProof,
            sessionKey));
    }

    public static BigInteger FromLittleEndian(ReadOnlySpan<byte> bytes)
    {
        return new BigInteger(bytes, isUnsigned: true, isBigEndian: false);
    }

    public static BigInteger FromBigEndian(ReadOnlySpan<byte> bytes)
    {
        return new BigInteger(bytes, isUnsigned: true, isBigEndian: true);
    }

    public static BigInteger FromBigEndianHex(string hex)
    {
        return FromBigEndian(Convert.FromHexString(NormalizeHex(hex)));
    }

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

    public static bool IsValidStoredSrpValue(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex) || hex.Length != SaltLength * 2)
        {
            return false;
        }

        return hex.All(Uri.IsHexDigit);
    }

    public static bool FixedTimeEquals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        return CryptographicOperations.FixedTimeEquals(left, right);
    }

    private static BigInteger PositiveMod(BigInteger value, BigInteger modulus)
    {
        BigInteger result = value % modulus;
        return result.Sign < 0 ? result + modulus : result;
    }

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
