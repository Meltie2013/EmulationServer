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

using EmulationServer.RealmServer.Realms;

/**
  * File overview: src/RealmServer/Auth/RealmListPacketBuilder.cs
  * This file belongs to the realm authentication, build validation, and realm list packet creation portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.RealmServer.Auth;

/**
  * Represents the realm list packet builder component in the realm authentication, build validation, and realm list packet creation area.
  * It builds structured payloads while keeping binary or text protocol formatting out of the caller.
  */
public sealed class RealmListPacketBuilder
{
    private const byte OfflineFlag = 0x02;

    /**
      * Stores the realm store dependency or runtime value for RealmListPacketBuilder.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly ConfiguredRealmStore _realmStore;

    /**
      * Creates a new RealmListPacketBuilder instance and stores the dependencies required by the component.
      * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
      */
    public RealmListPacketBuilder(ConfiguredRealmStore realmStore)
    {
        _realmStore = realmStore ?? throw new ArgumentNullException(nameof(realmStore));
    }

    /**
      * Builds a protocol payload or domain model from validated input values.
      * The method is part of RealmListPacketBuilder and keeps this workflow isolated from the caller.
      */
    public async Task<byte[]> BuildRealmListAsync(
        ushort build,
        byte accountSecurityLevel,
        uint accountId,
        CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(BuildRealmList(build, accountSecurityLevel, accountId));
    }

    /**
      * Builds a protocol payload or domain model from validated input values.
      * The method is part of RealmListPacketBuilder and keeps this workflow isolated from the caller.
      */
    public byte[] BuildRealmList(ushort build, byte accountSecurityLevel, uint accountId)
    {
        ConfiguredRealm[] realms = _realmStore.GetRealmsForBuild(build)
            .Where(realm => accountSecurityLevel >= realm.AllowedSecurityLevel || accountSecurityLevel > 0)
            .ToArray();

        return RealmBuilds.UsesModernRealmList(build)
            ? BuildModernRealmList(realms, accountSecurityLevel, accountId)
            : BuildVanillaRealmList(realms, accountSecurityLevel, accountId);
    }

    /**
      * Builds a protocol payload or domain model from validated input values.
      * The method is part of RealmListPacketBuilder and keeps this workflow isolated from the caller.
      */
    private static byte[] BuildVanillaRealmList(ConfiguredRealm[] realms, byte accountSecurityLevel, uint accountId)
    {
        ByteWriter body = new();
        body.WriteUInt32(0);
        body.WriteUInt8((byte)Math.Min(byte.MaxValue, realms.Length));

        foreach (ConfiguredRealm realm in realms.Take(byte.MaxValue))
        {
            byte realmFlags = GetRealmFlags(realm, accountSecurityLevel);

            body.WriteUInt32(realm.Icon);
            body.WriteUInt8(realmFlags);
            body.WriteCString(realm.Name);
            body.WriteCString(realm.ClientAddress);
            body.WriteFloat(realm.Population);
            body.WriteUInt8(realm.GetCharacterCount(accountId));
            body.WriteUInt8(realm.Timezone);
            body.WriteUInt8(0); // Unknown realm list value used by 1.x clients.
        }

        body.WriteUInt16(0x0002);

        return BuildRealmListPacket(body);
    }

    /**
      * Builds a protocol payload or domain model from validated input values.
      * The method is part of RealmListPacketBuilder and keeps this workflow isolated from the caller.
      */
    private static byte[] BuildModernRealmList(ConfiguredRealm[] realms, byte accountSecurityLevel, uint accountId)
    {
        ByteWriter body = new();
        body.WriteUInt32(0);
        body.WriteUInt16((ushort)Math.Min(ushort.MaxValue, realms.Length));

        foreach (ConfiguredRealm realm in realms.Take(ushort.MaxValue))
        {
            byte locked = accountSecurityLevel < realm.AllowedSecurityLevel ? (byte)1 : (byte)0;
            byte realmFlags = GetRealmFlags(realm, accountSecurityLevel);

            body.WriteUInt8((byte)realm.Icon);
            body.WriteUInt8(locked);
            body.WriteUInt8(realmFlags);
            body.WriteCString(realm.Name);
            body.WriteCString(realm.ClientAddress);
            body.WriteFloat(realm.Population);
            body.WriteUInt8(realm.GetCharacterCount(accountId));
            body.WriteUInt8(realm.Timezone);
            body.WriteUInt8(0); // Unknown realm list value.
        }

        body.WriteUInt16(0x0010);

        return BuildRealmListPacket(body);
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of RealmListPacketBuilder and keeps this workflow isolated from the caller.
      */
    private static byte GetRealmFlags(ConfiguredRealm realm, byte accountSecurityLevel)
    {
        byte realmFlags = realm.BaseRealmFlags;

        if (!realm.IsOnline || accountSecurityLevel < realm.AllowedSecurityLevel)
        {
            realmFlags |= OfflineFlag;
        }

        return realmFlags;
    }

    /**
      * Builds a protocol payload or domain model from validated input values.
      * The method is part of RealmListPacketBuilder and keeps this workflow isolated from the caller.
      */
    private static byte[] BuildRealmListPacket(ByteWriter body)
    {
        byte[] bodyBytes = body.ToArray();

        ByteWriter packet = new();
        packet.WriteUInt8((byte)RealmAuthOpCode.RealmList);
        packet.WriteUInt16((ushort)bodyBytes.Length);
        packet.WriteBytes(bodyBytes);

        return packet.ToArray();
    }
}
