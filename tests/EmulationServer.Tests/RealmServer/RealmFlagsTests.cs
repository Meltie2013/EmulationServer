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
// File: tests/EmulationServer.Tests/RealmServer/RealmFlagsTests.cs
// Purpose: Contains realm flags tests code for the realm server authentication, realm-list, and account connection layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.RealmServer.Auth;
using EmulationServer.RealmServer.Configuration;
using EmulationServer.RealmServer.Realms;

namespace EmulationServer.Tests.RealmServer;

// Type: RealmFlagsTests
// Purpose: Provides realm flags tests behavior for the realm server authentication, realm-list, and account connection layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class RealmFlagsTests
{
    // Constant: Defines the supported build constant used by the realm server authentication, realm-list, and account connection layer.
    // Value: fixed supported build value used anywhere this rule or protocol value is needed.
    private const ushort SupportedBuild = RealmBuilds.Vanilla1123;

    [Fact]
    // Method: RealmFlags_ShouldMatchMangosZeroValues
    // Purpose: Executes the realm flags should match mangos zero values operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmFlagsTests so callers do not duplicate validation, protocol, or persistence rules.
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

    [Fact]
    // Method: ParseConfigurationValue_ShouldAcceptNamedConfiguredFlags
    // Purpose: Converts incoming data into parse configuration value should accept named configured flags form for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmFlagsTests so callers do not duplicate validation, protocol, or persistence rules.
    public void ParseConfigurationValue_ShouldAcceptNamedConfiguredFlags()
    {
        RealmFlags flags = RealmFlagUtilities.ParseConfigurationValue("NewPlayers, Recommended");

        Assert.Equal(RealmFlags.NewPlayers | RealmFlags.Recommended, flags);
    }

    [Fact]
    // Method: ParseConfigurationValue_ShouldAcceptHexConfiguredFlags
    // Purpose: Converts incoming data into parse configuration value should accept hex configured flags form for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmFlagsTests so callers do not duplicate validation, protocol, or persistence rules.
    public void ParseConfigurationValue_ShouldAcceptHexConfiguredFlags()
    {
        RealmFlags flags = RealmFlagUtilities.ParseConfigurationValue("0x20|0x40");

        Assert.Equal(RealmFlags.NewPlayers | RealmFlags.Recommended, flags);
    }

    [Fact]
    // Method: ParseConfigurationValue_ShouldAcceptInvalidAsConfiguredHideFlag
    // Purpose: Converts incoming data into parse configuration value should accept invalid as configured hide flag form for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmFlagsTests so callers do not duplicate validation, protocol, or persistence rules.
    public void ParseConfigurationValue_ShouldAcceptInvalidAsConfiguredHideFlag()
    {
        RealmFlags flags = RealmFlagUtilities.ParseConfigurationValue("Invalid");

        Assert.Equal(RealmFlags.Invalid, flags);
    }

    [Fact]
    // Method: ParseConfigurationValue_ShouldRejectUnsupportedConfiguredFlags
    // Purpose: Converts incoming data into parse configuration value should reject unsupported configured flags form for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmFlagsTests so callers do not duplicate validation, protocol, or persistence rules.
    public void ParseConfigurationValue_ShouldRejectUnsupportedConfiguredFlags()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => RealmFlagUtilities.ParseConfigurationValue("Full"));

        Assert.Contains("Unsupported value: 0x80", exception.Message);
    }

    [Fact]
    // Method: BuildRealmList_ShouldWriteConfiguredRealmFlags
    // Purpose: Builds or writes build realm list should write configured realm flags output for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmFlagsTests so callers do not duplicate validation, protocol, or persistence rules.
    public void BuildRealmList_ShouldWriteConfiguredRealmFlags()
    {
        RealmListPacketBuilder builder = CreatePacketBuilder(RealmFlags.NewPlayers | RealmFlags.Recommended, online: true);

        byte[] packet = builder.BuildRealmList(SupportedBuild, accountSecurityLevel: 0, accountId: 1);

        Assert.Equal((byte)(RealmFlags.NewPlayers | RealmFlags.Recommended), packet[12]);
    }

    [Fact]
    // Method: BuildRealmList_ShouldAddOfflineFlagWhenRealmIsOffline
    // Purpose: Builds or writes build realm list should add offline flag when realm is offline output for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmFlagsTests so callers do not duplicate validation, protocol, or persistence rules.
    public void BuildRealmList_ShouldAddOfflineFlagWhenRealmIsOffline()
    {
        RealmListPacketBuilder builder = CreatePacketBuilder(RealmFlags.None, online: false);

        byte[] packet = builder.BuildRealmList(SupportedBuild, accountSecurityLevel: 0, accountId: 1);

        Assert.Equal((byte)RealmFlags.Offline, packet[12]);
    }

    [Fact]
    // Method: BuildRealmList_ShouldHideInvalidRealmFlags
    // Purpose: Builds or writes build realm list should hide invalid realm flags output for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmFlagsTests so callers do not duplicate validation, protocol, or persistence rules.
    public void BuildRealmList_ShouldHideInvalidRealmFlags()
    {
        RealmListPacketBuilder builder = CreatePacketBuilder(RealmFlags.Invalid, online: true);

        byte[] packet = builder.BuildRealmList(SupportedBuild, accountSecurityLevel: 0, accountId: 1);

        Assert.Equal((byte)0, packet[7]);
    }

    [Fact]
    // Method: BuildRealmList_ShouldAppendVersionToVanillaRealmNameWhenSpecifyBuildIsEnabled
    // Purpose: Builds or writes build realm list should append version to vanilla realm name when specify build is enabled output for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmFlagsTests so callers do not duplicate validation, protocol, or persistence rules.
    public void BuildRealmList_ShouldAppendVersionToVanillaRealmNameWhenSpecifyBuildIsEnabled()
    {
        RealmListPacketBuilder builder = CreatePacketBuilder(RealmFlags.SpecifyBuild, online: true);

        byte[] packet = builder.BuildRealmList(SupportedBuild, accountSecurityLevel: 0, accountId: 1);
        string packetText = System.Text.Encoding.ASCII.GetString(packet);

        Assert.Contains("Test Realm (1,12,3)", packetText);
    }

    [Fact]
    // Method: BuildRealmList_ShouldWriteModernVersionBytesWhenSpecifyBuildIsEnabled
    // Purpose: Builds or writes build realm list should write modern version bytes when specify build is enabled output for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmFlagsTests so callers do not duplicate validation, protocol, or persistence rules.
    public void BuildRealmList_ShouldWriteModernVersionBytesWhenSpecifyBuildIsEnabled()
    {
        RealmListPacketBuilder builder = CreatePacketBuilder(RealmFlags.SpecifyBuild, online: true, build: RealmBuilds.TheBurningCrusade243);

        byte[] packet = builder.BuildRealmList(RealmBuilds.TheBurningCrusade243, accountSecurityLevel: 0, accountId: 1);
        byte[] expectedTail = [2, 4, 3, 0x9E, 0x21, 0x10, 0x00];

        Assert.Equal(expectedTail, packet[^expectedTail.Length..]);
    }

    // Method: CreatePacketBuilder
    // Purpose: Applies create packet builder changes for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - flags: Flags value supplied by the caller for this operation.
    // - online: Online value supplied by the caller for this operation.
    // - build: Build value supplied by the caller for this operation.
    // Returns: Returns the realm list packet builder value produced by this operation.
    // Notes: This keeps the operation scoped to RealmFlagsTests so callers do not duplicate validation, protocol, or persistence rules.
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
