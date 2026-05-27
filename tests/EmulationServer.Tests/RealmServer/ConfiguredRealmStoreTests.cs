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

using EmulationServer.RealmServer.Configuration;
using EmulationServer.RealmServer.Realms;

/**
  * File overview: tests/EmulationServer.Tests/RealmServer/ConfiguredRealmStoreTests.cs
  * Documents realm-list visibility behavior for configured realms that are registered by WorldServer status packets.
  */

namespace EmulationServer.Tests.RealmServer;

/**
  * Owns tests for configured realm visibility, WorldServer registration, and stale realm hiding.
  */
public sealed class ConfiguredRealmStoreTests
{
    private const ushort SupportedBuild = 5875;

    /**
      * Verifies configured realms are loaded but hidden until WorldServer sends initial status.
      */
    [Fact]
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

    /**
      * Verifies configured realms become visible after the first trusted WorldServer status packet.
      */
    [Fact]
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

    /**
      * Verifies stale realms are hidden when stale hiding is enabled.
      */
    [Fact]
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

    /**
      * Verifies administrators can keep stale realms visible by disabling stale hiding.
      */
    [Fact]
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
