
using System.Text;

namespace EmulationServer.Tools.Extraction.Mpq;

internal static class MpqHash
{
    private const uint Seed1Initial = 0x7FED7FED;
    private const uint Seed2Initial = 0xEEEEEEEE;

    private static readonly uint[] CryptTable = BuildCryptTable();

    public static uint HashString(string value, MpqHashType hashType)
    {
        ArgumentNullException.ThrowIfNull(value);

        uint seed1 = Seed1Initial;
        uint seed2 = Seed2Initial;
        uint typeOffset = (uint)hashType << 8;

        foreach (byte valueByte in Encoding.ASCII.GetBytes(NormalizeFileName(value)))
        {
            byte upper = ToUpperAscii(valueByte);
            seed1 = CryptTable[(int)(typeOffset + upper)] ^ (seed1 + seed2);
            seed2 = upper + seed1 + seed2 + (seed2 << 5) + 3;
        }

        return seed1;
    }

    public static string NormalizeFileName(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        return fileName.Replace('/', '\\').TrimStart('\\');
    }

    public static string NormalizeDisplayName(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        return fileName.Replace('\\', '/').TrimStart('/');
    }

    private static byte ToUpperAscii(byte value)
    {
        return value is >= (byte)'a' and <= (byte)'z'
            ? (byte)(value - 0x20)
            : value;
    }

    private static uint[] BuildCryptTable()
    {
        uint seed = 0x00100001;
        uint[] table = new uint[0x500];

        for (uint index1 = 0; index1 < 0x100; index1++)
        {
            for (uint index2 = 0; index2 < 5; index2++)
            {
                uint tableIndex = index1 + (index2 * 0x100);

                seed = (seed * 125 + 3) % 0x2AAAAB;
                uint value1 = (seed & 0xFFFF) << 16;

                seed = (seed * 125 + 3) % 0x2AAAAB;
                uint value2 = seed & 0xFFFF;

                table[(int)tableIndex] = value1 | value2;
            }
        }

        return table;
    }

    public static void DecryptBlock(Span<byte> data, uint key)
    {
        uint seed = Seed2Initial;

        for (int offset = 0; offset + 4 <= data.Length; offset += 4)
        {
            seed += CryptTable[(int)(0x400 + (key & 0xFF))];

            uint encryptedValue = BitConverter.ToUInt32(data[offset..(offset + 4)]);
            uint decryptedValue = encryptedValue ^ (key + seed);

            key = ((~key << 21) + 0x11111111) | (key >> 11);
            seed = decryptedValue + seed + (seed << 5) + 3;

            BitConverter.GetBytes(decryptedValue).CopyTo(data[offset..(offset + 4)]);
        }
    }
}
