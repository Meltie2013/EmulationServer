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

namespace EmulationServer.RealmServer.Auth;

public sealed class RealmListPacketBuilder
{
    private const byte OfflineFlag = 0x02;

    private readonly ConfiguredRealmStore _realmStore;

    public RealmListPacketBuilder(ConfiguredRealmStore realmStore)
    {
        _realmStore = realmStore ?? throw new ArgumentNullException(nameof(realmStore));
    }

    public byte[] BuildRealmList(ushort build, byte accountSecurityLevel)
    {
        ConfiguredRealm[] realms = _realmStore.GetRealmsForBuild(build)
            .Where(realm => accountSecurityLevel >= realm.AllowedSecurityLevel || accountSecurityLevel > 0)
            .ToArray();

        return RealmBuilds.UsesModernRealmList(build)
            ? BuildModernRealmList(realms, accountSecurityLevel)
            : BuildVanillaRealmList(realms, accountSecurityLevel);
    }

    private static byte[] BuildVanillaRealmList(ConfiguredRealm[] realms, byte accountSecurityLevel)
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
            body.WriteUInt8(0); // Character count will come from WorldServer later.
            body.WriteUInt8(realm.Timezone);
            body.WriteUInt8(0); // Unknown realm list value used by 1.x clients.
        }

        body.WriteUInt16(0x0002);

        return BuildRealmListPacket(body);
    }

    private static byte[] BuildModernRealmList(ConfiguredRealm[] realms, byte accountSecurityLevel)
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
            body.WriteUInt8(0); // Character count will come from WorldServer later.
            body.WriteUInt8(realm.Timezone);
            body.WriteUInt8(0); // Unknown realm list value.
        }

        body.WriteUInt16(0x0010);

        return BuildRealmListPacket(body);
    }

    private static byte GetRealmFlags(ConfiguredRealm realm, byte accountSecurityLevel)
    {
        byte realmFlags = realm.BaseRealmFlags;

        if (!realm.IsOnline || accountSecurityLevel < realm.AllowedSecurityLevel)
        {
            realmFlags |= OfflineFlag;
        }

        return realmFlags;
    }

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
