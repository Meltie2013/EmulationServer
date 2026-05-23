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
using System.IO.Compression;

using EmulationServer.WorldServer.Characters;

namespace EmulationServer.WorldServer.Networking.Packets;

public static class WorldPacketBuilders
{
    private const int CharacterEquipmentSlotCount = 19;
    private const uint AtLoginFirst = 0x20;

    public static byte[] BuildAuthChallenge(uint serverSeed)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(serverSeed);
        return writer.ToArray();
    }

    public static byte[] BuildAuthResponse(AuthResponseCode code)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt8((byte)code);

        if (code == AuthResponseCode.Ok)
        {
            writer.WriteUInt32(0);
            writer.WriteUInt8(0);
            writer.WriteUInt32(0);
        }

        return writer.ToArray();
    }

    public static byte[] BuildAddonInfo(ReadOnlySpan<byte> clientAddonInfo)
    {
        if (clientAddonInfo.Length < sizeof(uint))
        {
            return [];
        }

        uint decompressedSize = BinaryPrimitives.ReadUInt32LittleEndian(clientAddonInfo[..sizeof(uint)]);
        if (decompressedSize == 0 || decompressedSize > 0xFFFFF)
        {
            return [];
        }

        byte[] decompressed;
        try
        {
            using MemoryStream compressedStream = new(clientAddonInfo[sizeof(uint)..].ToArray());
            using ZLibStream zlibStream = new(compressedStream, CompressionMode.Decompress);
            using MemoryStream decompressedStream = new((int)decompressedSize);
            zlibStream.CopyTo(decompressedStream);
            decompressed = decompressedStream.ToArray();
        }
        catch (InvalidDataException)
        {
            return [];
        }

        WorldPacketWriter writer = new();
        int offset = 0;

        while (offset < decompressed.Length)
        {
            int nameEnd = Array.IndexOf(decompressed, (byte)0, offset);
            if (nameEnd < offset)
            {
                break;
            }

            offset = nameEnd + 1;
            if (offset + 9 > decompressed.Length)
            {
                break;
            }

            uint crc = BinaryPrimitives.ReadUInt32LittleEndian(decompressed.AsSpan(offset, 4));
            offset += 4;
            offset += 4; // unknown uint32
            offset += 1; // unknown uint8

            writer.WriteUInt8(2);
            writer.WriteUInt8(1);

            byte needsPublicKey = crc == 0x1C776D01 ? (byte)0 : (byte)1;
            writer.WriteUInt8(needsPublicKey);
            if (needsPublicKey != 0)
            {
                writer.WriteBytes(AddonPublicKey);
            }

            writer.WriteUInt32(0);
            writer.WriteUInt8(0);
        }

        return writer.ToArray();
    }

    private static readonly byte[] AddonPublicKey =
    [
        0xC3, 0x5B, 0x50, 0x84, 0xB9, 0x3E, 0x32, 0x42, 0x8C, 0xD0, 0xC7, 0x48, 0xFA, 0x0E, 0x5D, 0x54,
        0x5A, 0xA3, 0x0E, 0x14, 0xBA, 0x9E, 0x0D, 0xB9, 0x5D, 0x8B, 0xEE, 0xB6, 0x84, 0x93, 0x45, 0x75,
        0xFF, 0x31, 0xFE, 0x2F, 0x64, 0x3F, 0x3D, 0x6D, 0x07, 0xD9, 0x44, 0x9B, 0x40, 0x85, 0x59, 0x34,
        0x4E, 0x10, 0xE1, 0xE7, 0x43, 0x69, 0xEF, 0x7C, 0x16, 0xFC, 0xB4, 0xED, 0x1B, 0x95, 0x28, 0xA8,
        0x23, 0x76, 0x51, 0x31, 0x57, 0x30, 0x2B, 0x79, 0x08, 0x50, 0x10, 0x1C, 0x4A, 0x1A, 0x2C, 0xC8,
        0x8B, 0x8F, 0x05, 0x2D, 0x22, 0x3D, 0xDB, 0x5A, 0x24, 0x7A, 0x0F, 0x13, 0x50, 0x37, 0x8F, 0x5A,
        0xCC, 0x9E, 0x04, 0x44, 0x0E, 0x87, 0x01, 0xD4, 0xA3, 0x15, 0x94, 0x16, 0x34, 0xC6, 0xC2, 0xC3,
        0xFB, 0x49, 0xFE, 0xE1, 0xF9, 0xDA, 0x8C, 0x50, 0x3C, 0xBE, 0x2C, 0xBB, 0x57, 0xED, 0x46, 0xB9,
        0xAD, 0x8B, 0xC6, 0xDF, 0x0E, 0xD6, 0x0F, 0xBE, 0x80, 0xB3, 0x8B, 0x1E, 0x77, 0xCF, 0xAD, 0x22,
        0xCF, 0xB7, 0x4B, 0xCF, 0xFB, 0xF0, 0x6B, 0x11, 0x45, 0x2D, 0x7A, 0x81, 0x18, 0xF2, 0x92, 0x7E,
        0x98, 0x56, 0x5D, 0x5E, 0x69, 0x72, 0x0A, 0x0D, 0x03, 0x0A, 0x85, 0xA2, 0x85, 0x9C, 0xCB, 0xFB,
        0x56, 0x6E, 0x8F, 0x44, 0xBB, 0x8F, 0x02, 0x22, 0x68, 0x63, 0x97, 0xBC, 0x85, 0xBA, 0xA8, 0xF7,
        0xB5, 0x40, 0x68, 0x3C, 0x77, 0x86, 0x6F, 0x4B, 0xD7, 0x88, 0xCA, 0x8A, 0xD7, 0xCE, 0x36, 0xF0,
        0x45, 0x6E, 0xD5, 0x64, 0x79, 0x0F, 0x17, 0xFC, 0x64, 0xDD, 0x10, 0x6F, 0xF3, 0xF5, 0xE0, 0xA6,
        0xC3, 0xFB, 0x1B, 0x8C, 0x29, 0xEF, 0x8E, 0xE5, 0x34, 0xCB, 0xD1, 0x2A, 0xCE, 0x79, 0xC3, 0x9A,
        0x0D, 0x36, 0xEA, 0x01, 0xE0, 0xAA, 0x91, 0x20, 0x54, 0xF0, 0x72, 0xD8, 0x1E, 0xC7, 0x89, 0xD2,
    ];

    public static byte[] BuildCharacterCreate(CharacterCreateResult result)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt8((byte)result);
        return writer.ToArray();
    }

    public static byte[] BuildCharacterDelete(CharacterDeleteResult result)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt8((byte)result);
        return writer.ToArray();
    }

    public static byte[] BuildAccountDataTimes()
    {
        WorldPacketWriter writer = new();

        // Vanilla keeps eight account-data blocks. Sending zero timestamps tells
        // the client there is no cached server-side UI data to request yet.
        for (int index = 0; index < 8; index++)
        {
            writer.WriteUInt32(0);
        }

        return writer.ToArray();
    }

    public static byte[] BuildUpdateAccountData(uint accountDataType)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(accountDataType);
        writer.WriteUInt32(0); // timestamp
        writer.WriteUInt32(0); // decompressed size; no payload follows
        return writer.ToArray();
    }

    public static byte[] BuildCharacterEnum(IReadOnlyList<CharacterListEntry> characters)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt8((byte)Math.Min(byte.MaxValue, characters.Count));

        foreach (CharacterListEntry character in characters.Take(byte.MaxValue))
        {
            writer.WriteUInt64(character.Guid);
            writer.WriteCString(character.Name);
            writer.WriteUInt8(character.Race);
            writer.WriteUInt8(character.Class);
            writer.WriteUInt8(character.Gender);
            writer.WriteUInt8((byte)(character.PlayerBytes & 0xFF));
            writer.WriteUInt8((byte)((character.PlayerBytes >> 8) & 0xFF));
            writer.WriteUInt8((byte)((character.PlayerBytes >> 16) & 0xFF));
            writer.WriteUInt8((byte)((character.PlayerBytes >> 24) & 0xFF));
            writer.WriteUInt8((byte)(character.PlayerBytes2 & 0xFF));
            writer.WriteUInt8(character.Level);
            writer.WriteUInt32(character.Zone);
            writer.WriteUInt32(character.Map);
            writer.WriteFloat(character.PositionX);
            writer.WriteFloat(character.PositionY);
            writer.WriteFloat(character.PositionZ);
            writer.WriteUInt32(character.GuildId);
            writer.WriteUInt32(BuildCharacterEnumFlags(character));
            writer.WriteUInt8((character.AtLogin & AtLoginFirst) != 0 ? (byte)1 : (byte)0);
            writer.WriteUInt32(0); // pet display id
            writer.WriteUInt32(0); // pet level
            writer.WriteUInt32(0); // pet family

            for (int slot = 0; slot < CharacterEquipmentSlotCount; slot++)
            {
                CharacterEquipmentDisplay equipment = slot < character.Equipment.Count
                    ? character.Equipment[slot]
                    : new CharacterEquipmentDisplay(0, 0, 0);

                writer.WriteUInt32(equipment.DisplayId);
                writer.WriteUInt8(equipment.InventoryType);
            }

            writer.WriteUInt32(0); // first bag display id
            writer.WriteUInt8(0); // first bag inventory type
        }

        return writer.ToArray();
    }

    private static uint BuildCharacterEnumFlags(CharacterListEntry character)
    {
        // Do not pass the server-side characters.playerFlags value directly here.
        // The Vanilla character list packet expects client enum flags, while
        // characters.playerFlags is a persisted in-world player state field.
        _ = character;
        return 0;
    }

    public static byte[] BuildPong(uint sequence)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(sequence);
        return writer.ToArray();
    }
}
