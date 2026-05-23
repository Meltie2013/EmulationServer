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
using EmulationServer.WorldServer.Chat;
using EmulationServer.WorldServer.Players;
using EmulationServer.WorldServer.WorldData;

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


    public static byte[] BuildCharacterLoginFailed(CharacterLoginFailureCode failureCode)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt8((byte)failureCode);
        return writer.ToArray();
    }

    public static byte[] BuildTransferAborted(uint mapId, TransferAbortReason reason)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(mapId);
        writer.WriteUInt8((byte)reason);
        return writer.ToArray();
    }

    public static byte[] BuildLoginVerifyWorld(PlayerLoginRecord player)
    {
        ArgumentNullException.ThrowIfNull(player);

        WorldPacketWriter writer = new();
        writer.WriteUInt32(player.Map);
        writer.WriteFloat(player.PositionX);
        writer.WriteFloat(player.PositionY);
        writer.WriteFloat(player.PositionZ);
        writer.WriteFloat(player.Orientation);
        return writer.ToArray();
    }

    public static byte[] BuildTutorialFlags()
    {
        WorldPacketWriter writer = new();

        // Vanilla expects eight tutorial flag blocks after SMSG_LOGIN_VERIFY_WORLD.
        // All bits set tells the client every tutorial has already been seen.
        for (int index = 0; index < 8; index++)
        {
            writer.WriteUInt32(uint.MaxValue);
        }

        return writer.ToArray();
    }

    public static byte[] BuildPlayerCreateUpdate(PlayerLoginRecord player)
    {
        ArgumentNullException.ThrowIfNull(player);

        WorldPacketWriter writer = new();
        writer.WriteUInt32(1); // amount_of_objects
        writer.WriteUInt8(0); // has_transport

        writer.WriteUInt8(3); // CREATE_OBJECT2
        WritePackedGuid(writer, player.ClientGuid);
        writer.WriteUInt8(4); // PLAYER
        WritePlayerMovementBlock(writer, player);
        WritePlayerCreateUpdateMask(writer, player);

        return writer.ToArray();
    }

    private static void WritePlayerMovementBlock(WorldPacketWriter writer, PlayerLoginRecord player)
    {
        const byte updateFlagsSelfAllLiving = 0x31;

        writer.WriteUInt8(updateFlagsSelfAllLiving);
        writer.WriteUInt32(0); // movement flags
        writer.WriteUInt32(unchecked((uint)Environment.TickCount));
        writer.WriteFloat(player.PositionX);
        writer.WriteFloat(player.PositionY);
        writer.WriteFloat(player.PositionZ);
        writer.WriteFloat(player.Orientation);
        writer.WriteFloat(0); // fall time
        writer.WriteFloat(2.5f); // walking speed
        writer.WriteFloat(7.0f); // running speed
        writer.WriteFloat(4.5f); // backwards running speed
        writer.WriteFloat(4.722222f); // swimming speed
        writer.WriteFloat(2.5f); // backwards swimming speed
        writer.WriteFloat(3.1415927f); // turn rate
        writer.WriteUInt32(1); // living movement block unknown1 used by the 1.12 client
    }

    private static void WritePlayerCreateUpdateMask(WorldPacketWriter writer, PlayerLoginRecord player)
    {
        const int ObjectFieldGuid = 0x0000;
        const int ObjectFieldType = 0x0002;
        const int ObjectFieldScaleX = 0x0004;
        const int UnitFieldHealth = 0x0016;
        const int UnitFieldMaxHealth = 0x001C;
        const int UnitFieldLevel = 0x0022;
        const int UnitFieldFactionTemplate = 0x0023;
        const int UnitFieldBytes0 = 0x0024;
        const int UnitFieldDisplayId = 0x0083;
        const int UnitFieldNativeDisplayId = 0x0084;

        Dictionary<int, uint> fields = [];
        ulong clientGuid = player.ClientGuid;
        uint health = player.Stats.Health == 0 ? 100 : player.Stats.Health;
        uint displayId = ResolvePlayerDisplayId(player.Race, player.Gender);

        fields[ObjectFieldGuid] = (uint)(clientGuid & uint.MaxValue);
        fields[ObjectFieldGuid + 1] = (uint)(clientGuid >> 32);
        fields[ObjectFieldType] = 0x19; // OBJECT | UNIT | PLAYER
        fields[ObjectFieldScaleX] = FloatToUInt32(1.0f);
        fields[UnitFieldHealth] = health;
        fields[UnitFieldMaxHealth] = health;
        fields[UnitFieldLevel] = Math.Max((uint)player.Level, 1u);
        fields[UnitFieldFactionTemplate] = ResolveFactionTemplateId(player.Race);
        fields[UnitFieldBytes0] = BuildUnitBytes0(player.Race, player.Class, player.Gender);
        fields[UnitFieldDisplayId] = displayId;
        fields[UnitFieldNativeDisplayId] = displayId;

        WriteUpdateMask(writer, fields);
    }

    private static void WriteUpdateMask(WorldPacketWriter writer, IReadOnlyDictionary<int, uint> fields)
    {
        if (fields.Count == 0)
        {
            writer.WriteUInt8(0);
            return;
        }

        int highestField = fields.Keys.Max();
        byte blockCount = checked((byte)((highestField / 32) + 1));
        uint[] blocks = new uint[blockCount];

        foreach (int field in fields.Keys)
        {
            blocks[field / 32] |= 1u << (field % 32);
        }

        writer.WriteUInt8(blockCount);
        foreach (uint block in blocks)
        {
            writer.WriteUInt32(block);
        }

        foreach (KeyValuePair<int, uint> field in fields.OrderBy(field => field.Key))
        {
            writer.WriteUInt32(field.Value);
        }
    }

    private static void WritePackedGuid(WorldPacketWriter writer, ulong guid)
    {
        Span<byte> guidBytes = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(guidBytes, guid);

        byte mask = 0;
        for (int index = 0; index < guidBytes.Length; index++)
        {
            if (guidBytes[index] != 0)
            {
                mask |= (byte)(1 << index);
            }
        }

        writer.WriteUInt8(mask);
        for (int index = 0; index < guidBytes.Length; index++)
        {
            if (guidBytes[index] != 0)
            {
                writer.WriteUInt8(guidBytes[index]);
            }
        }
    }

    private static uint BuildUnitBytes0(byte race, byte playerClass, byte gender)
    {
        return race | ((uint)playerClass << 8) | ((uint)gender << 16) | ((uint)ResolvePowerType(playerClass) << 24);
    }

    private static byte ResolvePowerType(byte playerClass)
    {
        return playerClass switch
        {
            1 => 1, // Warrior: rage
            4 => 3, // Rogue: energy
            _ => 0, // Vanilla player classes otherwise use mana here.
        };
    }

    private static uint ResolveFactionTemplateId(byte race)
    {
        return race switch
        {
            1 => 1, // Human
            2 => 2, // Orc
            3 => 3, // Dwarf
            4 => 4, // Night Elf
            5 => 5, // Undead
            6 => 6, // Tauren
            7 => 115, // Gnome
            8 => 116, // Troll
            _ => 1,
        };
    }

    private static uint ResolvePlayerDisplayId(byte race, byte gender)
    {
        bool female = gender == 1;
        return race switch
        {
            1 => female ? 50u : 49u,
            2 => female ? 52u : 51u,
            3 => female ? 54u : 53u,
            4 => female ? 56u : 55u,
            5 => female ? 58u : 57u,
            6 => female ? 60u : 59u,
            7 => female ? 1479u : 1478u,
            8 => female ? 1477u : 1476u,
            _ => 49u,
        };
    }

    private static uint FloatToUInt32(float value)
    {
        return BitConverter.SingleToUInt32Bits(value);
    }

    public static byte[] BuildLoginSetTimeSpeed(DateTimeOffset localTime, float gameSpeed = 0.01666667f)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(EncodePackedGameTime(localTime));
        writer.WriteFloat(gameSpeed);
        return writer.ToArray();
    }

    public static byte[] BuildMessageOfTheDay(string message)
    {
        WorldPacketWriter writer = new();
        string[] lines = string.IsNullOrWhiteSpace(message)
            ? ["Welcome to Emulation Server."]
            : message.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        writer.WriteUInt32((uint)lines.Length);
        foreach (string line in lines)
        {
            writer.WriteCString(line);
        }

        return writer.ToArray();
    }

    public static byte[] BuildInitialSpells()
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt8(0);
        writer.WriteUInt16(0); // spell count
        writer.WriteUInt16(0); // cooldown count
        return writer.ToArray();
    }

    public static byte[] BuildActionButtons()
    {
        WorldPacketWriter writer = new();
        for (int index = 0; index < 120; index++)
        {
            writer.WriteUInt32(0);
        }

        return writer.ToArray();
    }

    public static byte[] BuildInitializeFactions()
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(64);
        for (int index = 0; index < 64; index++)
        {
            writer.WriteUInt8(0);
            writer.WriteUInt32(0);
        }

        return writer.ToArray();
    }

    public static byte[] BuildBindPointUpdate(PlayerLoginRecord player)
    {
        ArgumentNullException.ThrowIfNull(player);

        WorldPacketWriter writer = new();
        writer.WriteFloat(player.PositionX);
        writer.WriteFloat(player.PositionY);
        writer.WriteFloat(player.PositionZ);
        writer.WriteUInt32(player.Map);
        writer.WriteUInt32(player.Zone);
        return writer.ToArray();
    }

    public static byte[] BuildSetRestStart(DateTimeOffset localTime)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32((uint)localTime.ToUnixTimeSeconds());
        return writer.ToArray();
    }

    public static byte[] BuildItemQuerySingleResponse(ItemTemplateRecord itemTemplate)
    {
        ArgumentNullException.ThrowIfNull(itemTemplate);

        WorldPacketWriter writer = new();
        writer.WriteUInt32(itemTemplate.Entry);
        writer.WriteUInt32(itemTemplate.Class);
        writer.WriteUInt32(itemTemplate.SubClass);
        writer.WriteCString(itemTemplate.Name);
        writer.WriteCString(string.Empty);
        writer.WriteCString(string.Empty);
        writer.WriteCString(string.Empty);
        writer.WriteUInt32(itemTemplate.DisplayId);
        writer.WriteUInt32(1); // quality; Common until full item_template loading is added
        writer.WriteUInt32(itemTemplate.Flags);
        writer.WriteUInt32(1); // buy count
        writer.WriteUInt32(0); // buy price
        writer.WriteUInt32(0); // sell price
        writer.WriteUInt32(itemTemplate.InventoryType);
        writer.WriteUInt32(0xFFFFFFFF); // allowable class
        writer.WriteUInt32(0xFFFFFFFF); // allowable race
        writer.WriteUInt32(1); // item level
        writer.WriteUInt32(0); // required level
        writer.WriteUInt32(0); // required skill
        writer.WriteUInt32(0); // required skill rank
        writer.WriteUInt32(0); // required spell
        writer.WriteUInt32(0); // required honor rank
        writer.WriteUInt32(0); // required city rank
        writer.WriteUInt32(0); // required reputation faction
        writer.WriteUInt32(0); // required reputation rank
        writer.WriteUInt32(0); // max count
        writer.WriteUInt32(1); // stackable
        writer.WriteUInt32(0); // container slots

        for (int index = 0; index < 10; index++)
        {
            writer.WriteUInt32(0); // stat type
            writer.WriteUInt32(0); // stat value
        }

        for (int index = 0; index < 5; index++)
        {
            writer.WriteFloat(0);
            writer.WriteFloat(0);
            writer.WriteUInt32(0);
        }

        writer.WriteUInt32(0); // armor
        writer.WriteUInt32(0); // holy resistance
        writer.WriteUInt32(0); // fire resistance
        writer.WriteUInt32(0); // nature resistance
        writer.WriteUInt32(0); // frost resistance
        writer.WriteUInt32(0); // shadow resistance
        writer.WriteUInt32(0); // arcane resistance
        writer.WriteUInt32(0); // delay
        writer.WriteUInt32(0); // ammo type
        writer.WriteFloat(0); // ranged mod range

        for (int index = 0; index < 5; index++)
        {
            writer.WriteUInt32(0); // spell id
            writer.WriteUInt32(0); // spell trigger
            writer.WriteUInt32(0); // spell charges
            writer.WriteUInt32(0); // spell cooldown
            writer.WriteUInt32(0); // spell category
            writer.WriteUInt32(0); // spell category cooldown
        }

        writer.WriteUInt32(0); // bonding
        writer.WriteCString(string.Empty); // description
        writer.WriteUInt32(0); // page text
        writer.WriteUInt32(0); // language id
        writer.WriteUInt32(0); // page material
        writer.WriteUInt32(0); // start quest
        writer.WriteUInt32(0); // lock id
        writer.WriteUInt32(0); // material
        writer.WriteUInt32(0); // sheath
        writer.WriteUInt32(0); // random property
        writer.WriteUInt32(0); // block
        writer.WriteUInt32(0); // item set
        writer.WriteUInt32(itemTemplate.MaxDurability);
        writer.WriteUInt32(0); // area
        writer.WriteUInt32(0); // map
        writer.WriteUInt32(0); // bag family

        return writer.ToArray();
    }

    public static byte[] BuildItemQuerySingleNotFound(uint itemEntry)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(itemEntry | 0x80000000u);
        return writer.ToArray();
    }

    public static byte[] BuildChatMessage(
        ChatMessageType messageType,
        ChatLanguage language,
        ulong senderGuid,
        string senderName,
        string text,
        string channelName = "")
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt8((byte)messageType);
        writer.WriteUInt32((uint)language);

        if (messageType == ChatMessageType.Channel)
        {
            writer.WriteCString(channelName);
        }

        writer.WriteUInt64(senderGuid);
        writer.WriteUInt32(0);
        writer.WriteUInt64(senderGuid);
        writer.WriteUInt32((uint)(text.Length + 1));
        writer.WriteCString(text);
        writer.WriteUInt8(0);
        _ = senderName;
        return writer.ToArray();
    }

    private static uint EncodePackedGameTime(DateTimeOffset localTime)
    {
        DateTime dateTime = localTime.DateTime;
        uint minute = (uint)dateTime.Minute;
        uint hour = (uint)dateTime.Hour;
        uint dayOfWeek = (uint)dateTime.DayOfWeek;
        uint day = (uint)(dateTime.Day - 1);
        uint month = (uint)(dateTime.Month - 1);
        uint year = (uint)Math.Max(0, dateTime.Year - 2000);

        return minute | (hour << 6) | (dayOfWeek << 11) | (day << 14) | (month << 20) | (year << 24);
    }

    public static byte[] BuildPong(uint sequence)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(sequence);
        return writer.ToArray();
    }
}
