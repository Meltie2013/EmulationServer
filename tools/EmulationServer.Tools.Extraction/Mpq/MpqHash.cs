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

using System.Text;

/**
  * File overview: tools/EmulationServer.Tools.Extraction/Mpq/MpqHash.cs
  * This file belongs to the developer tooling for data extraction, validation, and diagnostics portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Tools.Extraction.Mpq;

/**
  * Represents the mpq hash component in the developer tooling for data extraction, validation, and diagnostics area.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
internal static class MpqHash
{
    private const uint Seed1Initial = 0x7FED7FED;
    private const uint Seed2Initial = 0xEEEEEEEE;

    private static readonly uint[] CryptTable = BuildCryptTable();

    /**
      * Performs the hash string operation for MpqHash.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
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

    /**
      * Performs the normalize file name operation for MpqHash.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
    public static string NormalizeFileName(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        return fileName.Replace('/', '\\').TrimStart('\\');
    }

    /**
      * Performs the normalize display name operation for MpqHash.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
    public static string NormalizeDisplayName(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        return fileName.Replace('\\', '/').TrimStart('/');
    }

    /**
      * Performs the to upper ascii operation for MpqHash.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
    private static byte ToUpperAscii(byte value)
    {
        return value is >= (byte)'a' and <= (byte)'z'
            ? (byte)(value - 0x20)
            : value;
    }

    /**
      * Builds a protocol payload or domain model from validated input values.
      * The method is part of MpqHash and keeps this workflow isolated from the caller.
      */
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

    /**
      * Performs the decrypt block operation for MpqHash.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
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
