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
  * Documents the RealmListPacketBuilder source file in the realm authentication, realm-list handling, and external client login services area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.RealmServer.Auth;

/**
  * Owns the realm list packet builder behavior for the realm authentication, realm-list handling, and external client login services layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class RealmListPacketBuilder
{
    /**
      * Holds the private realm store state used by the owning component.
      * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
      */
    private readonly ConfiguredRealmStore _realmStore;

    /**
      * Initializes a new RealmListPacketBuilder instance with the dependencies required by the realm authentication, realm-list handling, and external client login services workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: realmStore.
      */
    public RealmListPacketBuilder(ConfiguredRealmStore realmStore)
    {
        _realmStore = realmStore ?? throw new ArgumentNullException();
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
            ? BuildModernRealmList(build, realms, accountSecurityLevel, accountId)
            : BuildVanillaRealmList(build, realms, accountSecurityLevel, accountId);
    }

    /**
      * Builds a protocol payload or domain model from validated input values.
      * The method is part of RealmListPacketBuilder and keeps this workflow isolated from the caller.
      */
    private static byte[] BuildVanillaRealmList(ushort build, ConfiguredRealm[] realms, byte accountSecurityLevel, uint accountId)
    {
        ByteWriter body = new();
        body.WriteUInt32(0);
        body.WriteUInt8((byte)Math.Min(byte.MaxValue, realms.Length));

        foreach (ConfiguredRealm realm in realms.Take(byte.MaxValue))
        {
            RealmFlags realmFlags = GetRealmFlags(realm, accountSecurityLevel);
            realmFlags = ClearSpecifyBuildWhenVersionIsUnknown(realmFlags, build);

            body.WriteUInt32(realm.Icon);
            body.WriteUInt8((byte)realmFlags);
            body.WriteCString(GetRealmDisplayName(realm, realmFlags, build));
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
    private static byte[] BuildModernRealmList(ushort build, ConfiguredRealm[] realms, byte accountSecurityLevel, uint accountId)
    {
        ByteWriter body = new();
        body.WriteUInt32(0);
        body.WriteUInt16((ushort)Math.Min(ushort.MaxValue, realms.Length));

        foreach (ConfiguredRealm realm in realms.Take(ushort.MaxValue))
        {
            byte locked = accountSecurityLevel < realm.AllowedSecurityLevel ? (byte)1 : (byte)0;
            RealmFlags realmFlags = GetRealmFlags(realm, accountSecurityLevel);
            realmFlags = ClearSpecifyBuildWhenVersionIsUnknown(realmFlags, build);

            body.WriteUInt8((byte)realm.Icon);
            body.WriteUInt8(locked);
            body.WriteUInt8((byte)realmFlags);
            body.WriteCString(realm.Name);
            body.WriteCString(realm.ClientAddress);
            body.WriteFloat(realm.Population);
            body.WriteUInt8(realm.GetCharacterCount(accountId));
            body.WriteUInt8(realm.Timezone);
            body.WriteUInt8(0); // Unknown realm list value.

            if (realmFlags.HasFlag(RealmFlags.SpecifyBuild))
            {
                WriteRealmBuildVersion(body, build);
            }
        }

        body.WriteUInt16(0x0010);

        return BuildRealmListPacket(body);
    }

    /**
      * Clears SpecifyBuild when no matching build metadata exists, preventing malformed modern realm-list rows.
      */
    private static RealmFlags ClearSpecifyBuildWhenVersionIsUnknown(RealmFlags realmFlags, ushort build)
    {
        if (realmFlags.HasFlag(RealmFlags.SpecifyBuild) && !RealmBuilds.TryGetVersionInfo(build, out _))
        {
            return realmFlags & ~RealmFlags.SpecifyBuild;
        }

        return realmFlags;
    }

    /**
      * Returns the name shown to 1.x clients, including version text when SpecifyBuild is configured.
      */
    private static string GetRealmDisplayName(ConfiguredRealm realm, RealmFlags realmFlags, ushort build)
    {
        if (!realmFlags.HasFlag(RealmFlags.SpecifyBuild) || !RealmBuilds.TryGetVersionInfo(build, out RealmBuildVersionInfo versionInfo))
        {
            return realm.Name;
        }

        return $"{realm.Name} ({versionInfo.MajorVersion}.{versionInfo.MinorVersion}.{versionInfo.PatchVersion}.{versionInfo.Build})";
    }

    /**
      * Writes version fields required by newer realm-list clients when SpecifyBuild is enabled.
      */
    private static void WriteRealmBuildVersion(ByteWriter body, ushort build)
    {
        if (!RealmBuilds.TryGetVersionInfo(build, out RealmBuildVersionInfo versionInfo))
        {
            return;
        }

        body.WriteUInt8(versionInfo.MajorVersion);
        body.WriteUInt8(versionInfo.MinorVersion);
        body.WriteUInt8(versionInfo.PatchVersion);
        body.WriteUInt16(versionInfo.Build);
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of RealmListPacketBuilder and keeps this workflow isolated from the caller.
      */
    private static RealmFlags GetRealmFlags(ConfiguredRealm realm, byte accountSecurityLevel)
    {
        RealmFlags realmFlags = realm.BaseRealmFlags;

        if (!realm.IsOnline || accountSecurityLevel < realm.AllowedSecurityLevel)
        {
            realmFlags |= RealmFlags.Offline;
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
