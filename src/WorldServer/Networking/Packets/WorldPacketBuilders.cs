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
using System.Text;
using System.Globalization;

using EmulationServer.Game.Characters;
using EmulationServer.Game.Chat;
using EmulationServer.Game.Players;
using EmulationServer.Game.WorldData;

/**
  * File overview: src/WorldServer/Networking/Packets/WorldPacketBuilders.cs
  * Documents the WorldPacketBuilders source file in the World of Warcraft packet opcode, reader, writer, and builder support area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.WorldServer.Networking.Packets;

/**
  * Owns the world packet builders behavior for the World of Warcraft packet opcode, reader, writer, and builder support layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public static class WorldPacketBuilders
{
    /**
      * Defines the constant value for character equipment slot count.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const int CharacterEquipmentSlotCount = 19;
    /**
      * Defines the constant value for at login first.
      * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
      */
    private const uint AtLoginFirst = 0x20;

    /**
      * Vanilla object create movement update flags.
      * These mirror the MaNGOS 1.12 layout used by Object::BuildMovementUpdate:
      * - write update flags first
      * - write MovementInfo when UPDATEFLAG_LIVING is present
      * - write movement speeds immediately after living MovementInfo
      * - write optional trailing fields in flag order.
      */
    [Flags]
    private enum VanillaUpdateFlags : byte
    {
        None = 0x00,
        Self = 0x01,
        Transport = 0x02,
        HasAttackingTarget = 0x04,
        HighGuid = 0x08,
        All = 0x10,
        Living = 0x20,
        HasPosition = 0x40,
    }

    private const float PlayerWalkSpeed = 2.5f;
    private const float PlayerRunSpeed = 7.0f;
    private const float PlayerRunBackSpeed = 4.5f;
    private const float PlayerSwimSpeed = 4.722222f;
    private const float PlayerSwimBackSpeed = 2.5f;
    private const float PlayerTurnRate = 3.1415927f;

    /**
      * Builds the build auth challenge result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: serverSeed.
      */
    public static byte[] BuildAuthChallenge(uint serverSeed)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(serverSeed);
        return writer.ToArray();
    }

    /**
      * Builds the build auth response result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: code.
      */
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

    /**
      * Builds the build addon info result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: clientAddonInfo.
      */
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

    /**
      * Stores the default addon public key value used when the caller does not supply an override.
      * Centralizing the default keeps configuration and packet behavior consistent across the server process.
      */
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

    /**
      * Builds the build character create result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: result.
      */
    public static byte[] BuildCharacterCreate(CharacterCreateResult result)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt8((byte)result);
        return writer.ToArray();
    }

    /**
      * Builds the build character delete result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: result.
      */
    public static byte[] BuildCharacterDelete(CharacterDeleteResult result)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt8((byte)result);
        return writer.ToArray();
    }

    /**
      * Builds the build account data times result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      */
    public static byte[] BuildAccountDataTimes()
    {
        WorldPacketWriter writer = new();

        // Vanilla sends thirty-two uint32 values here. MaNGOS Zero, CMangos,
        // and VMangos send all zeros at login. Sending only eight values leaves
        // the client UI/addon cache bootstrap incomplete and can destabilize
        // the packets that immediately follow world entry.
        for (int index = 0; index < 32; index++)
        {
            writer.WriteUInt32(0);
        }

        return writer.ToArray();
    }

    /**
      * Builds the build update account data result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: accountDataType.
      */
    public static byte[] BuildUpdateAccountData(uint accountDataType)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(accountDataType);
        writer.WriteUInt32(0); // timestamp
        writer.WriteUInt32(0); // decompressed size; no payload follows
        return writer.ToArray();
    }

    /**
      * Builds the build character enum result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: characters.
      */
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

    /**
      * Builds the build character enum flags result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: character.
      */
    private static uint BuildCharacterEnumFlags(CharacterListEntry character)
    {
        // Do not pass the server-side characters.playerFlags value directly here.
        // The Vanilla character list packet expects client enum flags, while
        // characters.playerFlags is a persisted in-world player state field.
        _ = character;
        return 0;
    }

    /**
      * Builds the build character login failed result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: failureCode.
      */
    public static byte[] BuildCharacterLoginFailed(CharacterLoginFailureCode failureCode)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt8((byte)failureCode);
        return writer.ToArray();
    }

    /**
      * Builds the build notification result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: message.
      */
    public static byte[] BuildNotification(string message)
    {
        WorldPacketWriter writer = new();
        writer.WriteCString(message);
        return writer.ToArray();
    }

    /**
      * Builds the build transfer aborted result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: mapId, reason.
      */
    public static byte[] BuildTransferAborted(uint mapId, TransferAbortReason reason)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(mapId);
        writer.WriteUInt8((byte)reason);
        return writer.ToArray();
    }

    /**
      * Builds the build login verify world result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: player.
      */
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

    /**
      * Builds the build tutorial flags result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: player.
      */
    public static byte[] BuildTutorialFlags(PlayerLoginRecord player)
    {
        ArgumentNullException.ThrowIfNull(player);

        WorldPacketWriter writer = new();
        uint[] flags = player.TutorialFlags.Length == 8
            ? player.TutorialFlags
            : Enumerable.Repeat(uint.MaxValue, 8).ToArray();

        for (int index = 0; index < 8; index++)
        {
            writer.WriteUInt32(flags[index]);
        }

        return writer.ToArray();
    }

    /**
      * Builds the build player create update result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: player.
      */
    public static byte[] BuildPlayerCreateUpdate(PlayerLoginRecord player)
    {
        ArgumentNullException.ThrowIfNull(player);

        // Keep world entry to one player create-object block, but include the
        // Vanilla visible-item fields for equipped items. This lets the client
        // render the character's worn equipment without creating separate item
        // objects during the fragile login transition.
        PlayerInventoryItem[] visibleInventory = player.Inventory
            .Where(item => item.IsEquipped && item.TemplateEntry != 0)
            .GroupBy(item => item.Slot)
            .Select(group => group.First())
            .OrderBy(item => item.Slot)
            .ToArray();

        WorldPacketWriter writer = new();
        writer.WriteUInt32(1); // amount_of_objects
        writer.WriteUInt8(0); // has_transport

        writer.WriteUInt8(3); // CREATE_OBJECT2
        WritePackedGuid(writer, player.ClientGuid);
        writer.WriteUInt8(4); // PLAYER
        WritePlayerMovementBlock(writer, player);
        WritePlayerCreateUpdateMask(writer, player, visibleInventory);

        return writer.ToArray();
    }

    /**
      * Writes write player movement block data to the target packet, stream, or persistent store.
      * The method keeps binary layout and serialization rules centralized for easier packet review and compatibility fixes.
      * Inputs used by this operation: writer, player.
      */
    private static void WritePlayerMovementBlock(WorldPacketWriter writer, PlayerLoginRecord player)
    {
        const VanillaUpdateFlags updateFlags = VanillaUpdateFlags.Self | VanillaUpdateFlags.All | VanillaUpdateFlags.Living;

        writer.WriteUInt8((byte)updateFlags);
        WritePlayerLivingMovementInfo(writer, player);
        WritePlayerMovementSpeeds(writer);

        // MaNGOS writes this field after the living movement/speed block when
        // UPDATEFLAG_ALL is set. This is not part of MovementInfo itself; it is
        // the optional UPDATEFLAG_ALL trailing field and should remain uint32 1.
        writer.WriteUInt32(1);
    }

    /**
      * Writes the Vanilla 1.12 MovementInfo layout used by UPDATEFLAG_LIVING.
      * The no-transport/no-swim/no-fall login state is intentionally minimal:
      * movement flags, client/server time, position, orientation, and fall time.
      */
    private static void WritePlayerLivingMovementInfo(WorldPacketWriter writer, PlayerLoginRecord player)
    {
        writer.WriteUInt32(0); // MovementFlags: player is spawned idle at login.
        writer.WriteUInt32(unchecked((uint)Environment.TickCount));
        writer.WriteFloat(player.PositionX);
        writer.WriteFloat(player.PositionY);
        writer.WriteFloat(player.PositionZ);
        writer.WriteFloat(player.Orientation);
        writer.WriteUInt32(0); // fallTime is uint32 in MovementInfo.
    }

    /**
      * Writes the Vanilla player speed block that follows living MovementInfo.
      * MaNGOS writes exactly six speeds for 1.12: walk, run, run-back, swim,
      * swim-back, and turn-rate.
      */
    private static void WritePlayerMovementSpeeds(WorldPacketWriter writer)
    {
        writer.WriteFloat(PlayerWalkSpeed);
        writer.WriteFloat(PlayerRunSpeed);
        writer.WriteFloat(PlayerRunBackSpeed);
        writer.WriteFloat(PlayerSwimSpeed);
        writer.WriteFloat(PlayerSwimBackSpeed);
        writer.WriteFloat(PlayerTurnRate);
    }

    /**
      * Writes write player create update mask data to the target packet, stream, or persistent store.
      * The method keeps binary layout and serialization rules centralized for easier packet review and compatibility fixes.
      * Inputs used by this operation: writer, player, visibleInventory.
      */
    private static void WritePlayerCreateUpdateMask(WorldPacketWriter writer, PlayerLoginRecord player, IReadOnlyList<PlayerInventoryItem> visibleInventory)
    {
        const int ObjectFieldGuid = 0x0000;
        const int ObjectFieldType = 0x0002;
        const int ObjectFieldScaleX = 0x0004;
        const int UnitFieldHealth = 0x0016;
        const int UnitFieldPower1 = 0x0017;
        const int UnitFieldMaxHealth = 0x001C;
        const int UnitFieldMaxPower1 = 0x001D;
        const int UnitFieldLevel = 0x0022;
        const int UnitFieldFactionTemplate = 0x0023;
        const int UnitFieldBytes0 = 0x0024;
        const int UnitFieldFlags = 0x002E;
        const int UnitFieldBaseAttackTime = 0x007E;
        const int UnitFieldRangedAttackTime = 0x0080;
        const int UnitFieldBoundingRadius = 0x0081;
        const int UnitFieldCombatReach = 0x0082;
        const int UnitFieldDisplayId = 0x0083;
        const int UnitFieldNativeDisplayId = 0x0084;
        const int UnitFieldMinDamage = 0x0086;
        const int UnitFieldMaxDamage = 0x0087;
        const int UnitFieldMinOffHandDamage = 0x0088;
        const int UnitFieldMaxOffHandDamage = 0x0089;
        const int UnitFieldBytes1 = 0x008A;
        const int UnitFieldDynamicFlags = 0x008B;
        const int UnitModCastSpeed = 0x008C;
        const int UnitFieldStat0 = 0x0096;
        const int UnitFieldResistances = 0x009B;
        const int UnitFieldBaseMana = 0x00A2;
        const int UnitFieldBaseHealth = 0x00A3;
        const int UnitFieldBytes2 = 0x00A4;
        const int UnitFieldAttackPower = 0x00A5;
        const int UnitFieldAttackPowerMods = 0x00A6;
        const int UnitFieldAttackPowerMultiplier = 0x00A7;
        const int UnitFieldRangedAttackPower = 0x00A8;
        const int UnitFieldRangedAttackPowerMods = 0x00A9;
        const int UnitFieldRangedAttackPowerMultiplier = 0x00AA;
        const int UnitFieldMinRangedDamage = 0x00AB;
        const int UnitFieldMaxRangedDamage = 0x00AC;
        const int PlayerFlags = 0x00BE;
        const int PlayerBytes = 0x00C1;
        const int PlayerBytes2 = 0x00C2;
        const int PlayerBytes3 = 0x00C3;
        const int PlayerVisibleItem1Item0 = 0x0104;
        const int PlayerVisibleItemFieldCount = 12;
        const int PlayerXp = 0x02CC;
        const int PlayerNextLevelXp = 0x02CD;
        const int PlayerRestStateExperience = 0x0497;
        const int PlayerFieldCoinage = 0x0498;
        const int PlayerFieldPosStat0 = 0x0499;
        const int PlayerFieldNegStat0 = 0x049E;
        const int PlayerFieldBytes = 0x04C6;
        const int PlayerFieldBytes2 = 0x04EC;
        const int PlayerFieldWatchedFactionIndex = 0x04ED;

        Dictionary<int, uint> fields = [];
        ulong clientGuid = player.ClientGuid;
        uint health = player.Stats.Health == 0 ? 100u : player.Stats.Health;
        uint mana = player.Stats.Power1;
        uint rage = player.Class == 1 ? 1000u : player.Stats.Power2;
        uint energy = player.Class == 4 ? 100u : player.Stats.Power4;
        uint displayId = ResolvePlayerDisplayId(player.Race, player.Gender);
        uint level = Math.Max((uint)player.Level, 1u);

        fields[ObjectFieldGuid] = (uint)(clientGuid & uint.MaxValue);
        fields[ObjectFieldGuid + 1] = (uint)(clientGuid >> 32);
        fields[ObjectFieldType] = 0x19; // OBJECT | UNIT | PLAYER
        fields[ObjectFieldScaleX] = FloatToUInt32(1.0f);

        fields[UnitFieldHealth] = health;
        fields[UnitFieldPower1] = mana;
        fields[UnitFieldPower1 + 1] = rage;
        fields[UnitFieldPower1 + 2] = player.Stats.Power3;
        fields[UnitFieldPower1 + 3] = energy;
        fields[UnitFieldPower1 + 4] = player.Stats.Power5;
        fields[UnitFieldMaxHealth] = health;
        fields[UnitFieldMaxPower1] = mana;
        fields[UnitFieldMaxPower1 + 1] = rage;
        fields[UnitFieldMaxPower1 + 2] = player.Stats.Power3;
        fields[UnitFieldMaxPower1 + 3] = energy;
        fields[UnitFieldMaxPower1 + 4] = player.Stats.Power5;
        fields[UnitFieldLevel] = level;
        fields[UnitFieldFactionTemplate] = ResolveFactionTemplateId(player.Race);
        fields[UnitFieldBytes0] = BuildUnitBytes0(player.Race, player.Class, player.Gender);
        fields[UnitFieldFlags] = 0;
        fields[UnitFieldBaseAttackTime] = 2000;
        fields[UnitFieldBaseAttackTime + 1] = 2000;
        fields[UnitFieldRangedAttackTime] = 2000;
        fields[UnitFieldBoundingRadius] = FloatToUInt32(0.389f);
        fields[UnitFieldCombatReach] = FloatToUInt32(1.5f);
        fields[UnitFieldDisplayId] = displayId;
        fields[UnitFieldNativeDisplayId] = displayId;
        fields[UnitFieldMinDamage] = FloatToUInt32(1.0f);
        fields[UnitFieldMaxDamage] = FloatToUInt32(2.0f);
        fields[UnitFieldMinOffHandDamage] = FloatToUInt32(0.0f);
        fields[UnitFieldMaxOffHandDamage] = FloatToUInt32(0.0f);
        fields[UnitFieldBytes1] = 0;
        fields[UnitFieldDynamicFlags] = 0;
        fields[UnitModCastSpeed] = FloatToUInt32(1.0f);

        fields[UnitFieldStat0] = Math.Max(player.Stats.Strength, 1u);
        fields[UnitFieldStat0 + 1] = Math.Max(player.Stats.Agility, 1u);
        fields[UnitFieldStat0 + 2] = Math.Max(player.Stats.Stamina, 1u);
        fields[UnitFieldStat0 + 3] = Math.Max(player.Stats.Intellect, 1u);
        fields[UnitFieldStat0 + 4] = Math.Max(player.Stats.Spirit, 1u);
        fields[UnitFieldResistances] = player.Stats.Armor;
        for (int school = 1; school < 7; school++)
        {
            fields[UnitFieldResistances + school] = 0;
        }

        fields[UnitFieldBaseMana] = mana;
        fields[UnitFieldBaseHealth] = health;
        fields[UnitFieldBytes2] = 0;
        fields[UnitFieldAttackPower] = Math.Max(1u, player.Stats.Strength * 2u);
        fields[UnitFieldAttackPowerMods] = 0;
        fields[UnitFieldAttackPowerMultiplier] = FloatToUInt32(0.0f);
        fields[UnitFieldRangedAttackPower] = player.Stats.Agility;
        fields[UnitFieldRangedAttackPowerMods] = 0;
        fields[UnitFieldRangedAttackPowerMultiplier] = FloatToUInt32(0.0f);
        fields[UnitFieldMinRangedDamage] = FloatToUInt32(0.0f);
        fields[UnitFieldMaxRangedDamage] = FloatToUInt32(0.0f);

        fields[PlayerFlags] = player.PlayerFlags;
        fields[PlayerBytes] = player.PlayerBytes;
        fields[PlayerBytes2] = player.PlayerBytes2;
        fields[PlayerBytes3] = 0;

        foreach (PlayerInventoryItem item in visibleInventory)
        {
            if (item.Slot >= CharacterEquipmentSlotCount)
            {
                continue;
            }

            int visibleItemBase = PlayerVisibleItem1Item0 + (item.Slot * PlayerVisibleItemFieldCount);
            fields[visibleItemBase] = item.TemplateEntry;
            if (item.EnchantmentId != 0)
            {
                fields[visibleItemBase + 1] = item.EnchantmentId;
            }

            // The inventory slot GUID fields are intentionally not written in
            // the initial player create block yet. They require matching item
            // object updates, and malformed item object updates were causing
            // Vanilla clients to crash during CMSG_PLAYER_LOGIN.
        }

        fields[PlayerXp] = player.Experience;
        fields[PlayerNextLevelXp] = player.NextLevelExperience == 0 ? BuildNextLevelExperience(player.Level) : player.NextLevelExperience;
        fields[PlayerRestStateExperience] = 0;
        fields[PlayerFieldCoinage] = player.Money;
        for (int index = 0; index < 5; index++)
        {
            fields[PlayerFieldPosStat0 + index] = 0;
            fields[PlayerFieldNegStat0 + index] = 0;
        }

        fields[PlayerFieldBytes] = player.PlayerBytes;
        fields[PlayerFieldBytes2] = player.PlayerBytes2;
        fields[PlayerFieldWatchedFactionIndex] = uint.MaxValue;

        WriteUpdateMask(writer, fields);
    }

    /**
      * Writes write item create update block data to the target packet, stream, or persistent store.
      * The method keeps binary layout and serialization rules centralized for easier packet review and compatibility fixes.
      * Inputs used by this operation: writer, player, item.
      */
    private static void WriteItemCreateUpdateBlock(WorldPacketWriter writer, PlayerLoginRecord player, PlayerInventoryItem item)
    {
        writer.WriteUInt8(3); // CREATE_OBJECT2
        WritePackedGuid(writer, CharacterGuid.ToClientGuid(item.ItemGuid));
        writer.WriteUInt8(1); // ITEM
        writer.WriteUInt8(0); // item update flags
        WriteItemCreateUpdateMask(writer, player, item);
    }

    /**
      * Writes write item create update mask data to the target packet, stream, or persistent store.
      * The method keeps binary layout and serialization rules centralized for easier packet review and compatibility fixes.
      * Inputs used by this operation: writer, player, item.
      */
    private static void WriteItemCreateUpdateMask(WorldPacketWriter writer, PlayerLoginRecord player, PlayerInventoryItem item)
    {
        const int ObjectFieldGuid = 0x0000;
        const int ObjectFieldType = 0x0002;
        const int ObjectFieldEntry = 0x0003;
        const int ObjectFieldScaleX = 0x0004;
        const int ItemFieldOwner = 0x0006;
        const int ItemFieldContained = 0x0008;
        const int ItemFieldStackCount = 0x000E;
        const int ItemFieldFlags = 0x0015;
        const int ItemFieldDurability = 0x002E;
        const int ItemFieldMaxDurability = 0x002F;

        Dictionary<int, uint> fields = ReadItemInstanceFields(item.InstanceData);
        ulong itemClientGuid = CharacterGuid.ToClientGuid(item.ItemGuid);
        ulong ownerClientGuid = player.ClientGuid;

        fields[ObjectFieldGuid] = (uint)(itemClientGuid & uint.MaxValue);
        fields[ObjectFieldGuid + 1] = (uint)(itemClientGuid >> 32);
        fields[ObjectFieldType] = 0x03; // OBJECT | ITEM
        fields[ObjectFieldEntry] = item.TemplateEntry;
        fields[ObjectFieldScaleX] = FloatToUInt32(1.0f);
        fields[ItemFieldOwner] = (uint)(ownerClientGuid & uint.MaxValue);
        fields[ItemFieldOwner + 1] = (uint)(ownerClientGuid >> 32);
        fields[ItemFieldContained] = fields[ItemFieldOwner];
        fields[ItemFieldContained + 1] = fields[ItemFieldOwner + 1];
        fields[ItemFieldStackCount] = fields.TryGetValue(ItemFieldStackCount, out uint stackCount) && stackCount != 0 ? stackCount : 1u;
        fields.TryAdd(ItemFieldFlags, 0);
        fields.TryAdd(ItemFieldDurability, 0);
        fields.TryAdd(ItemFieldMaxDurability, fields[ItemFieldDurability]);

        WriteUpdateMask(writer, fields);
    }

    private static Dictionary<int, uint> ReadItemInstanceFields(string instanceData)
    {
        Dictionary<int, uint> fields = [];
        if (string.IsNullOrWhiteSpace(instanceData))
        {
            return fields;
        }

        string[] parts = instanceData.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int count = Math.Min(parts.Length, 48);
        for (int index = 0; index < count; index++)
        {
            if (uint.TryParse(parts[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint value) && value != 0)
            {
                fields[index] = value;
            }
        }

        return fields;
    }

    /**
      * Writes write update mask data to the target packet, stream, or persistent store.
      * The method keeps binary layout and serialization rules centralized for easier packet review and compatibility fixes.
      * Inputs used by this operation: writer, fields.
      */
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

    /**
      * Builds the build movement broadcast result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: clientGuid, clientMovementPayload.
      */
    public static byte[] BuildMovementBroadcast(ulong clientGuid, ReadOnlySpan<byte> clientMovementPayload)
    {
        WorldPacketWriter writer = new();
        WritePackedGuid(writer, clientGuid);
        writer.WriteBytes(clientMovementPayload);
        return writer.ToArray();
    }

    /**
      * Writes write packed guid data to the target packet, stream, or persistent store.
      * The method keeps binary layout and serialization rules centralized for easier packet review and compatibility fixes.
      * Inputs used by this operation: writer, guid.
      */
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

    /**
      * Builds the build unit bytes 0 result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: race, playerClass, gender.
      */
    private static uint BuildUnitBytes0(byte race, byte playerClass, byte gender)
    {
        return race | ((uint)playerClass << 8) | ((uint)gender << 16) | ((uint)ResolvePowerType(playerClass) << 24);
    }

    /**
      * Resolves the power type value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: playerClass.
      */
    private static byte ResolvePowerType(byte playerClass)
    {
        return playerClass switch
        {
            1 => 1, // Warrior: rage
            4 => 3, // Rogue: energy
            _ => 0, // Vanilla player classes otherwise use mana here.
        };
    }

    /**
      * Resolves the faction template id value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: race.
      */
    private static uint ResolveFactionTemplateId(byte race)
    {
        return race switch
        {
            1 => 1,
            2 => 2,
            3 => 3,
            4 => 4,
            5 => 5,
            6 => 6,
            7 => 115,
            8 => 116,
            _ => 1,
        };
    }

    /**
      * Resolves the player display id value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: race, gender.
      */
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

    /**
      * Performs the float to u int 32 operation for the World of Warcraft packet opcode, reader, writer, and builder support workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: value.
      */
    private static uint FloatToUInt32(float value)
    {
        return BitConverter.SingleToUInt32Bits(value);
    }

    /**
      * Builds the build next level experience result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: level.
      */
    private static uint BuildNextLevelExperience(byte level)
    {
        uint safeLevel = Math.Max((uint)level, 1u);
        return safeLevel switch
        {
            1 => 400,
            2 => 900,
            3 => 1400,
            4 => 2100,
            5 => 2800,
            6 => 3600,
            7 => 4500,
            8 => 5400,
            9 => 6500,
            10 => 7600,
            _ => 7600 + ((safeLevel - 10) * 1100u),
        };
    }

    /**
      * Builds the build login set time speed result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: localTime, gameSpeed.
      */
    public static byte[] BuildLoginSetTimeSpeed(DateTimeOffset localTime, float gameSpeed = 0.01666667f)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(EncodePackedGameTime(localTime));
        writer.WriteFloat(gameSpeed);
        return writer.ToArray();
    }

    /**
      * Builds the build message of the day result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: message.
      */
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

    /**
      * Builds the build initial spells result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: player.
      */
    public static byte[] BuildInitialSpells(PlayerLoginRecord player)
    {
        ArgumentNullException.ThrowIfNull(player);

        ushort[] spellIds = GetLoginSpellIds(player).ToArray();
        WorldPacketWriter writer = new();
        writer.WriteUInt8(0); // MaNGOS Zero sends zero here for Vanilla.
        writer.WriteUInt16((ushort)spellIds.Length);
        foreach (ushort spellId in spellIds)
        {
            writer.WriteUInt16(spellId);
            writer.WriteUInt16(0);
        }

        writer.WriteUInt16(0); // cooldown count
        return writer.ToArray();
    }

    /**
      * Builds the build action buttons result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: player.
      */
    public static byte[] BuildActionButtons(PlayerLoginRecord player)
    {
        ArgumentNullException.ThrowIfNull(player);

        uint[] buttons = new uint[120];
        if (player.ActionButtons.Count != 0)
        {
            foreach (PlayerActionButton actionButton in player.ActionButtons)
            {
                if (actionButton.Button < buttons.Length)
                {
                    buttons[actionButton.Button] = actionButton.PackedValue;
                }
            }
        }
        else
        {
            ushort[] starterSpells = GetStarterActionButtonSpellIds(player.Class).ToArray();
            for (int index = 0; index < starterSpells.Length && index < buttons.Length; index++)
            {
                buttons[index] = starterSpells[index];
            }
        }

        WorldPacketWriter writer = new();
        for (int index = 0; index < buttons.Length; index++)
        {
            writer.WriteUInt32(buttons[index]);
        }

        return writer.ToArray();
    }

    /**
      * Resolves the login spell ids value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: player.
      */
    private static IEnumerable<ushort> GetLoginSpellIds(PlayerLoginRecord player)
    {
        SortedSet<ushort> spellIds = [];
        foreach (PlayerSpell spell in player.Spells)
        {
            if (!spell.Active || spell.Disabled || spell.SpellId == 0 || spell.SpellId > ushort.MaxValue)
            {
                continue;
            }

            spellIds.Add((ushort)spell.SpellId);
        }

        if (spellIds.Count == 0)
        {
            foreach (ushort fallbackSpell in GetInitialSpellIds(player))
            {
                spellIds.Add(fallbackSpell);
            }
        }
        else
        {
            foreach (ushort languageSpell in GetLanguageSpellIds(player.Race, player.Faction))
            {
                spellIds.Add(languageSpell);
            }

            spellIds.Add(81);   // Dodge
            spellIds.Add(203);  // Unarmed
            spellIds.Add(204);  // Defense
            spellIds.Add(522);  // SPELLDEFENSE client bookkeeping spell
            spellIds.Add(6603); // Auto Attack
        }

        return spellIds;
    }

    /**
      * Resolves the initial spell ids value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: player.
      */
    private static IEnumerable<ushort> GetInitialSpellIds(PlayerLoginRecord player)
    {
        SortedSet<ushort> spells =
        [
            81, // Dodge
            203, // Unarmed
            204, // Defense
            522, // SPELLDEFENSE client bookkeeping spell
            6603, // Auto Attack
        ];

        foreach (ushort languageSpell in GetLanguageSpellIds(player.Race, player.Faction))
        {
            spells.Add(languageSpell);
        }

        foreach (ushort classSpell in GetStarterActionButtonSpellIds(player.Class))
        {
            spells.Add(classSpell);
        }

        return spells;
    }

    /**
      * Resolves the language spell ids value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: race, faction.
      */
    private static IEnumerable<ushort> GetLanguageSpellIds(byte race, PlayerFaction faction)
    {
        yield return faction == PlayerFaction.Horde ? (ushort)669 : (ushort)668; // Orcish or Common.

        ushort raceLanguage = race switch
        {
            3 => 672, // Dwarven
            4 => 671, // Darnassian
            6 => 670, // Taurahe
            7 => 7340, // Gnomish
            8 => 7341, // Troll
            _ => 0,
        };

        if (raceLanguage != 0)
        {
            yield return raceLanguage;
        }
    }

    /**
      * Resolves the starter action button spell ids value requested by the caller.
      * Lookup logic is kept in this method so fallback rules, case handling, and missing-data behavior stay consistent across call sites.
      * Inputs used by this operation: playerClass.
      */
    private static IEnumerable<ushort> GetStarterActionButtonSpellIds(byte playerClass)
    {
        return playerClass switch
        {
            1 => new ushort[] { 78, 2457 }, // Warrior: Heroic Strike, Battle Stance
            2 => new ushort[] { 635, 21084 }, // Paladin: Holy Light, Seal of Righteousness
            3 => new ushort[] { 75, 2973 }, // Hunter: Auto Shot, Raptor Strike
            4 => new ushort[] { 1752 }, // Rogue: Sinister Strike
            5 => new ushort[] { 585, 2050 }, // Priest: Smite, Lesser Heal
            7 => new ushort[] { 403, 331 }, // Shaman: Lightning Bolt, Healing Wave
            8 => new ushort[] { 133, 168 }, // Mage: Fireball, Frost Armor
            9 => new ushort[] { 686, 687 }, // Warlock: Shadow Bolt, Demon Skin
            11 => new ushort[] { 5176, 5185 }, // Druid: Wrath, Healing Touch
            _ => Array.Empty<ushort>(),
        };
    }

    /**
      * Builds the build initialize factions result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      */
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

    /**
      * Builds the build bind point update result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: player.
      */
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

    /**
      * Builds the build set rest start result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: localTime.
      */
    public static byte[] BuildSetRestStart(DateTimeOffset localTime)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32((uint)localTime.ToUnixTimeSeconds());
        return writer.ToArray();
    }

    /**
      * Builds the build item query single response result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: itemTemplate.
      */
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

    /**
      * Builds the build item query single not found result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: itemEntry.
      */
    public static byte[] BuildItemQuerySingleNotFound(uint itemEntry)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(itemEntry | 0x80000000u);
        return writer.ToArray();
    }

    /**
      * Builds the build chat message result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: messageType, language, senderGuid, senderName, text, channelName....
      */
    public static byte[] BuildChatMessage(
        ChatMessageType messageType,
        ChatLanguage language,
        ulong senderGuid,
        string senderName,
        string text,
        string channelName = "",
        byte chatTag = 0,
        uint channelPlayerRank = 0)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt8((byte)messageType);
        writer.WriteUInt32((uint)language);

        switch (messageType)
        {
            case ChatMessageType.Say:
            case ChatMessageType.Party:
            case ChatMessageType.Yell:
                writer.WriteUInt64(senderGuid);
                writer.WriteUInt64(senderGuid);
                break;

            case ChatMessageType.Channel:
                writer.WriteCString(channelName);
                writer.WriteUInt32(channelPlayerRank);
                writer.WriteUInt64(senderGuid);
                break;

            default:
                writer.WriteUInt64(senderGuid);
                break;
        }

        writer.WriteUInt32((uint)(Encoding.UTF8.GetByteCount(text) + 1));
        writer.WriteCString(text);
        writer.WriteUInt8(chatTag);
        _ = senderName;
        return writer.ToArray();
    }

    /**
      * Builds the build name query response result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: character.
      */
    public static byte[] BuildNameQueryResponse(CharacterNameQueryResult character)
    {
        ArgumentNullException.ThrowIfNull(character);

        WorldPacketWriter writer = new();
        writer.WriteUInt64(CharacterGuid.ToClientGuid(character.Guid));
        writer.WriteCString(character.Name);
        writer.WriteCString(string.Empty); // realm name; empty means local realm
        writer.WriteUInt32(character.Race);
        writer.WriteUInt32(character.Gender);
        writer.WriteUInt32(character.Class);
        return writer.ToArray();
    }

    /**
      * Builds the build logout response result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: reason, instantLogout.
      */
    public static byte[] BuildLogoutResponse(uint reason = 0, bool instantLogout = true)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(reason);
        writer.WriteUInt8(instantLogout ? (byte)1 : (byte)0);
        return writer.ToArray();
    }

    /**
      * Builds the build logout complete result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      */
    public static byte[] BuildLogoutComplete()
    {
        return [];
    }

    /**
      * Builds the build logout cancel ack result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      */
    public static byte[] BuildLogoutCancelAck()
    {
        return [];
    }

    /**
      * Builds the build server time result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: localTime.
      */
    public static byte[] BuildServerTime(DateTimeOffset localTime)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(EncodePackedGameTime(localTime));
        return writer.ToArray();
    }

    /**
      * Builds the build played time result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: player.
      */
    public static byte[] BuildPlayedTime(PlayerLoginRecord player)
    {
        ArgumentNullException.ThrowIfNull(player);

        WorldPacketWriter writer = new();
        writer.WriteUInt32(player.TotalTime);
        writer.WriteUInt32(player.LevelTime);
        return writer.ToArray();
    }

    /**
      * Builds the build channel notify result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: notificationType, channelName, channelFlags.
      */
    public static byte[] BuildChannelNotify(byte notificationType, string channelName, uint channelFlags = 0)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt8(notificationType);
        writer.WriteCString(channelName);

        if (notificationType == 0x02) // YOU_JOINED
        {
            writer.WriteUInt32(channelFlags);
            writer.WriteUInt32(0);
            writer.WriteUInt8(0);
        }

        return writer.ToArray();
    }

    /**
      * Builds the build channel list result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: channelName, members, channelFlags.
      */
    public static byte[] BuildChannelList(string channelName, IReadOnlyList<PlayerLoginRecord> members, uint channelFlags = 0)
    {
        ArgumentNullException.ThrowIfNull(members);

        WorldPacketWriter writer = new();
        writer.WriteCString(channelName);
        writer.WriteUInt8((byte)(channelFlags & 0xFF));
        writer.WriteUInt32((uint)members.Count);
        foreach (PlayerLoginRecord member in members)
        {
            writer.WriteUInt64(member.ClientGuid);
            writer.WriteUInt8(0); // normal member flags
        }

        return writer.ToArray();
    }

    /**
      * Builds the build who response result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: players.
      */
    public static byte[] BuildWhoResponse(IReadOnlyList<PlayerLoginRecord> players)
    {
        ArgumentNullException.ThrowIfNull(players);

        WorldPacketWriter writer = new();
        uint count = (uint)Math.Min(players.Count, 50);
        writer.WriteUInt32(count);
        writer.WriteUInt32(count);

        foreach (PlayerLoginRecord player in players.Take((int)count))
        {
            writer.WriteCString(player.Name);
            writer.WriteCString(string.Empty); // guild
            writer.WriteUInt32(player.Level);
            writer.WriteUInt32(player.Class);
            writer.WriteUInt32(player.Race);
            writer.WriteUInt32(player.Zone);
        }

        return writer.ToArray();
    }

    /**
      * Builds the build item name query response result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: itemTemplate.
      */
    public static byte[] BuildItemNameQueryResponse(ItemTemplateRecord itemTemplate)
    {
        ArgumentNullException.ThrowIfNull(itemTemplate);

        WorldPacketWriter writer = new();
        writer.WriteUInt32(itemTemplate.Entry);
        writer.WriteCString(itemTemplate.Name);
        writer.WriteUInt32(itemTemplate.InventoryType);
        return writer.ToArray();
    }

    /**
      * Builds the build item name query not found result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: itemEntry.
      */
    public static byte[] BuildItemNameQueryNotFound(uint itemEntry)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(itemEntry | 0x80000000u);
        return writer.ToArray();
    }

    /**
      * Performs the encode packed game time operation for the World of Warcraft packet opcode, reader, writer, and builder support workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: localTime.
      */
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

    /**
      * Builds the build pong result needed by the caller.
      * Centralized construction keeps defaults, validation rules, and packet/data layout decisions in one documented location.
      * Inputs used by this operation: sequence.
      */
    public static byte[] BuildPong(uint sequence)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(sequence);
        return writer.ToArray();
    }
}
