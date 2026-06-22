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
// File: src/WorldServer/Networking/Packets/WorldPacketBuilders.cs
// Purpose: Contains world packet builders code for the world server gameplay, session, and character runtime layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using System.Globalization;

using EmulationServer.Game.Characters;
using EmulationServer.Game.Chat;
using EmulationServer.Game.Players;
using EmulationServer.Game.Reputation;
using EmulationServer.Game.Formulas;
using EmulationServer.Game.WorldData;

namespace EmulationServer.WorldServer.Networking.Packets;

// Type: WorldPacketBuilders
// Purpose: Provides world packet builders behavior for the world server gameplay, session, and character runtime layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class WorldPacketBuilders
{

    // Constant: Defines the character equipment slot count constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed character equipment slot count value used anywhere this rule or protocol value is needed.
    private const int CharacterEquipmentSlotCount = 19;
    // Constant: Defines the inventory slot bag end constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed inventory slot bag end value used anywhere this rule or protocol value is needed.
    private const int InventorySlotBagEnd = 23;
    // Constant: Defines the inventory slot item start constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed inventory slot item start value used anywhere this rule or protocol value is needed.
    private const int InventorySlotItemStart = 23;
    // Constant: Defines the inventory slot item end constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed inventory slot item end value used anywhere this rule or protocol value is needed.
    private const int InventorySlotItemEnd = 39;
    // Constant: Defines the bank slot item start constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed bank slot item start value used anywhere this rule or protocol value is needed.
    private const int BankSlotItemStart = 39;
    // Constant: Defines the bank slot item end constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed bank slot item end value used anywhere this rule or protocol value is needed.
    private const int BankSlotItemEnd = 63;
    // Constant: Defines the bank slot bag start constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed bank slot bag start value used anywhere this rule or protocol value is needed.
    private const int BankSlotBagStart = 63;
    // Constant: Defines the bank slot bag end constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed bank slot bag end value used anywhere this rule or protocol value is needed.
    private const int BankSlotBagEnd = 69;
    // Constant: Defines the keyring slot start constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed keyring slot start value used anywhere this rule or protocol value is needed.
    private const int KeyringSlotStart = 81;
    // Constant: Defines the keyring slot end constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed keyring slot end value used anywhere this rule or protocol value is needed.
    private const int KeyringSlotEnd = 113;

    // Constant: Defines the at login first constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed at login first value used anywhere this rule or protocol value is needed.
    private const uint AtLoginFirst = 0x20;

    [Flags]
    // Type: VanillaUpdateFlags
    // Purpose: Defines the allowed vanilla update flags values used by the world server gameplay, session, and character runtime layer.
    // Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
    private enum VanillaUpdateFlags : byte
    {
        // Enum Value: Defines the none enum value.
        // Value: explicit expression 0x00.
        None = 0x00,
        // Enum Value: Defines the self enum value.
        // Value: explicit expression 0x01.
        Self = 0x01,
        // Enum Value: Defines the transport enum value.
        // Value: explicit expression 0x02.
        Transport = 0x02,
        // Enum Value: Defines the has attacking target enum value.
        // Value: explicit expression 0x04.
        HasAttackingTarget = 0x04,
        // Enum Value: Defines the high GUID enum value.
        // Value: explicit expression 0x08.
        HighGuid = 0x08,
        // Enum Value: Defines the all enum value.
        // Value: explicit expression 0x10.
        All = 0x10,
        // Enum Value: Defines the living enum value.
        // Value: explicit expression 0x20.
        Living = 0x20,
        // Enum Value: Defines the has position enum value.
        // Value: explicit expression 0x40.
        HasPosition = 0x40,
    }

    // Constant: Defines the player walk speed constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed player walk speed value used anywhere this rule or protocol value is needed.
    private const float PlayerWalkSpeed = 2.5f;
    // Constant: Defines the player run speed constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed player run speed value used anywhere this rule or protocol value is needed.
    private const float PlayerRunSpeed = 7.0f;
    // Constant: Defines the player run back speed constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed player run back speed value used anywhere this rule or protocol value is needed.
    private const float PlayerRunBackSpeed = 4.5f;
    // Constant: Defines the player swim speed constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed player swim speed value used anywhere this rule or protocol value is needed.
    private const float PlayerSwimSpeed = 4.722222f;
    // Constant: Defines the player swim back speed constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed player swim back speed value used anywhere this rule or protocol value is needed.
    private const float PlayerSwimBackSpeed = 2.5f;
    // Constant: Defines the player turn rate constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed player turn rate value used anywhere this rule or protocol value is needed.
    private const float PlayerTurnRate = 3.1415927f;

    // Method: BuildAuthChallenge
    // Purpose: Builds or writes build auth challenge output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - serverSeed: Server seed value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildAuthChallenge(uint serverSeed)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(serverSeed);
        return writer.ToArray();
    }

    // Method: BuildAuthResponse
    // Purpose: Builds or writes build auth response output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - code: Code value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: BuildAddonInfo
    // Purpose: Builds or writes build addon info output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - clientAddonInfo: Client addon info value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
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
            offset += 4;
            offset += 1;

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

    // Method: BuildCharacterCreate
    // Purpose: Builds or writes build character create output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - result: Result value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildCharacterCreate(CharacterCreateResult result)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt8((byte)result);
        return writer.ToArray();
    }

    // Method: BuildCharacterDelete
    // Purpose: Builds or writes build character delete output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - result: Result value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildCharacterDelete(CharacterDeleteResult result)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt8((byte)result);
        return writer.ToArray();
    }

    // Method: BuildAccountDataTimes
    // Purpose: Builds or writes build account data times output for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildAccountDataTimes()
    {
        WorldPacketWriter writer = new();

        for (int index = 0; index < 32; index++)
        {
            writer.WriteUInt32(0);
        }

        return writer.ToArray();
    }

    // Method: BuildUpdateAccountData
    // Purpose: Builds or writes build update account data output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - accountDataType: Account data type value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildUpdateAccountData(uint accountDataType)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(accountDataType);
        writer.WriteUInt32(0);
        writer.WriteUInt32(0);
        return writer.ToArray();
    }

    // Method: BuildCharacterEnum
    // Purpose: Builds or writes build character enum output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - characters: Characters value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
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
            writer.WriteUInt32(0);
            writer.WriteUInt32(0);
            writer.WriteUInt32(0);

            for (int slot = 0; slot < CharacterEquipmentSlotCount; slot++)
            {
                CharacterEquipmentDisplay equipment = slot < character.Equipment.Count
                    ? character.Equipment[slot]
                    : new CharacterEquipmentDisplay(0, 0, 0);

                writer.WriteUInt32(equipment.DisplayId);
                writer.WriteUInt8(equipment.InventoryType);
            }

            writer.WriteUInt32(0);
            writer.WriteUInt8(0);
        }

        return writer.ToArray();
    }

    // Method: BuildCharacterEnumFlags
    // Purpose: Builds or writes build character enum flags output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - character: Character value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static uint BuildCharacterEnumFlags(CharacterListEntry character)
    {

        _ = character;
        return 0;
    }

    // Method: BuildCharacterLoginFailed
    // Purpose: Builds or writes build character login failed output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - failureCode: Failure code value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildCharacterLoginFailed(CharacterLoginFailureCode failureCode)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt8((byte)failureCode);
        return writer.ToArray();
    }

    // Method: BuildNotification
    // Purpose: Builds or writes build notification output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - message: Message value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildNotification(string message)
    {
        WorldPacketWriter writer = new();
        writer.WriteCString(message);
        return writer.ToArray();
    }

    // Method: BuildTransferAborted
    // Purpose: Builds or writes build transfer aborted output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - mapId: Map ID identifier used to select the exact record, object, or runtime owner.
    // - reason: Reason value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildTransferAborted(uint mapId, TransferAbortReason reason)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(mapId);
        writer.WriteUInt8((byte)reason);
        return writer.ToArray();
    }

    // Method: BuildLoginVerifyWorld
    // Purpose: Builds or writes build login verify world output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: BuildTutorialFlags
    // Purpose: Builds or writes build tutorial flags output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: BuildPlayerCreateUpdate
    // Purpose: Builds or writes build player create update output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildPlayerCreateUpdate(PlayerLoginRecord player)
    {
        ArgumentNullException.ThrowIfNull(player);

        PlayerInventoryItem[] inventoryItems = GetOrderedPlayerInventoryItems(player);

        WorldPacketWriter writer = new();
        writer.WriteUInt32((uint)(inventoryItems.Length + 1));
        writer.WriteUInt8(0);

        foreach (PlayerInventoryItem item in inventoryItems)
        {
            WriteItemCreateUpdateBlock(writer, player, item, inventoryItems);
        }

        WritePlayerCreateUpdateBlock(writer, player, inventoryItems, isSelf: true, includeInventorySlotGuids: true, includePrivatePlayerFields: true);

        return writer.ToArray();
    }

    // Method: BuildVisiblePlayerCreateUpdate
    // Purpose: Builds the create update used when this player becomes visible to a different client.
    // Parameters:
    // - player: Player being created on the receiving client.
    // Returns: Returns the SMSG_UPDATE_OBJECT payload for one visible player object.
    // Notes: This intentionally omits inventory item object creates and the self movement flag because the receiver does not own this player.
    public static byte[] BuildVisiblePlayerCreateUpdate(PlayerLoginRecord player)
    {
        ArgumentNullException.ThrowIfNull(player);

        PlayerInventoryItem[] inventoryItems = GetOrderedPlayerInventoryItems(player);
        WorldPacketWriter writer = new();
        writer.WriteUInt32(1);
        writer.WriteUInt8(0);
        WritePlayerCreateUpdateBlock(writer, player, inventoryItems, isSelf: false, includeInventorySlotGuids: false, includePrivatePlayerFields: false);
        return writer.ToArray();
    }

    // Method: GetOrderedPlayerInventoryItems
    // Purpose: Produces a stable player inventory order for object update blocks and visible equipment fields.
    // Parameters:
    // - player: Player that owns the inventory collection.
    // Returns: Returns valid inventory entries ordered by bag, slot, and item GUID.
    private static PlayerInventoryItem[] GetOrderedPlayerInventoryItems(PlayerLoginRecord player)
    {
        return player.Inventory
            .Where(item => item.ItemGuid != 0 && item.TemplateEntry != 0)
            .OrderBy(item => item.BagGuid == 0 ? 0 : 1)
            .ThenBy(item => item.BagGuid)
            .ThenBy(item => item.Slot)
            .ThenBy(item => item.ItemGuid)
            .ToArray();
    }

    // Method: WritePlayerCreateUpdateBlock
    // Purpose: Writes one player create object block with the correct self/non-self movement flags.
    // Parameters:
    // - writer: Destination packet writer.
    // - player: Player represented by this create block.
    // - inventoryItems: Ordered inventory entries used for equipment display and self inventory fields.
    // - isSelf: True when writing the object for the owning client.
    // - includeInventorySlotGuids: True when private inventory slot GUID fields should be sent.
    // Returns: none.
    private static void WritePlayerCreateUpdateBlock(
        WorldPacketWriter writer,
        PlayerLoginRecord player,
        IReadOnlyList<PlayerInventoryItem> inventoryItems,
        bool isSelf,
        bool includeInventorySlotGuids,
        bool includePrivatePlayerFields)
    {
        writer.WriteUInt8(3);
        WritePackedGuid(writer, player.ClientGuid);
        writer.WriteUInt8(4);
        WritePlayerMovementBlock(writer, player, isSelf);
        WritePlayerCreateUpdateMask(writer, player, inventoryItems, includeInventorySlotGuids, includePrivatePlayerFields);
    }

    // Method: BuildGameObjectCreateUpdate
    // Purpose: Builds or writes build game object create update output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - gameObjects: Game objects value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildGameObjectCreateUpdate(IReadOnlyList<GameObjectClientCreateRecord> gameObjects)
    {
        ArgumentNullException.ThrowIfNull(gameObjects);

        if (gameObjects.Count == 0)
        {
            return [];
        }

        WorldPacketWriter writer = new();
        writer.WriteUInt32((uint)gameObjects.Count);
        writer.WriteUInt8(0);

        foreach (GameObjectClientCreateRecord gameObject in gameObjects)
        {
            WriteGameObjectCreateUpdateBlock(writer, gameObject.Spawn, gameObject.Template);
        }

        return writer.ToArray();
    }

    // Method: BuildCreatureCreateUpdate
    // Purpose: Builds or writes build creature create update output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - creatures: Creatures value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildCreatureCreateUpdate(IReadOnlyList<CreatureClientCreateRecord> creatures)
    {
        ArgumentNullException.ThrowIfNull(creatures);

        if (creatures.Count == 0)
        {
            return [];
        }

        WorldPacketWriter writer = new();
        writer.WriteUInt32((uint)creatures.Count);
        writer.WriteUInt8(0);

        foreach (CreatureClientCreateRecord creature in creatures)
        {
            WriteCreatureCreateUpdateBlock(writer, creature.Spawn, creature.Template);
        }

        return writer.ToArray();
    }

    // Method: BuildDestroyObject
    // Purpose: Builds or writes build destroy object output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - clientGuid: Client GUID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildDestroyObject(ulong clientGuid)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt64(clientGuid);
        return writer.ToArray();
    }

    // Method: WriteCreatureCreateUpdateBlock
    // Purpose: Builds or writes write creature create update block output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - writer: Writer value supplied by the caller for this operation.
    // - spawn: Spawn value supplied by the caller for this operation.
    // - template: Template value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static void WriteCreatureCreateUpdateBlock(
        WorldPacketWriter writer,
        CreatureSpawnRecord spawn,
        CreatureTemplateRecord template)
    {
        ulong clientGuid = CharacterGuid.ToCreatureGuid(spawn.Guid, spawn.Entry);

        writer.WriteUInt8(3);
        WritePackedGuid(writer, clientGuid);
        writer.WriteUInt8(3);
        WriteCreatureMovementBlock(writer, spawn, template);
        WriteCreatureCreateUpdateMask(writer, spawn, template, clientGuid);
    }

    // Method: WriteCreatureMovementBlock
    // Purpose: Builds or writes write creature movement block output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - writer: Writer value supplied by the caller for this operation.
    // - spawn: Spawn value supplied by the caller for this operation.
    // - template: Template value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static void WriteCreatureMovementBlock(
        WorldPacketWriter writer,
        CreatureSpawnRecord spawn,
        CreatureTemplateRecord template)
    {
        const VanillaUpdateFlags updateFlags = VanillaUpdateFlags.All | VanillaUpdateFlags.Living;

        writer.WriteUInt8((byte)updateFlags);
        writer.WriteUInt32(0);
        writer.WriteUInt32(unchecked((uint)Environment.TickCount));
        writer.WriteFloat(spawn.PositionX);
        writer.WriteFloat(spawn.PositionY);
        writer.WriteFloat(spawn.PositionZ);
        writer.WriteFloat(spawn.Orientation);
        writer.WriteUInt32(0);
        WriteCreatureMovementSpeeds(writer, template);
        writer.WriteUInt32(1);
    }

    // Method: WriteCreatureMovementSpeeds
    // Purpose: Builds or writes write creature movement speeds output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - writer: Writer value supplied by the caller for this operation.
    // - template: Template value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static void WriteCreatureMovementSpeeds(WorldPacketWriter writer, CreatureTemplateRecord template)
    {
        float walkSpeed = template.GetEffectiveWalkSpeed() * PlayerWalkSpeed;
        float runSpeed = template.GetEffectiveRunSpeed() * PlayerRunSpeed;

        writer.WriteFloat(float.IsFinite(walkSpeed) && walkSpeed > 0.0f ? walkSpeed : PlayerWalkSpeed);
        writer.WriteFloat(float.IsFinite(runSpeed) && runSpeed > 0.0f ? runSpeed : PlayerRunSpeed);
        writer.WriteFloat(PlayerRunBackSpeed);
        writer.WriteFloat(PlayerSwimSpeed);
        writer.WriteFloat(PlayerSwimBackSpeed);
        writer.WriteFloat(PlayerTurnRate);
    }

    // Method: WriteCreatureCreateUpdateMask
    // Purpose: Builds or writes write creature create update mask output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - writer: Writer value supplied by the caller for this operation.
    // - spawn: Spawn value supplied by the caller for this operation.
    // - template: Template value supplied by the caller for this operation.
    // - clientGuid: Client GUID identifier used to select the exact record, object, or runtime owner.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static void WriteCreatureCreateUpdateMask(
        WorldPacketWriter writer,
        CreatureSpawnRecord spawn,
        CreatureTemplateRecord template,
        ulong clientGuid)
    {
        const int ObjectFieldGuid = 0x0000;
        const int ObjectFieldType = 0x0002;
        const int ObjectFieldEntry = 0x0003;
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
        const int UnitNpcFlags = 0x0093;
        const int UnitNpcEmoteState = 0x0094;
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

        Dictionary<int, uint> fields = [];
        uint health = template.GetEffectiveHealth(spawn.CurrentHealth);
        uint mana = template.GetEffectiveMana(spawn.CurrentMana);
        uint level = template.GetEffectiveMinLevel();
        uint displayId = CreatureDataValidation.ResolveDisplayModelId(spawn, template);
        uint factionTemplate = template.FactionAlliance != 0 ? template.FactionAlliance : template.FactionHorde;
        float scale = float.IsFinite(template.Scale) && template.Scale > 0.0f && template.Scale <= 100.0f ? template.Scale : 1.0f;
        float minMeleeDamage = template.MinMeleeDamage > 0.0f && float.IsFinite(template.MinMeleeDamage) ? template.MinMeleeDamage : 1.0f;
        float maxMeleeDamage = template.MaxMeleeDamage >= minMeleeDamage && float.IsFinite(template.MaxMeleeDamage) ? template.MaxMeleeDamage : minMeleeDamage + 1.0f;

        WriteGuidFields(fields, ObjectFieldGuid, clientGuid);
        fields[ObjectFieldType] = 0x09;
        fields[ObjectFieldEntry] = spawn.Entry;
        fields[ObjectFieldScaleX] = FloatToUInt32(scale);
        fields[UnitFieldHealth] = health;
        fields[UnitFieldPower1] = mana;
        fields[UnitFieldMaxHealth] = health;
        fields[UnitFieldMaxPower1] = mana;
        fields[UnitFieldLevel] = level;
        fields[UnitFieldFactionTemplate] = factionTemplate;
        fields[UnitFieldBytes0] = BuildCreatureBytes0(template);
        fields[UnitFieldFlags] = template.UnitFlags;
        fields[UnitFieldBaseAttackTime] = template.MeleeBaseAttackTime == 0 ? 2000u : template.MeleeBaseAttackTime;
        fields[UnitFieldBaseAttackTime + 1] = template.MeleeBaseAttackTime == 0 ? 2000u : template.MeleeBaseAttackTime;
        fields[UnitFieldRangedAttackTime] = template.RangedBaseAttackTime == 0 ? 2000u : template.RangedBaseAttackTime;
        fields[UnitFieldBoundingRadius] = FloatToUInt32(0.389f);
        fields[UnitFieldCombatReach] = FloatToUInt32(1.5f);
        fields[UnitFieldDisplayId] = displayId;
        fields[UnitFieldNativeDisplayId] = displayId;
        fields[UnitFieldMinDamage] = FloatToUInt32(minMeleeDamage);
        fields[UnitFieldMaxDamage] = FloatToUInt32(maxMeleeDamage);
        fields[UnitFieldMinOffHandDamage] = FloatToUInt32(0.0f);
        fields[UnitFieldMaxOffHandDamage] = FloatToUInt32(0.0f);
        fields[UnitFieldBytes1] = 0;
        fields[UnitFieldDynamicFlags] = template.DynamicFlags;
        fields[UnitModCastSpeed] = FloatToUInt32(1.0f);
        fields[UnitNpcFlags] = template.NpcFlags;
        fields[UnitNpcEmoteState] = 0;
        fields[UnitFieldResistances] = template.Armor;
        fields[UnitFieldResistances + 1] = ToClientUInt32(template.ResistanceHoly);
        fields[UnitFieldResistances + 2] = ToClientUInt32(template.ResistanceFire);
        fields[UnitFieldResistances + 3] = ToClientUInt32(template.ResistanceNature);
        fields[UnitFieldResistances + 4] = ToClientUInt32(template.ResistanceFrost);
        fields[UnitFieldResistances + 5] = ToClientUInt32(template.ResistanceShadow);
        fields[UnitFieldResistances + 6] = ToClientUInt32(template.ResistanceArcane);
        fields[UnitFieldBaseMana] = mana;
        fields[UnitFieldBaseHealth] = health;
        fields[UnitFieldBytes2] = 0;
        fields[UnitFieldAttackPower] = template.MeleeAttackPower;
        fields[UnitFieldAttackPowerMods] = 0;
        fields[UnitFieldAttackPowerMultiplier] = FloatToUInt32(0.0f);
        fields[UnitFieldRangedAttackPower] = template.RangedAttackPower;
        fields[UnitFieldRangedAttackPowerMods] = 0;
        fields[UnitFieldRangedAttackPowerMultiplier] = FloatToUInt32(0.0f);

        WriteUpdateMask(writer, fields);
    }

    // Method: BuildCreatureBytes0
    // Purpose: Builds or writes build creature bytes0 output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - template: Template value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static uint BuildCreatureBytes0(CreatureTemplateRecord template)
    {
        byte unitClass = template.GetEffectiveUnitClass();
        return ((uint)unitClass) << 8;
    }

    // Method: WriteGameObjectCreateUpdateBlock
    // Purpose: Builds or writes write game object create update block output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - writer: Writer value supplied by the caller for this operation.
    // - spawn: Spawn value supplied by the caller for this operation.
    // - template: Template value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static void WriteGameObjectCreateUpdateBlock(
        WorldPacketWriter writer,
        GameObjectSpawnRecord spawn,
        GameObjectTemplateRecord template)
    {
        ulong clientGuid = CharacterGuid.ToGameObjectGuid(spawn.Guid, spawn.Entry);

        writer.WriteUInt8(3);
        WritePackedGuid(writer, clientGuid);
        writer.WriteUInt8(5);
        WriteGameObjectMovementBlock(writer, spawn);
        WriteGameObjectCreateUpdateMask(writer, spawn, template, clientGuid);
    }

    // Method: WriteGameObjectMovementBlock
    // Purpose: Builds or writes write game object movement block output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - writer: Writer value supplied by the caller for this operation.
    // - spawn: Spawn value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static void WriteGameObjectMovementBlock(WorldPacketWriter writer, GameObjectSpawnRecord spawn)
    {
        const VanillaUpdateFlags updateFlags = VanillaUpdateFlags.All | VanillaUpdateFlags.HasPosition;

        writer.WriteUInt8((byte)updateFlags);
        writer.WriteFloat(spawn.PositionX);
        writer.WriteFloat(spawn.PositionY);
        writer.WriteFloat(spawn.PositionZ);
        writer.WriteFloat(spawn.Orientation);
        writer.WriteUInt32(1);
    }

    // Method: WriteGameObjectCreateUpdateMask
    // Purpose: Builds or writes write game object create update mask output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - writer: Writer value supplied by the caller for this operation.
    // - spawn: Spawn value supplied by the caller for this operation.
    // - template: Template value supplied by the caller for this operation.
    // - clientGuid: Client GUID identifier used to select the exact record, object, or runtime owner.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static void WriteGameObjectCreateUpdateMask(
        WorldPacketWriter writer,
        GameObjectSpawnRecord spawn,
        GameObjectTemplateRecord template,
        ulong clientGuid)
    {
        const int ObjectFieldGuid = 0x0000;
        const int ObjectFieldType = 0x0002;
        const int ObjectFieldEntry = 0x0003;
        const int ObjectFieldScaleX = 0x0004;
        const int GameObjectFieldCreatedBy = 0x0006;
        const int GameObjectDisplayId = 0x0008;
        const int GameObjectFlags = 0x0009;
        const int GameObjectRotation = 0x000A;
        const int GameObjectState = 0x000E;
        const int GameObjectPositionX = 0x000F;
        const int GameObjectPositionY = 0x0010;
        const int GameObjectPositionZ = 0x0011;
        const int GameObjectFacing = 0x0012;
        const int GameObjectDynamicFlags = 0x0013;
        const int GameObjectFaction = 0x0014;
        const int GameObjectTypeId = 0x0015;
        const int GameObjectLevel = 0x0016;
        const int GameObjectArtKit = 0x0017;
        const int GameObjectAnimProgress = 0x0018;

        Dictionary<int, uint> fields = [];
        float scale = float.IsFinite(template.Size) && template.Size > 0.0f && template.Size <= 100.0f
            ? template.Size
            : 1.0f;

        WriteGuidFields(fields, ObjectFieldGuid, clientGuid);
        fields[ObjectFieldType] = 0x21;
        fields[ObjectFieldEntry] = spawn.Entry;
        fields[ObjectFieldScaleX] = FloatToUInt32(scale);
        WriteGuidFields(fields, GameObjectFieldCreatedBy, 0);
        fields[GameObjectDisplayId] = template.DisplayId;
        fields[GameObjectFlags] = template.Flags;
        fields[GameObjectRotation] = FloatToUInt32(NormalizeFiniteFloat(spawn.Rotation0));
        fields[GameObjectRotation + 1] = FloatToUInt32(NormalizeFiniteFloat(spawn.Rotation1));
        fields[GameObjectRotation + 2] = FloatToUInt32(NormalizeFiniteFloat(spawn.Rotation2));
        fields[GameObjectRotation + 3] = FloatToUInt32(NormalizeFiniteFloat(spawn.Rotation3));
        fields[GameObjectState] = spawn.State;
        fields[GameObjectPositionX] = FloatToUInt32(spawn.PositionX);
        fields[GameObjectPositionY] = FloatToUInt32(spawn.PositionY);
        fields[GameObjectPositionZ] = FloatToUInt32(spawn.PositionZ);
        fields[GameObjectFacing] = FloatToUInt32(spawn.Orientation);
        fields[GameObjectDynamicFlags] = ResolveGameObjectDynamicFlags(template.Type);
        fields[GameObjectFaction] = template.Faction;
        fields[GameObjectTypeId] = template.Type;
        fields[GameObjectLevel] = 1;
        fields[GameObjectArtKit] = 0;
        fields[GameObjectAnimProgress] = spawn.AnimProgress;

        WriteUpdateMask(writer, fields);
    }

    // Method: ResolveGameObjectDynamicFlags
    // Purpose: Retrieves resolve game object dynamic flags data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - gameObjectType: Game object type value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static uint ResolveGameObjectDynamicFlags(byte gameObjectType)
    {
        const uint GoDynamicFlagActivate = 0x00000001;
        const uint GoDynamicFlagSparkle = 0x00000002;

        return gameObjectType switch
        {
            2 => GoDynamicFlagActivate,
            3 or 5 or 8 or 10 => GoDynamicFlagActivate | GoDynamicFlagSparkle,
            _ => 0u,
        };
    }

    // Method: WritePlayerMovementBlock
    // Purpose: Builds or writes write player movement block output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - writer: Writer value supplied by the caller for this operation.
    // - player: Player value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static void WritePlayerMovementBlock(WorldPacketWriter writer, PlayerLoginRecord player, bool isSelf)
    {
        VanillaUpdateFlags updateFlags = VanillaUpdateFlags.All | VanillaUpdateFlags.Living;
        if (isSelf)
        {
            updateFlags |= VanillaUpdateFlags.Self;
        }

        writer.WriteUInt8((byte)updateFlags);
        WritePlayerLivingMovementInfo(writer, player);
        WritePlayerMovementSpeeds(writer);

        writer.WriteUInt32(1);
    }

    // Method: WritePlayerLivingMovementInfo
    // Purpose: Builds or writes write player living movement info output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - writer: Writer value supplied by the caller for this operation.
    // - player: Player value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static void WritePlayerLivingMovementInfo(WorldPacketWriter writer, PlayerLoginRecord player)
    {
        writer.WriteUInt32(0);
        writer.WriteUInt32(unchecked((uint)Environment.TickCount));
        writer.WriteFloat(player.PositionX);
        writer.WriteFloat(player.PositionY);
        writer.WriteFloat(player.PositionZ);
        writer.WriteFloat(player.Orientation);
        writer.WriteUInt32(0);
    }

    // Method: WritePlayerMovementSpeeds
    // Purpose: Builds or writes write player movement speeds output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - writer: Writer value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static void WritePlayerMovementSpeeds(WorldPacketWriter writer)
    {
        writer.WriteFloat(PlayerWalkSpeed);
        writer.WriteFloat(PlayerRunSpeed);
        writer.WriteFloat(PlayerRunBackSpeed);
        writer.WriteFloat(PlayerSwimSpeed);
        writer.WriteFloat(PlayerSwimBackSpeed);
        writer.WriteFloat(PlayerTurnRate);
    }

    // Method: WritePlayerCreateUpdateMask
    // Purpose: Builds or writes write player create update mask output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - writer: Writer value supplied by the caller for this operation.
    // - player: Player value supplied by the caller for this operation.
    // - inventory: Inventory value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static void WritePlayerCreateUpdateMask(
        WorldPacketWriter writer,
        PlayerLoginRecord player,
        IReadOnlyList<PlayerInventoryItem> inventory,
        bool includeInventorySlotGuids,
        bool includePrivatePlayerFields)
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
        const int PlayerFieldInvSlotHead = 0x01E6;
        const int PlayerFieldPackSlot1 = 0x0214;
        const int PlayerFieldBankSlot1 = 0x0234;
        const int PlayerFieldBankBagSlot1 = 0x0264;
        const int PlayerFieldKeyringSlot1 = 0x0288;
        const int PlayerXp = 0x02CC;
        const int PlayerNextLevelXp = 0x02CD;
        const int PlayerSkillInfo1_1 = 0x02CE;
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
        fields[ObjectFieldType] = 0x19;
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

        foreach (PlayerInventoryItem item in inventory)
        {
            if (item.BagGuid != 0)
            {
                continue;
            }

            if (includeInventorySlotGuids && TryResolvePlayerInventoryGuidField(
                item.Slot,
                PlayerFieldInvSlotHead,
                PlayerFieldPackSlot1,
                PlayerFieldBankSlot1,
                PlayerFieldBankBagSlot1,
                PlayerFieldKeyringSlot1,
                out int inventoryField))
            {
                WriteGuidFields(fields, inventoryField, CharacterGuid.ToItemGuid(item.ItemGuid));
            }

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
        }

        if (includePrivatePlayerFields)
        {
            fields[PlayerXp] = player.Experience;
            fields[PlayerNextLevelXp] = player.NextLevelExperience == 0 ? BuildNextLevelExperience(player.Level) : player.NextLevelExperience;
            WritePlayerSkillFields(fields, PlayerSkillInfo1_1, player);
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
        }

        WriteUpdateMask(writer, fields);
    }

    // Constant: Defines the player skill info field count constant used by the world server gameplay, session, and character runtime layer.
    // Value: fixed player skill info field count value used anywhere this rule or protocol value is needed.
    private const int PlayerSkillInfoFieldCount = 128;

    // Method: WritePlayerSkillFields
    // Purpose: Builds or writes write player skill fields output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - fields: Fields value supplied by the caller for this operation.
    // - firstSkillField: First skill field value supplied by the caller for this operation.
    // - player: Player value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static void WritePlayerSkillFields(IDictionary<int, uint> fields, int firstSkillField, PlayerLoginRecord player)
    {
        IReadOnlyList<PlayerSkill> skills = LanguageKnowledgeSystem.EnsureInitialLanguageSkills(player.Race, player.Faction, player.Skills);
        int slot = 0;
        foreach (PlayerSkill skill in skills
            .Where(skill => skill.Skill != 0)
            .OrderBy(skill => skill.Skill))
        {
            if (slot >= PlayerSkillInfoFieldCount)
            {
                break;
            }

            int field = firstSkillField + (slot * 3);
            fields[field] = PackUInt16Pair(skill.Skill, 0);
            fields[field + 1] = PackUInt16Pair(skill.Value, skill.MaxValue == 0 ? skill.Value : skill.MaxValue);
            fields[field + 2] = 0;
            slot++;
        }
    }

    // Method: PackUInt16Pair
    // Purpose: Builds or writes pack U int16 pair output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - lowValue: Low value value supplied by the caller for this operation.
    // - highValue: High value value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static uint PackUInt16Pair(uint lowValue, uint highValue)
    {
        uint low = Math.Min(lowValue, ushort.MaxValue);
        uint high = Math.Min(highValue, ushort.MaxValue);
        return low | (high << 16);
    }

    // Method: TryResolvePlayerInventoryGuidField
    // Purpose: Attempts to retrieve or parse try resolve player inventory GUID field data without treating normal misses as failures.
    // Parameters:
    // - slot: Slot value supplied by the caller for this operation.
    // - playerFieldInvSlotHead: Player field inv slot head value supplied by the caller for this operation.
    // - playerFieldPackSlot1: Player field pack slot1 value supplied by the caller for this operation.
    // - playerFieldBankSlot1: Player field bank slot1 value supplied by the caller for this operation.
    // - playerFieldBankBagSlot1: Player field bank bag slot1 value supplied by the caller for this operation.
    // - playerFieldKeyringSlot1: Player field keyring slot1 value supplied by the caller for this operation.
    // - field: Field value supplied by the caller for this operation.
    // Returns: Returns true when try resolve player inventory GUID field succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static bool TryResolvePlayerInventoryGuidField(
        byte slot,
        int playerFieldInvSlotHead,
        int playerFieldPackSlot1,
        int playerFieldBankSlot1,
        int playerFieldBankBagSlot1,
        int playerFieldKeyringSlot1,
        out int field)
    {
        if (slot < InventorySlotBagEnd)
        {
            field = playerFieldInvSlotHead + (slot * 2);
            return true;
        }

        if (slot is >= InventorySlotItemStart and < InventorySlotItemEnd)
        {
            field = playerFieldPackSlot1 + ((slot - InventorySlotItemStart) * 2);
            return true;
        }

        if (slot is >= BankSlotItemStart and < BankSlotItemEnd)
        {
            field = playerFieldBankSlot1 + ((slot - BankSlotItemStart) * 2);
            return true;
        }

        if (slot is >= BankSlotBagStart and < BankSlotBagEnd)
        {
            field = playerFieldBankBagSlot1 + ((slot - BankSlotBagStart) * 2);
            return true;
        }

        if (slot is >= KeyringSlotStart and < KeyringSlotEnd)
        {
            field = playerFieldKeyringSlot1 + ((slot - KeyringSlotStart) * 2);
            return true;
        }

        field = 0;
        return false;
    }

    // Method: WriteItemCreateUpdateBlock
    // Purpose: Builds or writes write item create update block output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - writer: Writer value supplied by the caller for this operation.
    // - player: Player value supplied by the caller for this operation.
    // - item: Item value supplied by the caller for this operation.
    // - inventory: Inventory value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static void WriteItemCreateUpdateBlock(
        WorldPacketWriter writer,
        PlayerLoginRecord player,
        PlayerInventoryItem item,
        IReadOnlyList<PlayerInventoryItem> inventory)
    {
        writer.WriteUInt8(2);
        WritePackedGuid(writer, CharacterGuid.ToItemGuid(item.ItemGuid));
        writer.WriteUInt8(item.IsContainer ? (byte)2 : (byte)1);
        writer.WriteUInt8((byte)VanillaUpdateFlags.All);
        writer.WriteUInt32(1);
        WriteItemCreateUpdateMask(writer, player, item, inventory);
    }

    // Method: WriteItemCreateUpdateMask
    // Purpose: Builds or writes write item create update mask output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - writer: Writer value supplied by the caller for this operation.
    // - player: Player value supplied by the caller for this operation.
    // - item: Item value supplied by the caller for this operation.
    // - inventory: Inventory value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static void WriteItemCreateUpdateMask(
        WorldPacketWriter writer,
        PlayerLoginRecord player,
        PlayerInventoryItem item,
        IReadOnlyList<PlayerInventoryItem> inventory)
    {
        const int ObjectFieldGuid = 0x0000;
        const int ObjectFieldType = 0x0002;
        const int ObjectFieldEntry = 0x0003;
        const int ObjectFieldScaleX = 0x0004;
        const int ItemFieldOwner = 0x0006;
        const int ItemFieldContained = 0x0008;
        const int ItemFieldStackCount = 0x000E;
        const int ItemFieldDuration = 0x000F;
        const int ItemFieldFlags = 0x0015;
        const int ItemFieldRandomPropertiesId = 0x002C;
        const int ItemFieldDurability = 0x002E;
        const int ItemFieldMaxDurability = 0x002F;
        const int ContainerFieldNumSlots = 0x0030;
        const int ContainerFieldSlot1 = 0x0032;
        const int MaximumContainerSlots = 28;

        Dictionary<int, uint> fields = ReadItemInstanceFields(item.InstanceData);
        ulong itemClientGuid = CharacterGuid.ToItemGuid(item.ItemGuid);
        ulong ownerClientGuid = player.ClientGuid;
        ulong containedGuid = item.BagGuid == 0 ? ownerClientGuid : CharacterGuid.ToItemGuid(item.BagGuid);

        WriteGuidFields(fields, ObjectFieldGuid, itemClientGuid);
        fields[ObjectFieldType] = item.IsContainer ? 0x07u : 0x03u;
        fields[ObjectFieldEntry] = item.TemplateEntry;
        fields[ObjectFieldScaleX] = FloatToUInt32(1.0f);
        WriteGuidFields(fields, ItemFieldOwner, ownerClientGuid);
        WriteGuidFields(fields, ItemFieldContained, containedGuid);
        fields[ItemFieldStackCount] = fields.TryGetValue(ItemFieldStackCount, out uint stackCount) && stackCount != 0 ? stackCount : 1u;
        fields.TryAdd(ItemFieldDuration, 0);
        fields.TryAdd(ItemFieldFlags, 0);
        fields.TryAdd(ItemFieldRandomPropertiesId, 0);

        uint durability = fields.TryGetValue(ItemFieldDurability, out uint existingDurability)
            ? existingDurability
            : item.MaxDurability;
        uint maxDurability = fields.TryGetValue(ItemFieldMaxDurability, out uint existingMaxDurability)
            ? existingMaxDurability
            : item.MaxDurability;
        if (maxDurability == 0 && durability != 0)
        {
            maxDurability = durability;
        }

        fields[ItemFieldDurability] = durability;
        fields[ItemFieldMaxDurability] = maxDurability;

        if (item.IsContainer)
        {
            byte containerSlots = (byte)Math.Min((int)item.ContainerSlots, MaximumContainerSlots);
            fields[ContainerFieldNumSlots] = containerSlots;

            foreach (PlayerInventoryItem child in inventory)
            {
                if (child.BagGuid != item.ItemGuid || child.Slot >= containerSlots)
                {
                    continue;
                }

                WriteGuidFields(fields, ContainerFieldSlot1 + (child.Slot * 2), CharacterGuid.ToItemGuid(child.ItemGuid));
            }
        }

        WriteUpdateMask(writer, fields);
    }

    // Method: ReadItemInstanceFields
    // Purpose: Retrieves read item instance fields data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - instanceData: Instance data value supplied by the caller for this operation.
    // Returns: Returns the dictionary value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static Dictionary<int, uint> ReadItemInstanceFields(string instanceData)
    {
        Dictionary<int, uint> fields = [];
        if (string.IsNullOrWhiteSpace(instanceData))
        {
            return fields;
        }

        string[] parts = instanceData.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int count = Math.Min(parts.Length, 106);
        for (int index = 0; index < count; index++)
        {
            if (uint.TryParse(parts[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint value) && value != 0)
            {
                fields[index] = value;
            }
        }

        return fields;
    }

    // Method: WriteGuidFields
    // Purpose: Builds or writes write GUID fields output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - fields: Fields value supplied by the caller for this operation.
    // - fieldIndex: Field index value supplied by the caller for this operation.
    // - guid: Guid identifier used to select the exact record, object, or runtime owner.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static void WriteGuidFields(IDictionary<int, uint> fields, int fieldIndex, ulong guid)
    {
        fields[fieldIndex] = (uint)(guid & uint.MaxValue);
        fields[fieldIndex + 1] = (uint)(guid >> 32);
    }

    // Method: WriteUpdateMask
    // Purpose: Builds or writes write update mask output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - writer: Writer value supplied by the caller for this operation.
    // - fields: Fields value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: BuildShowBank
    // Purpose: Builds or writes build show bank output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - bankerGuid: Banker GUID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildShowBank(ulong bankerGuid)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt64(bankerGuid);
        return writer.ToArray();
    }

    // Method: BuildInventoryStateUpdate
    // Purpose: Builds or writes build inventory state update output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - createdItemGuids: Created item guids value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildInventoryStateUpdate(PlayerLoginRecord player, IReadOnlySet<uint>? createdItemGuids = null)
    {
        ArgumentNullException.ThrowIfNull(player);

        PlayerInventoryItem[] inventoryItems = player.Inventory
            .Where(item => item.ItemGuid != 0 && item.TemplateEntry != 0)
            .OrderBy(item => item.BagGuid == 0 ? 0 : 1)
            .ThenBy(item => item.BagGuid)
            .ThenBy(item => item.Slot)
            .ThenBy(item => item.ItemGuid)
            .ToArray();

        WorldPacketWriter writer = new();
        writer.WriteUInt32((uint)(inventoryItems.Length + 1));
        writer.WriteUInt8(0);

        if (createdItemGuids is not null)
        {
            foreach (PlayerInventoryItem item in inventoryItems)
            {
                if (createdItemGuids.Contains(item.ItemGuid))
                {
                    WriteItemCreateUpdateBlock(writer, player, item, inventoryItems);
                }
            }
        }

        WriteValuesUpdateBlock(writer, player.ClientGuid, BuildPlayerInventoryValueFields(player, inventoryItems));

        foreach (PlayerInventoryItem item in inventoryItems)
        {
            if (createdItemGuids is not null && createdItemGuids.Contains(item.ItemGuid))
            {
                continue;
            }

            WriteValuesUpdateBlock(writer, CharacterGuid.ToItemGuid(item.ItemGuid), BuildItemInventoryValueFields(player, item, inventoryItems));
        }

        return writer.ToArray();
    }

    // Method: BuildInventoryChangeFailure
    // Purpose: Builds or writes build inventory change failure output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - failureCode: Failure code value supplied by the caller for this operation.
    // - itemGuid: Item GUID identifier used to select the exact record, object, or runtime owner.
    // - itemGuid2: Item guid2 value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildInventoryChangeFailure(byte failureCode, ulong itemGuid = 0, ulong itemGuid2 = 0)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt8(failureCode);
        writer.WriteUInt64(itemGuid);
        writer.WriteUInt64(itemGuid2);
        writer.WriteUInt8(0);
        return writer.ToArray();
    }

    // Method: BuildPlayerInventoryValueFields
    // Purpose: Builds or writes build player inventory value fields output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - inventory: Inventory value supplied by the caller for this operation.
    // Returns: Returns the dictionary value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static Dictionary<int, uint> BuildPlayerInventoryValueFields(PlayerLoginRecord player, IReadOnlyList<PlayerInventoryItem> inventory)
    {
        const int PlayerVisibleItem1Item0 = 0x0104;
        const int PlayerVisibleItemFieldCount = 12;
        const int PlayerFieldInvSlotHead = 0x01E6;
        const int PlayerFieldPackSlot1 = 0x0214;
        const int PlayerFieldBankSlot1 = 0x0234;
        const int PlayerFieldBankBagSlot1 = 0x0264;
        const int PlayerFieldKeyringSlot1 = 0x0288;

        Dictionary<int, uint> fields = [];

        for (byte slot = 0; slot < InventorySlotBagEnd; slot++)
        {
            if (TryResolvePlayerInventoryGuidField(slot, PlayerFieldInvSlotHead, PlayerFieldPackSlot1, PlayerFieldBankSlot1, PlayerFieldBankBagSlot1, PlayerFieldKeyringSlot1, out int field))
            {
                WriteGuidFields(fields, field, 0);
            }
        }

        for (byte slot = InventorySlotItemStart; slot < InventorySlotItemEnd; slot++)
        {
            if (TryResolvePlayerInventoryGuidField(slot, PlayerFieldInvSlotHead, PlayerFieldPackSlot1, PlayerFieldBankSlot1, PlayerFieldBankBagSlot1, PlayerFieldKeyringSlot1, out int field))
            {
                WriteGuidFields(fields, field, 0);
            }
        }

        for (byte slot = BankSlotItemStart; slot < BankSlotItemEnd; slot++)
        {
            if (TryResolvePlayerInventoryGuidField(slot, PlayerFieldInvSlotHead, PlayerFieldPackSlot1, PlayerFieldBankSlot1, PlayerFieldBankBagSlot1, PlayerFieldKeyringSlot1, out int field))
            {
                WriteGuidFields(fields, field, 0);
            }
        }

        for (byte slot = BankSlotBagStart; slot < BankSlotBagEnd; slot++)
        {
            if (TryResolvePlayerInventoryGuidField(slot, PlayerFieldInvSlotHead, PlayerFieldPackSlot1, PlayerFieldBankSlot1, PlayerFieldBankBagSlot1, PlayerFieldKeyringSlot1, out int field))
            {
                WriteGuidFields(fields, field, 0);
            }
        }

        for (byte slot = KeyringSlotStart; slot < KeyringSlotEnd; slot++)
        {
            if (TryResolvePlayerInventoryGuidField(slot, PlayerFieldInvSlotHead, PlayerFieldPackSlot1, PlayerFieldBankSlot1, PlayerFieldBankBagSlot1, PlayerFieldKeyringSlot1, out int field))
            {
                WriteGuidFields(fields, field, 0);
            }
        }

        for (int slot = 0; slot < CharacterEquipmentSlotCount; slot++)
        {
            int visibleItemBase = PlayerVisibleItem1Item0 + (slot * PlayerVisibleItemFieldCount);
            for (int offset = 0; offset < PlayerVisibleItemFieldCount; offset++)
            {
                fields[visibleItemBase + offset] = 0;
            }
        }

        foreach (PlayerInventoryItem item in inventory)
        {
            if (item.BagGuid != 0)
            {
                continue;
            }

            if (TryResolvePlayerInventoryGuidField(
                item.Slot,
                PlayerFieldInvSlotHead,
                PlayerFieldPackSlot1,
                PlayerFieldBankSlot1,
                PlayerFieldBankBagSlot1,
                PlayerFieldKeyringSlot1,
                out int inventoryField))
            {
                WriteGuidFields(fields, inventoryField, CharacterGuid.ToItemGuid(item.ItemGuid));
            }

            if (item.Slot >= CharacterEquipmentSlotCount)
            {
                continue;
            }

            int visibleItemBase = PlayerVisibleItem1Item0 + (item.Slot * PlayerVisibleItemFieldCount);
            fields[visibleItemBase] = item.TemplateEntry;
            fields[visibleItemBase + 1] = item.EnchantmentId;
        }

        return fields;
    }

    // Method: BuildItemInventoryValueFields
    // Purpose: Builds or writes build item inventory value fields output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // - item: Item value supplied by the caller for this operation.
    // - inventory: Inventory value supplied by the caller for this operation.
    // Returns: Returns the dictionary value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static Dictionary<int, uint> BuildItemInventoryValueFields(PlayerLoginRecord player, PlayerInventoryItem item, IReadOnlyList<PlayerInventoryItem> inventory)
    {
        const int ItemFieldOwner = 0x0006;
        const int ItemFieldContained = 0x0008;
        const int ItemFieldStackCount = 0x000E;
        const int ContainerFieldNumSlots = 0x0030;
        const int ContainerFieldSlot1 = 0x0032;
        const int MaximumContainerSlots = 28;

        Dictionary<int, uint> fields = [];
        ulong ownerClientGuid = player.ClientGuid;
        ulong containedGuid = item.BagGuid == 0 ? ownerClientGuid : CharacterGuid.ToItemGuid(item.BagGuid);

        WriteGuidFields(fields, ItemFieldOwner, ownerClientGuid);
        WriteGuidFields(fields, ItemFieldContained, containedGuid);
        fields[ItemFieldStackCount] = Math.Max(item.StackCount, 1u);

        if (item.IsContainer)
        {
            byte containerSlots = (byte)Math.Min((int)item.ContainerSlots, MaximumContainerSlots);
            fields[ContainerFieldNumSlots] = containerSlots;

            for (int slot = 0; slot < containerSlots; slot++)
            {
                WriteGuidFields(fields, ContainerFieldSlot1 + (slot * 2), 0);
            }

            foreach (PlayerInventoryItem child in inventory)
            {
                if (child.BagGuid != item.ItemGuid || child.Slot >= containerSlots)
                {
                    continue;
                }

                WriteGuidFields(fields, ContainerFieldSlot1 + (child.Slot * 2), CharacterGuid.ToItemGuid(child.ItemGuid));
            }
        }

        return fields;
    }

    // Method: WriteValuesUpdateBlock
    // Purpose: Builds or writes write values update block output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - writer: Writer value supplied by the caller for this operation.
    // - guid: Guid identifier used to select the exact record, object, or runtime owner.
    // - fields: Fields value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static void WriteValuesUpdateBlock(WorldPacketWriter writer, ulong guid, IReadOnlyDictionary<int, uint> fields)
    {
        writer.WriteUInt8(0);
        WritePackedGuid(writer, guid);
        WriteUpdateMask(writer, fields);
    }

    // Method: BuildMovementBroadcast
    // Purpose: Builds or writes build movement broadcast output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - clientGuid: Client GUID identifier used to select the exact record, object, or runtime owner.
    // - clientMovementPayload: Client movement payload value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildMovementBroadcast(ulong clientGuid, ReadOnlySpan<byte> clientMovementPayload)
    {
        WorldPacketWriter writer = new();
        WritePackedGuid(writer, clientGuid);
        writer.WriteBytes(clientMovementPayload);
        return writer.ToArray();
    }

    // Method: WritePackedGuid
    // Purpose: Builds or writes write packed GUID output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - writer: Writer value supplied by the caller for this operation.
    // - guid: Guid identifier used to select the exact record, object, or runtime owner.
    // Returns: none.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: BuildUnitBytes0
    // Purpose: Builds or writes build unit bytes0 output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - race: Race value supplied by the caller for this operation.
    // - playerClass: Player class value supplied by the caller for this operation.
    // - gender: Gender value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static uint BuildUnitBytes0(byte race, byte playerClass, byte gender)
    {
        return race | ((uint)playerClass << 8) | ((uint)gender << 16) | ((uint)ResolvePowerType(playerClass) << 24);
    }

    // Method: ResolvePowerType
    // Purpose: Retrieves resolve power type data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - playerClass: Player class value supplied by the caller for this operation.
    // Returns: Returns the byte value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static byte ResolvePowerType(byte playerClass)
    {
        return playerClass switch
        {
            1 => 1,
            4 => 3,
            _ => 0,
        };
    }

    // Method: ResolveFactionTemplateId
    // Purpose: Retrieves resolve faction template ID data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - race: Race value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ResolvePlayerDisplayId
    // Purpose: Retrieves resolve player display ID data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - race: Race value supplied by the caller for this operation.
    // - gender: Gender value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: NormalizeFiniteFloat
    // Purpose: Converts incoming data into normalize finite float form for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the float value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static float NormalizeFiniteFloat(float value)
    {
        return float.IsFinite(value) ? value : 0.0f;
    }

    // Method: SanitizeClientCacheString
    // Purpose: Executes the sanitize client cache string operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static string SanitizeClientCacheString(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    // Method: FloatToUInt32
    // Purpose: Executes the float to U int32 operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static uint FloatToUInt32(float value)
    {
        return BitConverter.SingleToUInt32Bits(value);
    }

    // Method: ToClientUInt32
    // Purpose: Executes the to client U int32 operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static uint ToClientUInt32(int value)
    {
        return unchecked((uint)value);
    }

    // Method: ToClientSpellCharges
    // Purpose: Executes the to client spell charges operation for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - charges: Charges value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static uint ToClientSpellCharges(int charges)
    {
        if (charges == 0)
        {
            return 0;
        }

        long absoluteCharges = charges;
        if (absoluteCharges < 0)
        {
            absoluteCharges = -absoluteCharges;
        }

        return unchecked((uint)-absoluteCharges);
    }

    // Method: BuildNextLevelExperience
    // Purpose: Builds or writes build next level experience output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - level: Level value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static uint BuildNextLevelExperience(byte level)
    {
        return ExperienceFormula.GetFallbackNextLevelExperience(level);
    }

    // Method: BuildLoginSetTimeSpeed
    // Purpose: Builds or writes build login set time speed output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - localTime: Local time value supplied by the caller for this operation.
    // - gameSpeed: Game speed value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildLoginSetTimeSpeed(DateTimeOffset localTime, float gameSpeed = 0.01666667f)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(EncodePackedGameTime(localTime));
        writer.WriteFloat(gameSpeed);
        return writer.ToArray();
    }

    // Method: BuildMessageOfTheDay
    // Purpose: Builds or writes build message of the day output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - message: Message value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: BuildInitialSpells
    // Purpose: Builds or writes build initial spells output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildInitialSpells(PlayerLoginRecord player)
    {
        ArgumentNullException.ThrowIfNull(player);

        ushort[] spellIds = GetLoginSpellIds(player).ToArray();
        WorldPacketWriter writer = new();
        writer.WriteUInt8(0);
        writer.WriteUInt16((ushort)spellIds.Length);
        foreach (ushort spellId in spellIds)
        {
            writer.WriteUInt16(spellId);
            writer.WriteUInt16(0);
        }

        writer.WriteUInt16(0);
        return writer.ToArray();
    }

    // Method: BuildActionButtons
    // Purpose: Builds or writes build action buttons output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: GetLoginSpellIds
    // Purpose: Retrieves get login spell ids data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // Returns: Returns the I enumerable value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
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

            spellIds.Add(81);
            spellIds.Add(203);
            spellIds.Add(204);
            spellIds.Add(522);
            spellIds.Add(6603);
        }

        return spellIds;
    }

    // Method: GetInitialSpellIds
    // Purpose: Retrieves get initial spell ids data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // Returns: Returns the I enumerable value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static IEnumerable<ushort> GetInitialSpellIds(PlayerLoginRecord player)
    {
        SortedSet<ushort> spells =
        [
            81,
            203,
            204,
            522,
            6603,
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

    // Method: GetLanguageSpellIds
    // Purpose: Retrieves get language spell ids data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - race: Race value supplied by the caller for this operation.
    // - faction: Faction value supplied by the caller for this operation.
    // Returns: Returns the I enumerable value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static IEnumerable<ushort> GetLanguageSpellIds(byte race, PlayerFaction faction)
    {
        foreach (uint spellId in LanguageKnowledgeSystem.BuildInitialLanguageSpellIds(race, faction))
        {
            if (spellId <= ushort.MaxValue)
            {
                yield return (ushort)spellId;
            }
        }
    }

    // Method: GetStarterActionButtonSpellIds
    // Purpose: Retrieves get starter action button spell ids data for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - playerClass: Player class value supplied by the caller for this operation.
    // Returns: Returns the I enumerable value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    private static IEnumerable<ushort> GetStarterActionButtonSpellIds(byte playerClass)
    {
        return playerClass switch
        {
            1 => new ushort[] { 78, 2457 },
            2 => new ushort[] { 635, 21084 },
            3 => new ushort[] { 75, 2973 },
            4 => new ushort[] { 1752 },
            5 => new ushort[] { 585, 2050 },
            7 => new ushort[] { 403, 331 },
            8 => new ushort[] { 133, 168 },
            9 => new ushort[] { 686, 687 },
            11 => new ushort[] { 5176, 5185 },
            _ => Array.Empty<ushort>(),
        };
    }

    // Method: BuildInitializeFactions
    // Purpose: Builds or writes build initialize factions output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildInitializeFactions(PlayerLoginRecord player)
    {
        ArgumentNullException.ThrowIfNull(player);

        Dictionary<int, PlayerReputation> reputationsByListId = player.Reputations
            .Where(reputation => reputation.ReputationListId is >= 0 and < ReputationSystem.MaxReputationSlots)
            .GroupBy(reputation => reputation.ReputationListId)
            .ToDictionary(group => group.Key, group => group.First());

        WorldPacketWriter writer = new();
        writer.WriteUInt32(ReputationSystem.MaxReputationSlots);
        for (int index = 0; index < ReputationSystem.MaxReputationSlots; index++)
        {
            if (reputationsByListId.TryGetValue(index, out PlayerReputation? reputation))
            {
                writer.WriteUInt8((byte)(reputation.Flags & 0xFF));
                writer.WriteUInt32(unchecked((uint)reputation.Standing));
                continue;
            }

            writer.WriteUInt8(0);
            writer.WriteUInt32(0);
        }

        return writer.ToArray();
    }

    // Method: BuildBindPointUpdate
    // Purpose: Builds or writes build bind point update output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: BuildSetRestStart
    // Purpose: Builds or writes build set rest start output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - localTime: Local time value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildSetRestStart(DateTimeOffset localTime)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32((uint)localTime.ToUnixTimeSeconds());
        return writer.ToArray();
    }

    // Method: BuildItemQuerySingleResponse
    // Purpose: Builds or writes build item query single response output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - itemTemplate: Item template value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildItemQuerySingleResponse(ItemTemplateRecord itemTemplate)
    {
        ArgumentNullException.ThrowIfNull(itemTemplate);

        WorldPacketWriter writer = new();
        writer.WriteUInt32(itemTemplate.Entry);
        writer.WriteUInt32(itemTemplate.Class);
        writer.WriteUInt32(itemTemplate.Class == 0 ? 0u : itemTemplate.SubClass);
        writer.WriteCString(itemTemplate.Name);
        writer.WriteCString(string.Empty);
        writer.WriteCString(string.Empty);
        writer.WriteCString(string.Empty);
        writer.WriteUInt32(itemTemplate.DisplayId);
        writer.WriteUInt32(itemTemplate.Quality);
        writer.WriteUInt32(itemTemplate.Flags);

        writer.WriteUInt32(itemTemplate.BuyPrice);
        writer.WriteUInt32(itemTemplate.SellPrice);
        writer.WriteUInt32(itemTemplate.InventoryType);
        writer.WriteUInt32(ToClientUInt32(itemTemplate.AllowableClass));
        writer.WriteUInt32(ToClientUInt32(itemTemplate.AllowableRace));
        writer.WriteUInt32(itemTemplate.ItemLevel);
        writer.WriteUInt32(itemTemplate.RequiredLevel);
        writer.WriteUInt32(itemTemplate.RequiredSkill);
        writer.WriteUInt32(itemTemplate.RequiredSkillRank);
        writer.WriteUInt32(itemTemplate.RequiredSpell);
        writer.WriteUInt32(itemTemplate.RequiredHonorRank);
        writer.WriteUInt32(itemTemplate.RequiredCityRank);
        writer.WriteUInt32(itemTemplate.RequiredReputationFaction);
        writer.WriteUInt32(itemTemplate.RequiredReputationFaction > 0 ? itemTemplate.RequiredReputationRank : 0u);
        writer.WriteUInt32(itemTemplate.MaxCount);
        writer.WriteUInt32(itemTemplate.Stackable);
        writer.WriteUInt32(itemTemplate.ContainerSlots);

        for (int index = 0; index < 10; index++)
        {
            ItemTemplateStatRecord stat = itemTemplate.Stats[index];
            writer.WriteUInt32(stat.Type);
            writer.WriteUInt32(ToClientUInt32(stat.Value));
        }

        for (int index = 0; index < 5; index++)
        {
            ItemTemplateDamageRecord damage = itemTemplate.Damages[index];
            writer.WriteFloat(damage.Minimum);
            writer.WriteFloat(damage.Maximum);
            writer.WriteUInt32(damage.Type);
        }

        writer.WriteUInt32(itemTemplate.Armor);
        writer.WriteUInt32(itemTemplate.HolyResistance);
        writer.WriteUInt32(itemTemplate.FireResistance);
        writer.WriteUInt32(itemTemplate.NatureResistance);
        writer.WriteUInt32(itemTemplate.FrostResistance);
        writer.WriteUInt32(itemTemplate.ShadowResistance);
        writer.WriteUInt32(itemTemplate.ArcaneResistance);
        writer.WriteUInt32(itemTemplate.Delay);
        writer.WriteUInt32(itemTemplate.AmmoType);
        writer.WriteFloat(itemTemplate.RangedModRange);

        for (int index = 0; index < 5; index++)
        {
            ItemTemplateSpellRecord spell = itemTemplate.Spells[index];
            writer.WriteUInt32(spell.SpellId);
            writer.WriteUInt32(spell.Trigger);
            writer.WriteUInt32(ToClientSpellCharges(spell.Charges));
            writer.WriteUInt32(ToClientUInt32(spell.SpellId == 0 ? -1 : spell.Cooldown));
            writer.WriteUInt32(spell.SpellId == 0 ? 0u : spell.Category);
            writer.WriteUInt32(ToClientUInt32(spell.SpellId == 0 ? -1 : spell.CategoryCooldown));
        }

        writer.WriteUInt32(itemTemplate.Bonding);
        writer.WriteCString(itemTemplate.Description);
        writer.WriteUInt32(itemTemplate.PageText);
        writer.WriteUInt32(itemTemplate.LanguageId);
        writer.WriteUInt32(itemTemplate.PageMaterial);
        writer.WriteUInt32(itemTemplate.StartQuest);
        writer.WriteUInt32(itemTemplate.LockId);
        writer.WriteUInt32(ToClientUInt32(itemTemplate.Material));
        writer.WriteUInt32(itemTemplate.Sheath);
        writer.WriteUInt32(itemTemplate.RandomProperty);
        writer.WriteUInt32(itemTemplate.Block);
        writer.WriteUInt32(itemTemplate.ItemSet);
        writer.WriteUInt32(itemTemplate.MaxDurability);
        writer.WriteUInt32(itemTemplate.Area);
        writer.WriteUInt32(ToClientUInt32(itemTemplate.Map));
        writer.WriteUInt32(ToClientUInt32(itemTemplate.BagFamily));

        return writer.ToArray();
    }

    // Method: BuildItemQuerySingleNotFound
    // Purpose: Builds or writes build item query single not found output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - itemEntry: Item entry value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildItemQuerySingleNotFound(uint itemEntry)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(itemEntry | 0x80000000u);
        return writer.ToArray();
    }

    // Method: BuildChatMessage
    // Purpose: Builds or writes build chat message output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - messageType: Message type value supplied by the caller for this operation.
    // - language: Language value supplied by the caller for this operation.
    // - senderGuid: Sender GUID identifier used to select the exact record, object, or runtime owner.
    // - senderName: Sender name value supplied by the caller for this operation.
    // - text: Text value supplied by the caller for this operation.
    // - channelName: Channel name value supplied by the caller for this operation.
    // - chatTag: Chat tag value supplied by the caller for this operation.
    // - channelPlayerRank: Channel player rank value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: BuildNameQueryResponse
    // Purpose: Builds or writes build name query response output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - character: Character value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildNameQueryResponse(CharacterNameQueryResult character)
    {
        ArgumentNullException.ThrowIfNull(character);

        WorldPacketWriter writer = new();
        writer.WriteUInt64(CharacterGuid.ToClientGuid(character.Guid));
        writer.WriteCString(character.Name);
        writer.WriteCString(string.Empty);
        writer.WriteUInt32(character.Race);
        writer.WriteUInt32(character.Gender);
        writer.WriteUInt32(character.Class);
        return writer.ToArray();
    }

    // Method: BuildCreatureQueryResponse
    // Purpose: Builds or writes build creature query response output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - template: Template value supplied by the caller for this operation.
    // - spawn: Spawn value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildCreatureQueryResponse(CreatureTemplateRecord template, CreatureSpawnRecord? spawn = null)
    {
        ArgumentNullException.ThrowIfNull(template);

        WorldPacketWriter writer = new();
        uint displayId = spawn is null
            ? template.GetPreferredModelId()
            : CreatureDataValidation.ResolveDisplayModelId(spawn, template);

        writer.WriteUInt32(template.Entry);
        writer.WriteCString(SanitizeClientCacheString(template.Name));
        writer.WriteCString(string.Empty);
        writer.WriteCString(string.Empty);
        writer.WriteCString(string.Empty);
        writer.WriteCString(SanitizeClientCacheString(template.SubName));
        writer.WriteUInt32(template.CreatureTypeFlags);
        writer.WriteUInt32(template.CreatureType);
        writer.WriteUInt32(template.Family < 0 ? 0u : (uint)template.Family);
        writer.WriteUInt32(template.Rank);
        writer.WriteUInt32(0);
        writer.WriteUInt32(template.PetSpellDataId);
        writer.WriteUInt32(displayId);
        writer.WriteUInt8(template.Civilian);
        writer.WriteUInt8(template.RacialLeader);
        return writer.ToArray();
    }

    // Method: BuildCreatureQueryNotFound
    // Purpose: Builds or writes build creature query not found output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - entry: Entry value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildCreatureQueryNotFound(uint entry)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(entry | 0x80000000u);
        return writer.ToArray();
    }

    // Method: BuildGameObjectQueryResponse
    // Purpose: Builds or writes build game object query response output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - template: Template value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildGameObjectQueryResponse(GameObjectTemplateRecord template)
    {
        ArgumentNullException.ThrowIfNull(template);

        WorldPacketWriter writer = new();
        writer.WriteUInt32(template.Entry);
        writer.WriteUInt32(template.Type);
        writer.WriteUInt32(template.DisplayId);
        writer.WriteCString(SanitizeClientCacheString(template.Name));
        writer.WriteCString(string.Empty);
        writer.WriteCString(string.Empty);
        writer.WriteCString(string.Empty);
        writer.WriteCString(string.Empty);

        for (int index = 0; index < GameObjectTemplateRecord.DataFieldCount; index++)
        {
            writer.WriteUInt32(template.GetDataField(index));
        }

        return writer.ToArray();
    }

    // Method: BuildGameObjectQueryNotFound
    // Purpose: Builds or writes build game object query not found output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - entry: Entry value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildGameObjectQueryNotFound(uint entry)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(entry | 0x80000000u);
        return writer.ToArray();
    }

    // Method: BuildLogoutResponse
    // Purpose: Builds or writes build logout response output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - reason: Reason value supplied by the caller for this operation.
    // - instantLogout: Instant logout value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildLogoutResponse(uint reason = 0, bool instantLogout = true)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(reason);
        writer.WriteUInt8(instantLogout ? (byte)1 : (byte)0);
        return writer.ToArray();
    }

    // Method: BuildLogoutComplete
    // Purpose: Builds or writes build logout complete output for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildLogoutComplete()
    {
        return [];
    }

    // Method: BuildLogoutCancelAck
    // Purpose: Builds or writes build logout cancel ack output for the world server gameplay, session, and character runtime layer.
    // Parameters: none.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildLogoutCancelAck()
    {
        return [];
    }

    // Method: BuildServerTime
    // Purpose: Builds or writes build server time output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - localTime: Local time value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildServerTime(DateTimeOffset localTime)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(EncodePackedGameTime(localTime));
        return writer.ToArray();
    }

    // Method: BuildPlayedTime
    // Purpose: Builds or writes build played time output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - player: Player value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildPlayedTime(PlayerLoginRecord player)
    {
        ArgumentNullException.ThrowIfNull(player);

        WorldPacketWriter writer = new();
        writer.WriteUInt32(player.TotalTime);
        writer.WriteUInt32(player.LevelTime);
        return writer.ToArray();
    }

    // Method: BuildChannelNotify
    // Purpose: Builds or writes build channel notify output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - notificationType: Notification type value supplied by the caller for this operation.
    // - channelName: Channel name value supplied by the caller for this operation.
    // - channelFlags: Channel flags value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildChannelNotify(byte notificationType, string channelName, uint channelFlags = 0)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt8(notificationType);
        writer.WriteCString(channelName);

        if (notificationType == 0x02)
        {
            writer.WriteUInt32(channelFlags);
            writer.WriteUInt32(0);
            writer.WriteUInt8(0);
        }

        return writer.ToArray();
    }

    // Method: BuildChannelList
    // Purpose: Builds or writes build channel list output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - channelName: Channel name value supplied by the caller for this operation.
    // - members: Members value supplied by the caller for this operation.
    // - channelFlags: Channel flags value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
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
            writer.WriteUInt8(0);
        }

        return writer.ToArray();
    }

    // Method: BuildWhoResponse
    // Purpose: Builds or writes build who response output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - players: Players value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
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
            writer.WriteCString(string.Empty);
            writer.WriteUInt32(player.Level);
            writer.WriteUInt32(player.Class);
            writer.WriteUInt32(player.Race);
            writer.WriteUInt32(player.Zone);
        }

        return writer.ToArray();
    }

    // Method: BuildItemNameQueryResponse
    // Purpose: Builds or writes build item name query response output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - itemTemplate: Item template value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildItemNameQueryResponse(ItemTemplateRecord itemTemplate)
    {
        ArgumentNullException.ThrowIfNull(itemTemplate);

        WorldPacketWriter writer = new();
        writer.WriteUInt32(itemTemplate.Entry);
        writer.WriteCString(itemTemplate.Name);
        writer.WriteUInt32(itemTemplate.InventoryType);
        return writer.ToArray();
    }

    // Method: BuildItemNameQueryNotFound
    // Purpose: Builds or writes build item name query not found output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - itemEntry: Item entry value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildItemNameQueryNotFound(uint itemEntry)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(itemEntry | 0x80000000u);
        return writer.ToArray();
    }

    // Method: EncodePackedGameTime
    // Purpose: Builds or writes encode packed game time output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - localTime: Local time value supplied by the caller for this operation.
    // Returns: Returns the uint value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: BuildPong
    // Purpose: Builds or writes build pong output for the world server gameplay, session, and character runtime layer.
    // Parameters:
    // - sequence: Sequence value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to WorldPacketBuilders so callers do not duplicate validation, protocol, or persistence rules.
    public static byte[] BuildPong(uint sequence)
    {
        WorldPacketWriter writer = new();
        writer.WriteUInt32(sequence);
        return writer.ToArray();
    }
}
