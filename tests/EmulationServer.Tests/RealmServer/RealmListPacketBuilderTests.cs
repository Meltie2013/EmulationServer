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
// File: tests/EmulationServer.Tests/RealmServer/RealmListPacketBuilderTests.cs
// Purpose: Contains realm list packet builder tests code for the realm server authentication, realm-list, and account connection layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Buffers.Binary;
using System.Text;

using EmulationServer.RealmServer.Auth;
using EmulationServer.RealmServer.Configuration;
using EmulationServer.RealmServer.Realms;

namespace EmulationServer.Tests.RealmServer;

// Type: RealmListPacketBuilderTests
// Purpose: Provides realm list packet builder tests behavior for the realm server authentication, realm-list, and account connection layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class RealmListPacketBuilderTests
{

    [Fact]
    // Method: BuildRealmList_ShouldWriteVanillaPopulationIntoClientRealmRow
    // Purpose: Builds or writes build realm list should write vanilla population into client realm row output for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmListPacketBuilderTests so callers do not duplicate validation, protocol, or persistence rules.
    public void BuildRealmList_ShouldWriteVanillaPopulationIntoClientRealmRow()
    {
        ConfiguredRealmStore store = CreateStore();
        Assert.True(store.TrySetRealmStatus(1, true, activeConnections: 25, capacityLimit: 100, updatedUtc: DateTimeOffset.UnixEpoch));

        RealmListPacketBuilder builder = new(store);
        byte[] packet = builder.BuildRealmList(RealmBuilds.Vanilla1123, accountSecurityLevel: 0, accountId: 1);

        Assert.Equal((byte)RealmAuthOpCode.RealmList, packet[0]);
        Assert.Equal(packet.Length - 3, BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(1, 2)));
        Assert.Equal(0u, BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(3, 4)));
        Assert.Equal(1, packet[7]);

        int offset = 8;
        Assert.Equal(0u, BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(offset, 4)));
        offset += 4;

        Assert.Equal((byte)RealmFlags.None, packet[offset]);
        offset += 1;

        Assert.Equal("Test Realm", ReadCString(packet, ref offset));
        Assert.Equal("127.0.0.1:8085", ReadCString(packet, ref offset));

        float population = BinaryPrimitives.ReadSingleLittleEndian(packet.AsSpan(offset, 4));
        offset += 4;

        Assert.Equal(0.5f, population);
        Assert.Equal(0, packet[offset]);
        offset += 1;

        Assert.Equal(1, packet[offset]);
        offset += 1;

        Assert.Equal(0x00, packet[offset]);
        offset += 1;

        Assert.Equal(0x0002, BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(offset, 2)));
        offset += 2;

        Assert.Equal(packet.Length, offset);
    }

    [Fact]
    // Method: BuildRealmList_ShouldAttachPopulationToMatchingClientRealmRow
    // Purpose: Builds or writes build realm list should attach population to matching client realm row output for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmListPacketBuilderTests so callers do not duplicate validation, protocol, or persistence rules.
    public void BuildRealmList_ShouldAttachPopulationToMatchingClientRealmRow()
    {
        ConfiguredRealmStore store = new(
            [
                new ConfiguredRealmSettings
                {
                    Id = 1,
                    Name = "Realm One",
                    Address = "127.0.0.1",
                    Port = 8085,
                    Builds = new HashSet<ushort> { RealmBuilds.Vanilla1123 },
                },
                new ConfiguredRealmSettings
                {
                    Id = 2,
                    Name = "Realm Two",
                    Address = "127.0.0.1",
                    Port = 8086,
                    Builds = new HashSet<ushort> { RealmBuilds.Vanilla1123 },
                },
            ],
            new RealmListSettings
            {
                RequireWorldServerStatus = false,
                HideStaleRealms = false,
                StaleRealmTimeout = TimeSpan.FromMinutes(5),
            });

        Assert.True(store.TrySetRealmStatus(2, true, activeConnections: 50, capacityLimit: 100, updatedUtc: DateTimeOffset.UnixEpoch));

        RealmListPacketBuilder builder = new(store);
        byte[] packet = builder.BuildRealmList(RealmBuilds.Vanilla1123, accountSecurityLevel: 0, accountId: 1);
        IReadOnlyList<RealmListRow> rows = ReadVanillaRows(packet);

        Assert.Equal(2, rows.Count);
        Assert.Equal("Realm One", rows[0].Name);
        Assert.Equal(0.0f, rows[0].Population);
        Assert.Equal("Realm Two", rows[1].Name);
        Assert.Equal(1.0f, rows[1].Population);
    }

    [Fact]
    // Method: BuildRealmList_ShouldWriteModernRealmListUnknownValue
    // Purpose: Builds or writes build realm list should write modern realm list unknown value output for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmListPacketBuilderTests so callers do not duplicate validation, protocol, or persistence rules.
    public void BuildRealmList_ShouldWriteModernRealmListUnknownValue()
    {
        ConfiguredRealmStore store = new(
            [
                new ConfiguredRealmSettings
                {
                    Id = 1,
                    Name = "Test Realm",
                    Address = "127.0.0.1",
                    Port = 8085,
                    Builds = new HashSet<ushort> { RealmBuilds.TheBurningCrusade243 },
                },
            ],
            new RealmListSettings
            {
                RequireWorldServerStatus = false,
                HideStaleRealms = false,
                StaleRealmTimeout = TimeSpan.FromMinutes(5),
            });

        RealmListPacketBuilder builder = new(store);
        byte[] packet = builder.BuildRealmList(RealmBuilds.TheBurningCrusade243, accountSecurityLevel: 0, accountId: 1);

        int offset = 8;
        offset += 3;
        _ = ReadCString(packet, ref offset);
        _ = ReadCString(packet, ref offset);
        offset += 4;
        offset += 1;
        offset += 1;

        Assert.Equal(0x2C, packet[offset]);
    }

    // Method: CreateStore
    // Purpose: Applies create store changes for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: Returns the configured realm store value produced by this operation.
    // Notes: This keeps the operation scoped to RealmListPacketBuilderTests so callers do not duplicate validation, protocol, or persistence rules.
    private static ConfiguredRealmStore CreateStore()
    {
        return new ConfiguredRealmStore(
            [
                new ConfiguredRealmSettings
                {
                    Id = 1,
                    Name = "Test Realm",
                    Address = "127.0.0.1",
                    Port = 8085,
                    Builds = new HashSet<ushort> { RealmBuilds.Vanilla1123 },
                },
            ],
            new RealmListSettings
            {
                RequireWorldServerStatus = false,
                HideStaleRealms = false,
                StaleRealmTimeout = TimeSpan.FromMinutes(5),
            });
    }

    // Method: ReadVanillaRows
    // Purpose: Retrieves read vanilla rows data for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - bytepacket: Bytepacket value supplied by the caller for this operation.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to RealmListPacketBuilderTests so callers do not duplicate validation, protocol, or persistence rules.
    private static IReadOnlyList<RealmListRow> ReadVanillaRows(byte[] packet)
    {
        int count = packet[7];
        int offset = 8;
        List<RealmListRow> rows = [];

        for (int i = 0; i < count; i++)
        {
            offset += 4;
            offset += 1;
            string name = ReadCString(packet, ref offset);
            _ = ReadCString(packet, ref offset);
            float population = BinaryPrimitives.ReadSingleLittleEndian(packet.AsSpan(offset, 4));
            offset += 4;
            offset += 1;
            offset += 1;
            offset += 1;

            rows.Add(new RealmListRow(name, population));
        }

        return rows;
    }

    // Method: ReadCString
    // Purpose: Retrieves read C string data for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - bytepacket: Bytepacket value supplied by the caller for this operation.
    // - offset: Offset value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to RealmListPacketBuilderTests so callers do not duplicate validation, protocol, or persistence rules.
    private static string ReadCString(byte[] packet, ref int offset)
    {
        int start = offset;

        while (offset < packet.Length && packet[offset] != 0)
        {
            offset++;
        }

        string value = Encoding.UTF8.GetString(packet, start, offset - start);
        offset++;
        return value;
    }

    // Type: RealmListRow
    // Purpose: Represents realm list row data passed through the realm server authentication, realm-list, and account connection layer.
    // Constructor values:
    // - Name: Name value supplied by the caller for this operation.
    // - Population: Population value supplied by the caller for this operation.
    // Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
    private readonly record struct RealmListRow(string Name, float Population);
}
