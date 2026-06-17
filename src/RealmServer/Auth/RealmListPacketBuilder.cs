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
// File: src/RealmServer/Auth/RealmListPacketBuilder.cs
// Purpose: Contains realm list packet builder code for the realm server authentication, realm-list, and account connection layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.RealmServer.Realms;

namespace EmulationServer.RealmServer.Auth;

// Type: RealmListPacketBuilder
// Purpose: Provides realm list packet builder behavior for the realm server authentication, realm-list, and account connection layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class RealmListPacketBuilder
{

    // Field: Stores the realm store state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current realm store backing value maintained by the owning type.
    private readonly ConfiguredRealmStore _realmStore;

    // Constructor: RealmListPacketBuilder
    // Purpose: Initializes a new RealmListPacketBuilder instance with dependencies and values required by the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - realmStore: Realm store value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmListPacketBuilder so callers do not duplicate validation, protocol, or persistence rules.
    public RealmListPacketBuilder(ConfiguredRealmStore realmStore)
    {
        _realmStore = realmStore ?? throw new ArgumentNullException();
    }

    // Method: BuildRealmListAsync
    // Purpose: Builds or writes build realm list output for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - build: Build value supplied by the caller for this operation.
    // - accountSecurityLevel: Account security level value supplied by the caller for this operation.
    // - accountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that resolves to the requested result when the work completes.
    // Notes: This keeps the operation scoped to RealmListPacketBuilder so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public async Task<byte[]> BuildRealmListAsync(
        ushort build,
        byte accountSecurityLevel,
        uint accountId,
        CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(BuildRealmList(build, accountSecurityLevel, accountId));
    }

    // Method: BuildRealmList
    // Purpose: Builds or writes build realm list output for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - build: Build value supplied by the caller for this operation.
    // - accountSecurityLevel: Account security level value supplied by the caller for this operation.
    // - accountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to RealmListPacketBuilder so callers do not duplicate validation, protocol, or persistence rules.
    public byte[] BuildRealmList(ushort build, byte accountSecurityLevel, uint accountId)
    {
        ConfiguredRealm[] realms = _realmStore.GetRealmsForBuild(build)
            .Where(realm => accountSecurityLevel >= realm.AllowedSecurityLevel || accountSecurityLevel > 0)
            .ToArray();

        return RealmBuilds.UsesModernRealmList(build)
            ? BuildModernRealmList(build, realms, accountSecurityLevel, accountId)
            : BuildVanillaRealmList(build, realms, accountSecurityLevel, accountId);
    }

    // Method: BuildVanillaRealmList
    // Purpose: Builds or writes build vanilla realm list output for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - build: Build value supplied by the caller for this operation.
    // - ConfiguredRealmrealms: Configured realmrealms value supplied by the caller for this operation.
    // - accountSecurityLevel: Account security level value supplied by the caller for this operation.
    // - accountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to RealmListPacketBuilder so callers do not duplicate validation, protocol, or persistence rules.
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
            body.WriteUInt8(0);
        }

        body.WriteUInt16(0x0002);

        return BuildRealmListPacket(body);
    }

    // Method: BuildModernRealmList
    // Purpose: Builds or writes build modern realm list output for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - build: Build value supplied by the caller for this operation.
    // - ConfiguredRealmrealms: Configured realmrealms value supplied by the caller for this operation.
    // - accountSecurityLevel: Account security level value supplied by the caller for this operation.
    // - accountId: Account ID identifier used to select the exact record, object, or runtime owner.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to RealmListPacketBuilder so callers do not duplicate validation, protocol, or persistence rules.
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
            body.WriteUInt8(0x2C);

            if (realmFlags.HasFlag(RealmFlags.SpecifyBuild))
            {
                WriteRealmBuildVersion(body, build);
            }
        }

        body.WriteUInt16(0x0010);

        return BuildRealmListPacket(body);
    }

    // Method: ClearSpecifyBuildWhenVersionIsUnknown
    // Purpose: Applies clear specify build when version is unknown changes for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - realmFlags: Realm flags value supplied by the caller for this operation.
    // - build: Build value supplied by the caller for this operation.
    // Returns: Returns the realm flags value produced by this operation.
    // Notes: This keeps the operation scoped to RealmListPacketBuilder so callers do not duplicate validation, protocol, or persistence rules.
    private static RealmFlags ClearSpecifyBuildWhenVersionIsUnknown(RealmFlags realmFlags, ushort build)
    {
        if (realmFlags.HasFlag(RealmFlags.SpecifyBuild) && !RealmBuilds.TryGetVersionInfo(build, out _))
        {
            return realmFlags & ~RealmFlags.SpecifyBuild;
        }

        return realmFlags;
    }

    // Method: GetRealmDisplayName
    // Purpose: Retrieves get realm display name data for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - realm: Realm value supplied by the caller for this operation.
    // - realmFlags: Realm flags value supplied by the caller for this operation.
    // - build: Build value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to RealmListPacketBuilder so callers do not duplicate validation, protocol, or persistence rules.
    private static string GetRealmDisplayName(ConfiguredRealm realm, RealmFlags realmFlags, ushort build)
    {
        if (!realmFlags.HasFlag(RealmFlags.SpecifyBuild) || !RealmBuilds.TryGetVersionInfo(build, out RealmBuildVersionInfo versionInfo))
        {
            return realm.Name;
        }

        return $"{realm.Name} ({versionInfo.MajorVersion},{versionInfo.MinorVersion},{versionInfo.PatchVersion})";
    }

    // Method: WriteRealmBuildVersion
    // Purpose: Builds or writes write realm build version output for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - body: Body value supplied by the caller for this operation.
    // - build: Build value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmListPacketBuilder so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: GetRealmFlags
    // Purpose: Retrieves get realm flags data for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - realm: Realm value supplied by the caller for this operation.
    // - accountSecurityLevel: Account security level value supplied by the caller for this operation.
    // Returns: Returns the realm flags value produced by this operation.
    // Notes: This keeps the operation scoped to RealmListPacketBuilder so callers do not duplicate validation, protocol, or persistence rules.
    private static RealmFlags GetRealmFlags(ConfiguredRealm realm, byte accountSecurityLevel)
    {
        RealmFlags realmFlags = realm.BaseRealmFlags;

        if (!realm.IsOnline || accountSecurityLevel < realm.AllowedSecurityLevel)
        {
            realmFlags |= RealmFlags.Offline;
        }

        return realmFlags;
    }

    // Method: BuildRealmListPacket
    // Purpose: Builds or writes build realm list packet output for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - body: Body value supplied by the caller for this operation.
    // Returns: Returns the byte[] value produced by this operation.
    // Notes: This keeps the operation scoped to RealmListPacketBuilder so callers do not duplicate validation, protocol, or persistence rules.
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
