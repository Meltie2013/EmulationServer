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
// File: tests/EmulationServer.Tests/RealmServer/ConfiguredRealmStoreTests.cs
// Purpose: Contains configured realm store tests code for the realm server authentication, realm-list, and account connection layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.RealmServer.Configuration;
using EmulationServer.RealmServer.Realms;

namespace EmulationServer.Tests.RealmServer;

// Type: ConfiguredRealmStoreTests
// Purpose: Provides configured realm store tests behavior for the realm server authentication, realm-list, and account connection layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class ConfiguredRealmStoreTests
{
    // Constant: Defines the supported build constant used by the realm server authentication, realm-list, and account connection layer.
    // Value: fixed supported build value used anywhere this rule or protocol value is needed.
    private const ushort SupportedBuild = 5875;

    [Fact]
    // Method: GetRealmsForBuild_ShouldHideConfiguredRealmUntilWorldServerStatusArrives
    // Purpose: Retrieves get realms for build should hide configured realm until world server status arrives data for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to ConfiguredRealmStoreTests so callers do not duplicate validation, protocol, or persistence rules.
    public void GetRealmsForBuild_ShouldHideConfiguredRealmUntilWorldServerStatusArrives()
    {
        ConfiguredRealmStore store = CreateStore(new RealmListSettings
        {
            RequireWorldServerStatus = true,
            HideStaleRealms = true,
            StaleRealmTimeout = TimeSpan.FromMinutes(5),
        });

        IReadOnlyCollection<ConfiguredRealm> realms = store.GetRealmsForBuild(SupportedBuild, DateTimeOffset.UnixEpoch);

        Assert.Empty(realms);
    }

    [Fact]
    // Method: GetRealmsForBuild_ShouldShowRealmAfterWorldServerStatusArrives
    // Purpose: Retrieves get realms for build should show realm after world server status arrives data for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to ConfiguredRealmStoreTests so callers do not duplicate validation, protocol, or persistence rules.
    public void GetRealmsForBuild_ShouldShowRealmAfterWorldServerStatusArrives()
    {
        ConfiguredRealmStore store = CreateStore(new RealmListSettings
        {
            RequireWorldServerStatus = true,
            HideStaleRealms = true,
            StaleRealmTimeout = TimeSpan.FromMinutes(5),
        });

        DateTimeOffset now = DateTimeOffset.UnixEpoch;
        store.TrySetRealmStatus(1, true, 3, 100, now);

        IReadOnlyCollection<ConfiguredRealm> realms = store.GetRealmsForBuild(SupportedBuild, now.AddMinutes(1));

        ConfiguredRealm realm = Assert.Single(realms);
        Assert.Equal((uint)1, realm.Id);
        Assert.True(realm.IsOnline);
        Assert.Equal(3, realm.ActiveConnections);
    }

    [Fact]
    // Method: TrySetRealmStatus_ShouldKeepPopulationScopedToMatchingRealmId
    // Purpose: Executes the try set realm status should keep population scoped to matching realm ID operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to ConfiguredRealmStoreTests so callers do not duplicate validation, protocol, or persistence rules.
    public void TrySetRealmStatus_ShouldKeepPopulationScopedToMatchingRealmId()
    {
        ConfiguredRealmStore store = new(
            [
                new ConfiguredRealmSettings
                {
                    Id = 1,
                    Name = "Realm One",
                    Address = "127.0.0.1",
                    Port = 8085,
                    Builds = new HashSet<ushort> { SupportedBuild },
                },
                new ConfiguredRealmSettings
                {
                    Id = 2,
                    Name = "Realm Two",
                    Address = "127.0.0.1",
                    Port = 8086,
                    Builds = new HashSet<ushort> { SupportedBuild },
                },
            ],
            new RealmListSettings
            {
                RequireWorldServerStatus = false,
                HideStaleRealms = false,
                StaleRealmTimeout = TimeSpan.FromMinutes(5),
            });

        DateTimeOffset now = DateTimeOffset.UnixEpoch;

        Assert.True(store.TrySetRealmStatus(2, true, 25, 100, now));

        ConfiguredRealm[] realms = store.GetRealmsForBuild(SupportedBuild, now)
            .OrderBy(realm => realm.Id)
            .ToArray();

        Assert.Equal(2, realms.Length);
        Assert.Equal((uint)1, realms[0].Id);
        Assert.Equal(0.0f, realms[0].Population);
        Assert.Equal((uint)2, realms[1].Id);
        Assert.Equal(0.5f, realms[1].Population);
    }

    [Fact]
    // Method: GetRealmsForBuild_ShouldHideRealmAfterStaleTimeoutWhenEnabled
    // Purpose: Retrieves get realms for build should hide realm after stale timeout when enabled data for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to ConfiguredRealmStoreTests so callers do not duplicate validation, protocol, or persistence rules.
    public void GetRealmsForBuild_ShouldHideRealmAfterStaleTimeoutWhenEnabled()
    {
        ConfiguredRealmStore store = CreateStore(new RealmListSettings
        {
            RequireWorldServerStatus = true,
            HideStaleRealms = true,
            StaleRealmTimeout = TimeSpan.FromMinutes(5),
        });

        DateTimeOffset now = DateTimeOffset.UnixEpoch;
        store.TrySetRealmStatus(1, true, 3, 100, now);

        IReadOnlyCollection<ConfiguredRealm> realms = store.GetRealmsForBuild(SupportedBuild, now.AddMinutes(5));

        Assert.Empty(realms);
    }

    [Fact]
    // Method: GetRealmsForBuild_ShouldKeepStaleRealmVisibleWhenStaleHidingIsDisabled
    // Purpose: Retrieves get realms for build should keep stale realm visible when stale hiding is disabled data for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to ConfiguredRealmStoreTests so callers do not duplicate validation, protocol, or persistence rules.
    public void GetRealmsForBuild_ShouldKeepStaleRealmVisibleWhenStaleHidingIsDisabled()
    {
        ConfiguredRealmStore store = CreateStore(new RealmListSettings
        {
            RequireWorldServerStatus = true,
            HideStaleRealms = false,
            StaleRealmTimeout = TimeSpan.FromMinutes(5),
        });

        DateTimeOffset now = DateTimeOffset.UnixEpoch;
        store.TrySetRealmStatus(1, false, 0, 100, now);

        IReadOnlyCollection<ConfiguredRealm> realms = store.GetRealmsForBuild(SupportedBuild, now.AddHours(1));

        ConfiguredRealm realm = Assert.Single(realms);
        Assert.False(realm.IsOnline);
    }

    // Method: CreateStore
    // Purpose: Applies create store changes for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - settings: Settings values that control how this operation should run.
    // Returns: Returns the configured realm store value produced by this operation.
    // Notes: This keeps the operation scoped to ConfiguredRealmStoreTests so callers do not duplicate validation, protocol, or persistence rules.
    private static ConfiguredRealmStore CreateStore(RealmListSettings settings)
    {
        return new ConfiguredRealmStore(
            [
                new ConfiguredRealmSettings
                {
                    Id = 1,
                    Name = "Test Realm",
                    Address = "127.0.0.1",
                    Port = 8085,
                    Builds = new HashSet<ushort> { SupportedBuild },
                },
            ],
            settings);
    }
}
