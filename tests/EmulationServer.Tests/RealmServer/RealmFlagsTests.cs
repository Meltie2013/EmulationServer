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

using EmulationServer.RealmServer.Auth;
using EmulationServer.RealmServer.Configuration;
using EmulationServer.RealmServer.Realms;

/**
  * File overview: tests/EmulationServer.Tests/RealmServer/RealmFlagsTests.cs
  * Documents realm flag parsing, validation, and packet output behavior.
  */

namespace EmulationServer.Tests.RealmServer;

/**
  * Owns tests for realm-list flags used by configured realms.
  */
public sealed class RealmFlagsTests
{
    private const ushort SupportedBuild = RealmBuilds.Vanilla1123;

    /**
      * Verifies the project keeps the same numeric realm flag values as MaNGOS Zero.
      */
    [Fact]
    public void RealmFlags_ShouldMatchMangosZeroValues()
    {
        Assert.Equal(0x00, (byte)RealmFlags.None);
        Assert.Equal(0x01, (byte)RealmFlags.Invalid);
        Assert.Equal(0x02, (byte)RealmFlags.Offline);
        Assert.Equal(0x04, (byte)RealmFlags.SpecifyBuild);
        Assert.Equal(0x20, (byte)RealmFlags.NewPlayers);
        Assert.Equal(0x40, (byte)RealmFlags.Recommended);
        Assert.Equal(0x80, (byte)RealmFlags.Full);
    }

    /**
      * Verifies named flags can be used in realmserver.ini instead of hard-to-read numeric sums.
      */
    [Fact]
    public void ParseConfigurationValue_ShouldAcceptNamedConfiguredFlags()
    {
        RealmFlags flags = RealmFlagUtilities.ParseConfigurationValue("NewPlayers, Recommended");

        Assert.Equal(RealmFlags.NewPlayers | RealmFlags.Recommended, flags);
    }

    /**
      * Verifies hexadecimal values can be used in realmserver.ini comments and configuration.
      */
    [Fact]
    public void ParseConfigurationValue_ShouldAcceptHexConfiguredFlags()
    {
        RealmFlags flags = RealmFlagUtilities.ParseConfigurationValue("0x20|0x40");

        Assert.Equal(RealmFlags.NewPlayers | RealmFlags.Recommended, flags);
    }

    /**
      * Verifies Invalid can be used as do-not-show flag.
      */
    [Fact]
    public void ParseConfigurationValue_ShouldAcceptInvalidAsConfiguredHideFlag()
    {
        RealmFlags flags = RealmFlagUtilities.ParseConfigurationValue("Invalid");

        Assert.Equal(RealmFlags.Invalid, flags);
    }

    /**
      * Verifies unsupported protocol-only flags are rejected from administrator configuration.
      */
    [Fact]
    public void ParseConfigurationValue_ShouldRejectUnsupportedConfiguredFlags()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => RealmFlagUtilities.ParseConfigurationValue("Full"));

        Assert.Contains("Unsupported value: 0x80", exception.Message);
    }

    /**
      * Verifies configured flags are written into the vanilla realm-list packet.
      */
    [Fact]
    public void BuildRealmList_ShouldWriteConfiguredRealmFlags()
    {
        RealmListPacketBuilder builder = CreatePacketBuilder(RealmFlags.NewPlayers | RealmFlags.Recommended, online: true);

        byte[] packet = builder.BuildRealmList(SupportedBuild, accountSecurityLevel: 0, accountId: 1);

        Assert.Equal((byte)(RealmFlags.NewPlayers | RealmFlags.Recommended), packet[12]);
    }

    /**
      * Verifies offline status is applied automatically even when it is not part of the configured base flags.
      */
    [Fact]
    public void BuildRealmList_ShouldAddOfflineFlagWhenRealmIsOffline()
    {
        RealmListPacketBuilder builder = CreatePacketBuilder(RealmFlags.None, online: false);

        byte[] packet = builder.BuildRealmList(SupportedBuild, accountSecurityLevel: 0, accountId: 1);

        Assert.Equal((byte)RealmFlags.Offline, packet[12]);
    }

    /**
      * Verifies Invalid hides a realm from the packet list.
      */
    [Fact]
    public void BuildRealmList_ShouldHideInvalidRealmFlags()
    {
        RealmListPacketBuilder builder = CreatePacketBuilder(RealmFlags.Invalid, online: true);

        byte[] packet = builder.BuildRealmList(SupportedBuild, accountSecurityLevel: 0, accountId: 1);

        Assert.Equal((byte)0, packet[7]);
    }

    /**
      * Verifies vanilla clients receive a readable version suffix when SpecifyBuild is configured.
      */
    [Fact]
    public void BuildRealmList_ShouldAppendVersionToVanillaRealmNameWhenSpecifyBuildIsEnabled()
    {
        RealmListPacketBuilder builder = CreatePacketBuilder(RealmFlags.SpecifyBuild, online: true);

        byte[] packet = builder.BuildRealmList(SupportedBuild, accountSecurityLevel: 0, accountId: 1);
        string packetText = System.Text.Encoding.ASCII.GetString(packet);

        Assert.Contains("Test Realm (1.12.3.6141)", packetText);
    }

    /**
      * Verifies newer clients receive the version bytes required by the SpecifyBuild flag.
      */
    [Fact]
    public void BuildRealmList_ShouldWriteModernVersionBytesWhenSpecifyBuildIsEnabled()
    {
        RealmListPacketBuilder builder = CreatePacketBuilder(RealmFlags.SpecifyBuild, online: true, build: RealmBuilds.TheBurningCrusade243);

        byte[] packet = builder.BuildRealmList(RealmBuilds.TheBurningCrusade243, accountSecurityLevel: 0, accountId: 1);
        byte[] expectedTail = [2, 4, 3, 0x9E, 0x21, 0x10, 0x00];

        Assert.Equal(expectedTail, packet[^expectedTail.Length..]);
    }

    private static RealmListPacketBuilder CreatePacketBuilder(RealmFlags flags, bool online, ushort build = SupportedBuild)
    {
        ConfiguredRealmStore store = new(
            [
                new ConfiguredRealmSettings
                {
                    Id = 1,
                    Name = "Test Realm",
                    Address = "127.0.0.1",
                    Port = 8085,
                    RealmFlags = flags,
                    Online = online,
                    Builds = new HashSet<ushort> { build },
                },
            ],
            new RealmListSettings
            {
                RequireWorldServerStatus = false,
                HideStaleRealms = true,
                StaleRealmTimeout = TimeSpan.FromMinutes(5),
            });

        return new RealmListPacketBuilder(store);
    }
}
