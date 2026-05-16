
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
